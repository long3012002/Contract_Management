using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using demo1;

namespace demo1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Call Program.ConfigureServices to configure app services
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            var startup = new Startup(builder.Configuration);
            startup.Configure(app, app.Environment);

            app.Run();
        }

        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var startup = new Startup(configuration);
            startup.ConfigureServices(services);
        }
    }
}
