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

        // GET: Chapter/Index/5 (Show chapters for specific manga)
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

        // GET: Chapter/Create?mangaId=5
        public IActionResult Create(int mangaId)
        {
            ViewBag.MangaId = mangaId; // Important for the Hidden Input
            return View();
        }

        // POST: Chapter/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chapter chapter, List<IFormFile> chapterImages)
        {
            if (ModelState.IsValid)
            {
                chapter.DateAdded = DateTime.Now;
                _context.Add(chapter);
                await _context.SaveChangesAsync(); // First Save to get ID

                if (chapterImages != null && chapterImages.Count > 0)
                {
                    string chapterFolder = Path.Combine(_hostEnvironment.WebRootPath, "files", "chapters", chapter.Id.ToString());
                    if (!Directory.Exists(chapterFolder))
                    {
                        Directory.CreateDirectory(chapterFolder);
                    }

                    List<string> savedImagePaths = new List<string>();
                    
                    // Sorting by filename to ensure correct order if user selected multiple files
                    int index = 1;
                    foreach (var file in chapterImages.OrderBy(f => f.FileName))
                    {
                        if (file.Length > 0)
                        {
                            // Renaming sequentially: 001.jpg, 002.jpg...
                            string extension = Path.GetExtension(file.FileName);
                            string newFileName = $"{index:D3}{extension}";
                            string filePath = Path.Combine(chapterFolder, newFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            savedImagePaths.Add($"/files/chapters/{chapter.Id}/{newFileName}");
                            index++;
                        }
                    }

                    // Serialize to JSON and update the chapter
                    if (savedImagePaths.Any())
                    {
                        chapter.ImagePathsJson = System.Text.Json.JsonSerializer.Serialize(savedImagePaths);
                        _context.Update(chapter);
                        await _context.SaveChangesAsync(); // Second Save for the JSON
                    }
                }

                return RedirectToAction(nameof(Index), new { mangaId = chapter.MangaId });
            }
            ViewBag.MangaId = chapter.MangaId;
            return View(chapter);
        }

        // GET: Chapter/Edit/5
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

            // Deserialize JSON to get image paths for the view
            List<string> imagePaths = new List<string>();
            if (!string.IsNullOrEmpty(chapter.ImagePathsJson))
            {
                try 
                {
                    var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(chapter.ImagePathsJson);
                    if (paths != null) imagePaths = paths;
                }
                catch {}
            }
            ViewBag.ImagePaths = imagePaths;

            return View(chapter);
        }

        // POST: Chapter/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,MangaId,DateAdded,ImagePathsJson")] Chapter chapter, List<string>? imagesToDelete, List<IFormFile>? newImages)
        {
            if (id != chapter.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Get existing JSON from DB context (to ensure we work with current state, though binding has it too)
                    // Actually, we should merge.
                    
                    // Deserialize current paths
                    List<string> currentPaths = new List<string>();
                    if (!string.IsNullOrEmpty(chapter.ImagePathsJson))
                    {
                        try 
                        {
                            var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(chapter.ImagePathsJson);
                            if (paths != null) currentPaths = paths;
                        }
                        catch {}
                    }

                    // 2. Handle Deletions
                    if (imagesToDelete != null && imagesToDelete.Any())
                    {
                        foreach (var pathToDelete in imagesToDelete)
                        {
                            if (currentPaths.Contains(pathToDelete))
                            {
                                currentPaths.Remove(pathToDelete);
                                
                                // Delete physical file
                                string physicalPath = Path.Combine(_hostEnvironment.WebRootPath, pathToDelete.TrimStart('/'));
                                if (System.IO.File.Exists(physicalPath))
                                {
                                    System.IO.File.Delete(physicalPath);
                                }
                            }
                        }
                    }

                    // 3. Handle New Images
                    if (newImages != null && newImages.Count > 0)
                    {
                         string chapterFolder = Path.Combine(_hostEnvironment.WebRootPath, "files", "chapters", chapter.Id.ToString());
                        if (!Directory.Exists(chapterFolder))
                        {
                            Directory.CreateDirectory(chapterFolder);
                        }

                        // Determine start index for naming based on highest existing number
                        int nextIndex = 1;
                        if (currentPaths.Any())
                        {
                           // Extract numbers from filenames: 005.jpg -> 5
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

                    // 4. Update JSON
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

        // GET: Chapter/Delete/5
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

        // POST: Chapter/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter != null)
            {
                // Delete physical files
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

            // Deserialize JSON to get image paths
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
                    // Fallback or log error if JSON is corrupt
                }
            }

            ViewBag.ImagePaths = imagePaths;
            return View(chapter);
        }
    }
}
