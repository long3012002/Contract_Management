using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace demo1.Services
{
    public class RadiusClient
    {
        private readonly string _server;
        private readonly int _port;
        private readonly string _sharedSecret;
        private readonly int _timeoutMs;

        public RadiusClient(string server, int port, string sharedSecret, int timeoutMs = 3000)
        {
            _server = server;
            _port = port;
            _sharedSecret = sharedSecret;
            _timeoutMs = timeoutMs;
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
                var packetBytes = packet.ToArray();
                Console.WriteLine($"[RADIUS] Gửi Access-Request đến {_server}:{_port} cho user '{username}' (packet size: {packetBytes.Length} bytes)");
                await udpClient.SendAsync(packetBytes, packetBytes.Length);

                // Nhận phản hồi kèm timeout
                using var cts = new CancellationTokenSource(_timeoutMs);
                UdpReceiveResult result;
                try
                {
                    result = await udpClient.ReceiveAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[RADIUS] Timeout sau {_timeoutMs}ms - không nhận được phản hồi từ server");
                    return false;
                }

                // Kiểm tra độ dài tối thiểu của RADIUS response (20 bytes header)
                if (result.Buffer.Length < 20)
                {
                    Console.WriteLine($"[RADIUS] Phản hồi không hợp lệ: chỉ nhận được {result.Buffer.Length} bytes (cần tối thiểu 20)");
                    return false;
                }

                var responseCode = result.Buffer[0];
                Console.WriteLine($"[RADIUS] Nhận phản hồi: code={responseCode} ({(responseCode == 2 ? "Access-Accept" : responseCode == 3 ? "Access-Reject" : "Unknown")})");

                return responseCode == 2; // Access-Accept
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[RADIUS] Lỗi kết nối: {ex.Message} (SocketErrorCode: {ex.SocketErrorCode})");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RADIUS] Lỗi không xác định: {ex.Message}");
                return false;
            }
        }

        private byte[] EncryptPassword(string password, byte[] requestAuthenticator, string secret)
        {
            var pwdBytes = Encoding.UTF8.GetBytes(password);
            // RFC 2865: password phải được pad đủ bội số 16 bytes (kể cả khi rỗng)
            if (pwdBytes.Length % 16 != 0 || pwdBytes.Length == 0)
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
