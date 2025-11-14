using System.Reflection;

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
    }
}