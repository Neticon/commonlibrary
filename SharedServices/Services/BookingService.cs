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

namespace CommonLibrary.SharedServices.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBlocksRepository _blocksRepository;
        private readonly IValidationService _validationService;
        private readonly IBookingRepository _bookingRepository;
        private readonly IVenueRepository _venueRepository;
        private readonly IObfIndexRepository _obfIndexRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ISecretService _secretService;
        private readonly IEmailClient _emailClient;
        private readonly string[] INDEXED_FIELDS = ["u_first", "u_last", "u_email", "u_phone", "u_message", "u_reason"];
        public static string EMAIL_FROM_HD = Environment.GetEnvironmentVariable("EMAIL_FROM_HD");
        public static string EMAIL_FROM_NAME_HD = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME_HD");

        public BookingService(IBlocksRepository blocksRepository, IValidationService validationService, IBookingRepository bookingRepository, IVenueRepository venueRepository, IObfIndexRepository obfIndexRepository, ITenantRepository tenantRepository, ISecretService secretService, IEmailClient emailClient)
        {
            _blocksRepository = blocksRepository;
            _validationService = validationService;
            _bookingRepository = bookingRepository;
            _venueRepository = venueRepository;
            _obfIndexRepository = obfIndexRepository;
            _tenantRepository = tenantRepository;
            _secretService = secretService;
            _emailClient = emailClient;
        }

        public async Task<ServiceResponse> CreateBooking(BookingModelData data, string ip)
        {
            var response = new ServiceResponse { StatusCode = 200 };
            var org_code = (await _tenantRepository.GetOrgCodeAndName(data.tenant_id))?.Item1;
            if (string.IsNullOrEmpty(org_code))
                throw new Exception("Invalid tenant id");
            if (data.val_fail != null && data.val_fail.Count > 0)
            {
                var resp = await _validationService.ValidateRequest(new ValidateRequest { e = data.val_fail.Contains("email") ? data.u_email : "", p = data.val_fail.Contains("phone") ? data.u_phone : "" });
                if (resp.Item1 != 200)
                {
                    response.StatusCode = 422;
                    return response;
                }
            }
            var blockAvailability = await _blocksRepository.CheckBlocAvailability(data.block_start, data.block_end, data.type.ToString(), data.venue_id, data.date, data.service_id);
            if (blockAvailability.avail == -2)
                response.StatusCode = 403;
            else if (blockAvailability.avail == -1)
                response.StatusCode = 404;
            else if (blockAvailability.avail > 0)
            {
                var deviceIntelInfos = await _validationService.GetRedisDeviceIntel(data.u_email, data.u_phone, ip);
                if (deviceIntelInfos == null)
                {
                    response.StatusCode = 422;
                    return response;
                }
                var secret = await _secretService.GetSecret(org_code);

                var date = DateTime.Parse(data.date);
                var timeZone = await _venueRepository.GetVenueTimezone(data.venue_id);
                var venueJobject = await _venueRepository.GetData(new GraphApiPayload { data = new Venue { time_zone = "", street = "", street_number = "", street_addition = "", city = "", postal_code = "", province_name = "", country_code = "", name = "", phone = "", email = "" }, filters = new VenueModelFilter { venue_id = data.venue_id.ToString() } }, secret);
                var venue = JsonConvert.DeserializeObject<Venue>(JsonConvert.SerializeObject(venueJobject.rows[0]));
                var venueTimezoneOffset = TimeZoneInfo.FindSystemTimeZoneById(timeZone).GetUtcOffset(DateTime.UtcNow);
                var startTs = new DateTimeOffset(date.AddMinutes(data.block_start), venueTimezoneOffset);
                var endTs = new DateTimeOffset(date.AddMinutes(data.block_end), venueTimezoneOffset);
                var u_reasonDb = !string.IsNullOrEmpty(data.u_reason) ? data.u_reason : (data.service_id == "DEFAULT" ? null : data.service_id);
                var booking = new Booking
                {
                    booking_id = Guid.NewGuid(),
                    date = date,
                    tenant_id = data.tenant_id,
                    venue_id = data.venue_id,
                    create_bu = "web_api",
                    type = data.type,
                    start_ts = startTs.ToString(Constants.PSQLTimestampWithTZFromat),
                    end_ts = endTs.ToString(Constants.PSQLTimestampWithTZFromat),
                    block_start = data.block_start,
                    block_end = data.block_end,
                    u_salutation = data.u_salutation,
                    u_first = data.u_first,
                    u_last = data.u_last,
                    u_phone = data.u_phone,
                    u_phone_local = deviceIntelInfos.LocalPhone,
                    u_email = data.u_email,
                    u_message = data.u_message,
                    u_reason = u_reasonDb,
                    evs_id = new Guid(deviceIntelInfos.EmailValidation),
                    pnvs_id = new Guid(deviceIntelInfos.PhoneValidation),
                    ip_id = new Guid(deviceIntelInfos.IPValidation),
                };
                var result = await _bookingRepository.SaveEntity(booking, secret);

                if (result != null && result.success)
                {
                    response.Result = result;

                    //insert indexes 
                    var obfIndexes = new List<ObfIndexDBModel>();
                    var dateIndex = DateTime.UtcNow.ToString("yyyy-MM-dd");

                    foreach (var field in INDEXED_FIELDS)
                    {
                        var value = booking.GetPropertyValue(field);
                        if (value == null)
                            continue;
                        var stringValue = value.ToString();
                        if (!string.IsNullOrEmpty(stringValue))
                        {
                            obfIndexes.Add(new ObfIndexDBModel
                            {
                                booking_id = booking.booking_id,
                                venue_id = data.venue_id,
                                org_code = org_code,
                                field = field,
                                raw_value = stringValue,
                                date = dateIndex,
                                salt = secret
                            });
                        }
                    }

                    //retur response to user, indexes are inserted in background, send emails in background
                     _obfIndexRepository.InsertBulkIndexes(obfIndexes);
                    SendEmail(booking, data, $"{startTs.Hour}:{startTs.Minute}", $"{endTs.Hour}:{endTs.Minute}", endTs.Date.ToString(), venue);
                }
                else
                {
                    response.StatusCode = 204;
                }
            }
            else
                response.StatusCode = 204;
            return response;
        }

        public async Task<ServiceResponse> UpdateBooking(BookingUpdateModel data, string venue_id)
        {
            var response = new ServiceResponse { StatusCode = 200 };
            if (data.data.block_status == "RESCHEDULED")
            {
                if (data.data.block_start == null || data.data.block_end == null)
                {
                    response.Result = 204;
                    return response;
                }
                var bookingReason = await _bookingRepository.GetBookingReason(new Guid(data.filters.booking_id));
                //CHECK PATTERN LATER WITH FE {SRV*}
                var service = bookingReason.StartsWith("SRV", StringComparison.OrdinalIgnoreCase) ? bookingReason : "DEFAULT";
                var blockAvailability = await _blocksRepository.CheckBlocAvailability(data.data.block_start.Value, data.data.block_end.Value, data.data.type, new Guid(venue_id), data.data.date, service);
                if (blockAvailability.avail == -2)
                    response.StatusCode = 403;
                else if (blockAvailability.avail == -1)
                    response.StatusCode = 404;
                else if (blockAvailability.avail > 0)
                {
                    var result = await _bookingRepository.UpdateEntity(data, ignoreEncryption: true);
                    if (result != null && result.success)
                        response.Result = result;
                    else
                    {
                        response.StatusCode = 204;
                    }
                };
            }
            else
            {
                var result = await _bookingRepository.UpdateEntity(data, ignoreEncryption: true);
                if (result != null && result.success)
                    response.Result = result;
                else
                {
                    response.StatusCode = 204;
                }
            }

            return response;
        }

        public async Task<ServiceResponse> RateBooking(BookingRateModel data)
        {
            var response = new ServiceResponse { StatusCode = 200 };
            var result = await _bookingRepository.RateBooking(data.filters.booking_id, data.filters.date, data.data.customer_rank);
            response.Result = result;

            return response;
        }

        public async Task<ServiceResponse> GetBooking(Guid bookingId)
        {
            var result = new ServiceResponse { StatusCode = 200 };
            result.Result = await _bookingRepository.GetBooking(bookingId);

            return result;
        }

        private async Task<string> SendEmail(Booking booking, BookingModelData modelData, string start, string end, string date, Venue venue)
        {
            Console.WriteLine("EMAIL SEND1");
            try
            {
                var request = new SendEmailRequest
            {
                TemplateId =$"booking_scheduled_{modelData.type}".ToLower(),
                ReferenceEntity = $"{booking._schema}.{booking._table}",
                ReferenceId = Guid.NewGuid().ToString(),
                Subject = "Il tuo appuntamento è stato confermato",
                MessageType = "BookingScheduled",
                TenantId = modelData.tenant_id.ToString(),
                FromEmail = EMAIL_FROM_HD,
                FromName = EMAIL_FROM_NAME_HD
            };
            request.EmailTo.Add(modelData.u_email);
            request.Substitutions.Add("{{first_name}}", modelData.u_first);
            request.Substitutions.Add("{{last_name}}", modelData.u_last);
            request.Substitutions.Add("{{date}}", date);
            request.Substitutions.Add("{{start_hour}}", start);
            request.Substitutions.Add("{{end_hour}}", end);
            request.Substitutions.Add("{{street}}", venue.street);
            request.Substitutions.Add("{{street_number}}", venue.street_number);
            request.Substitutions.Add("{{street_additional}}", venue.street_addition);
            request.Substitutions.Add("{{postal_code}}", venue.postal_code);
            request.Substitutions.Add("{{city}}", venue.city);
            request.Substitutions.Add("{{region_code}}", venue.province_name);
            request.Substitutions.Add("{{country_name}}", venue.country_code);

            request.Substitutions.Add("{{phone}}", venue.phone);
            request.Substitutions.Add("{{e-mail}}", venue.email);
            request.Substitutions.Add("{{dynamic_modify_link}}", "");
            request.Substitutions.Add("{{venue_name}}", venue.name);
          
                Console.WriteLine("Sending EMAIL_REQUEST=>" + JsonConvert.SerializeObject(request));
                var response = await _emailClient.SendEmailAsync(request);
                return response;
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            return null;

        }
    }
}
