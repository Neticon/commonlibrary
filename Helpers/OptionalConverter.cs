using Newtonsoft.Json;

namespace CommonLibrary.Helpers
{
    public class OptionalConverter<T> : JsonConverter<Optional<T>>
    {
        public override Optional<T> ReadJson(
            JsonReader reader,
            Type objectType,
            Optional<T> existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            // If property exists, this method is called.
            // Even if value is null.

            if (reader.TokenType == JsonToken.Null)
            {
                return new Optional<T>(default);
            }

            var value = serializer.Deserialize<T>(reader);
            return new Optional<T>(value);
        }

        public override void WriteJson(
            JsonWriter writer,
            Optional<T> value,
            JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Value);
        }
    }
}
