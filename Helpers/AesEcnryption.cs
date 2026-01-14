using System.Security.Cryptography;
using System.Text;

namespace CommonLibrary.Helpers
{
    public static class AesEncryption
    {
        // Convert string key -> fixed 32 bytes (AES-256)
        private static byte[] GetKeyBytes(string key)
        {
            // Hash the key string into 256 bits using SHA256 (always 32 bytes)
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        }

        // Encrypt a string -> Base64(IV + Ciphertext)
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            byte[] keyBytes = GetKeyBytes(key);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV(); // fresh IV for every encryption

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            // Write IV first
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        // Decrypt Base64(IV + Ciphertext) -> string
        public static string Decrypt(string cipherTextBase64, string key)
        {
            if (string.IsNullOrEmpty(cipherTextBase64))
                throw new ArgumentNullException(nameof(cipherTextBase64));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherTextBase64);
                byte[] keyBytes = GetKeyBytes(key);

                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV (first 16 bytes)
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                aes.IV = iv;

                // Extract ciphertext (rest)
                byte[] cipherBytes = new byte[fullCipher.Length - iv.Length];
                Array.Copy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(cipherBytes);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }catch (Exception ex)
            {
                Console.WriteLine("Error trying to encrypt=>" +ex.ToString());
                return cipherTextBase64;
            }
        }

        public static string GenerateSecureString(int length = 32)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+";
            byte[] data = RandomNumberGenerator.GetBytes(length);

            var sb = new StringBuilder(length);
            foreach (var b in data)
                sb.Append(chars[b % chars.Length]);

            return sb.ToString();
        }
    }
}
