using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using demo1.DTOs.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace demo1.Services
{
    public class RadiusClient
    {
        private readonly RadiusSettings _settings;
        private readonly ILogger<RadiusClient> _logger;

        public RadiusClient(IOptions<RadiusSettings> settings, ILogger<RadiusClient> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public bool IsEnabled => _settings.Enabled;
        public bool IsConfigured => _settings.IsConfigured;
        public string Server => _settings.Server;
        public int Port => _settings.Port;
        public int Timeout => _settings.Timeout;

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            if (!_settings.Enabled)
            {
                _logger.LogWarning("[RADIUS] Authentication is disabled by configuration.");
                return false;
            }

            if (!_settings.IsConfigured)
            {
                _logger.LogError("[RADIUS] Configuration is incomplete. Server/SharedSecret/Port/Timeout must be configured.");
                return false;
            }

            try
            {
                using var udpClient = new UdpClient();
                udpClient.Connect(_settings.Server, _settings.Port);

                byte code = 1; // Access-Request
                byte identifier = (byte)RandomNumberGenerator.GetInt32(1, 255);
                byte[] authenticator = RandomBytes(16);

                List<byte> packet = new();
                packet.Add(code);
                packet.Add(identifier);
                packet.Add(0);
                packet.Add(0);
                packet.AddRange(authenticator);

                AddStringAttribute(packet, 1, username);

                packet.Add(2); // User-Password
                var encryptedPassword = EncryptPassword(password, authenticator, _settings.SharedSecret);
                packet.Add((byte)(encryptedPassword.Length + 2));
                packet.AddRange(encryptedPassword);

                ushort length = (ushort)packet.Count;
                packet[2] = (byte)(length >> 8);
                packet[3] = (byte)(length & 0xFF);

                var packetBytes = packet.ToArray();
                _logger.LogInformation(
                    "[RADIUS] Sending Access-Request to {Server}:{Port} for user '{Username}' (packet size: {PacketSize} bytes)",
                    _settings.Server,
                    _settings.Port,
                    username,
                    packetBytes.Length);

                await udpClient.SendAsync(packetBytes, packetBytes.Length);

                using var cts = new CancellationTokenSource(_settings.Timeout);
                UdpReceiveResult result;
                try
                {
                    result = await udpClient.ReceiveAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(
                        "[RADIUS] Timeout after {Timeout}ms without response from {Server}:{Port}",
                        _settings.Timeout,
                        _settings.Server,
                        _settings.Port);
                    throw new TimeoutException("Hết thời gian chờ phản hồi từ máy chủ Radius.");
                }

                if (result.Buffer.Length < 20)
                {
                    _logger.LogWarning("[RADIUS] Invalid response length: {Length} bytes", result.Buffer.Length);
                    return false;
                }

                var responseCode = result.Buffer[0];
                _logger.LogInformation(
                    "[RADIUS] Received response code {ResponseCode} ({ResponseName})",
                    responseCode,
                    responseCode == 2 ? "Access-Accept" : responseCode == 3 ? "Access-Reject" : "Unknown");

                return responseCode == 2;
            }
            catch (SocketException ex)
            {
                _logger.LogError(
                    ex,
                    "[RADIUS] Socket error while connecting to {Server}:{Port}. SocketErrorCode: {SocketErrorCode}",
                    _settings.Server,
                    _settings.Port,
                    ex.SocketErrorCode);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RADIUS] Unexpected authentication error.");
                throw;
            }
        }

        private static void AddStringAttribute(List<byte> packet, byte type, string value)
        {
            packet.Add(type);
            var bytes = Encoding.UTF8.GetBytes(value);
            packet.Add((byte)(bytes.Length + 2));
            packet.AddRange(bytes);
        }

        private static byte[] EncryptPassword(string password, byte[] requestAuthenticator, string secret)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            if (passwordBytes.Length % 16 != 0 || passwordBytes.Length == 0)
            {
                Array.Resize(ref passwordBytes, ((passwordBytes.Length / 16) + 1) * 16);
            }

            var secretBytes = Encoding.UTF8.GetBytes(secret);
            List<byte> result = new();
            byte[] lastBlock = requestAuthenticator;

            using var md5 = MD5.Create();
            for (int i = 0; i < passwordBytes.Length; i += 16)
            {
                var block = passwordBytes.Skip(i).Take(16).ToArray();
                var hash = md5.ComputeHash(secretBytes.Concat(lastBlock).ToArray());
                var encryptedBlock = block.Zip(hash, (x, y) => (byte)(x ^ y)).ToArray();
                result.AddRange(encryptedBlock);
                lastBlock = encryptedBlock;
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
