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

        public VenueService(IGenericEntityRepository<Venue> genericRepository, IValidationService validationService, ICurrentUserService currentUserService, IVenueGenerationService venueGenerationService, IMicrosoftClient microsoftClient, ITenantRepository tenantRepository) : base(currentUserService)
        {
            _genericRepository = genericRepository;
            _validationService = validationService;
            _venueGenerationService = venueGenerationService;
            _microsoftClient = microsoftClient;
            _tenantRepository = tenantRepository;
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

            var resp = await _genericRepository.SaveEntity(venue, CurrentUser.OrgSecret);
            if (resp.success == true && data.isPublish)
            {
                var tenantIntConfig = await _tenantRepository.GetIntegrationConfig(CurrentUser.TenantId);
                if (tenantIntConfig == INTG_VIDEO.CONVENTUS_TEAMS.ToString() || tenantIntConfig == INTG_VIDEO.TEAMS.ToString())
                {
                    var bookingMode = GetBookingModeFromConfig(data.data.configuration);
                    CreateMicrosoftConferenseUsers(venue, CurrentUser.OrgCode, bookingMode == "SERVICE");
                    _venueGenerationService.ReplaceJs(CurrentUser.TenantId.ToString());
                }
            }
            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> DeleteVenue(DeleteVenueModel data)
        {
            //check is tenant same as in context
            if (!CurrentUser.TenantId.ToString().Equals(data.filters.tenant_id))
                throw new Exception($"Cross tenant deletion!");
            data.filters.tenant_id = null;
            data.data.is_deleted = true;
            var resp = await _genericRepository.UpdateEntity(data, CurrentUser.OrgSecret);
            var tenantIntConfig = await _tenantRepository.GetIntegrationConfig(CurrentUser.TenantId);
            if(tenantIntConfig == INTG_VIDEO.CONVENTUS_TEAMS.ToString() || tenantIntConfig == INTG_VIDEO.TEAMS.ToString())
            {
                var venue = (await _genericRepository.GetDataTyped(new GraphApiPayload { data = new Venue { venue_id = new Guid(), configuration = new object(), reasons = new object(), name = "", country_code = "" }, filters = data.filters }, CurrentUser.OrgSecret)).rows.First();
                var bookingMode = GetBookingModeFromConfig(venue.configuration);
                _ = RemoveMicrosoftConferenseUsers(venue, CurrentUser.OrgCode, bookingMode == "SERVICE", tenantIntConfig);
            }

            return new ServiceResponse { Result = resp };
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

            var payload = new GraphApiPayload { data = venue, filters = data.filters };

            var resp = await _genericRepository.UpdateEntity(payload, CurrentUser.OrgSecret, includeNullList: includeNullList);
            var bookingMode = GetBookingModeFromConfig(data.data.configuration);
            var tenantIntConfig = await _tenantRepository.GetIntegrationConfig(CurrentUser.TenantId);
            if (bookingMode != null && (tenantIntConfig == INTG_VIDEO.CONVENTUS_TEAMS.ToString() || tenantIntConfig == INTG_VIDEO.TEAMS.ToString()))
            {
                var currentVenueJson = (await _genericRepository.GetData(new GraphApiPayload { data = new Venue { venue_id = new Guid(), configuration = new object(), reasons = new object(), name = "", country_code = "" }, filters = data.filters }, CurrentUser.OrgSecret)).rows.First();
                var currentVenue = JsonConvert.DeserializeObject<Venue>(currentVenueJson.ToString());
                var currentBookingMode = GetBookingModeFromConfig(currentVenue.configuration);
                if (currentBookingMode != bookingMode)
                {
                    if (venue.name == null)
                        venue.name = currentVenue.name;
                    if (venue.reasons == null)
                        venue.reasons = currentVenue.reasons;
                    venue.venue_id = currentVenue.venue_id;
                    if (venue.country_code == null)
                        venue.country_code = currentVenue.country_code;
                    await RemoveMicrosoftConferenseUsers(venue, CurrentUser.OrgCode, bookingMode == "SERVICE", tenantIntConfig);
                    await CreateMicrosoftConferenseUsers(venue, CurrentUser.OrgCode, bookingMode == "SERVICE");
                }
            }
            if (resp.success == true && data.isPublish)
                _venueGenerationService.ReplaceJs(CurrentUser.TenantId.ToString());
            return new ServiceResponse { Result = resp };
        }

        private async Task CreateMicrosoftConferenseUsers(Venue venue, string orgCode, bool serviceBased)
        {
            //get config from tenant if it is client teams
            var usageLocation = venue.country_code;
            for (int i = 1; i <= 4; i++)
            {
                if (!serviceBased)
                {
                    var displayName = MicrosoftIntegrationHelper.BuildDisplayName(orgCode, venue.name, null, i);
                    var localUpn = MicrosoftIntegrationHelper.BuildLocalUpn(orgCode, venue.venue_id.ToString(), null, i);
                    var fullUpn = MicrosoftIntegrationHelper.BuildFullUpn(localUpn, "");
                    var password = MicrosoftIntegrationHelper.BuildPassword(orgCode, null, i);
                    var resp = await _microsoftClient.CreateMicrosoftUser(new MicrosoftUserRequest
                    {
                        DisplayName = displayName,
                        FullUpn = fullUpn,
                        Password = password,
                        UsageLocation = usageLocation,
                    });
                }

                else
                {
                    var services = JsonConvert.DeserializeObject<List<JObject>>(venue.reasons.ToString());
                    foreach (var service in services)
                    {
                        var service_id = service["id"].ToString();
                        var displayName = MicrosoftIntegrationHelper.BuildDisplayName(orgCode, venue.name, service_id, i);
                        var localUpn = MicrosoftIntegrationHelper.BuildLocalUpn(orgCode, venue.venue_id.ToString(), service_id, i);
                        var fullUpn = MicrosoftIntegrationHelper.BuildFullUpn(localUpn, "neticon.onmicrosoft.com");
                        var password = MicrosoftIntegrationHelper.BuildPassword(orgCode, service_id, i);
                        var request = new MicrosoftUserRequest
                        {
                            DisplayName = displayName,
                            FullUpn = fullUpn,
                            Password = password,
                            UsageLocation = usageLocation,
                        };
                        try
                        {
                            var resp = await _microsoftClient.CreateMicrosoftUser(request);
                        }
                        catch (Exception ex)
                        {
                            var a = 1;
                        }
                    }
                }

            }
        }

        private async Task RemoveMicrosoftConferenseUsers(Venue venue, string orgCode, bool serviceBased, string config)
        {
            for (int i = 1; i <= 4; i++)
            {
                if (serviceBased) // in this case remove DEFAULT users, and create service
                {
                    var localUpn = MicrosoftIntegrationHelper.BuildLocalUpn(orgCode, venue.venue_id.ToString(), null, i);
                    var fullUpn = MicrosoftIntegrationHelper.BuildFullUpn(localUpn, "neticon.onmicrosoft.com");
                    var resp = await _microsoftClient.RemoveMicrosoftUser(new RemoveMicrosoftUserRequest
                    {
                        FullUpn = fullUpn,
                        Config = config
                    });
                }
                else
                {
                    var services = JsonConvert.DeserializeObject<List<JObject>>(venue.reasons.ToString());
                    foreach (var service in services)
                    {
                        var service_id = service["id"].ToString();
                        var localUpn = MicrosoftIntegrationHelper.BuildLocalUpn(orgCode, venue.venue_id.ToString(), service_id, i);
                        var fullUpn = MicrosoftIntegrationHelper.BuildFullUpn(localUpn, "neticon.onmicrosoft.com");
                        var request = new RemoveMicrosoftUserRequest
                        {
                            FullUpn = fullUpn,
                            Config = config
                        };
                        try
                        {
                            var resp = await _microsoftClient.RemoveMicrosoftUser(request);
                        }
                        catch (Exception ex)
                        {
                            var a = 1;
                        }
                    }
                }

            }
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
