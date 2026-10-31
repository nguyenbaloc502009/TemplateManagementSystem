using PresentationTemplateSystem.Common;
using PresentationTemplateSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemplateManagement.Core.Security;
using TemplateManagement.Core.Web;
using TemplateManagement.Core.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".TemplateManagement.Presentation.Auth";
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = ".TemplateManagement.Presentation.Antiforgery";
});
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<UserDeckService>();
builder.Services.AddSingleton<IObjectStorage>(serviceProvider =>
    new MinioObjectStorage(serviceProvider.GetRequiredService<IConfiguration>()));
builder.Services.AddRouting(options => options.LowercaseUrls = true);

var app = builder.Build();
TemplateAssetStorage.Initialize(
    app.Environment.WebRootPath,
    builder.Configuration["UploadSettings:ThumbnailUploadFolder"]);
_ = app.Services.GetRequiredService<IObjectStorage>();
TemplateAssetStorage.EnsureFallbackThumbnail();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSafeExceptionHandling();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Presentation}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
