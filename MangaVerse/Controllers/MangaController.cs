using MangaVerse.Data;
using MangaVerse.Helpers;
using MangaVerse.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MangaVerse.Controllers
{
    public class MangaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment; //за картинките да се качват на сървъра
        public MangaController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }
        public async Task<IActionResult> Index(string searchString, int? pageNumber, MangaGenre? genreFilter, string sortOrder, DateTime? yearFrom, DateTime? yearTo)
        {
            // Настройка на сортирането за изгледа
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["AuthorSortParm"] = sortOrder == "author" ? "author_desc" : "author";
            ViewData["YearSortParm"] = sortOrder == "year" ? "year_desc" : "year";

            // Запазване на текущите филтри
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentGenre"] = genreFilter.HasValue ? ((int)genreFilter.Value).ToString() : "";

            var mangaQuery = _context.Mangas.AsQueryable();//заявка към базата данни

            if (!string.IsNullOrEmpty(searchString))
            {
                if (!mangaQuery.Any())
                {
                    ViewBag.Message = "Няма намерени резултати за търсенето.";
                }
                mangaQuery = mangaQuery.Where(m => m.Title.Contains(searchString) || m.Author.Contains(searchString));

            }
            
            if(genreFilter.HasValue)
            {
                mangaQuery = mangaQuery.Where(m => m.Genre == genreFilter.Value);
            }

            // Сортиране
            switch (sortOrder)
            {
                case "name_desc": mangaQuery = mangaQuery.OrderByDescending(m => m.Title); break;
                case "author": mangaQuery = mangaQuery.OrderBy(m => m.Author); break;
                case "author_desc": mangaQuery = mangaQuery.OrderByDescending(m => m.Author); break;
                case "year": mangaQuery = mangaQuery.OrderBy(m => m.ReleaseYear); break;
                case "year_desc": mangaQuery = mangaQuery.OrderByDescending(m => m.ReleaseYear); break;
                default: mangaQuery = mangaQuery.OrderBy(m => m.Title); break;
            }

            int pageSize = 5; // Колко манги да има на една страница

            // Вместо ToListAsync(), използваме нашия CreateAsync от PaginatedList
            return View(await PaginatedList<Manga>.CreateAsync(mangaQuery.AsNoTracking(), pageNumber ?? 1, pageSize));
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Manga manga, IFormFile imageFile)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if(imageFile == null)
            {
                ModelState.AddModelError("CoverImageUrl", "Корица е задължителна.");
            }
            if(imageFile != null)
            {
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("CoverImageUrl", "Корицата трябва да е във формат JPG или PNG.");
                }
            }
            if (ModelState.IsValid)
            {
                manga.CoverImageUrl = await SaveImage(imageFile);
                _context.Add(manga);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
          
            return View(manga);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var manga = await _context.Mangas.FindAsync(id);
            if (manga == null) return NotFound();
            return View(manga);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Manga manga, IFormFile? imageFile)
        {
            if (id != manga.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null)
                    {
                        // Вземаме стария път САМО за да изтрием физическия файл
                        var oldPath = await _context.Mangas
                            .Where(m => m.Id == id)
                            .Select(m => m.CoverImageUrl)
                            .FirstOrDefaultAsync();

                        if (!string.IsNullOrEmpty(oldPath)) DeleteFile(oldPath);

                        manga.CoverImageUrl = await SaveImage(imageFile);
                    }

                    _context.Update(manga);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Възникна грешка при запис: " + ex.Message);
                }
            }
            return View(manga);
        }
        // Details (За всички)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var manga = await _context.Mangas
                .Include(m => m.Chapters)
                .FirstOrDefaultAsync(m => m.Id == id);
            return manga == null ? NotFound() : View(manga);
        }

        // Помощни методи за работа с файлове
        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "covers");
            Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return "/images/covers/" + uniqueFileName;
        }
        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var manga = await _context.Mangas.FindAsync(id);
            if (manga == null) return NotFound();
            return View(manga);
        }

        [HttpPost, ActionName("Delete")]//задавам му име в URL Delete, а не DeleteConfirmed за по-лесно
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var manga = await _context.Mangas.FindAsync(id);
            if (manga != null)
            {
                // 1. Първо изтриваме файла на снимката от сървъра
                DeleteFile(manga.CoverImageUrl);

                // 2. След това изтриваме записа от базата
                _context.Mangas.Remove(manga);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private void DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            string physicalPath = Path.Combine(_hostEnvironment.WebRootPath, relativePath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);
        }
        public IActionResult Catalog(string searchString)
        {
            var mangas = _context.Mangas.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                mangas = mangas.Where(m => m.Title.Contains(searchString) || m.Author.Contains(searchString));
            }

            ViewData["CurrentSearch"] = searchString; // За да се запази текста в кутията, ако имаш търсачка и там
            return View(mangas.ToList());
        }
    }
}
    

