using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Integrations;
using CommonLibrary.Models;
using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.Repository.Redis;
using CommonLibrary.SharedServices.Interfaces;
using Integration.Grpc;
using Mapster;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CommonLibrary.Domain.PSQL;
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
        private readonly IRedisService _redisService;
        private const string VENUE_PULICATION_STATE_PREFIX = "venue_publication:";

        public VenueService(IGenericEntityRepository<Venue> genericRepository, IValidationService validationService, ICurrentUserService currentUserService, IVenueGenerationService venueGenerationService, IMicrosoftClient microsoftClient, ITenantRepository tenantRepository, IGenericEntityRepository<ProductPlans> productPlanRepo, IRedisService redisService) : base(currentUserService)
        {
            _genericRepository = genericRepository;
            _validationService = validationService;
            _venueGenerationService = venueGenerationService;
            _microsoftClient = microsoftClient;
            _tenantRepository = tenantRepository;
            _productPlanRepo = productPlanRepo;
            _redisService = redisService;
        }

        public async Task<ServiceResponse> CreateVenue(VenueModel data)
        {
            var validationResult = await _validationService.GetRedisDeviceIntel(data.data.email, data.data.phone, "");
            if (validationResult == null)
                throw new Exception("Invalid email or phone");

            var venue = data.data.Adapt<Venue>();
            venue.venue_id = Guid.NewGuid();
            venue.tenant_id = CurrentUser.TenantId;
            venue.create_bu = CurrentUser.Decr_Email;
            venue.evs_id = new Guid(validationResult.EmailValidation);
            venue.pnvs_id = new Guid(validationResult.PhoneValidation);

            var tenant = (await _tenantRepository.GetDataTyped(new GraphApiPayload { data = new Tenant { intg_video = "", cntrct_plan = "" }, filters = new Tenant { tenant_id = CurrentUser.TenantId } })).rows.First();
            var tenantIntConfig = tenant.intg_video;

            if (tenantIntConfig == INTG_VIDEO.CONVENTUS_TEAMS.ToString() || tenantIntConfig == INTG_VIDEO.TEAMS.ToString() && data.isPublish)
            {
                var bookingMode = GetBookingModeFromConfig(data.data.configuration);
                var productPlan = (await _productPlanRepo.GetDataTyped(new GraphApiPayload { data = new ProductPlans { service_limit = 1, simultaneous_limit = 1 }, filters = new ProductPlans { plan_id = tenant.cntrct_plan } })).rows.First();

                return await ExecuteWithTeamsPublication(
                    venue.venue_id.Value,
                    venue,
                    CurrentUser.OrgCode,
                    bookingMode == "SERVICE",
                    productPlan.simultaneous_limit.Value,
                    null,
                    () => _genericRepository.SaveEntity(venue, CurrentUser.OrgSecret));
            }
            else
            {
                var respDB = await _genericRepository.SaveEntity(venue, CurrentUser.OrgSecret);
                if (respDB.success && data.isPublish)
                {
                    var publicationState = new VenuePublicationState { venue_id = new Guid(data.filters.venue_id), status = VenuePublicationStatus.busy };
                    await SaveCurrentPublicationState(publicationState);
                    publicationState.status = VenuePublicationStatus.idle;
                    await TryReplaceJs(CurrentUser.TenantId, publicationState);
                    _ = SaveCurrentPublicationState(publicationState);
                }

                return new ServiceResponse { Result = respDB };
            }
        }

        public async Task<ServiceResponse> DeleteVenue(DeleteVenueModel data)
        {
            //data.filters.tenant_id = null;
            data.data.is_deleted = true;
            var tenant = (await _tenantRepository.GetDataTyped(new GraphApiPayload { data = new Tenant { intg_video = "", cntrct_plan = "" }, filters = new Tenant { tenant_id = CurrentUser.TenantId } })).rows.First();
            var tenantIntConfig = tenant.intg_video;

            if (tenantIntConfig == INTG_VIDEO.CONVENTUS_TEAMS.ToString() || tenantIntConfig == INTG_VIDEO.TEAMS.ToString() && data.isPublish)
            {
                var publicationState = new VenuePublicationState { venue_id = new Guid(data.filters.venue_id), status = VenuePublicationStatus.busy };
                await SaveCurrentPublicationState(publicationState);
                publicationState.status = VenuePublicationStatus.idle;

                var venue = (await _genericRepository.GetDataTyped(new GraphApiPayload { data = new Venue { venue_id = new Guid(), configuration = new object(), conf_user = new List<ConfUser>() }, filters = data.filters }, CurrentUser.OrgSecret)).rows.First();
                var bookingMode = GetBookingModeFromConfig(venue.configuration);
                if (venue.conf_user != null)
                {
                    var remove = await RemoveMicrosoftConferenseUsers(venue.conf_user.Select(q => q.upn).ToList());
                    if (!remove)
                    {
                        SetPublicationError(publicationState, "Failed to execute microsoft integrations for users", "ENTRA_REMOVE_USER");
                        _ = SaveCurrentPublicationState(publicationState);
                        return new ServiceResponse
                        {
                            Result = new GraphAPIResponse<Venue>
                            {
                                success = false,
                                operation = publicationState.Step,
                                message = publicationState.Message
                            }
                        };
                    }
                }

                var resp = await _genericRepository.UpdateEntity(data, CurrentUser.OrgSecret);
                if (resp.success)
                {
                    await TryReplaceJs(new Guid(data.filters.tenant_id), publicationState);
                }
                else
                {
                    SetPublicationError(publicationState, resp.message, "DB_UPDATE");
                    var productPlan = (await _productPlanRepo.GetDataTyped(new GraphApiPayload { data = new ProductPlans { service_limit = 1, simultaneous_limit = 1 }, filters = new ProductPlans { plan_id = tenant.cntrct_plan } })).rows.First();
                    await RunTeamsUsersCheckAndRebuild(venue, CurrentUser.OrgCode, bookingMode == "SERVICE", productPlan.simultaneous_limit.Value, null);
                }

                if (resp.success && publicationState.status == VenuePublicationStatus.error)
                {
                    resp.success = false;
                    resp.operation = publicationState.Step;
                    resp.message = publicationState.Message;
                }

                _ = SaveCurrentPublicationState(publicationState);
                return new ServiceResponse { Result = resp };
            }
            else
            {
                var resp = await _genericRepository.UpdateEntity(data, CurrentUser.OrgSecret);
                if (resp.success && data.isPublish)
                {
                    var publicationState = new VenuePublicationState { venue_id = new Guid(data.filters.venue_id), status = VenuePublicationStatus.busy };
                    await SaveCurrentPublicationState(publicationState);
                    publicationState.status = VenuePublicationStatus.idle;
                    await TryReplaceJs(new Guid(data.filters.tenant_id), publicationState);
                    _ = SaveCurrentPublicationState(publicationState);
                }

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

                var bookingMode = GetBookingModeFromConfig(venue.configuration);
                if (bookingMode == null)
                    bookingMode = GetBookingModeFromConfig(currentVenue.configuration);

                return await ExecuteWithTeamsPublication(
                    new Guid(data.filters.venue_id),
                    venue,
                    CurrentUser.OrgCode,
                    bookingMode == "SERVICE",
                    productPlan.simultaneous_limit.Value,
                    currentVenue,
                    () => _genericRepository.UpdateEntity(new GraphApiPayload { data = venue, filters = data.filters }, CurrentUser.OrgSecret, includeNullList: includeNullList),
                    () => CleanUpPendingDeleteUsers(venue, data.filters));
            }
            else
            {
                var payload = new GraphApiPayload { data = venue, filters = data.filters };
                var resp = await _genericRepository.UpdateEntity(payload, CurrentUser.OrgSecret, includeNullList: includeNullList);
                if (resp.success && data.isPublish)
                {
                    var publicationState = new VenuePublicationState { venue_id = new Guid(data.filters.venue_id), status = VenuePublicationStatus.busy };
                    await SaveCurrentPublicationState(publicationState);
                    publicationState.status = VenuePublicationStatus.idle;
                    await TryReplaceJs(CurrentUser.TenantId, publicationState);
                    _ = SaveCurrentPublicationState(publicationState);
                }

                return new ServiceResponse { Result = resp };
            }
        }

        private async Task<ServiceResponse> ExecuteWithTeamsPublication(
            Guid venueId,
            Venue venue,
            string orgCode,
            bool serviceBased,
            int simultaneousLimit,
            Venue currentVenue,
            Func<Task<GraphAPIResponse<Venue>>> dbOperation,
            Func<Task<bool>> postPublishOperation = null)
        {
            var publicationState = new VenuePublicationState { venue_id = venueId, status = VenuePublicationStatus.busy };
            await SaveCurrentPublicationState(publicationState);
            publicationState.status = VenuePublicationStatus.idle;

            var teamsResult = await RunTeamsUsersCheckAndRebuild(venue, orgCode, serviceBased, simultaneousLimit, currentVenue);
            if (!teamsResult.Success)
            {
                SetPublicationError(publicationState, teamsResult.Message, "ENTRA_CREATE_USER");
                _ = SaveCurrentPublicationState(publicationState);
                return new ServiceResponse
                {
                    Result = new GraphAPIResponse<Venue>
                    {
                        success = false,
                        operation = publicationState.Step,
                        message = publicationState.Message,
                        affected_ids = new List<object> { venueId }
                    }
                };
            }

            var resp = await dbOperation();
            if (resp.success)
            {
                var jsGenerated = await TryReplaceJs(CurrentUser.TenantId, publicationState);
                if (jsGenerated && postPublishOperation != null)
                {
                    var postSuccess = await postPublishOperation();
                    if (!postSuccess)
                        SetPublicationError(publicationState, "Failed to remove old entra users.", "ENTRA_REMOVE_USER");
                }
            }
            else
            {
                SetPublicationError(publicationState, resp.message, "DB_UPDATE");
                _ = RemoveMicrosoftConferenseUsers(venue.conf_user.Where(q => q.status == ConfUserStatus.Active).Select(q => q.upn).ToList());
            }

            if (resp.success && publicationState.status == VenuePublicationStatus.error)
            {
                resp.success = false;
                resp.operation = publicationState.Step;
                resp.message = publicationState.Message;
            }

            if (resp.affected_ids == null || !resp.affected_ids.Any())
                resp.affected_ids = new List<object> { venueId };

            _ = SaveCurrentPublicationState(publicationState);
            return new ServiceResponse { Result = resp };
        }

        public async Task<bool> TryReplaceJs(Guid tenantId, VenuePublicationState publicationState = null)
        {
            try
            {
                await _venueGenerationService.ReplaceJs(tenantId.ToString());
                return true;
            }
            catch (Exception ex)
            {
                if (publicationState != null)
                    SetPublicationError(publicationState, ex.Message, "JS_GENERATE_PUBLISH");
                return false;
            }
        }

        private static void SetPublicationError(VenuePublicationState state, string message, string step)
        {
            state.status = VenuePublicationStatus.error;
            state.Message = message;
            state.Step = step;
        }

        private async Task<(bool Success, string? Message)> RunTeamsUsersCheckAndRebuild(Venue venue, string orgCode, bool serviceBased, int slots, Venue currentVenue)
        {
            var currentUsersUpn = new List<string>();
            if (currentVenue != null && currentVenue.conf_user != null && currentVenue.conf_user.Count > 0)
                currentUsersUpn = currentVenue.conf_user.Select(q => q.upn).ToList();
            var configUsers = CreateMicrosoftConferenseUsers(venue, orgCode, serviceBased, slots, currentVenue);
            var configUsersUpn = configUsers.Select(q => q.FullUpn);

            var newUsersUpn = configUsersUpn.Except(currentUsersUpn).ToList();
            var removeUsersUpn = currentUsersUpn.Except(configUsersUpn);

            if (newUsersUpn.Count > 0)
            {
                var licenseCheck = await _microsoftClient.CheckLicenseAvailability(
                    new CheckLicenseAvailabilityRequest { Required = newUsersUpn.Count });
                if (!licenseCheck.Success)
                    return (false, licenseCheck.Message);
            }

            var usersCreated = new List<string>();
            var success = true;

            foreach (var user in newUsersUpn)
            {
                var request = configUsers.FirstOrDefault(q => q.FullUpn == user);
                var result = await _microsoftClient.CreateMicrosoftUser(request);
                if (result.UserId == "")
                {
                    success = false;
                    break;
                }
                else
                    usersCreated.Add(user);
            }

            if (!success)
            {
                foreach (var user in usersCreated)
                    await _microsoftClient.RemoveMicrosoftUser(new RemoveMicrosoftUserRequest { FullUpn = user });

                return (false, "Failed to create Microsoft conference users.");
            }

            venue.conf_user = configUsersUpn.Select(q => new ConfUser { upn = q, status = ConfUserStatus.Active }).ToList();
            venue.conf_user.AddRange(removeUsersUpn.Select(q => new ConfUser { upn = q, status = ConfUserStatus.Pending_Delete }));

            return (true, null);
        }

        private List<MicrosoftUserRequest> CreateMicrosoftConferenseUsers(Venue venue, string orgCode, bool serviceBased, int slots, Venue oldVenue)
        {
            var usersByConfig = new List<MicrosoftUserRequest>();

            var configSource = venue.configuration ?? oldVenue.configuration;
            var serviceReasons = configSource != null ? ((JToken)configSource)["service_reason"] : null;

            var usageLocation = venue.country_code ?? oldVenue.country_code;
            for (int i = 1; i <= slots; i++)
            {
                if (!serviceBased)
                {
                    var hasV = serviceReasons?
                        .Where(sr => sr["service_id"]?.ToString().Equals("DEFAULT", StringComparison.OrdinalIgnoreCase) == true)
                        .SelectMany(sr => sr["blocks"] ?? Enumerable.Empty<JToken>())
                        .Any(block => block["v"]?.Count() > 0) == true;
                    if (!hasV)
                        break;

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

                        var hasV = serviceReasons?
                            .Where(sr => sr["service_id"]?.ToString().Equals(service_id, StringComparison.OrdinalIgnoreCase) == true)
                            .SelectMany(sr => sr["blocks"] ?? Enumerable.Empty<JToken>())
                            .Any(block => block["v"]?.Count() > 0) == true;
                        if (!hasV)
                            continue;

                        var displayName = MicrosoftIntegrationHelper.BuildDisplayName(orgCode, venue.name ?? oldVenue.name, service_id, i);
                        var localUpn = MicrosoftIntegrationHelper.BuildLocalUpn(orgCode, venue.venue_id == null ? oldVenue.venue_id.ToString() : venue.venue_id.ToString(), service_id, i);
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

        private async Task<bool> CleanUpPendingDeleteUsers(Venue venue, object filters)
        {
            var removeResp = await RemoveMicrosoftConferenseUsers(
                venue.conf_user.Where(q => q.status == ConfUserStatus.Pending_Delete).Select(q => q.upn).ToList());
            if (removeResp)
                await _genericRepository.UpdateEntity(
                    new GraphApiPayload { data = new Venue { conf_user = venue.conf_user.Where(q => q.status == ConfUserStatus.Active).ToList() }, filters = filters },
                    ignoreEncryption: true);
            return removeResp;
        }

        private async Task<bool> RemoveMicrosoftConferenseUsers(List<string> removeUpns)
        {
            foreach (var upn in removeUpns)
            {
                var request = new RemoveMicrosoftUserRequest
                {
                    FullUpn = upn,
                };
                var resp = await _microsoftClient.RemoveMicrosoftUser(request);
                if (!resp.Removed)
                    return false;
            }
            return true;
        }

        private string GetBookingModeFromConfig(object config)
        {
            if (config == null)
                return null;
            var configJson = (JToken)config;
            if (configJson["booking_mode"] == null)
                return null;
            return configJson["booking_mode"].ToString();
        }

        private async Task SaveCurrentPublicationState(VenuePublicationState state)
        {
            var key = $"{VENUE_PULICATION_STATE_PREFIX}{state.venue_id}";
            await _redisService.SetString(key, JsonConvert.SerializeObject(state));
        }

        public async Task<ServiceResponse> RetryPublish(string venueId)
        {
            var key = $"{VENUE_PULICATION_STATE_PREFIX}{venueId}";
            var redisValue = await _redisService.GetString(key);
            if (redisValue == null)
                return new ServiceResponse { StatusCode = 404 };

            var state = JsonConvert.DeserializeObject<VenuePublicationState>(redisValue);
            if (state.status != VenuePublicationStatus.error)
                return new ServiceResponse
                {
                    Result = new GraphAPIResponse<Venue> { success = false, message = "Publication is not in error state" }
                };

            var filters = new VenueModelFilter { venue_id = venueId };

            state.status = VenuePublicationStatus.busy;
            await SaveCurrentPublicationState(state);
            state.status = VenuePublicationStatus.idle;

            if (state.Step == "JS_GENERATE_PUBLISH")
            {
                var success = await TryReplaceJs(CurrentUser.TenantId, state);
                _ = SaveCurrentPublicationState(state);
                return new ServiceResponse
                {
                    Result = new GraphAPIResponse<Venue> { success = success, message = state.Message, operation = state.Step }
                };
            }

            if (state.Step == "ENTRA_REMOVE_USER")
            {
                var venue = (await _genericRepository.GetDataTyped(new GraphApiPayload
                {
                    data = new Venue { venue_id = new Guid(), conf_user = new List<ConfUser>() },
                    filters = filters
                }, CurrentUser.OrgSecret)).rows.First();

                var removeSuccess = await CleanUpPendingDeleteUsers(venue, filters);
                if (!removeSuccess)
                {
                    SetPublicationError(state, "Failed to remove old entra users.", "ENTRA_REMOVE_USER");
                    _ = SaveCurrentPublicationState(state);
                    return new ServiceResponse
                    {
                        Result = new GraphAPIResponse<Venue> { success = false, message = state.Message, operation = state.Step }
                    };
                }

                await TryReplaceJs(CurrentUser.TenantId, state);
                _ = SaveCurrentPublicationState(state);
                return new ServiceResponse
                {
                    Result = new GraphAPIResponse<Venue> { success = state.status != VenuePublicationStatus.error, message = state.Message, operation = state.Step }
                };
            }

            state.status = VenuePublicationStatus.error;
            _ = SaveCurrentPublicationState(state);
            return new ServiceResponse
            {
                StatusCode = 422,
                Result = new GraphAPIResponse<Venue> { success = false, message = $"Publication step '{state.Step}' cannot be automatically retried.", operation = state.Step }
            };
        }

        public async Task<ServiceResponse> GetPublicationState(string venueId)
        {
            var key = $"{VENUE_PULICATION_STATE_PREFIX}{venueId}";
            var redisValue = await _redisService.GetString(key);
            if (redisValue == null)
            {
                return new ServiceResponse { StatusCode = 404 };
            }
            var state = JsonConvert.DeserializeObject<VenuePublicationState>(redisValue);
            return new ServiceResponse { Result = state };
        }
    }
}
