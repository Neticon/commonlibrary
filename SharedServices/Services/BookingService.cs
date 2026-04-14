using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Integrations;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Models;
using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using ServicePortal.Application.Interfaces;
using WebApp.API.Controllers.Helper;

namespace CommonLibrary.SharedServices.Services
{
    public class BookingService : AppServiceBase, IBookingService
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

        public BookingService(IBlocksRepository blocksRepository, IValidationService validationService, IBookingRepository bookingRepository, IVenueRepository venueRepository, IObfIndexRepository obfIndexRepository, ITenantRepository tenantRepository, ISecretService secretService, IEmailClient emailClient, ICurrentUserService currentUserService): base(currentUserService)
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
            var tenantJobject = await _tenantRepository.GetData(new GraphApiPayload { data = new Tenant { org_code = "", web_pages = new List<string>(), org_name = "" }, filters = new TenantIdModel { tenant_id = data.tenant_id } });
            var tenant = JsonConvert.DeserializeObject<Tenant>(JsonConvert.SerializeObject(tenantJobject.rows[0]));
            var org_code = tenant.org_code;
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
                var venue = await GetVenueObject(data.venue_id.ToString(), secret);

                var date = DateTime.Parse(data.date);
                var venueTimezoneOffset = TimeZoneInfo.FindSystemTimeZoneById(venue.time_zone).GetUtcOffset(DateTime.UtcNow);

                var startTs = new DateTimeOffset(date.AddMinutes(data.block_start), venueTimezoneOffset);
                var endTs = new DateTimeOffset(date.AddMinutes(data.block_end), venueTimezoneOffset);
                var u_reasonDb = !string.IsNullOrEmpty(data.u_reason) ? data.u_reason : (data.service_id == "DEFAULT" ? null : data.service_id);
                var booking = new Booking
                {
                    booking_id = Guid.NewGuid(),
                    date = date,
                    tenant_id = data.tenant_id,
                    venue_id = data.venue_id,
                    create_bu = data.create_bu ?? CurrentUser.Decr_Email,
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
                        var value = data.GetPropertyValue(field);
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

                    var dateS = startTs.ToString("dd/MM/yyyy");
                    var start = startTs.ToString("HH:mm");
                    var end = endTs.ToString("HH:mm");

                    //retur response to user, indexes are inserted in background, send emails in background
                    _obfIndexRepository.InsertBulkIndexes(obfIndexes);
                    if (venue.notifications.notify == 1)
                    {
                        var reason = BookingEmailHelper.GetReasonForMail(u_reasonDb, venue).Item1;

                        SendEmail($"booking_scheduled_{data.type}".ToLower(), "📅 Il tuo appuntamento è stato confermato", "booking_scheduled", booking, data, start, end, dateS, venue, tenant.web_pages.Last(), reason, tenant.org_name);
                        SendEmailToStaff($"booking_scheduled_venue", "📅 Il tuo appuntamento è stato confermato", "booking_scheduled_venue", booking, venue.users, data, secret, start, end, dateS, reason, venue.name, tenant.org_name, tenant.web_pages.Last());
                    }
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
            var dataFromDB = await _bookingRepository.GetBookingUpdateData(data.filters.booking_id);
            var dataObject = dataFromDB.rows[0];
            var tenant = JsonConvert.DeserializeObject<Tenant>(dataObject["tenant"].ToString());
            var secret = await _secretService.GetSecret(tenant.org_code);

            var booking = JsonConvert.DeserializeObject<Booking>(dataObject["booking"].ToString());
            var encryptPaths = EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(Booking));
            ObjectEncryption.DecryptObject(booking, secret, encryptPaths.Item1, encryptPaths.Item2);

