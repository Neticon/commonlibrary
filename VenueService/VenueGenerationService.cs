using CommonLibrary.Helpers;
using CommonLibrary.Integrations;
using CommonLibrary.Repository.Interfaces;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using WebApp.API.Controllers.Helper;

namespace VenueGenerationService
{
    public class VenueGenerationService : IVenueGenerationService
    {
        private readonly IS3Service _s3Servce;
        private const string ObfuscationPlaceholder = "__OBF_HASH__";
        private const string VenueDataPlaceholder = "\"__VENUE_DATA__\"";
        private const string DomHashPlaceholder = "\"__DOM_HASH__\"";
        private const string CustomerIdPlaceholder = "\"__CUSTOMER_ID__\"";
        private const string TimestampPlaceholder = "\"__TIMESTAMP__\"";
        private const string UISettingsPlaceholder = "\"__UI_SETTINGS__\"";
        private const string S3Bucket = "venues-cloudfront-nonprod";
        private const string JsTemplateFile = "source/base.js";
        private const string JSFilename = "wb.js";
        private const int HmacLength = 64;
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Default,
            WriteIndented = true
        };

        private const string SecretKey = "keyTest";
        private readonly IGenericRepository<object> _repository;
        private readonly ITenantRepository _tenantRepository;

        public VenueGenerationService(IGenericRepository<object> repository, IS3Service s3Service, ITenantRepository tenantRepository)
        {
            _repository = repository;
            _s3Servce = s3Service;
            _tenantRepository = tenantRepository;
        }

        public async Task ReplaceJs(string tenantId)
        {

            var templateJs = await _s3Servce.DownloadFileStringAsync(S3Bucket, JsTemplateFile);

            var venuesData = await GetVenueData(tenantId);
            var jsValue = JsonConvert.SerializeObject(venuesData, Formatting.None);
            templateJs = templateJs.Replace(VenueDataPlaceholder, jsValue);

            var tenantData = await GetTenantData(tenantId);
            if (tenantData == null)
                throw new Exception("Failed to get domains hash and org_code");
            templateJs = templateJs.Replace(DomHashPlaceholder, JsonConvert.SerializeObject(tenantData.web_pages));

            var org_code = tenantData.org_code;

            templateJs = templateJs.Replace(CustomerIdPlaceholder, $"\"{tenantId}\"");
            templateJs = templateJs.Replace(TimestampPlaceholder, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString());
            templateJs = templateJs.Replace(UISettingsPlaceholder, JsonConvert.SerializeObject(tenantData.library));

            templateJs = ProcessLibrary(templateJs);

            //add timestamp after to be able to compare ob

            await WriteJsFileToS3(org_code, templateJs);
        }

        private async Task WriteJsFile(string filePath, string jsContent)
        {

            await File.WriteAllTextAsync(filePath, jsContent);
        }

        private async Task WriteJsFileToS3(string orgCode, string jsContent)
        {

            var resp = await _s3Servce.UploadStreamAsync(S3Bucket, $"r/{orgCode}/{JSFilename}", new MemoryStream(Encoding.UTF8.GetBytes(jsContent)), "application/javascript");
        }

        private async Task<List<object>> GetVenueData(string tenantId)
        {
            var query = PredefinedQueryPatterns.GET_VENUE_DATA_REPLACEMENT_JS_QUERY.Replace(PredefinedQueryPatternsReplacements.GET_VENUE_DATA_REPLACEMENT_JS_TENANT, tenantId);
            var results = await _repository.ExecuteDoOperationsCommand(query);
            return results.rows;
        }

        private async Task<TenantData> GetTenantData(string tenantId)
        {
            var query = PredefinedQueryPatterns.GET_TENANT_REPLACEMENT_JS_QUERY.Replace(PredefinedQueryPatternsReplacements.GET_VENUE_DATA_REPLACEMENT_JS_TENANT, tenantId);
            var results = await _repository.ExecuteDoOperationsCommand(query);
            var tenantData = results.rows.Select(q => JsonConvert.DeserializeObject<TenantData>(q.ToString()));
            if (tenantData.Count() == 0)
                return null;
            var tenant = tenantData.FirstOrDefault();
            if (tenant == null)
                return null;
            var domainsHash = new List<string>();
            foreach (var domain in tenant.web_pages)
            {
                domainsHash.Add(CreateDomainHash(domain));
            }

            tenant.web_pages = domainsHash;

            return tenant;
        }

