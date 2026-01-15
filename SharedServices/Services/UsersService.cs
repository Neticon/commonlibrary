using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Integrations;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommonLibrary.SharedServices.Services
{
    public class UsersService : IUserService
    {

        private readonly IValidationService _validationService;
        private readonly IGenericEntityRepository<User> _genericEntityRepo;
        private readonly ITenantRepository _tenantRepository;
        private readonly ISecretService _secretService;

        public UsersService(IValidationService validationService, IGenericEntityRepository<User> genericEntityRepository, ITenantRepository tenantRepository, ISecretService secretService)
        {
            _validationService = validationService;
            _genericEntityRepo = genericEntityRepository;
            _tenantRepository = tenantRepository;
            _secretService = secretService;
        }
        //todo- get from config
        public static string UserPoolId = Environment.GetEnvironmentVariable("AWS_COGNITO_POOL_ID");
        public static string[] ValidRoles = { "ASSISTANT", "VENUE_MANAGER" };

        public async Task CreateNewUser(CreateUserModel model)
        {
            var validation = await _validationService.ValidateRequest(new ValidateRequest { p = model.data.phone_number });
            if (validation.Item1 != 200)
                throw new Exception("Phone is not valid");
            model.data.role = model.data.role.ToUpper();
            if (!ValidRoles.Contains(model.data.role))
                throw new Exception("Invalid role");
            var idpCode = "";
            if (model.data.tenant_id != null)
            {
                idpCode = await _tenantRepository.GetOrgCode(model.data.tenant_id.Value);
                if (string.IsNullOrEmpty(idpCode))
                    throw new Exception("Failed to get idp_group from tenant");
            }
            var secret = await _secretService.GetSecret(idpCode);
            var hashedMail = AesEncryption.Encrypt(model.data.email, secret);
            var request = new AdminCreateUserRequest
            {
                UserPoolId = UserPoolId,
                Username = model.data.email,
                UserAttributes = new List<AttributeType>
                {
                    new AttributeType { Name = "email", Value = model.data.email },
                    new AttributeType { Name = "custom:role", Value = model.data.role },
                    new AttributeType { Name = "custom:group", Value = idpCode  },
                    new AttributeType { Name = "custom:postal_code", Value = hashedMail  },
                    new AttributeType { Name = "custom:organizationCode", Value = idpCode },
                },
                TemporaryPassword = "tempPass", // sendNotification with temp Passwod
                MessageAction = "SUPPRESS" // Suppress email invitation (change to get invitation)
            };
            var provider = GetCognitoProvider();
            AdminCreateUserResponse createUserResponse;
            try
            {
                createUserResponse = await provider.AdminCreateUserAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create cognito user." + ex.Message);
            }

            var addToGroupRequest = new AdminAddUserToGroupRequest
            {
                UserPoolId = UserPoolId,
                Username = model.data.email,
                GroupName = idpCode
            };
            await provider.AdminAddUserToGroupAsync(addToGroupRequest);

            var user = new User
            {
                create_bu = model.data.create_bu,
                email = model.data.email,
                first_name = model.data.first_name,
                last_name = model.data.last_name,
                phone_number = model.data.phone_number,
                role = model.data.role,
                idp_group = idpCode,
                tenant_id = model.data.tenant_id,
                idp_attribute = createUserResponse.User.Username
            };

            await _genericEntityRepo.SaveEntity(user, secret);
        }

        public async Task UpdateUser(UpdateUserModel model)
        {
            var validation = await _validationService.ValidateRequest(new ValidateRequest { p = model.data.phone_number });
            if (validation.Item1 != 200)
                throw new Exception("Phone is not valid");
            model.data.role = model.data.role.ToUpper();
            if (!ValidRoles.Contains(model.data.role))
                throw new Exception("Invalid role");
            var secret = await _secretService.GetSecret(model.filters.idp_group);
            var resp = await _genericEntityRepo.UpdateEntity(model, secret);
        }

        public async Task<object> GetUsers(string model)
        {
            var jObject = JsonConvert.DeserializeObject<JObject>(model);
            var orgCode = jObject["idp_group"].ToString();
            if (string.IsNullOrEmpty(orgCode))
                throw new Exception("idp_group is empty!");
            var secret = await _secretService.GetSecret(orgCode);
            var resp = await _genericEntityRepo.GetData(model);
            foreach (var row in resp.rows)
            {
                ObjectEncryption.DecryptObject(row, orgCode, EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(User)));
            }
            return resp.rows;
        }

        public async Task DeleteUser(DeleteUserModel model)
        {
            var prefix = "DELETED|";
            model.data.email = $"{prefix}{model.filters.email}";
            model.data.delete_dt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff+00");
            var secret = await _secretService.GetSecret(model.filters.idp_group);
            var resp = await _genericEntityRepo.UpdateEntity(model, secret);
            //todo updateMods
        }

        private AmazonCognitoIdentityProviderClient GetCognitoProvider()
        {
            var accessKey = Environment.GetEnvironmentVariable("AWS_COGNITO_ACCESS_KEY");
            var accessSecret = Environment.GetEnvironmentVariable("AWS_COGNITO_ACCESS_SECRET");
            //todo - get config from secrets
            var awsCredentials = new BasicAWSCredentials(accessKey, accessSecret);
            return new AmazonCognitoIdentityProviderClient(awsCredentials, RegionEndpoint.EUCentral1);
        }

    }
}
