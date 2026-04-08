using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music_ASM.Models;
using Microsoft.AspNetCore.Authorization;

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
            var topSongs = await _context.Songs
                .Include(s => s.Artist)
                .OrderByDescending(s => s.ListenCount)
                .Take(10)
                .ToListAsync();

            ViewBag.Genres = await _context.Genres.ToListAsync();

            return View(topSongs);
        }

        // 🔥 API PLAY (AJAX)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PlaySong(int id)
        {
            // Tăng lượt nghe bằng SQL (chuẩn)
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Songs SET ListenCount = ListenCount + 1 WHERE SongId = {0}", id);

            var song = await _context.Songs
                .Include(s => s.Artist)
                .FirstOrDefaultAsync(s => s.SongId == id);

            if (song == null) return NotFound();

            return Json(new
            {
                songId = song.SongId,
                title = song.Title,
                artist = song.Artist.Name,
                filePath = song.FilePath,
                cover = song.CoverImageUrl,
                listenCount = song.ListenCount
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
    }
}