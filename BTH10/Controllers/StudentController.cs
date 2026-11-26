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
    public class StudentController : Controller
    {
        private const int PageSize = 10;
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }
        private string GenerateStudentCode(int facultyId)
        {
            string year = "28";      // Năm nhập học
            string system = "21";    // Đại học chính quy


            // Lấy mã ngành theo khoa được chọn
            var faculty = _context.Faculty
                .FirstOrDefault(f => f.FacultyId == facultyId);


            string major = faculty?.FacultyCode ?? "00";


            var lastNumber = _context.Students
                .Select(student => new
                {
                    student.StudentCode,
                    FacultyCode = student.Faculty!.FacultyCode
                })
                .AsEnumerable()
                .Select(student => GetStudentCodeSequence(
                    student.StudentCode,
                    $"{year}{system}{student.FacultyCode}"))
                .DefaultIfEmpty()
                .Max();

            var number = lastNumber + 1;
            return $"{year}{system}{major}{number:D4}";
        }

        private static int GetStudentCodeSequence(string studentCode, string prefix)
        {
            if (!studentCode.StartsWith(prefix, StringComparison.Ordinal))
            {
                return 0;
            }

            return int.TryParse(studentCode.AsSpan(prefix.Length), out var sequence)
                ? sequence
                : 0;
        }

        // GET: Student
        public async Task<IActionResult> Index(string? searchTerm, int page = 1)
        {
            var studentsQuery = _context.Students
                .AsNoTracking()
                .Include(s => s.Faculty)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                studentsQuery = studentsQuery.Where(student =>
                    student.StudentCode.Contains(term) ||
                    student.Name.Contains(term) ||
                    student.Email.Contains(term) ||
                    (student.Faculty != null &&
                     (student.Faculty.FacultyCode.Contains(term) ||
                      student.Faculty.FacultyName.Contains(term))));
            }

            var totalCount = await studentsQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            var students = await studentsQuery
                .OrderByDescending(student => student.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["SearchTerm"] = searchTerm?.Trim() ?? string.Empty;
            ViewData["Page"] = page;
            ViewData["TotalPages"] = totalPages;

            return IsAjaxRequest()
                ? PartialView("_StudentTable", students)
                : View(students);
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return IsAjaxRequest()
                ? PartialView("_StudentDetails", student)
                : View(student);
        }

        // GET: Student/Create
        public async Task<IActionResult> Create()
        {
            await PopulateFacultyOptionsAsync();
            var student = new Student();

            return IsAjaxRequest()
                ? FormPartial(student, nameof(Create))
                : View(student);
        }

        // POST: Student/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,FacultyId")] Student student)
        {
            await ValidateFacultyAsync(student);

            if (ModelState.IsValid)
            {
                student.StudentCode = GenerateStudentCode(student.FacultyId);

                _context.Add(student);
                await _context.SaveChangesAsync();

                return IsAjaxRequest()
                    ? Json(new { success = true })
                    : RedirectToAction(nameof(Index));
            }

            await PopulateFacultyOptionsAsync(student.FacultyId);
            return IsAjaxRequest()
                ? FormPartial(student, nameof(Create))
                : View(student);
        }

        // GET: Student/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            await PopulateFacultyOptionsAsync(student.FacultyId);
            return IsAjaxRequest()
                ? FormPartial(student, nameof(Edit))
                : View(student);
        }

        // POST: Student/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,FacultyId")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            await ValidateFacultyAsync(student);

            if (ModelState.IsValid)
            {
                var existingStudent = await _context.Students.FindAsync(id);
                if (existingStudent == null)
                {
                    return NotFound();
                }

                try
                {
                    existingStudent.Name = student.Name;
                    existingStudent.Email = student.Email;
                    existingStudent.FacultyId = student.FacultyId;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
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

            await PopulateFacultyOptionsAsync(student.FacultyId);
            return IsAjaxRequest()
                ? FormPartial(student, nameof(Edit))
                : View(student);
        }

        // GET: Student/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return IsAjaxRequest()
                ? PartialView("_StudentDelete", student)
                : View(student);
        }

        // POST: Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return IsAjaxRequest()
                ? Json(new { success = true })
                : RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }

        private async Task PopulateFacultyOptionsAsync(int? selectedFacultyId = null)
        {
            var faculties = await _context.Faculty
                .AsNoTracking()
                .OrderBy(faculty => faculty.FacultyName)
                .ToListAsync();

            ViewData["FacultyId"] = new SelectList(
                faculties,
                "FacultyId",
                "FacultyName",
                selectedFacultyId);
        }

        private async Task ValidateFacultyAsync(Student student)
        {
            if (student.FacultyId <= 0 ||
                !await _context.Faculty.AnyAsync(faculty => faculty.FacultyId == student.FacultyId))
            {
                ModelState.AddModelError(nameof(student.FacultyId), "Vui lòng chọn một khoa hợp lệ.");
            }
        }

        private PartialViewResult FormPartial(Student student, string actionName)
        {
            ViewData["FormAction"] = actionName;
            ViewData["ModalTitle"] = actionName == nameof(Create)
                ? "Thêm sinh viên"
                : "Cập nhật sinh viên";

            return PartialView("_StudentForm", student);
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
