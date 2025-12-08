using CommonLibrary.Helpers;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Models;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.Repository.Redis;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using VenueGenerationService;

namespace CommonLibrary.Integrations
{
    public class ValidationService : IValidationService
    {
        private readonly BytePlanConfiguration _configuration;
        private readonly IDeviceIntelRepository _deviceIntelRepository;
        private readonly IVenueGenerationService _venueGenerationService;
        private readonly IRedisService _redisService;
        private readonly List<int> valid_mail_statuses = new List<int> { 200, 207, 215 };
        private readonly List<string> valid_phone_statuses = new List<string> { "VALID_CONFIRMED", "VALID_UNCONFIRMED", "DELAYED" };
        private readonly string REDIS_PREFIX = "di::";

        public ValidationService(IOptions<BytePlanConfiguration> configuration, IDeviceIntelRepository deviceIntelRepository, IVenueGenerationService venueGenerationService, IRedisService redisService)
        {
            _configuration = configuration.Value;
            _deviceIntelRepository = deviceIntelRepository;
            _venueGenerationService = venueGenerationService;
            _redisService = redisService;
            _configuration.PhoneKey = Environment.GetEnvironmentVariable("BYTEPLAN_PHONE_KEY");
        }

        public async Task<ValueTuple<int, string>> ValidateRequest(ValidateRequest data, bool apiCall = false)
        {
            var email = data.e.ToLower();
            var phone = data.p.ToLower();
            var emailValid = true;
            var phoneValid = true;
            var origin = apiCall ? 0 : 1;
            if (!string.IsNullOrEmpty(data.e) && !string.IsNullOrEmpty(data.p))
            {
                var hashedMail = _venueGenerationService.GenerateHmac(email, "");
                var hashedPhone = _venueGenerationService.GenerateHmac(phone, "");
                var redisKeys = new List<string> { GetRedisKey(hashedMail), GetRedisKey(hashedPhone) };
                var results = await _redisService.MGet(redisKeys);
                var resultEmail = results[0];
                var resultPhone = results[1];
                if (resultEmail == "0")
                    emailValid = false;
                else if (resultEmail == "")
                    emailValid = await ValidateEmail(email, hashedMail, origin);

                if (resultPhone == "0")
                    phoneValid = false;
                else if (resultPhone == "")
                    phoneValid = await ValidatePhone(phone, hashedPhone, origin);
            }
            else if (!string.IsNullOrEmpty(data.e))
            {
                var hashedMail = _venueGenerationService.GenerateHmac(email, "");
                var redisValue = await _redisService.GetString(GetRedisKey(hashedMail));
                if (redisValue == "0")
                    emailValid = false;
                else if (redisValue == null)
                    emailValid = await ValidateEmail(email, hashedMail, origin);
            }
            else if (!string.IsNullOrEmpty(data.p))
            {
                var hashedPhone = _venueGenerationService.GenerateHmac(phone, "");
                var redisValue = await _redisService.GetString(GetRedisKey(hashedPhone));
                if (redisValue == "0")
                    phoneValid = false;
                else if (redisValue == null)
                    phoneValid = await ValidatePhone(phone, hashedPhone, origin);
            }
            if (emailValid && phoneValid)
            {
                return (200, "");
            }
            else if (phoneValid && !emailValid)
            {
                return (202, "{\"e\": false}");
            }
            else if (emailValid && !phoneValid)
            {
                return (202, "{\"p\": false}");
            }
            else
            {
                return (204, "");
            }
        }

        public async Task<bool> ValidateEmail(string email, string hashedEmail, int origin)
        {
            var runApiCheck = false;
            var deviceIntel = await _deviceIntelRepository.GetDeviceIntel(hashedEmail);
            if (deviceIntel == null)
                runApiCheck = true;
            else if (deviceIntel.create_dt.AddDays(180) < DateTime.UtcNow)
                runApiCheck = true;

            if (runApiCheck) //Record does not exist or expired
            {
                var resultObject = await EmailApiValidation(email);
                var valid = valid_mail_statuses.Contains(resultObject.status);
                if (deviceIntel == null)
                {
                    var id = Guid.NewGuid();
                    SaveDeviceIntelRecord(id, id.ToString(), "EVS", origin, JsonConvert.SerializeObject(resultObject), hashedEmail, valid);
                    return valid;
                }
                else
                {
                    _deviceIntelRepository.UpdateDeviceIntel(deviceIntel.id, deviceIntel.type, JsonConvert.SerializeObject(resultObject));
                    _redisService.SetString(GetRedisKey(hashedEmail), valid ? deviceIntel.id.ToString() : "0", TimeSpan.FromDays(180));
                }
                return valid;
            }
            else
            {
                var resultObject = JsonConvert.DeserializeObject<EmailValidateResult>(deviceIntel.intel.ToString());
                var valid = valid_mail_statuses.Contains(resultObject.status);
                _redisService.SetString(GetRedisKey(hashedEmail), valid ? deviceIntel.id.ToString() : "0", TimeSpan.FromDays(180));
                return valid;
            }
        }

