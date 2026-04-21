using CommonLibrary.Domain.Entities;
using MimeDetective.Diagnostics;

namespace CommonLibrary.Helpers
{
    public static class MicrosoftIntegrationHelper
    {
        public static string BuildPassword(string orgCode, string service, int counter)
        {
            var servicePart = string.IsNullOrWhiteSpace(service)? "0": service.ToLower();
            return $"{orgCode.Reverse()}{DateTime.Now.Day}:{servicePart}#s{counter}";
        }

        public static string BuildLocalUpn (string org_code, string venue_id ,string service, int counter)
        {
            var servicePart = string.IsNullOrWhiteSpace(service) ? "" : $"-{service.ToLower()}";

            return $"{org_code}_{venue_id}{servicePart}_{counter}";
        }

        public static string BuildDisplayName(string orgCode, string venueName, string service, int counter)
        {
            var servicePart = string.IsNullOrWhiteSpace(service) ? "" : $"- {service.ToLower()}";

            return $"CONF {orgCode} {venueName} {servicePart} - {counter}";
        }

        public static string BuildFullUpn(string localPart, string domain) => $"{localPart}@{domain}";


    }
}

