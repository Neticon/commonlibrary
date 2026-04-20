using Newtonsoft.Json;

namespace CommonLibrary.Models
{
    public class ServiceResponse
    {
        public object Result { get; set; }
        public int StatusCode { get; set; } = 200;
        public object ResultJson => JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(Result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
    }
}
