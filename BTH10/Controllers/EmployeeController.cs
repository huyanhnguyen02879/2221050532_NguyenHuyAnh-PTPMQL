using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BTH7.Data;
using BTH7.Models;

namespace BTH7.Controllers
{
    public class EmployeeController : Controller
    {
        private const int PageSize = 10;
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Employee
        public async Task<IActionResult> Index(string? searchTerm, int page = 1)
        {
            var employeesQuery = _context.Employees.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                employeesQuery = employeesQuery.Where(employee =>
                    employee.Id.Contains(term) ||
                    employee.Name.Contains(term) ||
                    employee.Email.Contains(term));
            }

            var totalCount = await employeesQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            var employees = await employeesQuery
                .OrderByDescending(employee => employee.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["SearchTerm"] = searchTerm?.Trim() ?? string.Empty;
            ViewData["Page"] = page;
            ViewData["TotalPages"] = totalPages;

            return IsAjaxRequest()
                ? PartialView("_EmployeeTable", employees)
                : View(employees);
        }

        // GET: Employee/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return IsAjaxRequest()
                ? PartialView("_EmployeeDetails", employee)
                : View(employee);
        }

        // GET: Employee/Create
        public IActionResult Create()
        {
            var employee = new Employee();
            return IsAjaxRequest()
                ? FormPartial(employee, nameof(Create))
                : View(employee);
        }

        // POST: Employee/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email")] Employee employee)
        {
            if (!string.IsNullOrWhiteSpace(employee.Id) &&
                await _context.Employees.AnyAsync(existing => existing.Id == employee.Id))
            {
                ModelState.AddModelError(nameof(employee.Id), "Mã nhân viên đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();

                return IsAjaxRequest()
                    ? Json(new { success = true })
                    : RedirectToAction(nameof(Index));
            }

            return IsAjaxRequest()
                ? FormPartial(employee, nameof(Create))
                : View(employee);
        }

        // GET: Employee/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return IsAjaxRequest()
                ? FormPartial(employee, nameof(Edit))
                : View(employee);
        }

        // POST: Employee/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Name,Email")] Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingEmployee = await _context.Employees.FindAsync(id);
                if (existingEmployee == null)
                {
                    return NotFound();
                }

                try
                {
                    existingEmployee.Name = employee.Name;
                    existingEmployee.Email = employee.Email;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return IsAjaxRequest()
                    ? Json(new { success = true })
                    : RedirectToAction(nameof(Index));
            }

            return IsAjaxRequest()
                ? FormPartial(employee, nameof(Edit))
                : View(employee);
        }

        // GET: Employee/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return IsAjaxRequest()
                ? PartialView("_EmployeeDelete", employee)
                : View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return IsAjaxRequest()
                ? Json(new { success = true })
                : RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(string id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }

        private PartialViewResult FormPartial(Employee employee, string actionName)
        {
            ViewData["FormAction"] = actionName;
            ViewData["ModalTitle"] = actionName == nameof(Create)
                ? "Thêm nhân viên"
                : "Cập nhật nhân viên";

            return PartialView("_EmployeeForm", employee);
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
