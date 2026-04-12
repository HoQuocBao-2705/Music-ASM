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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Song song, IFormFile? audioFile, IFormFile? imageFile)
        {
            // ❗ Validate file
            if (audioFile == null || audioFile.Length == 0)
            {
                ModelState.AddModelError("audioFile", "Vui lòng chọn file MP3");
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "Vui lòng chọn ảnh bìa");
            }

            // ❗ Validate dropdown
            if (song.ArtistId == 0)
            {
                ModelState.AddModelError("ArtistId", "Vui lòng chọn nghệ sĩ");
            }

            if (song.GenreId == 0)
            {
                ModelState.AddModelError("GenreId", "Vui lòng chọn thể loại");
            }

            // ❗ Gán mặc định ngày
            if (song.ReleaseDate == null)
            {
                song.ReleaseDate = DateOnly.FromDateTime(DateTime.Now);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);
                return View(song);
            }

            try
            {
                // upload audio
                var audioPath = await ProcessFileAsync(audioFile, "music");
                if (!string.IsNullOrEmpty(audioPath))
                {
                    song.FilePath = audioPath;
                }

                // upload image
                var imagePath = await ProcessFileAsync(imageFile, "images/songs");
                if (!string.IsNullOrEmpty(imagePath))
                {
                    song.CoverImageUrl = imagePath;
                }

                song.ListenCount = 0;

                _context.Songs.Add(song);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm bài hát thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);

                ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);

                return View(song);
            }
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

        // ✏️ POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Song song, IFormFile? audioFile, IFormFile? imageFile)
        {
            if (id != song.SongId) return NotFound();

            // ❗ Bỏ validation navigation
            ModelState.Remove("FilePath");
            ModelState.Remove("CoverImageUrl");
            ModelState.Remove("Artist");
            ModelState.Remove("Genre");
            ModelState.Remove("Album");
            ModelState.Remove("PlaylistSongs");

            // ========================
            // 🔒 RÀNG BUỘC KHÔNG ĐƯỢC ĐỂ TRỐNG
            // ========================

            // 1. Kiểm tra Title
            if (string.IsNullOrWhiteSpace(song.Title))
            {
                ModelState.AddModelError("Title", "Tên bài hát không được để trống");
            }

            // 2. Kiểm tra Duration
            if (song.Duration <= 0)
            {
                ModelState.AddModelError("Duration", "Thời lượng phải lớn hơn 0 giây");
            }

            // 3. Kiểm tra ArtistId
            if (song.ArtistId == 0)
            {
                ModelState.AddModelError("ArtistId", "Vui lòng chọn nghệ sĩ");
            }

            // 4. Kiểm tra GenreId
            if (song.GenreId == 0)
            {
                ModelState.AddModelError("GenreId", "Vui lòng chọn thể loại");
            }

            // 5. Kiểm tra ReleaseDate
            if (song.ReleaseDate == null)
            {
                ModelState.AddModelError("ReleaseDate", "Vui lòng chọn ngày phát hành");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);
                return View(song);
            }

            try
            {
                var existingSong = await _context.Songs.FindAsync(id);
                if (existingSong == null) return NotFound();

                // ========================
                // 📝 CẬP NHẬT THÔNG TIN VĂN BẢN
                // ========================
                existingSong.Title = song.Title.Trim();
                existingSong.ArtistId = song.ArtistId;
                existingSong.GenreId = song.GenreId;
                existingSong.Duration = song.Duration;
                existingSong.ReleaseDate = song.ReleaseDate;

                // ========================
                // 🎵 CẬP NHẬT FILE NHẠC
                // ========================
                if (audioFile != null && audioFile.Length > 0)
                {
                    // Kiểm tra định dạng file
                    if (audioFile.ContentType != "audio/mpeg" && !audioFile.FileName.EndsWith(".mp3"))
                    {
                        ModelState.AddModelError("audioFile", "Chỉ chấp nhận file MP3");
                        ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                        ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);
                        return View(song);
                    }

                    // Xóa file cũ
                    if (!string.IsNullOrEmpty(existingSong.FilePath))
                    {
                        var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, existingSong.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // Upload file mới
                    var newPath = await ProcessFileAsync(audioFile, "music");
                    if (string.IsNullOrEmpty(newPath))
                    {
                        ModelState.AddModelError("audioFile", "Upload file nhạc thất bại");
                        ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                        ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);
                        return View(song);
                    }
                    existingSong.FilePath = newPath;
                }
                else
                {
                    // Nếu không có file mới và file cũ cũng không tồn tại -> bắt buộc upload
                    if (string.IsNullOrEmpty(existingSong.FilePath))
                    {
                        ModelState.AddModelError("audioFile", "Vui lòng chọn file nhạc MP3");
                        ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                        ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);
                        return View(song);
                    }
                }

                // ========================
                // 🖼️ CẬP NHẬT ẢNH
                // ========================
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Kiểm tra định dạng ảnh
                    if (!imageFile.ContentType.StartsWith("image/"))
                    {
                        ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh (jpg, png, gif)");
                        ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                        ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);
                        return View(song);
                    }

                    // Xóa ảnh cũ
                    if (!string.IsNullOrEmpty(existingSong.CoverImageUrl))
                    {
                        var oldImgPath = Path.Combine(_webHostEnvironment.WebRootPath, existingSong.CoverImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImgPath))
                        {
                            System.IO.File.Delete(oldImgPath);
                        }
                    }

                    // Upload ảnh mới
                    var newImagePath = await ProcessFileAsync(imageFile, "images/songs");
                    if (string.IsNullOrEmpty(newImagePath))
                    {
                        ModelState.AddModelError("imageFile", "Upload ảnh thất bại");
                        ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                        ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);
                        return View(song);
                    }
                    existingSong.CoverImageUrl = newImagePath;
                }
                else
                {
                    // Nếu không có ảnh mới và ảnh cũ cũng không tồn tại -> bắt buộc upload
                    if (string.IsNullOrEmpty(existingSong.CoverImageUrl))
                    {
                        ModelState.AddModelError("imageFile", "Vui lòng chọn ảnh bìa");
                        ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                        ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);
                        return View(song);
                    }
                }

                // ========================
                // 💾 LƯU VÀO DATABASE
                // ========================
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật bài hát thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
                ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);
                return View(song);
            }
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

        // ===== EDIT ARTIST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditArtist(int ArtistId, string Name, string Bio, string AvatarUrl)
        {
            try
            {
                var artist = await _context.Artists.FindAsync(ArtistId);
                if (artist == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nghệ sĩ!";
                    return RedirectToAction("Artists");
                }

                if (string.IsNullOrWhiteSpace(Name))
                {
                    TempData["ErrorMessage"] = "Tên nghệ sĩ không được để trống!";
                    return RedirectToAction("Artists");
                }

                artist.Name = Name.Trim();
                artist.Bio = Bio;
                artist.AvatarUrl = AvatarUrl;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cập nhật nghệ sĩ {artist.Name} thành công!";
                return RedirectToAction("Artists");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Artists");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteArtist(int id)
        {
            try
            {
                var artist = await _context.Artists
                    .Include(a => a.Songs)
                    .FirstOrDefaultAsync(a => a.ArtistId == id);

                if (artist == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nghệ sĩ!";
                    return RedirectToAction("Artists");
                }

                // ✅ KIỂM TRA: Nghệ sĩ có bài hát không?
                if (artist.Songs != null && artist.Songs.Any())
                {
                    TempData["ErrorMessage"] = $"Không thể xóa nghệ sĩ vì có {artist.Songs.Count} bài hát liên quan. Vui lòng xóa hoặc chuyển bài hát sang nghệ sĩ khác trước!";
                    return RedirectToAction("Artists");
                }

                _context.Artists.Remove(artist);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Xóa nghệ sĩ {artist.Name} thành công!";
                return RedirectToAction("Artists");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa: {ex.Message}";
                return RedirectToAction("Artists");
            }
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

        // ===== EDIT GENRE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGenre(int GenreId, string Name, string Description, string ImageUrl)
        {
            try
            {
                var genre = await _context.Genres.FindAsync(GenreId);
                if (genre == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thể loại!";
                    return RedirectToAction("Genres");
                }

                if (string.IsNullOrWhiteSpace(Name))
                {
                    TempData["ErrorMessage"] = "Tên thể loại không được để trống!";
                    return RedirectToAction("Genres");
                }

                genre.Name = Name.Trim();
                genre.Description = Description;
                genre.ImageUrl = ImageUrl;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cập nhật thể loại {genre.Name} thành công!";
                return RedirectToAction("Genres");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Genres");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            try
            {
                var genre = await _context.Genres
                    .Include(g => g.Songs)
                    .FirstOrDefaultAsync(g => g.GenreId == id);

                if (genre == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thể loại!";
                    return RedirectToAction("Genres");
                }

                // ✅ KIỂM TRA: Thể loại có bài hát không?
                if (genre.Songs != null && genre.Songs.Any())
                {
                    TempData["ErrorMessage"] = $"Không thể xóa thể loại vì có {genre.Songs.Count} bài hát liên quan. Vui lòng xóa hoặc chuyển bài hát sang thể loại khác trước!";
                    return RedirectToAction("Genres");
                }

                _context.Genres.Remove(genre);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Xóa thể loại {genre.Name} thành công!";
                return RedirectToAction("Genres");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa: {ex.Message}";
                return RedirectToAction("Genres");
            }
        }
    }
}