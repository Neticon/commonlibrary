using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Integrations;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Models;
using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Integration.Grpc;
using Newtonsoft.Json;
using Npgsql;
using WebApp.API.Controllers.Helper;

namespace CommonLibrary.SharedServices.Services
{
    public class UsersService : AppServiceBase, IUserService
    {
        private readonly IValidationService _validationService;
        private readonly IGenericEntityRepository<User> _genericEntityRepo;
        private readonly ITenantRepository _tenantRepository;
        private readonly ISecretService _secretService;
        private readonly IEmailClient _emailClient;

        public UsersService(IValidationService validationService, IGenericEntityRepository<User> genericEntityRepository, ITenantRepository tenantRepository, ISecretService secretService, IEmailClient emailClient, ICurrentUserService currentUserService) : base(currentUserService)
        {
            _validationService = validationService;
            _genericEntityRepo = genericEntityRepository;
            _tenantRepository = tenantRepository;
            _secretService = secretService;
            _emailClient = emailClient;
        }
        public static string UserPoolId = Environment.GetEnvironmentVariable("AWS_COGNITO_POOL_ID");
        public static string[] ValidRoles = { "ASSISTANT", "VENUE_MANAGER" };
        public static string EMAIL_FROM_HD = Environment.GetEnvironmentVariable("EMAIL_FROM_HD");
        public static string EMAIL_FROM_NAME_HD = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME_HD");

        public async Task CreateNewUser(CreateUserModel model)
        {
            if (model.data.role != UserRole.SUPER.ToString())
            {
                var existingUsers = (await _genericEntityRepo.GetDataTyped(
                    new GraphApiPayload
                    {
                        data = new User { tenant_id = new Guid() },
                        filters = new User { tenant_id = CurrentUser.TenantId, is_deleted = false }
                    }, CurrentUser.OrgSecret)).rows;
                if (existingUsers.Count >= CurrentUser.ProductPlans.user_limit)
                    throw new Exception("User limit reached for your subscription plan.");
            }

            var validation = await _validationService.ValidateRequest(new ValidateRequest { p = model.data.phone_number });
            if (validation.Item1 != 200)
                throw new Exception("Phone is not valid");
            model.data.role = model.data.role.ToUpper();
            if (!IsHelpDesk && !ValidRoles.Contains(model.data.role))
                throw new Exception("Invalid role");
            var idpCode = CommonConstants.Org_Code_Conventus;
            var orgName = "";
            if (model.data.tenant_id != null)
            {
                var tenantData = await _tenantRepository.GetOrgCodeAndName(model.data.tenant_id.Value);
                if (tenantData == null)
                    throw new Exception("Failed to get idp_group from tenant");
                idpCode = tenantData.Item1;
                orgName = tenantData.Item2;
            }
            var secret = await _secretService.GetEncryptionSecret(idpCode);
            var hashedMail = AesEncryption.EncryptEcb(model.data.email, secret);

            //cognito is not creating for SUPER ROLE
            var idp_attribute = "";
            var temp_password = "";
            if (model.data.role != UserRole.SUPER.ToString())
            {
                temp_password = CommonHelperFunctions.GeneratePassword();
                var request = new AdminCreateUserRequest
                {
                    UserPoolId = UserPoolId,
                    Username = model.data.email,
                    UserAttributes = new List<AttributeType>
                {
                    new AttributeType { Name = "email", Value = model.data.email },
                    new AttributeType { Name = "email_verified", Value = "true" },
                    new AttributeType { Name = "custom:group", Value = idpCode  },
                    new AttributeType { Name = "phone_number", Value = model.data.phone_number },
                    new AttributeType { Name = "phone_number_verified", Value = "true" },
                    new AttributeType { Name = "given_name", Value = model.data.first_name },
                    new AttributeType { Name = "family_name", Value = model.data.last_name },
                    new AttributeType { Name = "custom:postal_code", Value = hashedMail  },
                    new AttributeType { Name = "custom:organization_code", Value = idpCode },
                },
                    TemporaryPassword = temp_password, // sendNotification with temp Passwod
                    MessageAction = "SUPPRESS" // Suppress email invitation (change to get invitation)
                };
                var provider = GetCognitoProvider();
                AdminCreateUserResponse createUserResponse;

                createUserResponse = await provider.AdminCreateUserAsync(request);

                var addToGroupRequest = new AdminAddUserToGroupRequest
                {
                    UserPoolId = UserPoolId,
                    Username = model.data.email,
                    GroupName = idpCode
                };
                await provider.AdminAddUserToGroupAsync(addToGroupRequest);
                idp_attribute = createUserResponse.User.Username;
            }

            var user = new User
            {
                create_bu = model.data.create_bu,
                email = hashedMail,
                first_name = model.data.first_name,
                last_name = model.data.last_name,
                phone_number = model.data.phone_number,
                role = model.data.role,
                idp_group = idpCode,
                tenant_id = model.data.tenant_id,
                idp_attribute = idp_attribute
            };

            await _genericEntityRepo.SaveEntity(user, secret);

            if (model.data.role != UserRole.SUPER.ToString())
                _ = SendEmail(model.data.email, user, temp_password, model.data.first_name, orgName);
        }

