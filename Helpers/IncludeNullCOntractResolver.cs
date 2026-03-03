using CommonLibrary.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace CommonLibrary.Helpers
{
    public class IncludeNullContractResolver : DefaultContractResolver
    {
        private readonly HashSet<string> _includeNullProperties;

        public IncludeNullContractResolver(IEnumerable<string> includeNullProperties)
        {
            _includeNullProperties = new HashSet<string>(includeNullProperties, StringComparer.OrdinalIgnoreCase);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (_includeNullProperties.Contains(property.PropertyName))
            {
                property.NullValueHandling = NullValueHandling.Include;
            }
            else
            {
                property.NullValueHandling = NullValueHandling.Ignore;
            }
            var hasIgnoreAttr = member.GetCustomAttribute<IgnoreForSerializationAttribute>() != null;
            if (hasIgnoreAttr)
            {
                property.Ignored = true; // tell Json.NET to ignore it
            }

            return property;
        }
    }
}