        public async Task<bool> ValidatePhone(string phone, string hashedPhone, int origin)
        {
            var runApiCheck = false;
            var deviceIntel = await _deviceIntelRepository.GetDeviceIntel(hashedPhone);
            if (deviceIntel == null)
                runApiCheck = true;
            else if (deviceIntel.create_dt.AddDays(180) < DateTime.UtcNow)
                runApiCheck = true;
            if (runApiCheck) //Record does not exist or expired
            {
                var resultObject = await PhoneApiValidation(phone);
                var valid = valid_phone_statuses.Contains(resultObject.status);
                if (deviceIntel == null)
                {
                    var id = Guid.NewGuid();
                    var redisData = new PhoneValidationRedisModel { DeviceIntelId = id.ToString(), LocalPhone = resultObject.formatnational };
                    SaveDeviceIntelRecord(id, JsonConvert.SerializeObject(redisData), "PNVS", origin, JsonConvert.SerializeObject(resultObject), hashedPhone, valid);
                    return valid;
                }
                else
                {
                    var redisData = new PhoneValidationRedisModel { DeviceIntelId = deviceIntel.id.ToString(), LocalPhone = resultObject.formatnational };
                    _deviceIntelRepository.UpdateDeviceIntel(deviceIntel.id, deviceIntel.type, JsonConvert.SerializeObject(resultObject));
                    _redisService.SetString(GetRedisKey(hashedPhone), valid ? JsonConvert.SerializeObject(redisData) : "0", TimeSpan.FromDays(180));
                }
                return valid;
            }
            else
            {
                var a = deviceIntel.intel.ToString();
                var intel = JsonConvert.DeserializeObject<PhoneValidateResult>(a);
                var redisData = new PhoneValidationRedisModel { DeviceIntelId = deviceIntel.id.ToString(), LocalPhone = intel.formatnational };
                var resultObject = JsonConvert.DeserializeObject<PhoneValidateResult>(deviceIntel.intel.ToString());
                var valid = valid_phone_statuses.Contains(resultObject.status);
                _redisService.SetString(GetRedisKey(hashedPhone), valid ? JsonConvert.SerializeObject(redisData) : "0", TimeSpan.FromDays(180));
                return valid;
            }
        }

        public async Task<PhoneValidateResult> PhoneApiValidation(string phoneNumber)
        {
            using (var client = new HttpClient())
            {
                var postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("PhoneNumber", phoneNumber));
                postData.Add(new KeyValuePair<string, string>("APIKey", _configuration.PhoneKey));

                HttpContent content = new FormUrlEncodedContent(postData);

                HttpResponseMessage result = await client.PostAsync(_configuration.PhoneApi, content);
                string resultContent = await result.Content.ReadAsStringAsync();
                var resultObject = JsonConvert.DeserializeObject<PhoneValidateResult>(resultContent);
                return resultObject;
                // await SaveDeviceIntelRecord(Guid.NewGuid(), "PNVS", origin, JsonConvert.SerializeObject(resultObject), hashedPhone, valid);
                //return valid;
            }
        }