        public async Task UpdateUser(UpdateUserModel model)
        {
            if (!string.IsNullOrEmpty(model.data.phone_number))
            {
                var validation = await _validationService.ValidateRequest(new ValidateRequest { p = model.data.phone_number });
                if (validation.Item1 != 200)
                    throw new Exception("Phone is not valid");
            }
            if (!string.IsNullOrEmpty(model.data.role))
            {
                model.data.role = model.data.role.ToUpper();
                if (!ValidRoles.Contains(model.data.role))
                    throw new Exception("Invalid role");
            }
            model.data.modify_dt = DateTime.Now;

            var updateUserAttributes = new List<AttributeType>();
            if (!string.IsNullOrEmpty(model.data.phone_number))
                updateUserAttributes.Add(new AttributeType { Name = "phone_number", Value = model.data.phone_number });

            if (!string.IsNullOrEmpty(model.data.first_name))
                updateUserAttributes.Add(new AttributeType { Name = "given_name", Value = model.data.first_name });

            if (!string.IsNullOrEmpty(model.data.last_name))
                updateUserAttributes.Add(new AttributeType { Name = "family_name", Value = model.data.last_name });

            var resp = await _genericEntityRepo.UpdateEntity(model, CurrentUser.OrgSecret);

            if (resp.success && updateUserAttributes.Count > 0)
            {
                var emailDecr = AesEncryption.DecryptEcb(model.filters.email, CurrentUser.OrgSecret);
                var request = new AdminUpdateUserAttributesRequest
                {
                    UserPoolId = UserPoolId,
                    Username = emailDecr,
                    UserAttributes = updateUserAttributes
                };
                var provider = GetCognitoProvider();
                try
                {
                    _ = provider.AdminUpdateUserAttributesAsync(request);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to delete cognito user." + ex.Message);
                }
            }
        }

        public async Task<object> GetUsers(string model, string orgSecret)
        {
            var resp = await _genericEntityRepo.GetData(model, orgSecret);
            return resp;
        }

        public async Task DeleteUser(DeleteUserModel model)
        {
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
            var prefix = $"DELETED|{timestamp}|";

            model.data.email = $"{prefix}{model.filters.email}";
            model.data.delete_dt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff+00");

            var resp = await _genericEntityRepo.UpdateEntity(model, CurrentUser.OrgSecret);

            _ = RemoveUserFromVenues(model.filters.tenant_id, model.filters.email);

            var emailDecr = AesEncryption.DecryptEcb(model.filters.email, CurrentUser.OrgSecret);
            var request = new AdminDeleteUserRequest
            {
                UserPoolId = UserPoolId,
                Username = emailDecr
            };
            var provider = GetCognitoProvider();
            try
            {
                _ = provider.AdminDeleteUserAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to delete cognito user." + ex.Message);
            }
        }

        public async Task ResetPassword(string email)
        {
            var users = (await _genericEntityRepo.GetDataTyped(
                new GraphApiPayload
                {
                    data = new User { first_name = null, tenant_id = new Guid() },
                    filters = new User { email = email, is_deleted = false }
                }, CurrentUser.OrgSecret)).rows;

            var user = users.FirstOrDefault() ?? throw new Exception("User not found.");

            var tenantData = await _tenantRepository.GetOrgCodeAndName(user.tenant_id.Value);
            if (tenantData == null)
                throw new Exception("Failed to get tenant data.");

            var emailDecr = AesEncryption.DecryptEcb(email, CurrentUser.OrgSecret);
            var tempPassword = CommonHelperFunctions.GeneratePassword();

            var setPasswordRequest = new AdminSetUserPasswordRequest
            {
                UserPoolId = UserPoolId,
                Username = emailDecr,
                Password = tempPassword,
                Permanent = false
            };
            var provider = GetCognitoProvider();
            try
            {
                await provider.AdminSetUserPasswordAsync(setPasswordRequest);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to reset password for cognito user. " + ex.Message);
            }

            _ = SendResetPasswordEmail(emailDecr, user, tempPassword, user.first_name, tenantData.Item2);
        }

