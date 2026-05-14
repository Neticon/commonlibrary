using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Integrations;
using CommonLibrary.Models;
using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Integration.Grpc;
using Mapster;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServicePortal.Application.Interfaces;
using VenueGenerationService;

namespace CommonLibrary.SharedServices.Services
{
    public class VenueService : AppServiceBase, IVenueService
    {
        private readonly IGenericEntityRepository<Venue> _genericRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IValidationService _validationService;
        private readonly IVenueGenerationService _venueGenerationService;
        private readonly IMicrosoftClient _microsoftClient;
        private readonly IGenericEntityRepository<ProductPlans> _productPlanRepo;


        public VenueService(IGenericEntityRepository<Venue> genericRepository, IValidationService validationService, ICurrentUserService currentUserService, IVenueGenerationService venueGenerationService, IMicrosoftClient microsoftClient, ITenantRepository tenantRepository, IGenericEntityRepository<ProductPlans> productPlanRepo) : base(currentUserService)
        {
            _genericRepository = genericRepository;
            _validationService = validationService;
            _venueGenerationService = venueGenerationService;
            _microsoftClient = microsoftClient;
            _tenantRepository = tenantRepository;
            _productPlanRepo = productPlanRepo;
        }

        public async Task<ServiceResponse> CreateVenue(VenueModel data)
        {
            var validationResult = await _validationService.GetRedisDeviceIntel(data.data.email, data.data.phone, "");
            if (validationResult == null)
                throw new Exception("Invalid email or phone");

            var venue = data.data.Adapt<Venue>();
            venue.venue_id = Guid.NewGuid();
            venue.tenant_id = CurrentUser.TenantId; //GetFromUserContext
            venue.create_bu = CurrentUser.Decr_Email;
            venue.evs_id = new Guid(validationResult.EmailValidation);
            venue.pnvs_id = new Guid(validationResult.PhoneValidation);

            var tenant = (await _tenantRepository.GetDataTyped(new GraphApiPayload { data = new Tenant { intg_video = "", cntrct_plan = "" }, filters = new Tenant { tenant_id = CurrentUser.TenantId } })).rows.First();
            var tenantIntConfig = tenant.intg_video;
            if (tenantIntConfig == INTG_VIDEO.CONVENTUS_TEAMS.ToString() || tenantIntConfig == INTG_VIDEO.TEAMS.ToString() && data.isPublish)
            {
                var bookingMode = GetBookingModeFromConfig(data.data.configuration);
                var productPlan = (await _productPlanRepo.GetDataTyped(new GraphApiPayload { data = new ProductPlans { service_limit = 1, simultaneous_limit = 1 }, filters = new ProductPlans { plan_id = tenant.cntrct_plan } })).rows.First();
                var createUsers = await RunTeamsUsersCheckAndRebuild(venue, CurrentUser.OrgCode, bookingMode == "SERVICE", productPlan.simultaneous_limit.Value, null);

                if (createUsers)
                {
                    var respDB = await _genericRepository.SaveEntity(venue, CurrentUser.OrgSecret);
                    if (respDB.success)
                    {
                        _venueGenerationService.ReplaceJs(CurrentUser.TenantId.ToString());
                    }
                    else //remove created users if fails
                    {
                        RemoveMicrosoftConferenseUsers(venue.conf_user.Select(q => q.upn).ToList());
                    }
                    return new ServiceResponse { Result = respDB };
                }
                else
                {
                    return new ServiceResponse
                    {
                        Result = "Failed to execute microsoft integrations for users"
                    };
                }
            }
            else
            {
                var respDB = await _genericRepository.SaveEntity(venue, CurrentUser.OrgSecret);
                if (respDB.success)
                {
                    _venueGenerationService.ReplaceJs(CurrentUser.TenantId.ToString());
                }

                return new ServiceResponse { Result = respDB };
            }
        }

