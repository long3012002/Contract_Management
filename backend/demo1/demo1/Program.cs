using Microsoft.EntityFrameworkCore;
using demo1.Data;
using demo1.Services;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Mapper;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
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

app.UseHttpsRedirection();
app.UseCors("DefaultCors");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
}

app.Run();
