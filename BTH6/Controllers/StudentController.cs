using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BTH6.Models;

namespace BTH6.Controllers;

public class StudentController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

// Hiển thị form nhập
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Thêm sinh viên";

        return View();
    }


    // Nhận dữ liệu từ form
    [HttpPost]
    public IActionResult Create(Student student)
    {
        // ViewBag nhận dữ liệu sinh viên vừa nhập
        ViewBag.Student = student;


        // ViewData hiển thị tiêu đề
        ViewData["Title"] = "Thông tin sinh viên";


        // TempData hiển thị thông báo
        TempData["Success"] = "Thêm sinh viên thành công!";


        return View("Result");
    }
}