        public async Task<ServiceResponse> DeleteVenue(DeleteVenueModel data)
        {
            //check is tenant same as in context
            if (!CurrentUser.TenantId.ToString().Equals(data.filters.tenant_id))
                throw new Exception($"Cross tenant deletion!");
            data.filters.tenant_id = null;
            data.data.is_deleted = true;
            var tenant = (await _tenantRepository.GetDataTyped(new GraphApiPayload { data = new Tenant { intg_video = "", cntrct_plan = "" }, filters = new Tenant { tenant_id = CurrentUser.TenantId } })).rows.First();
            var tenantIntConfig = tenant.intg_video;
            if (tenantIntConfig == INTG_VIDEO.CONVENTUS_TEAMS.ToString() || tenantIntConfig == INTG_VIDEO.TEAMS.ToString() && data.isPublish)
            {
                var venue = (await _genericRepository.GetDataTyped(new GraphApiPayload { data = new Venue { venue_id = new Guid(), configuration = new object(), conf_user = new List<ConfUser>() }, filters = data.filters }, CurrentUser.OrgSecret)).rows.First();
                var bookingMode = GetBookingModeFromConfig(venue.configuration);
                var remove = await RemoveMicrosoftConferenseUsers(venue.conf_user.Select(q => q.upn).ToList());
                if (remove)
                {
                    var resp = await _genericRepository.UpdateEntity(data, CurrentUser.OrgSecret);
                    if (resp.success)
                        _venueGenerationService.ReplaceJs(CurrentUser.TenantId.ToString());
                    else
                    {
                        var productPlan = (await _productPlanRepo.GetDataTyped(new GraphApiPayload { data = new ProductPlans { service_limit = 1, simultaneous_limit = 1 }, filters = new ProductPlans { plan_id = tenant.cntrct_plan } })).rows.First();
                        var createUsers = await RunTeamsUsersCheckAndRebuild(venue, CurrentUser.OrgCode, bookingMode == "SERVICE", productPlan.simultaneous_limit.Value, null);
                    }
                    return new ServiceResponse { Result = resp };
                }
                else
                {
                    return new ServiceResponse
                    {
                        Result = "Failed to execute microsoft integrations for users"
                    };
                }
            }
            else
            {
                var resp = await _genericRepository.UpdateEntity(data, CurrentUser.OrgSecret);
                if (resp.success)
                    _venueGenerationService.ReplaceJs(CurrentUser.TenantId.ToString());
                return new ServiceResponse { Result = resp };

            }
        }

