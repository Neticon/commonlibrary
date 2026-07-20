using Amazon.CognitoIdentityProvider.Model;
using CommonLibrary.Domain.Entities;
using CommonLibrary.Exceptions;
using CommonLibrary.SharedServices.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CommonLibrary.Exceptions.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILogService logService)
        {
            var message = "";
            var statusCode = 0;
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                switch (exception)
                {
                    case PsqlResponseFailException:
                        statusCode = 400;
                        message = exception.Message;
                        break;
                    case UsernameExistsException:
                        statusCode = 409;
                        message = exception.Message;
                        break;
                    default:
                        statusCode = 500;
                        message = $"Internal server error => {exception.Message} ";
                        break;
                }

                if (statusCode == 500)
                    await logService.WriteSystemLog(LogLevel.ERROR, OperationType.GLOBAL_EXCEPTION, exception.ToString(), nameof(ExceptionMiddleware));

                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(message);
            }
        }
    }
}
