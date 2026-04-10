using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
namespace Music_ASM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSongsController : Controller
    {
        private readonly MusicAsmDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminSongsController(MusicAsmDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ===== HÀM XỬ LÝ UPLOAD FILE CHUNG =====
        private async Task<string> ProcessFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            // 1. Tạo tên file duy nhất để tránh trùng lặp
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var safeFileName = $"{originalFileName}_{DateTime.Now.Ticks}{extension}".Replace(" ", "_");

            // 2. Tạo đường dẫn lưu trữ tuyệt đối
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, folderName);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, safeFileName);

            // 3. Lưu file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{folderName}/{safeFileName}";
        }

        // 📄 Danh sách bài hát
        public async Task<IActionResult> Index()
        {
            var songs = _context.Songs
                .Include(s => s.Artist)
                .Include(s => s.Genre)
                .OrderByDescending(s => s.SongId);

            return View(await songs.ToListAsync());
        }

        // ➕ GET: Create
        public IActionResult Create()
        {
            ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name");
            ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Song song, IFormFile? audioFile, IFormFile? imageFile)
        {
            if (audioFile == null)
            {
                ModelState.AddModelError("audioFile", "Chưa chọn file mp3");
            }

            if (imageFile == null)
            {
                ModelState.AddModelError("imageFile", "Chưa chọn ảnh");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name");
                ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name");
                return View(song);
            }

            // upload audio
            var audioName = Guid.NewGuid() + Path.GetExtension(audioFile.FileName);
            var audioPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/music", audioName);

            using (var stream = new FileStream(audioPath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            song.FilePath = "/music/" + audioName;

            // upload image
            var imgName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
            var imgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/songs", imgName);

            using (var stream = new FileStream(imgPath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            song.CoverImageUrl = "/images/songs/" + imgName;

            _context.Songs.Add(song);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ✏️ GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null) return NotFound();

            ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
            ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);

            return View(song);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Song song, IFormFile? audioFile, IFormFile? imageFile)
        {
            if (id != song.SongId) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);
                return View(song);
            }

            // ===== UPDATE AUDIO =====
            if (audioFile != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(audioFile.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/music", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                song.FilePath = "/music/" + fileName;
            }

            // ===== UPDATE IMAGE =====
            if (imageFile != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/songs", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                song.CoverImageUrl = "/images/songs/" + fileName;
            }

            _context.Update(song);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ❌ Delete
        [HttpPost]
        [ValidateAntiForgeryToken]  // ← QUAN TRỌNG: Thêm dòng này
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var song = await _context.Songs
                    .Include(s => s.PlaylistSongs)
                    .FirstOrDefaultAsync(s => s.SongId == id);

                if (song == null) return NotFound();

                // Xóa file nhạc trên server
                if (!string.IsNullOrEmpty(song.FilePath))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, song.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // Xóa ảnh trên server
                if (!string.IsNullOrEmpty(song.CoverImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, song.CoverImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // Xóa các tham chiếu trong PlaylistSongs
                var playlistSongs = _context.PlaylistSongs.Where(ps => ps.SongId == id);
                _context.PlaylistSongs.RemoveRange(playlistSongs);

                // Xóa khỏi danh sách yêu thích
                var favSongs = _context.FavoriteSongs.Where(f => f.SongId == id);
                _context.FavoriteSongs.RemoveRange(favSongs);

                // Xóa khỏi lịch sử nghe nhạc
                var histories = _context.ListeningHistory.Where(h => h.SongId == id);
                _context.ListeningHistory.RemoveRange(histories);

                // Xóa bài hát
                _context.Songs.Remove(song);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa bài hát thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ===== ARTIST MANAGEMENT =====
        public IActionResult Artists()
        {
            var list = _context.Artists.ToList();
            return View(list);
        }

        public IActionResult CreateArtist()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateArtist(Artist artist)
        {
            if (ModelState.IsValid)
            {
                _context.Artists.Add(artist);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Thêm nghệ sĩ thành công!";
                return RedirectToAction("Artists");
            }
            return View(artist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteArtist(int id)
        {
            var artist = await _context.Artists.FindAsync(id);
            if (artist != null)
            {
                _context.Artists.Remove(artist);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa nghệ sĩ thành công!";
            }
            return RedirectToAction("Artists");
        }

        // ===== GENRE MANAGEMENT =====
        public IActionResult Genres()
        {
            var list = _context.Genres.ToList();
            return View(list);
        }

        public IActionResult CreateGenre()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGenre(Genre genre)
        {
            if (ModelState.IsValid)
            {
                _context.Genres.Add(genre);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Thêm thể loại thành công!";
                return RedirectToAction("Genres");
            }
            return View(genre);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre != null)
            {
                _context.Genres.Remove(genre);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa thể loại thành công!";
            }
            return RedirectToAction("Genres");
        }
    }
}   