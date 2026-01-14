using CommonLibrary.Domain;
using CommonLibrary.Helpers;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Reflection;

public static class ObjectEncryption
{
    public static void EncryptObject(object obj, string key)
    {
        ProcessObject(obj, key, encrypt: true);
    }

    public static void DecryptObject(object obj, string key)
    {
        ProcessObject(obj, key, encrypt: false);
    }

    public static void EncryptObject(object obj, string key, List<string> fieldsToEncrypt)
    {
        if (obj.GetType() == typeof(JObject))
            ProcessJObject(obj as JObject, key, fieldsToEncrypt, encrypt: true);
        else
            ProcessObject(obj, key, fieldsToEncrypt, encrypt: true);
    }

    public static void DecryptObject(object obj, string key, List<string> fieldsToEncrypt)
    {
        if (obj.GetType() == typeof(JObject))
            ProcessJObject(obj as JObject, key, fieldsToEncrypt, encrypt: false);
        else
            ProcessObject(obj, key, fieldsToEncrypt, encrypt: false);
    }

    public static void DecryptObjectPaths(JObject jObject, string key, List<string> paths)
        => Process(jObject, paths, key, encrypt: false);

    private static void Process(JObject root, List<string> paths, string key, bool encrypt)
    {
        foreach (var path in paths)
        {
            ApplyToPath(root, path.Split('.'), 0, key, encrypt);
        }
    }

    private static void ApplyToPath(JToken token, string[] parts, int index, string key, bool encrypt)
    {
        if (index >= parts.Length) return;

        string current = parts[index];
        bool isArrayWildcard = current.EndsWith("[]");
        string propName = isArrayWildcard ? current.Replace("[]", "") : current;

        if (token is JObject obj)
        {
            var prop = obj.Property(propName);
            if (prop == null) return;

            JToken next = prop.Value;

            if (index == parts.Length - 1)
            {
                // LAST PART → Encrypt/Decrypt string value
                if (next.Type == JTokenType.String)
                {
                    string value = next.ToString();
                    try
                    {
                        prop.Value = encrypt ? AesEncryption.Encrypt(value, key) : AesEncryption.Decrypt(value, key);
                    }
                    catch
                    {
                        Console.WriteLine($"Error trying to {(encrypt ? "encrypt" : "decrypt")} value => {value}");
                    }
                }
                return;
            }

            // Traverse deeper
            if (next.Type == JTokenType.Object)
            {
                ApplyToPath(next, parts, index + 1, key, encrypt);
            }
            else if (next.Type == JTokenType.Array && isArrayWildcard)
            {
                foreach (var item in (JArray)next)
                {
                    if (item.Type == JTokenType.Object)
                        ApplyToPath(item, parts, index + 1, key, encrypt);
                }
            }
        }
        else if (token is JArray arr)
        {
            foreach (var item in arr)
            {
                ApplyToPath(item, parts, index, key, encrypt);
            }
        }
    }

    private static void ProcessObject(object obj, string key, bool encrypt)
    {
        if (obj == null) return;

        var type = obj.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;

            var value = prop.GetValue(obj);

            // Handle string properties with [Encrypted]
            if (prop.GetCustomAttribute<EncryptAttribute>() != null && value is string strVal)
            {
                prop.SetValue(obj, encrypt ? AesEncryption.Encrypt(strVal, key) : AesEncryption.Decrypt(strVal, key));
            }
            // Handle nested objects
            else if (value != null && !prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
            {
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    foreach (var item in (IEnumerable)value)
                    {
                        ProcessObject(item, key, encrypt);
                    }
                }
                else
                {
                    ProcessObject(value, key, encrypt);
                }
            }
        }
    }

    private static void ProcessJObject(JObject jObject, string key, List<string> fieldsToEncrypt, bool encrypt)
    {
        foreach (var property in jObject.Properties())
        {
            var value = property.Value;

            // If it's a property that should be encrypted/decrypted
            if (fieldsToEncrypt.Contains(property.Name, StringComparer.OrdinalIgnoreCase) &&
                value.Type == JTokenType.String)
            {
                var strVal = value.ToString();
                try
                {
                    property.Value = encrypt ? AesEncryption.Encrypt(strVal, key) : AesEncryption.Decrypt(strVal, key);
                }
                catch
                {
                    Console.WriteLine($"Error trying to {(encrypt ? "encrypt" : "decrypt")} value => {value}");
                }
            }
            else if (value.Type == JTokenType.Object)
            {
                ProcessJObject((JObject)value, key, fieldsToEncrypt, encrypt);
            }
            else if (value.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)value)
                {
                    if (item.Type == JTokenType.Object)
                    {
                        ProcessJObject((JObject)item, key, fieldsToEncrypt, encrypt);
                    }
                }
            }
        }
    }

    private static void ProcessObject(object obj, string key, List<string> fieldsToEncrypt, bool encrypt, string parentPath = "")
    {
        if (obj == null) return;

        var type = obj.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            //skipArrays, if it is nececearly add implementation for that as well, but for now only string field should support encryption
            if (type.IsGenericType) // && type.GetGenericArguments()[0] != typeof(string))
                continue;

            var value = prop.GetValue(obj);
            var currentPath = string.IsNullOrEmpty(parentPath)
            ? prop.Name
            : $"{parentPath}.{prop.Name}";

            // Handle string properties with [Encrypted]
            if (fieldsToEncrypt.Contains(currentPath, StringComparer.OrdinalIgnoreCase) && value is string strVal)
            {
                prop.SetValue(obj, encrypt ? AesEncryption.Encrypt(strVal, key) : AesEncryption.Decrypt(strVal, key));
            }
            // Handle nested objects
            else if (value != null && !prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
            {
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    foreach (var item in (IEnumerable)value)
                    {
                        ProcessObject(item, key, fieldsToEncrypt, encrypt, currentPath);
                    }
                }
                else
                {
                    ProcessObject(value, key, fieldsToEncrypt, encrypt, currentPath);
                }
            }
        }
    }
}

