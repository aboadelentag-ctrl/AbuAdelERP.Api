using System.Net;
using System.Text.Json;
using KayanERP.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace KayanERP.Api.Middleware;

/// <summary>
/// Middleware يعالج كل الـ Exceptions اللي بتحصل في الـ Pipeline.
/// يحوّلها لـ JSON Response منظم + يمنع تسريب Stack Traces.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 1. تحديد الـ Status Code والـ Response
        var (statusCode, title, errorCode, errors) = exception switch
        {
            // ===== Exceptions مخصصة =====
            ValidationException ve => (
                ve.StatusCode,
                ve.Message,
                ve.ErrorCode,
                (object?)ve.Errors
            ),

            AppException appEx => (
                appEx.StatusCode,
                appEx.Message,
                appEx.ErrorCode,
                null
            ),

            // ===== Unauthorized =====
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "غير مصرح لك بالوصول. يرجى تسجيل الدخول مرة أخرى.",
                "Unauthorized",
                null
            ),

            // ===== Database Issues =====
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "تم تعديل البيانات من قبل مستخدم آخر. يرجى إعادة المحاولة.",
                "ConcurrencyConflict",
                null
            ),

            Microsoft.EntityFrameworkCore.DbUpdateException dbe => (
                StatusCodes.Status409Conflict,
                "حدث تعارض في البيانات. قد تكون بعض البيانات مكررة.",
                "DatabaseConflict",
                null
            ),

            // ===== Timeout / Network =====
            TaskCanceledException or OperationCanceledException => (
                StatusCodes.Status408RequestTimeout,
                "انتهت مهلة الطلب. يرجى المحاولة مرة أخرى.",
                "RequestTimeout",
                null
            ),

            // ===== أي Exception تاني =====
            _ => (
                StatusCodes.Status500InternalServerError,
                "حدث خطأ غير متوقع في الخادم. يرجى المحاولة لاحقاً.",
                "InternalServerError",
                null
            )
        };

        // 2. تسجيل الخطأ
        LogException(context, exception, statusCode);

        // 3. بناء الـ Response
        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = title,
            Status = statusCode,
            Instance = context.Request.Path,
            Extensions =
            {
                ["errorCode"] = errorCode,
                ["traceId"] = context.TraceIdentifier,
                ["timestamp"] = DateTime.UtcNow
            }
        };

        if (errors != null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        // 4. تفاصيل إضافية في Development فقط
        if (_env.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["exceptionMessage"] = exception.Message;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace?.Split('\n').Take(10).ToArray();
        }

        // 5. إرسال الرد
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsJsonAsync(problemDetails, options);
    }

    private void LogException(HttpContext context, Exception exception, int statusCode)
    {
        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            401 or 403 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(
            logLevel,
            exception,
            "Exception handled: {ExceptionType} at {Path} → HTTP {StatusCode}",
            exception.GetType().Name,
            context.Request.Path,
            statusCode);
    }
}

/// <summary>
/// Extension method لتسجيل الـ Middleware بسهولة في Program.cs
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}