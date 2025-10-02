using Newtonsoft.Json;
using System.Net;
namespace CommonLibrary.Helpers
{
    public class IPAddressConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(IPAddress);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            writer.WriteValue(((IPAddress)value)?.ToString());

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            IPAddress.Parse(reader.Value.ToString());
    }
}
