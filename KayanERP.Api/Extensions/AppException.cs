namespace KayanERP.Api.Exceptions;

/// <summary>
/// Base class للـ Exceptions المخصصة في التطبيق.
/// بيساعد في التحكم في الـ HTTP Status Code والـ Response.
/// </summary>
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
    public virtual string ErrorCode => GetType().Name;

    protected AppException(string message) : base(message) { }
    protected AppException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// للطلبات غير الصالحة (Validation, Business Rules).
/// HTTP 400 Bad Request
/// </summary>
public sealed class BadRequestException : AppException
{
    public override int StatusCode => StatusCodes.Status400BadRequest;

    public BadRequestException(string message) : base(message) { }
    public BadRequestException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// عندما لا يملك المستخدم صلاحية الوصول.
/// HTTP 403 Forbidden
/// </summary>
public sealed class ForbiddenException : AppException
{
    public override int StatusCode => StatusCodes.Status403Forbidden;

    public ForbiddenException(string message = "ليس لديك صلاحية للوصول لهذا المورد.")
        : base(message) { }
}

/// <summary>
/// عندما لا يتم العثور على المورد.
/// HTTP 404 Not Found
/// </summary>
public sealed class NotFoundException : AppException
{
    public override int StatusCode => StatusCodes.Status404NotFound;

    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// عند حدوث تعارض (مثلاً: كود مكرر).
/// HTTP 409 Conflict
/// </summary>
public sealed class ConflictException : AppException
{
    public override int StatusCode => StatusCodes.Status409Conflict;

    public ConflictException(string message) : base(message) { }
}

/// <summary>
/// عند حدوث خطأ في التحقق من صحة البيانات.
/// HTTP 422 Unprocessable Entity
/// </summary>
public sealed class ValidationException : AppException
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("حدثت أخطاء في التحقق من صحة البيانات.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : base("حدثت أخطاء في التحقق من صحة البيانات.")
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, new[] { error } }
        };
    }
}