using Integration.Grpc;

namespace CommonLibrary.Helpers
{
    public static class BookingEmailHelper
    {
        public static string EMAIL_FROM_NOTIFICATIONS = Environment.GetEnvironmentVariable("EMAIL_FROM_NOTIFICATIONS");
        public static string NO_REPLY_EMAIL = Environment.GetEnvironmentVariable("NO_REPLY_EMAIL");
        public static string SP_URL = Environment.GetEnvironmentVariable("SERVICE_PORTAL_URL");

        public static SendEmailRequest GetClientEmailRequest(BookingNotificationClientEmailModel data)
        {
            if (!data.page_url.StartsWith("https"))
                data.page_url = $"https://{data.page_url}";

            var request = new SendEmailRequest
            {
                TemplateId = data.templateId,
                ReferenceEntity = $"{data.referenceSchema}.{data.referenceEntity}",
                ReferenceId = data.booking_id.ToString(),
                Subject = data.subject,
                MessageType = data.messageType,
                TenantId = data.tenant_id.ToString(),
                FromEmail = EMAIL_FROM_NOTIFICATIONS,
                FromName = $"{data.envPrefix} {data.venue_name} - notifiche sulle prenotazioni da Conventus",
                ReplyTo = NO_REPLY_EMAIL
            };
            request.EmailTo.Add(data.u_email);
            request.Substitutions.Add("{{first_name}}", data.u_first);
            request.Substitutions.Add("{{last_name}}", data.u_last);
            request.Substitutions.Add("{{date}}", data.date);
            request.Substitutions.Add("{{start_hour}}", data.local_start);
            request.Substitutions.Add("{{end_hour}}", data.local_end);

            if (data.type.ToString().ToLower() == "p" && !data.isCancel)
            {
                request.Substitutions.Add("{{street_street_number}}", $"{data.street} {data.street_number}");
                request.Substitutions.Add("{{street_additional}}", data.street_addition ?? "");
                request.Substitutions.Add("{{postal_code}}", data.postal_code);
                request.Substitutions.Add("{{city}}", data.city);
                request.Substitutions.Add("({{region_code}})", $"({data.province_code})" ?? "");
                request.Substitutions.Add("{{country_name}}", data.country_name);
            }
            if (data.isCancel)
            {
                request.Substitutions.Add("{{make_appointment_link}}", $"{data.page_url}");
                request.Substitutions.Add("{{hour}}", data.local_start);
            }
            request.Substitutions.Add("{{reason_service_none}}", data.u_reason ?? "");
            request.Substitutions.Add("{{phone}}", data.venue_phone);
            request.Substitutions.Add("{{e-mail}}", data.venue_email);
            request.Substitutions.Add("{{dynamic_modify_link}}", $"{data.page_url}?modify={data.booking_id}");
            request.Substitutions.Add("{{dynamic_cancel_link}}", $"{data.page_url}?cancel={data.booking_id}");
            request.Substitutions.Add("{{venue_name}}", data.venue_name);
            request.Substitutions.Add("{{tenant.org_name}}", data.org_name);

            request.Substitutions.Add("{{appointee_email}}", data.u_email);
            request.Substitutions.Add("{{tenant_domain}}", data.page_url);

            return request;
        }

        public static SendEmailRequest GetVenueStaffEmailRequest(BookingNotificationVenueEmailModel data)
        {
            if (!data.pageUrl.StartsWith("https"))
                data.pageUrl = $"https://{data.pageUrl}";

            var request = new SendEmailRequest
            {
                TemplateId = data.templateId,
                ReferenceEntity = $"{data.referenceSchema}.{data.referenceEntity}",
                ReferenceId = data.booking_id.ToString(),
                Subject = data.subject,
                MessageType = data.messageType,
                TenantId = data.tenant_id.ToString(),
                FromEmail = EMAIL_FROM_NOTIFICATIONS,
                FromName = $"{data.envPrefix} {data.venue_name} - notifiche sulle prenotazioni da Conventus",
                ReplyTo = NO_REPLY_EMAIL
            };
            request.EmailTo.AddRange(data.emails);
            request.Substitutions.Add("{{appointee_first_name}}", data.appointee_first_name);
            request.Substitutions.Add("{{appointee_last_name}}", data.appointee_last_name);
            request.Substitutions.Add("{{appointee_email}}", data.appointee_email);
            request.Substitutions.Add("{{date}}", data.date);
            request.Substitutions.Add("{{start_hour}}", data.start_hour);
            request.Substitutions.Add("{{end_hour}}", data.end_hour);
            request.Substitutions.Add("{{venue_or_online}}", data.type.ToUpper());
            request.Substitutions.Add("{{reason_service_none}}", data.u_reason ?? "");
            request.Substitutions.Add("{{sp_booking_link}}", $"{SP_URL.TrimEnd('/')}/appointments/{data.booking_id}");
            request.Substitutions.Add("{{venue_name}}", data.venue_name);
            request.Substitutions.Add("{{tenant.org_name}}", data.tenant_name);

            request.Substitutions.Add("{{tenant_domain}}", data.pageUrl);

            return request;
        }

