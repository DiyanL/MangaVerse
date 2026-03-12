using MangaVerse.Data;
using MangaVerse.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangaVerse.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ChapterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ChapterController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Chapter/Index/5 (Показва главите за конкретна манга)
        public async Task<IActionResult> Index(int mangaId)
        {
            var manga = await _context.Mangas
                .Include(m => m.Chapters)
                .FirstOrDefaultAsync(m => m.Id == mangaId);

            if (manga == null)
            {
                return NotFound();
            }

            ViewBag.MangaId = mangaId;
            ViewBag.MangaTitle = manga.Title;
            return View(manga.Chapters.OrderBy(c => c.Id).ToList());
        }

        public IActionResult Create(int mangaId)
        {
            ViewBag.MangaId = mangaId; // Важно за скрития входен елемент (Hidden Input)
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chapter chapter, List<IFormFile> chapterImages)
        {
            if (ModelState.IsValid)
            {
                chapter.DateAdded = DateTime.Now;
                _context.Add(chapter);
                await _context.SaveChangesAsync(); // Първо записване, за да се получи ID

                if (chapterImages != null && chapterImages.Count > 0)
                {
                    string chapterFolder = Path.Combine(_hostEnvironment.WebRootPath, "files", "chapters", chapter.Id.ToString());
                    if (!Directory.Exists(chapterFolder))
                    {
                        Directory.CreateDirectory(chapterFolder);
                    }

                    List<string> savedImagePaths = new List<string>();

                    for (int i = 0; i < chapterImages.Count; i++)
                    {
                        var file = chapterImages[i];
                        if (file.Length > 0)
                        {
                            string extension = Path.GetExtension(file.FileName);
                            string fileName = $"{(i + 1):D3}{extension}";
                            string filePath = Path.Combine(chapterFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            savedImagePaths.Add($"/files/chapters/{chapter.Id}/{fileName}");
                        }
                    }

                    // Сериализиране към JSON и обновяване на главата
                    if (savedImagePaths.Any())
                    {
                        chapter.ImagePathsJson = System.Text.Json.JsonSerializer.Serialize(savedImagePaths);
                        _context.Update(chapter);
                        await _context.SaveChangesAsync(); // Второ записване за JSON-а
                    }
                }

                return RedirectToAction(nameof(Index), new { mangaId = chapter.MangaId });
            }
            ViewBag.MangaId = chapter.MangaId;
            return View(chapter);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter == null)
            {
                return NotFound();
            }

            // Десериализиране на JSON, за да се вземат пътищата към изображенията за изгледа
            List<string> imagePaths = new List<string>();
            if (!string.IsNullOrEmpty(chapter.ImagePathsJson))
            {
                try
                {
                    var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(chapter.ImagePathsJson);
                    if (paths != null) imagePaths = paths;
                }
                catch { }
            }
            ViewBag.ImagePaths = imagePaths;

            return View(chapter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Chapter chapter, List<string>? imagesToDelete, List<IFormFile>? newImages)
        {
            if (id != chapter.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Вземане на съществуващия JSON от контекста на БД (за да сме сигурни, че работим с текущото състояние)
                    // Десериализиране на текущите пътища
                    List<string> currentPaths = new List<string>();
                    if (!string.IsNullOrEmpty(chapter.ImagePathsJson))
                    {
                        try
                        {
                            var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(chapter.ImagePathsJson);
                            if (paths != null) currentPaths = paths;
                        }
                        catch { }
                    }

                    // 2. Обработка на изтриванията
                    if (imagesToDelete != null && imagesToDelete.Any())
                    {
                        foreach (var pathToDelete in imagesToDelete)
                        {
                            if (currentPaths.Contains(pathToDelete))
                            {
                                currentPaths.Remove(pathToDelete);

                                // Изтриване на физическия файл
                                string physicalPath = Path.Combine(_hostEnvironment.WebRootPath, pathToDelete.TrimStart('/'));
                                if (System.IO.File.Exists(physicalPath))
                                {
                                    System.IO.File.Delete(physicalPath);
                                }
                            }
                        }
                    }

                    // 3. Обработка на нови изображения
                    if (newImages != null && newImages.Count > 0)
                    {
                        string chapterFolder = Path.Combine(_hostEnvironment.WebRootPath, "files", "chapters", chapter.Id.ToString());
                        if (!Directory.Exists(chapterFolder))
                        {
                            Directory.CreateDirectory(chapterFolder);
                        }

                        // Определяне на начален индекс за именуване въз основа на най-високия съществуващ номер
                        int nextIndex = 1;
                        if (currentPaths.Any())
                        {
                            // Извличане на номера от имената на файловете: 005.jpg -> 5
                            var maxIndex = currentPaths
                                .Select(p => Path.GetFileNameWithoutExtension(p))
                                .Where(n => int.TryParse(n, out _))
                                .Select(n => int.Parse(n))
                                .DefaultIfEmpty(0)
                                .Max();
                            nextIndex = maxIndex + 1;
                        }

                        foreach (var file in newImages.OrderBy(f => f.FileName))
                        {
                            if (file.Length > 0)
                            {
                                string extension = Path.GetExtension(file.FileName);
                                string newFileName = $"{nextIndex:D3}{extension}";
                                string filePath = Path.Combine(chapterFolder, newFileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                currentPaths.Add($"/files/chapters/{chapter.Id}/{newFileName}");
                                nextIndex++;
                            }
                        }
                    }

                    // 4. Обновяване на JSON
                    chapter.ImagePathsJson = System.Text.Json.JsonSerializer.Serialize(currentPaths);

                    _context.Update(chapter);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChapterExists(chapter.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { mangaId = chapter.MangaId });
            }
            return View(chapter);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chapter = await _context.Chapters
                .Include(c => c.Manga)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chapter == null)
            {
                return NotFound();
            }

            return View(chapter);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter != null)
            {
                // Изтриване на физическите файлове
                string chapterFolder = Path.Combine(_hostEnvironment.WebRootPath, "files", "chapters", chapter.Id.ToString());
                if (Directory.Exists(chapterFolder))
                {
                    Directory.Delete(chapterFolder, true);
                }

                _context.Chapters.Remove(chapter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { mangaId = chapter.MangaId });
            }
            return RedirectToAction(nameof(Index), new { mangaId = chapter?.MangaId ?? 0 });
        }

        private bool ChapterExists(int id)
        {
            return _context.Chapters.Any(e => e.Id == id);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Read(int id)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Manga)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chapter == null) return NotFound();

            // Десериализиране на JSON, за да се вземат пътищата към изображенията
            List<string> imagePaths = new List<string>();
            if (!string.IsNullOrEmpty(chapter.ImagePathsJson))
            {
                try
                {
                    var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(chapter.ImagePathsJson);
                    if (paths != null) imagePaths = paths;
                }
                catch
                {
                    // Връщане към стандартно състояние или логване на грешка, ако JSON-ът е повреден
                }
            }

            ViewBag.ImagePaths = imagePaths;
            return View(chapter);
        }
    }
}