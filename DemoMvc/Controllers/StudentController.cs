using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DemoMvc.Models;

namespace DemoMvc.Controllers;

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
    
   
    // Hiển thị form
    [HttpGet]
    public IActionResult test()
    {
        return View();
    }


    // Nhận dữ liệu từ form
    [HttpPost]
    public IActionResult test(Student student)
    {
        ViewBag.Student = $"Tên: {student.name}, Địa chỉ: {student.address}, Trường: {student.uni}";
    
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    
}
