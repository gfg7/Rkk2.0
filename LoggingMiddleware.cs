using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#nullable enable
namespace PackageRequest
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _request;
        private readonly ILogger<LoggingMiddleware>? _logger;
        private EventId _event => new EventId(new Random().Next(), nameof(LoggingMiddleware));

        public LoggingMiddleware(RequestDelegate request, ILogger<LoggingMiddleware> logger, IOptions<AppOptions> options)
        {
            _request = request;

            if (options.Value.LogIncomming)
            {
                _logger = logger;
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = $"{context.TraceIdentifier} {context.Request.Method} {context.Request.Path}?{context.Request.QueryString}";
            var @event = _event;
            _logger?.LogInformation(@event, $"income {request}");

            try
            {
                await _request.Invoke(context);
                _logger?.LogInformation(@event, $"finished {request} {context.Response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(@event, ex, $"failed {request} {context.Response.StatusCode}");
            }
        }
    }
}