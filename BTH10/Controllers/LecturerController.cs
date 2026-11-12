using BTH7.Data;
using BTH7.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTH7.Controllers
{
    public class LecturerController : Controller
    {
        private const int PageSize = 10;
        private readonly ApplicationDbContext _context;

        public LecturerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchTerm, int page = 1)
        {
            var lecturersQuery = _context.Lecturers.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                lecturersQuery = lecturersQuery.Where(lecturer =>
                    lecturer.Name.Contains(term) ||
                    lecturer.Email.Contains(term) ||
                    lecturer.Phone.Contains(term));
            }

            var totalCount = await lecturersQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            var lecturers = await lecturersQuery
                .OrderByDescending(lecturer => lecturer.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["SearchTerm"] = searchTerm?.Trim() ?? string.Empty;
            ViewData["Page"] = page;
            ViewData["TotalPages"] = totalPages;

            return IsAjaxRequest()
                ? PartialView("_LecturerTable", lecturers)
                : View(lecturers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lecturer = await _context.Lecturers
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id);
            if (lecturer == null)
            {
                return NotFound();
            }

            return IsAjaxRequest()
                ? PartialView("_LecturerDetails", lecturer)
                : RedirectToAction(nameof(Index));
        }

        public IActionResult Create()
        {
            return IsAjaxRequest()
                ? FormPartial(new Lecturer(), nameof(Create))
                : RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone")] Lecturer lecturer)
        {
            if (ModelState.IsValid)
            {
                _context.Lecturers.Add(lecturer);
                await _context.SaveChangesAsync();

                return IsAjaxRequest()
                    ? Json(new { success = true })
                    : RedirectToAction(nameof(Index));
            }

            return FormPartial(lecturer, nameof(Create));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer == null)
            {
                return NotFound();
            }

            return IsAjaxRequest()
                ? FormPartial(lecturer, nameof(Edit))
                : RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Phone")] Lecturer lecturer)
        {
            if (id != lecturer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingLecturer = await _context.Lecturers.FindAsync(id);
                if (existingLecturer == null)
                {
                    return NotFound();
                }

                try
                {
                    existingLecturer.Name = lecturer.Name;
                    existingLecturer.Email = lecturer.Email;
                    existingLecturer.Phone = lecturer.Phone;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LecturerExists(id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return IsAjaxRequest()
                    ? Json(new { success = true })
                    : RedirectToAction(nameof(Index));
            }

            return FormPartial(lecturer, nameof(Edit));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lecturer = await _context.Lecturers
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id);
            if (lecturer == null)
            {
                return NotFound();
            }

            return IsAjaxRequest()
                ? PartialView("_LecturerDelete", lecturer)
                : RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer == null)
            {
                return NotFound();
            }

            _context.Lecturers.Remove(lecturer);
            await _context.SaveChangesAsync();

            return IsAjaxRequest()
                ? Json(new { success = true })
                : RedirectToAction(nameof(Index));
        }

        private bool LecturerExists(int id)
        {
            return _context.Lecturers.Any(lecturer => lecturer.Id == id);
        }

        private PartialViewResult FormPartial(Lecturer lecturer, string actionName)
        {
            ViewData["FormAction"] = actionName;
            ViewData["ModalTitle"] = actionName == nameof(Create)
                ? "Thêm giảng viên"
                : "Cập nhật giảng viên";

            return PartialView("_LecturerForm", lecturer);
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