        private AmazonCognitoIdentityProviderClient GetCognitoProvider()
        {
            var accessKey = Environment.GetEnvironmentVariable("AWS_COGNITO_ACCESS_KEY");
            var accessSecret = Environment.GetEnvironmentVariable("AWS_COGNITO_ACCESS_SECRET");
            //todo - get config from secrets
            var awsCredentials = new BasicAWSCredentials(accessKey, accessSecret);
            return new AmazonCognitoIdentityProviderClient(awsCredentials, RegionEndpoint.EUCentral1);
        }

        public async Task SendVerificationCode(string email)
        {
            var emailDecr = AesEncryption.DecryptEcb(email, CurrentUser.OrgSecret);
            var request = new AdminResetUserPasswordRequest
            {
                UserPoolId = UserPoolId,
                Username = emailDecr
            };
            var provider = GetCognitoProvider();
            try
            {
                await provider.AdminResetUserPasswordAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to send verification code for cognito user. " + ex.Message);
            }
        }

        private async Task<string> SendResetPasswordEmail(string email, User user, string tempPass, string firstName, string tenantName)
        {
            var envPrefix = CommonHelperFunctions.GetEnvPrefix(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            var request = new SendEmailRequest
            {
                TemplateId = "user_create",
                ReferenceEntity = $"{user._schema}.{user._table}",
                ReferenceId = Guid.NewGuid().ToString(),
                Subject = "🔐 Reset della tua password Conventus",
                MessageType = "password_reset",
                TenantId = user.tenant_id.ToString(),
                FromEmail = EMAIL_FROM_HD,
                FromName = $"{envPrefix} Conventus Service Portal".TrimStart(' ')
            };
            request.EmailTo.Add(email);
            request.Substitutions.Add("{{first_name}}", firstName);
            request.Substitutions.Add("{{tenant.org_name}}", tenantName);
            request.Substitutions.Add("{{temp_pw}}", tempPass);
            request.Substitutions.Add("{{email}}", email);
            request.Substitutions.Add("{{conventus_user_email}}", email);
            try
            {
                var response = await _emailClient.SendEmailAsync(request);
                return response;
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            return null;
        }

        private async Task<string> SendEmail(string email, User user, string tempPass, string firstName, string tenantName)
        {
            var envPrefix = CommonHelperFunctions.GetEnvPrefix(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            var request = new SendEmailRequest
            {
                TemplateId = "user_create",
                ReferenceEntity = $"{user._schema}.{user._table}",
                ReferenceId = Guid.NewGuid().ToString(),
                Subject = "📧 Il tuo Conventus account è stato creato – Attivazione richiesta",
                MessageType = "user_create",
                TenantId = user.tenant_id.ToString(),
                FromEmail = EMAIL_FROM_HD,
                FromName = $"{envPrefix} Conventus Service Portal".TrimStart(' ')
            };
            request.EmailTo.Add(email);
            request.Substitutions.Add("{{first_name}}", firstName);
            request.Substitutions.Add("{{tenant.org_name}}", tenantName);
            request.Substitutions.Add("{{temp_pw}}", tempPass);
            request.Substitutions.Add("{{email}}", email);
            request.Substitutions.Add("{{conventus_user_email}}", email);
            try
            {
                Console.WriteLine("Sending EMAIL_REQUEST=>" + JsonConvert.SerializeObject(request));
                var response = await _emailClient.SendEmailAsync(request);
                return response;
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            return null;
        }

        private async Task RemoveUserFromVenues(Guid tenant_id, string email)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.REMOVE_USER_FROM_VENUE);//@p_tenant_id uuid, @p_u_email
            query.Parameters.AddWithValue("@p_tenant_id", NpgsqlTypes.NpgsqlDbType.Uuid, tenant_id);
            query.Parameters.AddWithValue("@p_u_email", NpgsqlTypes.NpgsqlDbType.Text, email);

            await _genericEntityRepo.ExecuteCommandVoid(query);
        }
    }
}
