using CommonLibrary.SharedServices.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CommonLibrary.Exceptions.Middleware
{
    public class ResponseLoggerMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseLoggerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ICounterService counterService)
        {
            context.Response.OnCompleted(async () =>
            {
                var method = context.Request.Method;
                if (!context.Request.Path.Equals("/api/v1/vas", StringComparison.OrdinalIgnoreCase) && !context.Request.Path.Equals("/api/v1/res", StringComparison.OrdinalIgnoreCase) && !context.Request.Path.StartsWithSegments("/api/v1/cnt", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantId);
                    context.Request.Headers.TryGetValue("X-Request-ID", out var venueId);

                    var path = context.Request.Path.Value.Split("/").Last();
                    var statusCode = context.Response.StatusCode;
                    if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(venueId))
                    {
                        if (path == "book" && method == "PUT")
                        {
                            var bookingOp = context.Items["booking_op"]?.ToString();
                            if (bookingOp == "CANCELLED" || bookingOp == "RESCHEDULED")
                            {
                                var field = statusCode == 200
                                    ? (bookingOp == "CANCELLED" ? "mod_canc" : "mod_ok")
                                    : $"mod_err_{(statusCode >= 500 ? "5xx" : statusCode.ToString())}";
                                await counterService.TriggerCounter(tenantId, venueId, field);
                            }
                        }
                        else if (path == "blk" && statusCode == 200)
                        {
                            var cnt = context.Items.ContainsKey("blk_cnt") || context.Request.Query["cnt"] == "true";
                            if (cnt)
                                await counterService.ProcessCounter(tenantId, venueId, path, statusCode, method);
                        }
                        else
                        {
                            await counterService.ProcessCounter(tenantId, venueId, path, statusCode, method);
                        }
                    }
                }
                else if (context.Request.Path.Equals("/api/v1/vas", StringComparison.OrdinalIgnoreCase) || context.Request.Path.Equals("/api/v1/res", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Headers.TryGetValue("X-Request-ID", out var tenantId);
                    var path = context.Request.Path.Value.Split("/").Last();
                    var statusCode = context.Response.StatusCode;
                    await counterService.ProcessCounter(tenantId, null, path, statusCode, method);
                }
            });
            await _next(context);
        }
    }
}
