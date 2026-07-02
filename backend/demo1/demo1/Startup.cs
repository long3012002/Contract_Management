using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using demo1.Data;
using demo1.Data.Repositories;
using demo1.Services;

namespace demo1
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            services.AddAutoMapper(typeof(Mappings.MappingProfile));

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IBaseService<,,,>), typeof(BaseService<,,,>));

            services.AddScoped<IRadiusClient>(sp => {
                var config = sp.GetRequiredService<IConfiguration>();
                var server = config["Radius:Server"] ?? "127.0.0.1";
                var port = int.Parse(config["Radius:Port"] ?? "1812");
                var sharedSecret = config["Radius:SharedSecret"] ?? "";
                return new RawRadiusClient(server, port, sharedSecret);
            });

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }

        public void Configure(WebApplication app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();
        }
    }
}
