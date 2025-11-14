using CommonLibrary.Domain;
using System.Reflection;

namespace CommonLibrary.Helpers
{
    public static class EncryptionMetadataHelper
    {
        public static List<string> GetEncryptedPropertyPaths(Type type, string parentPath = "")
        {
            var result = new List<string>();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var path = string.IsNullOrEmpty(parentPath) ? prop.Name : $"{parentPath}.{prop.Name}";

                if (prop.GetCustomAttribute<EncryptAttribute>() != null)
                {
                    result.Add(path);
                }

                if (!prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string) && prop.PropertyType != typeof(DateTime) && prop.PropertyType != typeof(Guid) && prop.PropertyType != typeof(TimeSpan) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    // Recurse into nested classes
                    result.AddRange(GetEncryptedPropertyPaths(prop.PropertyType, path));
                }
            }

            return result;
        }
    }
}
