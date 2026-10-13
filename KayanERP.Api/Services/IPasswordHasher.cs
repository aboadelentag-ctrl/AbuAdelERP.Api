namespace KayanERP.Api.Services;

/// <summary>
/// واجهة خدمة تشفير والتحقق من الباسوردات.
/// تفصل منطق الأمان عن باقي طبقات التطبيق.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// يشفّر باسورد نص صريح ويُرجع Hash آمن.
    /// يُستخدم عند التسجيل أو تغيير الباسورد.
    /// </summary>
    string Hash(string plainPassword);

    /// <summary>
    /// يتحقق إن الباسورد النص صريح يطابق الـ Hash المخزن.
    /// يُستخدم عند تسجيل الدخول.
    /// </summary>
    bool Verify(string plainPassword, string storedHash);

    /// <summary>
    /// يتحقق إن الـ Hash قديم ومحتاج إعادة تشفير.
    /// BCrypt بيقولك لو الـ work factor اتغير.
    /// </summary>
    bool NeedsRehash(string storedHash);
}

/// <summary>
/// تنفيذ IPasswordHasher باستخدام BCrypt.
/// Work Factor = 12 (توازن جيد بين الأمان والسرعة على السيرفر).
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    // Work factor 12 = 2^12 = 4096 تكرار.
    // زيادة الرقم = أبطأ لكن أكثر أمانًا. 12 هو المعيار الحالي.
    private const int WorkFactor = 12;

    public string Hash(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("Password cannot be empty.", nameof(plainPassword));

        return BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);
    }

    public bool Verify(string plainPassword, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(plainPassword) ||
            string.IsNullOrWhiteSpace(storedHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, storedHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // الـ hash ده مش hash BCrypt — يمكن باسورد قديم نص صريح
            return false;
        }
        catch (Exception)
        {
            // أي خطأ تاني — نرفض
            return false;
        }
    }

    public bool NeedsRehash(string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            return true;

        try
        {
            return BCrypt.Net.BCrypt.PasswordNeedsRehash(storedHash, WorkFactor);
        }
        catch
        {
            // لو مش hash BCrypt أصلاً، فمحتاج يتغير
            return true;
        }
    }
}