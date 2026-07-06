using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace demo1.Services
{
    public class TotpService
    {
        private static readonly string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public string GenerateSecret()
        {
            byte[] buffer = new byte[10];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
            return Base32Encode(buffer);
        }

        public string GetQrCodeUrl(string username, string secret)
        {
            string label = Uri.EscapeDataString($"ContractManagement:{username}@co-opbank.vn");
            string issuer = Uri.EscapeDataString("ContractManagement");
            return $"otpauth://totp/{label}?secret={secret}&issuer={issuer}";
        }

        public bool VerifyCode(string secret, string code, int timeWindowRange = 1)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code)) return false;
            
            byte[] key;
            try
            {
                key = Base32Decode(secret);
            }
            catch
            {
                return false;
            }

            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long currentStep = unixTime / 30;

            for (int i = -timeWindowRange; i <= timeWindowRange; i++)
            {
                long step = currentStep + i;
                byte[] stepBytes = BitConverter.GetBytes(step);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(stepBytes);
                }

                using (var hmac = new HMACSHA1(key))
                {
                    byte[] hash = hmac.ComputeHash(stepBytes);
                    int offset = hash[hash.Length - 1] & 0xf;
                    int binary = ((hash[offset] & 0x7f) << 24)
                               | ((hash[offset + 1] & 0xff) << 16)
                               | ((hash[offset + 2] & 0xff) << 8)
                               | (hash[offset + 3] & 0xff);
                    int otp = binary % 1000000;
                    string expectedCode = otp.ToString("D6");
                    Console.WriteLine($"[TOTP DEBUG] Step offset {i}: Expected OTP from Server = {expectedCode}");

                    if (expectedCode == code.Trim())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private string Base32Encode(byte[] data)
        {
            StringBuilder result = new StringBuilder((data.Length + 7) * 8 / 5);
            int offset = 0;
            int buffer = 0;
            int bitsLeft = 0;

            while (offset < data.Length)
            {
                buffer = (buffer << 8) | data[offset++];
                bitsLeft += 8;
                while (bitsLeft >= 5)
                {
                    int index = (buffer >> (bitsLeft - 5)) & 0x1f;
                    result.Append(Base32Chars[index]);
                    bitsLeft -= 5;
                }
            }

            if (bitsLeft > 0)
            {
                int index = (buffer << (5 - bitsLeft)) & 0x1f;
                result.Append(Base32Chars[index]);
            }

            return result.ToString();
        }

        private byte[] Base32Decode(string input)
        {
            input = input.Trim().ToUpperInvariant();
            List<byte> bytes = new List<byte>();
            int buffer = 0;
            int bitsLeft = 0;

            foreach (char c in input)
            {
                int val = Base32Chars.IndexOf(c);
                if (val < 0) continue;
                buffer = (buffer << 5) | val;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bytes.Add((byte)(buffer >> (bitsLeft - 8)));
                    bitsLeft -= 8;
                }
            }

            return bytes.ToArray();
        }
    }
}
