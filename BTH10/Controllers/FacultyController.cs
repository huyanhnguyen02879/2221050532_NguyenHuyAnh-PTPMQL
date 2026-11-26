using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BTH7.Data;
using BTH7.Models;

namespace BTH7.Controllers
{
    public class FacultyController : Controller
    {
        private const int PageSize = 10;
        private readonly ApplicationDbContext _context;

        public FacultyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Faculty
        public async Task<IActionResult> Index(string? searchTerm, int page = 1)
        {
            var facultiesQuery = _context.Faculty.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                facultiesQuery = facultiesQuery.Where(faculty =>
                    faculty.FacultyCode.Contains(term) ||
                    faculty.FacultyName.Contains(term));
            }

            var totalCount = await facultiesQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            var faculties = await facultiesQuery
                .OrderByDescending(faculty => faculty.FacultyId)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["SearchTerm"] = searchTerm?.Trim() ?? string.Empty;
            ViewData["Page"] = page;
            ViewData["TotalPages"] = totalPages;

            return IsAjaxRequest()
                ? PartialView("_FacultyTable", faculties)
                : View(faculties);
        }

        // GET: Faculty/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faculty = await _context.Faculty
                .FirstOrDefaultAsync(m => m.FacultyId == id);
            if (faculty == null)
            {
                return NotFound();
            }

            return IsAjaxRequest()
                ? PartialView("_FacultyDetails", faculty)
                : View(faculty);
        }

        // GET: Faculty/Create
        public IActionResult Create()
        {
            var faculty = new Faculty();
            return IsAjaxRequest()
                ? FormPartial(faculty, nameof(Create))
                : View(faculty);
        }

        // POST: Faculty/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FacultyId,FacultyCode,FacultyName")] Faculty faculty)
        {
            if (ModelState.IsValid)
            {
                _context.Add(faculty);
                await _context.SaveChangesAsync();

                return IsAjaxRequest()
                    ? Json(new { success = true })
                    : RedirectToAction(nameof(Index));
            }

            return IsAjaxRequest()
                ? FormPartial(faculty, nameof(Create))
                : View(faculty);
        }

        // GET: Faculty/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faculty = await _context.Faculty.FindAsync(id);
            if (faculty == null)
            {
                return NotFound();
            }
            return IsAjaxRequest()
                ? FormPartial(faculty, nameof(Edit))
                : View(faculty);
        }

        // POST: Faculty/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FacultyId,FacultyCode,FacultyName")] Faculty faculty)
        {
            if (id != faculty.FacultyId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingFaculty = await _context.Faculty.FindAsync(id);
                if (existingFaculty == null)
                {
                    return NotFound();
                }

                try
                {
                    existingFaculty.FacultyCode = faculty.FacultyCode;
                    existingFaculty.FacultyName = faculty.FacultyName;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FacultyExists(faculty.FacultyId))
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
                ? FormPartial(faculty, nameof(Edit))
                : View(faculty);
        }

        // GET: Faculty/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faculty = await _context.Faculty
                .FirstOrDefaultAsync(m => m.FacultyId == id);
            if (faculty == null)
            {
                return NotFound();
            }

            await SetLinkedStudentCountAsync(faculty.FacultyId);
            return IsAjaxRequest()
                ? PartialView("_FacultyDelete", faculty)
                : View(faculty);
        }

        // POST: Faculty/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var faculty = await _context.Faculty.FindAsync(id);
            if (faculty == null)
            {
                return NotFound();
            }

            if (await _context.Students.AnyAsync(student => student.FacultyId == id))
            {
                ModelState.AddModelError(string.Empty,
                    "Không thể xóa khoa đang có sinh viên. Hãy chuyển sinh viên sang khoa khác trước.");
                await SetLinkedStudentCountAsync(id);
                return IsAjaxRequest()
                    ? PartialView("_FacultyDelete", faculty)
                    : View(faculty);
            }

            _context.Faculty.Remove(faculty);
            await _context.SaveChangesAsync();

            return IsAjaxRequest()
                ? Json(new { success = true })
                : RedirectToAction(nameof(Index));
        }

        private bool FacultyExists(int id)
        {
            return _context.Faculty.Any(e => e.FacultyId == id);
        }

        private async Task SetLinkedStudentCountAsync(int facultyId)
        {
            ViewData["LinkedStudentCount"] = await _context.Students
                .CountAsync(student => student.FacultyId == facultyId);
        }

        private PartialViewResult FormPartial(Faculty faculty, string actionName)
        {
            ViewData["FormAction"] = actionName;
            ViewData["ModalTitle"] = actionName == nameof(Create)
                ? "Thêm khoa"
                : "Cập nhật khoa";

            return PartialView("_FacultyForm", faculty);
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
