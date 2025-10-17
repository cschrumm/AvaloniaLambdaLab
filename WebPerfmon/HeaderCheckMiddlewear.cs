namespace WebApplication1;

public class HeaderCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApiKeyValidationService _apiKeyValidationService;

    public HeaderCheckMiddleware(RequestDelegate next, IApiKeyValidationService validationService)
    {
        _next = next;
        _apiKeyValidationService = validationService;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("apikey", out var headerValue))
        {
            if(headerValue.Count==0)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Missing required header.");
                return;
            }
            var rslt = await _apiKeyValidationService.IsValidApiKeyAsync(headerValue[0]!);

            if (!rslt)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid API key.");
                return;
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Missing required header.");
            return;
        }

        // Proceed to next middleware or controller
        await _next(context);
    }
}