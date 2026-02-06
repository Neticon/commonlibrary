using System.Reflection;
using System.Text.Json;

namespace CommonLibrary.Helpers
{
    public static class ObjectExtensions
    {
        public static void SetPropertyValue(this object obj, string propertyName, object value)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanWrite)
                throw new ArgumentException($"Property '{propertyName}' not found or not writable on {obj.GetType().Name}.");

            // Convert value type if needed
            if (value != null && prop.PropertyType != value.GetType())
            {
                value = Convert.ChangeType(value, prop.PropertyType);
            }

            prop.SetValue(obj, value);
        }

        public static object GetPropertyValue(this object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanRead)
                throw new ArgumentException($"Property '{propertyName}' not found or not readable on {obj.GetType().Name}.");

            return prop.GetValue(obj);
        }

        public static string? FindPropertyValue(string json, string propertyName)
        {
            using var doc = JsonDocument.Parse(json);
            return FindPropertyValue(doc.RootElement, propertyName);
        }

        public static string? FindPropertyValue(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.NameEquals(propertyName))
                        return prop.Value.ToString();

                    var nested = FindPropertyValue(prop.Value, propertyName);
                    if (nested != null)
                        return nested;
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindPropertyValue(item, propertyName);
                    if (nested != null)
                        return nested;
                }
            }

            return null;
        }

        public static List<string> FindAllPropertyValues(string json, string propertyName)
        {
            using var doc = JsonDocument.Parse(json);
            var results = new List<string>();

            FindAllPropertyValues(doc.RootElement, propertyName, results);
            return results;
        }

        static void FindAllPropertyValues(
            JsonElement element,
            string propertyName,
            List<string> results)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    // case-insensitive compare
                    if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(prop.Value.ToString());
                    }

                    FindAllPropertyValues(prop.Value, propertyName, results);
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    FindAllPropertyValues(item, propertyName, results);
                }
            }
        }
    }
}