        public string GenerateHmac(string message, string secretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(string.IsNullOrEmpty(secretKey) ? SecretKey : secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private string ProcessLibrary(string jsContent)
        {
            // Perform the actual replacement
            return jsContent.Replace(ObfuscationPlaceholder, GetObfuscationValue(jsContent));
        }

        private string GetObfuscationValue(string jsContent)
        {
            var originalLength = Encoding.UTF8.GetByteCount(jsContent);
            var lengthAfterReplacement = originalLength; // Same length replacement
                                                         // Create temporary content with placeholder to calculate HMAC
            var tempContent = jsContent.Replace(ObfuscationPlaceholder, new string('0', HmacLength));
            tempContent = tempContent
                .Replace("\r", "")
                .Replace("\uFEFF", "")
                .Replace("\u200B", "")
                .Replace("\u200C", "")
                .Replace("\u200D", "")
                .Normalize(NormalizationForm.FormC);

            tempContent = CommonHelperFunctions.ReplaceWhitespace(tempContent, "");
            var byteLength = Encoding.UTF8.GetByteCount(tempContent);
            var hmac = GenerateHmac(tempContent, byteLength.ToString());
            tempContent = tempContent.TrimEnd();
            var bytes = Encoding.UTF8.GetBytes(tempContent);
            Console.WriteLine(Encoding.UTF8.EncodingName);
            var a = tempContent.TrimStart('\uFEFF');
            var bytes1 = Encoding.UTF8.GetBytes(a);
            return hmac;
        }

        public bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }

        public async Task<Tuple<List<string>, string>> GetVerifyData(string tenantId)
        {
            var org_code = await _tenantRepository.GetOrgCode(new Guid(tenantId));
            if (string.IsNullOrEmpty(org_code))
                throw new Exception("Invalid tenant id");
            var jsFile = "";
            try
            {
                jsFile = await _s3Servce.DownloadFileStringAsync(S3Bucket, $"r/{org_code}/{JSFilename}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error trying to get JS file for tenant => {org_code}, {ex.Message}", ex);
            }
            var templateJs = CommonHelperFunctions.ReplaceWhitespace(jsFile, "");
            var domains = new List<string>();
            var domainsHash = GetValueFromFile("dHS:[", "],oHS", templateJs);
            if (domainsHash.Contains(','))
                domains.AddRange(domainsHash.Split(",").Select(q => q.Trim('"')));
            else
                domains.Add(domainsHash.Trim('"'));

            var oHS = GetValueFromFile("oHS:", ",", templateJs).Trim('\'').Trim('"');

            //var oHS = GetObfuscationValue(templateJs, SecretKey);
            return new Tuple<List<string>, string>(domains, oHS);
        }

        private string GetValueFromFile(string start, string end, string input)
        {

            int startIndex = input.IndexOf(start) + start.Length;
            int endIndex = input.IndexOf(end, startIndex);

            return input.Substring(startIndex, endIndex - startIndex);
        }

        private string CreateDomainHash(string domain)
        {
            var domainSecret = Convert.ToBase64String(Encoding.UTF8.GetBytes(domain)).Reverse();
            return GenerateHmac(domain, new string(domainSecret.ToArray()));
        }

        public async Task<DateTime> GetExpiry(string tenantId)
        {
            return DateTime.UtcNow;
        }

        public class ValidationModel
        {
            public List<string?> dHs { get; set; }
            public string oHS { get; set; }
        }

        private class TenantData
        {
            public List<string> web_pages { get; set; }
            public string org_code { get; set; }
            public object library { get; set; } 
        }
    }
}
