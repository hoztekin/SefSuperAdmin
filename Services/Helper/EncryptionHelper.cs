using System.Security.Cryptography;

namespace App.Services.Helper
{
    public static class EncryptionHelper
    {
        private const int SaltSize = 16; // Salt size (16 bytes)
        private const int KeySize = 32; // Key size (32 bytes = 256-bit)
        private const int Iterations = 10000; // Number of PBKDF2 iterations

        public static string Encrypt(string plainText, string password)
        {
            var salt = GenerateRandomSalt();
            using var aes = Aes.Create();

            aes.Key = DeriveKey(password, salt, KeySize);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            ms.Write(salt, 0, salt.Length); // First 16 bytes will be the salt
            ms.Write(aes.IV, 0, aes.IV.Length); // Next 16 bytes will be the IV

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string encryptedText, string password)
        {
            var fullCipher = Convert.FromBase64String(encryptedText);

            // Extract salt and IV
            var salt = new byte[SaltSize];
            Array.Copy(fullCipher, 0, salt, 0, SaltSize);

            var iv = new byte[16];
            Array.Copy(fullCipher, SaltSize, iv, 0, iv.Length);

            using var aes = Aes.Create();
            aes.Key = DeriveKey(password, salt, KeySize);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(fullCipher, SaltSize + iv.Length, fullCipher.Length - SaltSize - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }

        public static async Task EncryptToFileAsync(string plainText, string outputFile, string password)
        {
            var salt = GenerateRandomSalt();
            using var aes = Aes.Create();

            aes.Key = DeriveKey(password, salt, KeySize);
            aes.GenerateIV();

            await using var fs = new FileStream(outputFile, FileMode.Create);

            await fs.WriteAsync(salt, 0, salt.Length); // First 16 bytes will be the salt
            await fs.WriteAsync(aes.IV, 0, aes.IV.Length); // Next 16 bytes will be the IV

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            await using var cs = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);

            await sw.WriteAsync(plainText);
        }

        public static async Task<string> DecryptFromFileAsync(string inputFile, string password)
        {
            await using var fs = new FileStream(inputFile, FileMode.Open);

            var salt = new byte[SaltSize];
            await fs.ReadAsync(salt, 0, salt.Length); // Read the salt

            var iv = new byte[16];
            await fs.ReadAsync(iv, 0, iv.Length); // Read the IV

            using var aes = Aes.Create();
            aes.Key = DeriveKey(password, salt, KeySize);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            await using var cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return await sr.ReadToEndAsync();
        }

        private static byte[] GenerateRandomSalt()
        {
            var salt = new byte[SaltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }

        private static byte[] DeriveKey(string password, byte[] salt, int keySize)
        {
            using var rfc2898 = new Rfc2898DeriveBytes(password, salt, Iterations);
            return rfc2898.GetBytes(keySize);
        }
    }
}
