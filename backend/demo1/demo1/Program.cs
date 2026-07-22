using demo1.Data;
using demo1.Logging;
using demo1.Middleware;
using demo1.Services;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFile(Path.Combine(builder.Environment.ContentRootPath, "debug.log"));

// Configure Services using ConfigureServices extension method
builder.Services.ConfigureServices(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("DefaultCors");
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard();
app.MapControllers();
app.MapHub<demo1.Hubs.NotificationHub>("/hub/notifications");

// Initialize & Seed Fake Data if empty
await app.CreateFakeDataAsync();

app.Run();
