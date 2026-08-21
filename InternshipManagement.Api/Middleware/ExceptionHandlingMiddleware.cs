using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InternshipManagement.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Message = exception.Message,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Timestamp = DateTime.UtcNow
            };

            // Handle specific exception types
            switch (exception)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "You are not authorized to perform this action.";
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "The requested resource was not found.";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An error occurred while processing your request.";
                    break;
            }

            context.Response.StatusCode = response.StatusCode;
            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public class ErrorResponse
    {
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
    }
}