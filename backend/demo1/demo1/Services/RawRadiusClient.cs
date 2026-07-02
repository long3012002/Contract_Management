using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace demo1.Services
{
    public class RawRadiusClient : IRadiusClient
    {
        private readonly string _server;
        private readonly int _port;
        private readonly string _sharedSecret;

        public RawRadiusClient(string server, int port, string sharedSecret)
        {
            _server = server;
            _port = port;
            _sharedSecret = sharedSecret;
        }

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {
                using var udpClient = new UdpClient();
                udpClient.Connect(_server, _port);

                byte code = 1; // Access-Request
                byte identifier = (byte)new Random().Next(1, 255);
                byte[] authenticator = RandomBytes(16);

                List<byte> packet = new();
                packet.Add(code); // 1 byte
                packet.Add(identifier); // 1 byte
                packet.Add(0); packet.Add(0); // placeholder length (2 byte)
                packet.AddRange(authenticator);

                // User-Name attribute (type 1)
                packet.Add(1); // Type
                var userBytes = Encoding.UTF8.GetBytes(username);
                packet.Add((byte)(userBytes.Length + 2));
                packet.AddRange(userBytes);

                // User-Password attribute (type 2)
                packet.Add(2);
                var encryptedPassword = EncryptPassword(password, authenticator, _sharedSecret);
                packet.Add((byte)(encryptedPassword.Length + 2));
                packet.AddRange(encryptedPassword);

                // Tính lại length
                ushort length = (ushort)packet.Count;
                packet[2] = (byte)(length >> 8);
                packet[3] = (byte)(length & 0xFF);

                // Gửi packet
                await udpClient.SendAsync(packet.ToArray(), packet.Count);

                // Timeout-safe receive (3 seconds timeout)
                var receiveTask = udpClient.ReceiveAsync();
                if (await Task.WhenAny(receiveTask, Task.Delay(3000)) == receiveTask)
                {
                    var result = await receiveTask;
                    if (result.Buffer.Length > 0)
                    {
                        var responseCode = result.Buffer[0];
                        return responseCode == 2; // Access-Accept
                    }
                }
            }
            catch (Exception)
            {
                // In a production application, you should log the exception.
            }
            return false;
        }

        private byte[] EncryptPassword(string password, byte[] requestAuthenticator, string secret)
        {
            var pwdBytes = Encoding.UTF8.GetBytes(password);
            if (pwdBytes.Length % 16 != 0)
            {
                Array.Resize(ref pwdBytes, ((pwdBytes.Length / 16) + 1) * 16); // pad with 0
            }

            var secretBytes = Encoding.UTF8.GetBytes(secret);
            List<byte> result = new();
            byte[] lastBlock = requestAuthenticator;

            using var md5 = MD5.Create();
            for (int i = 0; i < pwdBytes.Length; i += 16)
            {
                var b = pwdBytes.Skip(i).Take(16).ToArray();
                var hash = md5.ComputeHash(secretBytes.Concat(lastBlock).ToArray());
                var xor = b.Zip(hash, (x, y) => (byte)(x ^ y)).ToArray();
                result.AddRange(xor);
                lastBlock = xor;
            }

            return result.ToArray();
        }

        private static byte[] RandomBytes(int length)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }
    }
}