        public async Task<RedisDeviceIntel> GetRedisDeviceIntel(string email, string phone, string ip)
        {
            Console.WriteLine("VALUES=>" + email + "," + phone + "," + ip);
            var keys = new List<string>();
            if (!string.IsNullOrEmpty(email))
                keys.Add(GetRedisKey(_venueGenerationService.GenerateHmac(email, "")));
            if (!string.IsNullOrEmpty(phone))
                keys.Add(GetRedisKey(_venueGenerationService.GenerateHmac(phone, "")));
            if (!string.IsNullOrEmpty(ip))
                keys.Add(GetRedisKey(_venueGenerationService.GenerateHmac(ip, "")));
            var redisResult = await _redisService.MGet(keys);
            //some key not found - not valid
            foreach (var result in redisResult)
            {
                if (string.IsNullOrEmpty(result))
                    return null;
            }
            var phoneValidation = new PhoneValidationRedisModel();
            if (!string.IsNullOrEmpty(phone))
                phoneValidation = JsonConvert.DeserializeObject<PhoneValidationRedisModel>(redisResult[1]);
            return new RedisDeviceIntel
            {
                EmailValidation = redisResult[0],
                PhoneValidation = phoneValidation.DeviceIntelId,
                LocalPhone = phoneValidation.LocalPhone,
                IPValidation = redisResult[2].Split(',')[0]
            };
        }

        private async Task SaveDeviceIntelRecord(Guid id, string redisBody, string type, int origin, string apiJsonResponse, string hash, bool valid)
        {
            if (string.IsNullOrEmpty(redisBody))
                redisBody = id.ToString();
            await _deviceIntelRepository.CreateDeviceIntel(id, type, origin, apiJsonResponse, hash);
            await _redisService.SetString(GetRedisKey(hash), valid ? redisBody : "0", TimeSpan.FromDays(180));
        }

        private string GetRedisKey(string hashedValue)
        {
            return $"{REDIS_PREFIX}{hashedValue}";
        }

        private async Task<EmailValidateResult> EmailApiValidation(string email)
        {
            using (var client = new HttpClient())
            {
                var postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("EmailAddress", email));
                postData.Add(new KeyValuePair<string, string>("APIKey", _configuration.EmailKey));

                HttpContent content = new FormUrlEncodedContent(postData);

                HttpResponseMessage result = await client.PostAsync(_configuration.EmailApi, content);
                string resultContent = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<EmailValidateResult>(resultContent);
            }
        }

        public async Task<GeoIPResponse> ValidateIp(string ip, int origin)
        {
            //localhost
            if (ip == "::1")
                return null;
            var hashedIp = _venueGenerationService.GenerateHmac(ip, "");
            var redisValue = await _redisService.GetString(GetRedisKey(hashedIp));
            if (redisValue != null)
            {
                return new GeoIPResponse { country = new Country { isoCode = redisValue.Split(',')[1] } };
            }
            if (redisValue == null)
            {
                var deviceIntel = await _deviceIntelRepository.GetDeviceIntel(hashedIp);
                if (deviceIntel != null)
                {
                    var intel = JsonConvert.DeserializeObject<GeoIPResponse>(deviceIntel.intel.ToString());
                    _redisService.SetString(GetRedisKey(hashedIp), $"{deviceIntel.id},{intel.country.isoCode}");
                    return intel;
                }

            }
            using (var reader = new DatabaseReader("../external_path/GeoIP2-City.mmdb"))
            {
                var response = reader.City(ip);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new IPAddressConverter());
                settings.Converters.Add(new MaxMindNetworkConverter());

                var dbResponseFromat = JsonConvert.DeserializeObject<GeoIPResponse>(JsonConvert.SerializeObject(response, settings));
                var id = Guid.NewGuid();
                SaveDeviceIntelRecord(id, $"{id},{dbResponseFromat.country.isoCode}", "IP", origin, JsonConvert.SerializeObject(dbResponseFromat), hashedIp, true);
                return dbResponseFromat;
            }
        }

        public async Task<GeoIPResponse> GetIpData(string ip)
        {
            using (var reader = new DatabaseReader("../external_path/GeoIP2-City.mmdb"))
            {
                var response = reader.City(ip);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new IPAddressConverter());
                settings.Converters.Add(new MaxMindNetworkConverter());

                var dbResponseFromat = JsonConvert.DeserializeObject<GeoIPResponse>(JsonConvert.SerializeObject(response, settings));
                return dbResponseFromat;
            }
        }

        //private asyTask<List<string>> GetRedisResults(string email, string phone)
        //{
        //    if(!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(phone))
        //    {
        //        var hashedMail = _venueGenerationService.GenerateHmac(email, "");
        //        var hashedPhone = _venueGenerationService.GenerateHmac(phone, "");
        //        var redisKeys = new List<string> { GetRedisKey(hashedMail), GetRedisKey(hashedPhone) };
        //        return await _redisService.MGet(redisKeys);
        //    }
        //}
    }

}
