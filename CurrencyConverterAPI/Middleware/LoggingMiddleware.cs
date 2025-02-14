using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var clientId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        var method = request.Method;
        var endpoint = request.Path;

        _logger.LogInformation("Request started: {Method} {Endpoint} from IP {ClientIp} by {ClientId}",
            method, endpoint, clientIp, clientId);

        await _next(context);

        stopwatch.Stop();
        var responseCode = context.Response.StatusCode;
        var responseTime = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation("Request completed: {Method} {Endpoint} with Status {ResponseCode} in {ResponseTime}ms",
            method, endpoint, responseCode, responseTime);
    }
}
