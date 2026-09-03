using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DemoMvc.Models;

namespace DemoMvc.Controllers;

public class DemoController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Create()
    {
        return Content("Đây là trang Create");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