        public static SendEmailRequest GetVenueThankYouEmailRequest(BookingNotificationThankYouEmailModel data)
        {
            if (!data.pageUrl.StartsWith("https"))
                data.pageUrl = $"https://{data.pageUrl}";

            var request = new SendEmailRequest
            {
                TemplateId = data.templateId,
                ReferenceEntity = $"{data.referenceSchema}.{data.referenceEntity}",
                ReferenceId = data.booking_id.ToString(),
                Subject = data.subject,
                MessageType = data.messageType,
                TenantId = data.tenant_id.ToString(),
                FromEmail = EMAIL_FROM_NOTIFICATIONS,
                FromName = $"{data.envPrefix} {data.venue_name} - notifiche sulle prenotazioni da Conventus",
                ReplyTo = NO_REPLY_EMAIL
            };
            request.EmailTo.Add(data.appointee_email);
            request.Substitutions.Add("{{first_name}}", data.appointee_first_name);
            request.Substitutions.Add("{{last_name}}", data.appointee_first_name);
            request.Substitutions.Add("{{appointee_email}}", data.appointee_email);
            request.Substitutions.Add("{{venue_name}}", data.venue_name);
            request.Substitutions.Add("{{feedback_link}}", "");
            request.Substitutions.Add("{{venue_name}}", data.venue_name);
            request.Substitutions.Add("{{tenant.org_name}}", data.tenant_name);
            request.Substitutions.Add("{{tenant_domain}}", data.pageUrl);

            return request;
        }
    }

    public class BookingNotificationClientEmailModel
    {
        public string city { get; set; }
        public string date { get; set; }
        public string type { get; set; }
        public string street { get; set; }
        public string u_last { get; set; }
        public string u_email { get; set; }
        public string u_first { get; set; }
        public string org_name { get; set; }
        public string local_end { get; set; }
        public string tenant_id { get; set; }
        public string booking_id { get; set; }
        public string venue_name { get; set; }
        public string local_start { get; set; }
        public string postal_code { get; set; }
        public string venue_email { get; set; }
        public string venue_phone { get; set; }
        public string? province_code { get; set; }
        public string street_number { get; set; }
        public string? street_addition { get; set; }
        public string page_url { get; set; }
        public string templateId { get; set; }
        public string subject { get; set; }
        public string messageType { get; set; }
        public bool isCancel { get; set; }
        public string referenceEntity { get; set; }
        public string referenceSchema { get; set; }
        public string envPrefix { get; set; }
        public string u_reason { get; set; }
        public string country_name { get; set; }
    }

    public class BookingNotificationVenueEmailModel
    {
        public string templateId { get; set; }
        public string subject { get; set; }
        public string messageType { get; set; }
        public string referenceEntity { get; set; }
        public string referenceSchema { get; set; }
        public string appointee_first_name { get; set; }
        public string appointee_last_name { get; set; }
        public string appointee_email { get; set; }
        public string date { get; set; }
        public string start_hour { get; set; }
        public string end_hour { get; set; }
        public string type { get; set; }
        public string u_reason { get; set; }
        public string venue_name { get; set; }
        public string tenant_name { get; set; }
        public string booking_id { get; set; }
        public string envPrefix { get; set; }
        public string tenant_id { get; set; }
        public List<string> emails { get; set; }
        public string pageUrl { get; set; }
    }

    public class BookingNotificationThankYouEmailModel
    {
        public string templateId { get; set; }
        public string subject { get; set; }
        public string messageType { get; set; }
        public string referenceEntity { get; set; }
        public string referenceSchema { get; set; }
        public string appointee_first_name { get; set; }
        public string appointee_last_name { get; set; }
        public string appointee_email { get; set; }
        public string venue_name { get; set; }
        public string tenant_name { get; set; }
        public string booking_id { get; set; }
        public string envPrefix { get; set; }
        public string tenant_id { get; set; }
        public string pageUrl { get; set; }
    }
}
