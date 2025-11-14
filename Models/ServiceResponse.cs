using Newtonsoft.Json;

namespace CommonLibrary.Models
{
    public class ServiceResponse
    {
        public object Result { get; set; }
        public int StatusCode { get; set; } = 200;
        public string ResultJson => JsonConvert.SerializeObject(Result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
    }
}
