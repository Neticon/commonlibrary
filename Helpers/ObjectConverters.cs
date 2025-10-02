using System.Reflection;

namespace CommonLibrary.Helpers
{
    public static class ObjectConverters
    {
        public static Dictionary<string, string> ToPropertyDictionary(this object obj, bool skipNulls = false)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return obj.GetType()
                      .GetProperties()
                      .Where(p => (!skipNulls || p.GetValue(obj) != null) && p.Name != "_table" && p.Name != "_schema")
                      .ToDictionary(
                          prop => prop.Name,
                          prop => GetPropertyValue(prop, obj),
                          StringComparer.OrdinalIgnoreCase // keys are case-insensitive
                      );
        }

        private static string GetPropertyValue(PropertyInfo prop, object obj)
        {
            var addQuotes = false;
            var propValue = prop.GetValue(obj, null);
            var value = propValue != null ? propValue.ToString() : "";
            if (prop.PropertyType == typeof(bool))
                value = value.ToLower();
            if (prop.PropertyType == typeof(DateTime))
            {
                var valueDate = (DateTime)prop.GetValue(obj, null);
                value = valueDate.ToString("yyyy-MM-dd");
            }
            var type = prop.PropertyType;
            if (Nullable.GetUnderlyingType(type) != null)
            {
                type = Nullable.GetUnderlyingType(type);
            }
            if (type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(char))
                addQuotes = true;
            return addQuotes ? $"\"{value}\"" : $"{value}";
        }
    }
}