            var venue = JsonConvert.DeserializeObject<Venue>(dataObject["venue"].ToString());
            var encryptPathsVenue = EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(Venue));
            ObjectEncryption.DecryptObject(venue, secret, encryptPathsVenue.Item1, encryptPathsVenue.Item2);

            var reasonResult = BookingEmailHelper.GetReasonForMail(booking.u_reason, venue);
            var reason = reasonResult.Item1;

            if (string.IsNullOrEmpty(data.data.modify_bu))
                data.data.modify_bu = CurrentUser.Decr_Email;
            data.data.modify_dt = DateTime.UtcNow;

            if (data.data.block_status == "RESCHEDULED")
            {
                if (data.data.block_start == null || data.data.block_end == null)
                {
                    response.Result = 204;
                    return response;
                }
                //CHECK PATTERN LATER WITH FE {SRV*}
                var service = "";
                if (booking.u_reason == null)
                {
                    service = "DEFAULT";
                }
                else
                {
                    service = reasonResult.Item2 ? booking.u_reason : "DEFAULT";
                }
                var blockAvailability = await _blocksRepository.CheckBlocAvailability(data.data.block_start.Value, data.data.block_end.Value, data.data.type, new Guid(venue_id), data.data.date, service);
                if (blockAvailability.avail == -2)
                    response.StatusCode = 403;
                else if (blockAvailability.avail == -1)
                    response.StatusCode = 404;
                else if (blockAvailability.avail > 0)
                {
                    var venueTimezoneOffset = TimeZoneInfo.FindSystemTimeZoneById(venue.time_zone).GetUtcOffset(DateTime.UtcNow);

                    var date = DateTime.Parse(data.data.date);
                    var startTs = new DateTimeOffset(date.AddMinutes(data.data.block_start.Value), venueTimezoneOffset);
                    var endTs = new DateTimeOffset(date.AddMinutes(data.data.block_end.Value), venueTimezoneOffset);

                    data.data.start_ts = startTs.ToString(Constants.PSQLTimestampWithTZFromat);
                    data.data.end_ts = endTs.ToString(Constants.PSQLTimestampWithTZFromat);

                    var result = await _bookingRepository.UpdateEntity(data, ignoreEncryption: true);
                    if (result != null && result.success)
                    {
                        response.Result = result;
                        if (venue.notifications.rescedule == 1)
                        {
                            var dateS = startTs.ToString("dd/MM/yyyy");
                            var start = startTs.ToString("HH:mm");
                            var end = endTs.ToString("HH:mm");
                            var bookingData = new BookingModelData { tenant_id = tenant.tenant_id.Value, u_first = booking.u_first, u_last = booking.u_last, u_email = booking.u_email, type = booking.type, u_reason = "" };
                            SendEmail($"booking_rescheduled_{data.data.type}".ToLower(), "🔁 Il tuo appuntamento è stato riprogrammato", "booking_rescheduled", booking, bookingData, start, end, dateS, venue, tenant.web_pages.Last(), reason, tenant.org_name);
                            SendEmailToStaff($"booking_rescheduled_venue", "🔁 Il tuo appuntamento è stato riprogrammato", "booking_rescheduled_venue", booking, venue.users, bookingData, secret, start, end, dateS, reason, venue.name, tenant.org_name, tenant.web_pages.Last());
                        }
                    }
                    else
                    {
                        response.StatusCode = 204;
                    }
                };
            }
            else if (data.data.block_status == "CANCELLED")
            {
                var result = await _bookingRepository.UpdateEntity(data, ignoreEncryption: true);
                if (result != null && result.success)
                {
                    response.Result = result;
                    if (venue.notifications.cancel == 1)
                    {
                        var dateS = booking.date.ToString("dd/MM/yyyy");
                        var start = DateTime.Parse(booking.start_ts).ToString("HH:mm");
                        var end = DateTime.Parse(booking.end_ts).ToString("HH:mm");
                        var subject = "❌ Il tuo appuntamento è stato annullato";
                        var bookingData = new BookingModelData { tenant_id = tenant.tenant_id.Value, u_first = booking.u_first, u_last = booking.u_last, u_email = booking.u_email, type = booking.type };
                        SendEmail($"booking_cancellation", subject, "booking_cancelled", booking, bookingData, start, end, dateS, venue, tenant.web_pages.Last(), reason, tenant.org_name, true);
                        SendEmailToStaff($"booking_cancellation_venue", subject, "booking_cancelled_venue", booking, venue.users, bookingData, secret, start, end, dateS, reason, venue.name, tenant.org_name, tenant.web_pages.Last());
                    }
                }
                else
                {
                    response.StatusCode = 204;
                }
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

        public async Task<OBFSearchResponse> SearchBooking(ObfSearchModel searchModel)
        {
            var secret = await _secretService.GetSecret(searchModel.org_code);
            searchModel.salt = secret;
            var resp = await _obfIndexRepository.SearchBooking(searchModel);
            var respObject = JsonConvert.DeserializeObject<OBFSearchResponse>(resp);
            ObjectEncryption.DecryptObject(respObject, secret);
            return respObject;
        }

        private async Task<string> SendEmail(string templateId, string subject, string messageType, Booking booking, BookingModelData modelData, string start, string end, string date, Venue venue, string pageUrl, string reason, string tenantName, bool cancel = false)
        {
            try
            {
                var envPrefix = CommonHelperFunctions.GetEnvPrefix(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
                var request = BookingEmailHelper.GetClientEmailRequest(new BookingNotificationClientEmailModel
                {
                    page_url = pageUrl,
                    templateId = templateId,
                    referenceSchema = booking._schema,
                    referenceEntity = booking._table,
                    subject = subject,
                    messageType = messageType,
                    tenant_id = modelData.tenant_id.ToString(),
                    envPrefix = envPrefix,
                    venue_name = venue.name,
                    u_email = modelData.u_email,
                    u_first = modelData.u_first,
                    u_last = modelData.u_last,
                    date = date,
                    local_end = end,
                    local_start = start,
                    street = venue.street,
                    street_addition = venue.street_addition,
                    street_number = venue.street_number,
                    postal_code = venue.postal_code,
                    city = venue.city,
                    country_name = venue.country_code,
                    u_reason = reason,
                    booking_id = booking.booking_id.ToString(),
                    isCancel = cancel,
                    org_name = tenantName,
                    province_code = venue.province_code,
                    type = modelData.type.ToString(),
                    venue_email = venue.email,
                    venue_phone = venue.phone,
                });

                var response = await _emailClient.SendEmailAsync(request);
                return response;
            }
            catch (Exception ex) { Console.WriteLine(ex + ex.Message + ex.StackTrace); }
            return null;
        }

        private async Task SendEmailToStaff(string templateId, string subject, string messageType, Booking booking, List<VenueUser> users, BookingModelData modelData, string secret, string start, string end, string date, string u_reason, string venueName, string tenantName, string pageUrl)
        {
            Console.WriteLine("SEND STAFF EMAIL");
            var venueStaffEmails = users.Where(q => !q.r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) && q.n == 1).Select(q => q.u);
            var emailsTo = new List<string>();
            foreach (var email in venueStaffEmails)
            {
                var failed = false;
                try
                {
                    var dec = AesEncryption.DecryptEcb(email, secret);
                    emailsTo.Add(dec);
                }
                catch
                {
                    failed = true;
                }
                if (failed)
                { //for now because we still have NON ECB values somewhere in DEV
                    try
                    {
                        var dec = AesEncryption.Decrypt(email, secret);
                        emailsTo.Add(dec);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            try
            {
                Console.WriteLine("SEND STAFF EMAIL => " + JsonConvert.SerializeObject(emailsTo));
                if (!pageUrl.StartsWith("https"))
                    pageUrl = $"https://{pageUrl}";
                var envPrefix = CommonHelperFunctions.GetEnvPrefix(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

                var request = BookingEmailHelper.GetVenueStaffEmailRequest(new BookingNotificationVenueEmailModel
                {
                    templateId = templateId,
                    referenceEntity = booking._table,
                    referenceSchema = booking._schema,
                    appointee_email = modelData.u_email,
                    appointee_first_name = modelData.u_first,
                    appointee_last_name = modelData.u_last,
                    booking_id = booking.booking_id.ToString(),
                    date = date,
                    envPrefix = envPrefix,
                    venue_name = venueName,
                    emails = emailsTo,
                    end_hour = end,
                    start_hour = start,
                    messageType = messageType,
                    pageUrl = pageUrl,
                    subject = subject,
                    tenant_id = modelData.tenant_id.ToString(),
                    tenant_name = tenantName,
                    type = modelData.type.ToString(),
                    u_reason = u_reason,
                });

                var response = await _emailClient.SendEmailAsync(request);
            }
            catch (Exception ex) { Console.WriteLine(ex + ex.Message + ex.StackTrace); }
        }

        private async Task<Venue> GetVenueObject(string venueId, string secret)
        {
            var venueJobject = await _venueRepository.GetData(new GraphApiPayload { data = new Venue { tenant_id = new Guid(), time_zone = "", street = "", street_number = "", street_addition = "", city = "", postal_code = "", province_code = "", country_code = "", name = "", phone = "", email = "", users = new List<VenueUser> { }, notifications = new VenueNotifications { }, reasons = new object() }, filters = new VenueModelFilter { venue_id = venueId } }, secret);
            var venue = JsonConvert.DeserializeObject<Venue>(JsonConvert.SerializeObject(venueJobject.rows[0]));
            return venue;
        }

        private async Task<Tenant> GetTenantObject(Guid tenantId)
        {
            var tenantJobject = await _tenantRepository.GetData(new GraphApiPayload { data = new Tenant { org_code = "", web_pages = new List<string>(), org_name = "" }, filters = new TenantIdModel { tenant_id = tenantId } });
            var tenant = JsonConvert.DeserializeObject<Tenant>(JsonConvert.SerializeObject(tenantJobject.rows[0]));
            return tenant;
        }
    }
}
