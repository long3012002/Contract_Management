using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace demo1.Services.Helpers
{
    /// <summary>
    /// Tiện ích mã hóa & giải mã mật khẩu nhạy cảm (như Password Email) bằng AES-256-CBC.
    /// Giúp che giấu mật khẩu trong file cấu hình, tránh bị đọc trực tiếp dưới dạng plain-text.
    /// </summary>
    public static class SecretProtector
    {
        // Khóa mã hóa nội bộ cố định (kết hợp chuỗi nhị phân nội bộ của app)
        private static readonly byte[] EncryptionKey = new byte[]
        {
            0x43, 0x6F, 0x6F, 0x70, 0x62, 0x61, 0x6E, 0x6B,
            0x5F, 0x51, 0x4C, 0x44, 0x41, 0x5F, 0x53, 0x65,
            0x63, 0x75, 0x72, 0x69, 0x74, 0x79, 0x5F, 0x32,
            0x30, 0x32, 0x36, 0x21, 0x40, 0x23, 0x24, 0x25
        }; // 32 bytes = 256 bits

        /// <summary>
        /// Mã hóa chuỗi văn bản thuần (plain text) thành chuỗi Base64 (bao gồm IV ngẫu nhiên).
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.GenerateIV(); // Tạo IV ngẫu nhiên 16 bytes

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            // Ghi IV vào đầu stream để dùng khi giải mã
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Giải mã chuỗi Base64 đã mã hóa bằng Encrypt về lại chuỗi văn bản thuần.
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);
                if (fullCipher.Length < 16)
                    return string.Empty;

                using var aes = Aes.Create();
                aes.Key = EncryptionKey;

                // Tách IV 16 bytes đầu tiên
                var iv = new byte[16];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                aes.IV = iv;

                // Đọc phần ciphertext còn lại
                using var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16);
                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);

                return sr.ReadToEnd();
            }
            catch
            {
                // Nếu không giải mã được (ví dụ mật khẩu cũ chưa mã hóa), trả về chuỗi rỗng
                return string.Empty;
            }
        }
    }
}
