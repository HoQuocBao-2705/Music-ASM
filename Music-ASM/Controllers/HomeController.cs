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
    }
}