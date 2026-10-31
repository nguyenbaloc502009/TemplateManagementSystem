using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateManagement.Core.Security;

namespace PresentationTemplateSystem.Controllers;

[AllowAnonymous]
public sealed class AccountController : Controller
{
    private readonly UserAccountService _accounts;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserAccountService accounts, ILogger<AccountController> logger)
    {
        _accounts = accounts;
        _logger = logger;
    }

    [HttpGet]
    /// <summary>Hiển thị biểu mẫu đăng nhập và lưu đường dẫn quay lại.</summary>
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    /// <summary>Xác thực thông tin đăng nhập và tạo phiên cookie cho người dùng.</summary>
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AppUser? user = _accounts.VerifyCredentials(model.Email, model.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        await SignInUser(user);
        return RedirectToLocal(model.ReturnUrl, nameof(PresentationController.Index), "Presentation");
    }

    [HttpGet]
    /// <summary>Hiển thị biểu mẫu đăng ký tài khoản.</summary>
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    /// <summary>Tạo tài khoản mới và đăng nhập người dùng sau khi đăng ký.</summary>
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            AppUser user = _accounts.Register(model.Email, model.DisplayName, model.Password);
            await SignInUser(user);
            return RedirectToAction(nameof(PresentationController.Index), "Presentation");
        }
        catch (DuplicateAccountException)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được đăng ký.");
            return View(model);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to register an account.");
            ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản lúc này. Vui lòng thử lại.");
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    /// <summary>Kết thúc phiên đăng nhập hiện tại.</summary>
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(PresentationController.Index), "Presentation");
    }

    /// <summary>Tạo danh tính cookie từ thông tin tài khoản.</summary>
    private async Task SignInUser(AppUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }

    /// <summary>Chỉ chuyển hướng đến URL nội bộ; nếu không hợp lệ thì dùng trang mặc định.</summary>
    private IActionResult RedirectToLocal(string? returnUrl, string action, string controller) =>
        // Kiểm tra URL cục bộ để tránh chuyển hướng người dùng ra ngoài website.
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(action, controller);
}