        public async Task<ServiceResponse> GetVenues(object data)
        {
            var resp = await _genericRepository.GetData(data, CurrentUser.OrgSecret);
            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> UpdateVenue(VenueModel data)
        {
            var includeNullList = new List<string>();
            if (data.data.reasons.HasValue)
                includeNullList.Add("reasons");
            var venue = data.data.Adapt<Venue>();
            if (data.data.reasons.HasValue)
                venue.reasons = data.data.reasons.Value.Value;
            venue.modify_bu = CurrentUser.Decr_Email;
            venue.modify_dt = DateTime.UtcNow;
            var emailUpdate = !string.IsNullOrEmpty(data.data.email);
            var phoneUpdate = !string.IsNullOrEmpty(data.data.phone);
            if (emailUpdate || phoneUpdate)
            {
                var validationResult = await _validationService.GetRedisDeviceIntel(data.data.email, data.data.phone, "");
                if (validationResult == null)
                    throw new Exception("Invalid email or phone");
                if (emailUpdate)
                    venue.evs_id = new Guid(validationResult.EmailValidation);
                if (phoneUpdate)
                    venue.pnvs_id = new Guid(validationResult.PhoneValidation);
            }
            var tenant = (await _tenantRepository.GetDataTyped(new GraphApiPayload { data = new Tenant { intg_video = "", cntrct_plan = "" }, filters = new Tenant { tenant_id = CurrentUser.TenantId } })).rows.First();
            var tenantIntConfig = tenant.intg_video;

            if (data.isPublish && (tenantIntConfig == INTG_VIDEO.CONVENTUS_TEAMS.ToString() || tenantIntConfig == INTG_VIDEO.TEAMS.ToString()))
            {
                var currentVenueJson = (await _genericRepository.GetData(new GraphApiPayload { data = new Venue { venue_id = new Guid(), configuration = new object(), reasons = new object(), name = "", country_code = "", conf_user = new List<ConfUser>() }, filters = data.filters }, CurrentUser.OrgSecret)).rows.First();

                var currentVenue = JsonConvert.DeserializeObject<Venue>(currentVenueJson.ToString());

                var productPlan = (await _productPlanRepo.GetDataTyped(new GraphApiPayload { data = new ProductPlans { service_limit = 1, simultaneous_limit = 1 }, filters = new ProductPlans { plan_id = tenant.cntrct_plan } })).rows.First();

                var bookingMode = GetBookingModeFromConfig(currentVenue.configuration);

                var microsoftResult = await RunTeamsUsersCheckAndRebuild(venue, CurrentUser.OrgCode, bookingMode == "SERVICE", productPlan.simultaneous_limit.Value, currentVenue);

                if (microsoftResult)
                {
                    var payload = new GraphApiPayload { data = venue, filters = data.filters };
                    var resp = await _genericRepository.UpdateEntity(payload, CurrentUser.OrgSecret, includeNullList: includeNullList);
                    if (resp.success)
                    {
                        _venueGenerationService.ReplaceJs(CurrentUser.TenantId.ToString());
                    }
                    else // need to restore old microsoft users so sending old data as new and new as old do compare 
                    {
                        var restoreMicrosoftResult = await RunTeamsUsersCheckAndRebuild(currentVenue, CurrentUser.OrgCode, bookingMode == "SERVICE", productPlan.simultaneous_limit.Value, venue);
                    }
                    return new ServiceResponse
                    {
                        Result = resp
                    };
                }
                else
                {
                    return new ServiceResponse
                    {
                        Result = "Failed to execute microsoft integrations for users"
                    };
                }
            }
            else
            {

                var payload = new GraphApiPayload { data = venue, filters = data.filters };
                var resp = await _genericRepository.UpdateEntity(payload, CurrentUser.OrgSecret, includeNullList: includeNullList);
                if (resp.success && data.isPublish)
                    _ = _venueGenerationService.ReplaceJs(CurrentUser.TenantId.ToString());

                return new ServiceResponse
                {
                    Result = resp
                };

            }
        }

        private async Task<bool> RunTeamsUsersCheckAndRebuild(Venue venue, string orgCode, bool serviceBased, int slots, Venue currentVenue)
        {
            var success = true;
            var currentUsersUpn = new List<string>();
            if (currentVenue.conf_user != null && currentVenue.conf_user.Count > 0)
                currentUsersUpn = currentVenue.conf_user.Select(q => q.upn).ToList();
            var configUsers = CreateMicrosoftConferenseUsers(venue, orgCode, serviceBased, slots, currentVenue);
            var configUsersUpn = configUsers.Select(q => q.FullUpn);

            var newUsersUpn = configUsersUpn.Except(currentUsersUpn);
            var removeUsersUpn = currentUsersUpn.Except(configUsersUpn);

            foreach (var user in newUsersUpn)
            {
                var request = configUsers.FirstOrDefault(q => q.FullUpn == user);
                var result = await _microsoftClient.CreateMicrosoftUser(request);
                if (result.UserId == null)
                {
                    return false;
                }

            }

            foreach (var user in removeUsersUpn)
            {
                var resp = await _microsoftClient.RemoveMicrosoftUser(new RemoveMicrosoftUserRequest { FullUpn = user });
                if (resp.Removed == false)
                {
                    return false;
                }
            }

            if (success)
                venue.conf_user = configUsersUpn.Select(q => new ConfUser { upn = q }).ToList();

            return success;
        }

        private List<MicrosoftUserRequest> CreateMicrosoftConferenseUsers(Venue venue, string orgCode, bool serviceBased, int slots, Venue oldVenue)
        {
            var usersByConfig = new List<MicrosoftUserRequest>();
            //get config from tenant if it is client teams
            var usageLocation = venue.country_code ?? oldVenue.country_code;
            for (int i = 1; i <= slots; i++)
            {
                if (!serviceBased)
                {
                    var displayName = MicrosoftIntegrationHelper.BuildDisplayName(orgCode, venue.name ?? oldVenue.name, null, i);
                    var localUpn = MicrosoftIntegrationHelper.BuildLocalUpn(orgCode, venue.venue_id == null ? oldVenue.venue_id.ToString() : venue.venue_id.ToString(), "DEFAULT", i);
                    var password = MicrosoftIntegrationHelper.BuildPassword(orgCode, null, i);
                    usersByConfig.Add(new MicrosoftUserRequest
                    {
                        DisplayName = displayName,
                        FullUpn = localUpn,
                        Password = password,
                        UsageLocation = usageLocation,
                    });
                }

                else
                {
                    var services = JsonConvert.DeserializeObject<List<JObject>>(venue.reasons != null ? venue.reasons.ToString() : oldVenue.reasons.ToString());
                    foreach (var service in services)
                    {
                        var service_id = service["id"].ToString();
                        var displayName = MicrosoftIntegrationHelper.BuildDisplayName(orgCode, venue.name, service_id, i);
                        var localUpn = MicrosoftIntegrationHelper.BuildLocalUpn(orgCode, venue.venue_id.ToString(), service_id, i);
                        var password = MicrosoftIntegrationHelper.BuildPassword(orgCode, service_id, i);
                        var request = new MicrosoftUserRequest
                        {
                            DisplayName = displayName,
                            FullUpn = localUpn,
                            Password = password,
                            UsageLocation = usageLocation,
                        };
                        usersByConfig.Add(request);
                    }
                }
            }
            return usersByConfig;
        }

        private async Task<bool> RemoveMicrosoftConferenseUsers(List<string> removeUpns)
        {
            foreach (var upn in removeUpns)
            {
                var request = new RemoveMicrosoftUserRequest
                {
                    FullUpn = upn,
                    //   Config = config
                };
                var resp = await _microsoftClient.RemoveMicrosoftUser(request);
                if (!resp.Removed)
                    return false;

            }
            return true;
        }

        private string GetBookingModeFromConfig(object config)
        {
            var configJson = (JToken)config;
            if (configJson["booking_mode"] == null)
                return null;
            return configJson["booking_mode"].ToString();
        }
    }
}
