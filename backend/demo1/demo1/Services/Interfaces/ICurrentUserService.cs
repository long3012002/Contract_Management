namespace demo1.Services.Interfaces
{
    public interface ICurrentUserService
    {
        string? GetUsername();
        string? GetIpAddress();
    }
}
