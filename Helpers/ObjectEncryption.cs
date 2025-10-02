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
        ProcessObject(obj, key, fieldsToEncrypt, encrypt: true);
    }

    public static void DecryptObject(object obj, string key, List<string> fieldsToEncrypt)
    {
        if (obj.GetType() == typeof(JObject))
            ProcessJObject(obj as JObject, key, fieldsToEncrypt, encrypt: false);
        else
            ProcessObject(obj, key, fieldsToEncrypt, encrypt: false);
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
                property.Value = encrypt ?
                    AesEncryption.Encrypt(strVal, key) : AesEncryption.Decrypt(strVal, key);
            }
            else if (value.Type == JTokenType.Object)
            {
                ProcessObject((JObject)value, key, fieldsToEncrypt, encrypt);
            }
            else if (value.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)value)
                {
                    if (item.Type == JTokenType.Object)
                    {
                        ProcessObject((JObject)item, key, fieldsToEncrypt, encrypt);
                    }
                }
            }
        }
    }

    private static void ProcessObject(object obj, string key, List<string> fieldsToEncrypt, bool encrypt)
    {
        if (obj == null) return;

        var type = obj.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;

            var value = prop.GetValue(obj);

            // Handle string properties with [Encrypted]
            if (fieldsToEncrypt.Contains(prop.Name, StringComparer.OrdinalIgnoreCase) && value is string strVal)
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
}

