using System.Threading.Tasks;

namespace demo1.Services
{
    public interface IRadiusClient
    {
        Task<bool> AuthenticateAsync(string username, string password);
    }
}
