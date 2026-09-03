using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PickleHub.Common.DTOs;
using PickleHub.Common.Exceptions;
using System.Net;

namespace PickleHub.Common.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
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
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

                var (statusCode, message, errors) = ex switch
                {
                    NotFoundException nf => (HttpStatusCode.NotFound, nf.Message, (Dictionary<string, string[]>?)null),
                    ConflictException cf => (HttpStatusCode.Conflict, cf.Message, (Dictionary<string, string[]>?)null),
                    ForbiddenException fb => (HttpStatusCode.Forbidden, fb.Message, (Dictionary<string, string[]>?)null),
                    UnauthorizedException ua => (HttpStatusCode.Unauthorized, ua.Message, (Dictionary<string, string[]>?)null),
                    DomainException de => (HttpStatusCode.BadRequest, de.Message, (Dictionary<string, string[]>?)null),
                    FluentValidation.ValidationException vl => (
                        HttpStatusCode.BadRequest,
                        "Validation failed.",
                        vl.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray()
                            )),
                    _ => (HttpStatusCode.InternalServerError, "Đã có lỗi xảy ra phía hệ thống.", (Dictionary<string, string[]>?)null)
                };

                context.Response.StatusCode = (int)statusCode;

                await context.Response.WriteAsJsonAsync(new ErrorResponseDto
                {
                    Error = new ErrorDetail
                    {
                        Message = message,
                        Errors = errors
                    }
                });
            }
        }
    }
}
