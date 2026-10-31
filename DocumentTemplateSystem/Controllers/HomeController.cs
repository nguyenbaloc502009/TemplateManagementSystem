using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DocumentTemplateSystem.Models;

namespace DocumentTemplateSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>Hiển thị trang chủ.</summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>Hiển thị thông tin quyền riêng tư.</summary>
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    /// <summary>Hiển thị trang lỗi cùng mã định danh yêu cầu hiện tại.</summary>
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
