using Newtonsoft.Json;

namespace CommonLibrary.Helpers
{
    public static class JsonDefaults
    {
        static JsonDefaults()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new AttributeIgnoreContractResolver(),
                Formatting = Formatting.Indented
            };
        }

        public static void EnsureInitialized() { /* triggers static ctor */ }
    }
}
