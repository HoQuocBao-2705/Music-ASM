using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Music_ASM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSongsController : Controller
    {
        private readonly MusicAsmDbContext _context;

        public AdminSongsController(MusicAsmDbContext context)
        {
            _context = context;
        }

        // 📄 Danh sách
        public async Task<IActionResult> Index()
        {
            var songs = _context.Songs
                .Include(s => s.Artist)
                .Include(s => s.Genre);

            return View(await songs.ToListAsync());
        }

        // ➕ GET: Create
        public IActionResult Create()
        {
            ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name");
            ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name");

            return View();
        }

        // ➕ POST: Create
        [HttpPost]
        public async Task<IActionResult> Create(Song song)
        {
            if (ModelState.IsValid)
            {
                _context.Add(song);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(song);
        }

        // ✏️ Edit
        public async Task<IActionResult> Edit(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null) return NotFound();

            ViewBag.Artists = new SelectList(_context.Artists, "ArtistId", "Name", song.ArtistId);
            ViewBag.Genres = new SelectList(_context.Genres, "GenreId", "Name", song.GenreId);

            return View(song);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Song song)
        {
            if (id != song.SongId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(song);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(song);
        }

        // ❌ Delete
        public async Task<IActionResult> Delete(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null) return NotFound();

            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}