using Newtonsoft.Json;

namespace CommonLibrary.Helpers
{
    public class MaxMindNetworkConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
           objectType == typeof(MaxMind.Db.Network);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            writer.WriteValue(((MaxMind.Db.Network)value)?.ToString());

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            reader.Value.ToString();
    }
}
