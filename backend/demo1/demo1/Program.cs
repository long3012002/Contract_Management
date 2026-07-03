using Microsoft.EntityFrameworkCore;
using demo1.Data;
using demo1.Services;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Mapper;
using demo1.Middleware;
using demo1.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
}

var mysqlServerVersion = Version.Parse(builder.Configuration["Database:ServerVersion"] ?? "8.0.36");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(mysqlServerVersion)));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(item => item.Value?.Errors.Count > 0)
            .ToDictionary(
                item => item.Key,
                item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new ApiErrorResponse
        {
            Message = "Validation failed.",
            Errors = errors
        });
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "Chuoi_Bi_Mat_Sieu_Manh_Voi_Do_Dai_Toi_Thieu_32_Ky_Tu_!!!";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "ContractManagementBackend",
        ValidAudience = jwtSettings["Audience"] ?? "ContractManagementFrontend",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IBidPackageService, BidPackageService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IResolutionService, ResolutionService>();
builder.Services.AddScoped<IWarningService, WarningService>();
builder.Services.AddSingleton<RadiusClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var server = config["Radius:Server"] ?? "127.0.0.1";
    var port = int.TryParse(config["Radius:Port"], out var parsedPort) ? parsedPort : 1812;
    var sharedSecret = config["Radius:SharedSecret"] ?? string.Empty;
    var timeout = int.TryParse(config["Radius:Timeout"], out var parsedTimeout) ? parsedTimeout : 3000;

    return new RadiusClient(server, port, sharedSecret, timeout);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Configuration.GetValue<bool>("Database:AutoMigrate") ||
    app.Configuration.GetValue<bool>("Database:SeedSampleData"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
    {
        context.Database.Migrate();
    }

    if (app.Configuration.GetValue<bool>("Database:SeedSampleData"))
    {
        await DatabaseSeeder.SeedAsync(context);
    }
}

app.Run();
