using CommonLibrary.Domain.Entities;
using MimeDetective.Diagnostics;
using System.Text;

namespace CommonLibrary.Helpers
{
    public static class MicrosoftIntegrationHelper
    {
        public static string BuildPassword(string orgCode, string service, int counter)
        {
            var servicePart = string.IsNullOrWhiteSpace(service)? "0": service.ToLower();
            var base36OrgCode = ToBase36(orgCode);
            var reversedOrgCode = new string(orgCode.Reverse().ToArray());

            return $"{reversedOrgCode}{base36OrgCode}:{servicePart}#s{counter}";
        }

        public static string BuildLocalUpn (string org_code, string venue_id ,string service, int counter)
        {
            var servicePart = string.IsNullOrWhiteSpace(service) ? "" : $"-{service}";

            return $"{org_code}_{venue_id}{servicePart}_{counter}";
        }

        public static string BuildDisplayName(string orgCode, string venueName, string service, int counter)
        {
            var servicePart = string.IsNullOrWhiteSpace(service) ? "" : $" - {service}";

            return $"{venueName}{servicePart} ({counter}) - Conventus";
        }

        public static string BuildFullUpn(string localPart, string domain) => $"{localPart}@{domain}";

        private static string ToBase36(string input)
        {
            // Convert string to numeric representation first
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);

            // Add 0 byte to avoid negative BigInteger
            var value = new System.Numerics.BigInteger(bytes.Concat(new byte[] { 0 }).ToArray());

            const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";

            if (value == 0)
                return "0";

            var result = new StringBuilder();

            while (value > 0)
            {
                value = System.Numerics.BigInteger.DivRem(value, 36, out var remainder);
                result.Insert(0, chars[(int)remainder]);
            }

            return result.ToString();
        }


    }
}

