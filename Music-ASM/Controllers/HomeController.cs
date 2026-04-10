using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music_ASM.Models;
using System.Security.Claims;

namespace Music_ASM.Controllers
{
    public class HomeController : Controller
    {
        private readonly MusicAsmDbContext _context;

        public HomeController(MusicAsmDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 📁 Lấy danh sách playlist (có kèm bài hát đầu tiên để hiển thị ảnh bìa)
            var playlists = await _context.Playlists
                .Include(p => p.PlaylistSongs)
                    .ThenInclude(ps => ps.Song)
                .ToListAsync();

            // 🔥 Top Songs (Thịnh hành)
            var topSongs = await _context.Songs
                .Include(s => s.Artist)
                .OrderByDescending(s => s.ListenCount)
                .Take(10)
                .ToListAsync();

            // 🎵 Genres + Songs (Thể loại)
            var genres = await _context.Genres
                .Select(g => new
                {
                    g.GenreId,
                    g.Name,
                    Songs = _context.Songs
                        .Include(s => s.Artist)
                        .Where(s => s.GenreId == g.GenreId)
                        .Take(4)
                        .ToList()
                })
                .ToListAsync();

            var model = new HomeViewModel
            {
                Playlists = playlists,  // ← Gán danh sách playlist
                TopSongs = topSongs,
                Genres = genres.Cast<dynamic>().ToList()
            };

            return View(model);
        }

        // 🔥 API PLAY (AJAX)
        [AllowAnonymous]
        [HttpGet]
        public IActionResult PlaySong(int id)
        {
            var song = _context.Songs
                .Include(s => s.Artist)
                .FirstOrDefault(s => s.SongId == id);

            if (song == null) return NotFound();

            return Json(new
            {
                filePath = song.FilePath,
                title = song.Title,
                artist = song.Artist.Name,
                cover = song.CoverImageUrl
            });
        }
        [HttpGet]
        public async Task<IActionResult> Search(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return View(new List<Song>());
            }

            var songs = await _context.Songs
                .Include(s => s.Artist)
                .Include(s => s.Genre)
                .Where(s =>
                    s.Title.Contains(keyword) ||
                    s.Artist.Name.Contains(keyword) ||
                    s.Genre.Name.Contains(keyword)
                )
                .ToListAsync();

            ViewBag.Keyword = keyword;

            return View(songs);
        }
        public IActionResult Library()
        {
            var songs = _context.Songs
                .Include(s => s.Artist)   // ⚠️ BẮT BUỘC để lấy tên ca sĩ
                .ToList();

            return View(songs);
        }
        // API ghi nhận lượt nghe
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> TrackListening(int songId, int duration)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized();

                var userId = int.Parse(userIdClaim);

                // Kiểm tra user và song tồn tại
                var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
                var songExists = await _context.Songs.AnyAsync(s => s.SongId == songId);

                if (!userExists || !songExists)
                {
                    return BadRequest("User hoặc Song không tồn tại");
                }

                var history = new ListeningHistory
                {
                    UserId = userId,
                    SongId = songId,
                    ListenedAt = DateTime.Now,
                    Duration = duration
                };

                _context.ListeningHistory.Add(history);

                // Tăng lượt nghe cho bài hát
                var song = await _context.Songs.FindAsync(songId);
                if (song != null)
                {
                    song.ListenCount = (song.ListenCount ?? 0) + 1;
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message;
                Console.WriteLine($"Lỗi DB: {innerMessage}");
                return StatusCode(500, $"Lỗi database: {innerMessage}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }
    }
}