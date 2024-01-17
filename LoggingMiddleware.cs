using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PackageRequest
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _request;
        private readonly ILogger<LoggingMiddleware> _logger;
        private EventId _event => new EventId(new Random().Next(), nameof(LoggingMiddleware));

        public LoggingMiddleware(RequestDelegate request, ILogger<LoggingMiddleware> logger)
        {
            _request = request;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var header = "";
            foreach (var item in context.Request.Headers)
            {
                header+=item.Key+":"+item.Value+"\t";
            }

            var request = $"{context.Request.Method} {context.Request.Path} {header}";
            var @event = _event;
            _logger.LogInformation(@event, $"income {request}");

            try
            {
                await _request.Invoke(context);
                _logger.LogInformation(@event, $"finished {request} {context.Response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(@event, ex, $"failed {request}");
                throw ex;
            }
        }
    }
}