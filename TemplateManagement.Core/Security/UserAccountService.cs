using System.Collections;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TemplateManagement.Core.Data;

namespace TemplateManagement.Core.Security;

public sealed class DuplicateAccountException : Exception
{
    public DuplicateAccountException() : base("An account with this email already exists.")
    {
    }
}

public sealed class UserAccountService
{
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public UserAccountService(IConfiguration configuration, IPasswordHasher<AppUser> passwordHasher)
    {
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    /// <summary>Chuẩn hóa email, băm mật khẩu và lưu tài khoản mới.</summary>
    public AppUser Register(string email, string displayName, string password)
    {
        string normalizedEmail = NormalizeEmail(email);
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            DisplayName = displayName.Trim()
        };
        // Chỉ lưu hash do PasswordHasher tạo; mật khẩu thô không được ghi vào database.
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        try
        {
            using var db = new DatabaseManager(_configuration);
            db.ExecuteNonQuery(
                """
                INSERT INTO app_users (id, email, display_name, password_hash)
                VALUES (@id, @email, @displayName, @passwordHash);
                """,
                new Hashtable
                {
                    { "id", user.Id },
                    { "email", user.Email },
                    { "displayName", user.DisplayName },
                    { "passwordHash", user.PasswordHash }
                });
            return user;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateAccountException();
        }
    }

    /// <summary>Tìm tài khoản theo email và xác minh mật khẩu với hash đã lưu.</summary>
    public AppUser? VerifyCredentials(string email, string password)
    {
        using var db = new DatabaseManager(_configuration);
        AppUser? user = db.SelectSingle<AppUser>(
            """
            SELECT id AS Id, email AS Email, display_name AS DisplayName, password_hash AS PasswordHash
            FROM app_users
            WHERE lower(email) = @email;
            """,
            new Hashtable { { "email", NormalizeEmail(email) } });

        if (user == null)
        {
            return null;
        }

        PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    /// <summary>Chuẩn hóa email để tìm kiếm và kiểm tra duy nhất không phân biệt hoa thường.</summary>
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
