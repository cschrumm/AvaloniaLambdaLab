using System.Text.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization.Metadata;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApplication1;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IApiKeyValidationService, ApiKeyValidationService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("MyFixedWindowPolicy", fixedWindowOptions =>
    {
        fixedWindowOptions.PermitLimit = 60; // Allow 60 requests
        fixedWindowOptions.Window = TimeSpan.FromSeconds(3); // Within a 10-second window
        fixedWindowOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        fixedWindowOptions.QueueLimit = 4; // No queueing, reject immediately
    });
});







var app = builder.Build();
app.UseMiddleware<HeaderCheckMiddleware>();
app.MapControllers();
app.UseRateLimiter();

// determine if there is a debugger attached
if (System.Diagnostics.Debugger.IsAttached)
{
    var svr = app.Services.GetService<IApiKeyValidationService>();
    svr.AddKey("testkey");
    
    
}
app.Run();
