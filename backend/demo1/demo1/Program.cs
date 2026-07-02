using demo1.Services;
using demo1.Services.Implements;
using demo1.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton<IProjectService, ProjectService>();
builder.Services.AddSingleton<IPartnerService, PartnerService>();
builder.Services.AddSingleton<IBidPackageService, BidPackageService>();
builder.Services.AddSingleton<IContractService, ContractService>();
builder.Services.AddSingleton<IResolutionService, ResolutionService>();
builder.Services.AddSingleton<IWarningService, WarningService>();

builder.Services.AddScoped<IRadiusClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var server = config["Radius:Server"] ?? "127.0.0.1";
    var port = int.TryParse(config["Radius:Port"], out var parsedPort) ? parsedPort : 1812;
    var sharedSecret = config["Radius:SharedSecret"] ?? string.Empty;

    return new RawRadiusClient(server, port, sharedSecret);
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

app.Run();
