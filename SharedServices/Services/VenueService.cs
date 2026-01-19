using CommonLibrary.Domain.Entities;
using CommonLibrary.Integrations;
using CommonLibrary.Models;
using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Mapster;
using ServicePortal.Application.Interfaces;

namespace CommonLibrary.SharedServices.Services
{
    public class VenueService : AppServiceBase, IVenueService
    {
        private readonly IGenericEntityRepository<Venue> _genericRepository;
        private readonly IValidationService _validationService;

        public VenueService(IGenericEntityRepository<Venue> genericRepository, IValidationService validationService, ICurrentUserService currentUserService) : base(currentUserService)
        {
            _genericRepository = genericRepository;
            _validationService = validationService;
        }

        public async Task<ServiceResponse> CreateVenue(VenueModelData data)
        {
            var validationResult = await _validationService.GetRedisDeviceIntel(data.email, data.phone, "");
            if (validationResult == null)
                throw new Exception("Invalid email or phone");

            var venue = data.Adapt<Venue>();
            venue.venue_id = Guid.NewGuid();
            venue.tenant_id = CurrentUser.TenantId; //GetFromUserContext
            venue.create_bu = CurrentUser.Email;
            venue.evs_id = new Guid(validationResult.EmailValidation);
            venue.pnvs_id = new Guid(validationResult.PhoneValidation);

            var resp = await _genericRepository.SaveEntity(venue, CurrentUser.OrgSecret);
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
            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> GetVenues(object data)
        {
            var resp = await _genericRepository.GetData(data, CurrentUser.OrgSecret);
            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> UpdateVenue(VenueModel data)
        {

            //check flags for phone and email update? // it should come from FE??
            var emailUpdate = false;
            var phoneUpdate = false;
            if (emailUpdate || phoneUpdate)
            {
                var validationResult = await _validationService.GetRedisDeviceIntel(emailUpdate ? data.data.email : "", phoneUpdate ? data.data.phone : "", "");
            }

            var venue = data.data.Adapt<Venue>();
            venue.modify_bu = CurrentUser.Email;
            venue.modify_dt = DateTime.UtcNow;
            if (emailUpdate || phoneUpdate)
            {
                var validationResult = await _validationService.GetRedisDeviceIntel(emailUpdate ? data.data.email : "", phoneUpdate ? data.data.phone : "", "");
                if (validationResult == null)
                    throw new Exception("Invalid email or phone");
                if (emailUpdate)
                    venue.evs_id = new Guid(validationResult.EmailValidation);
                if (phoneUpdate)
                    venue.pnvs_id = new Guid(validationResult.PhoneValidation);
            }

            var payload = new GraphApiPayload { data = venue, filters = data.filters };

            var resp = await _genericRepository.UpdateEntity(payload, CurrentUser.OrgSecret);
            return new ServiceResponse { Result = resp };
        }
    }
}
