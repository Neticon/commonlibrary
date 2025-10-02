using CommonLibrary.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

public class AttributeIgnoreContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        // check if property has our custom attribute
        var hasIgnoreAttr = member.GetCustomAttribute<IgnoreForSerializationAttribute>() != null;
        if (hasIgnoreAttr)
        {
            property.Ignored = true; // tell Json.NET to ignore it
        }

        return property;
    }
}
