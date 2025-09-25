using System.Text.Json;
using System.Runtime.InteropServices;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApplication1;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddScoped<IApiKeyValidationService, ApiKeyValidationService>();
builder.Services.AddControllers();

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
app.MapControllers();
app.UseRateLimiter();
app.Run();
