using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Music_ASM.Models;

namespace Music_ASM.Controllers
{
    [Authorize] // Bắt buộc login
    public class SongsController : Controller
    {
        private readonly MusicAsmDbContext _context;

        public SongsController(MusicAsmDbContext context)
        {
            _context = context;
        }

        // =========================
        // 1. LIST (INDEX)
        // =========================
        public async Task<IActionResult> Index()
        {
            var songs = _context.Songs
                .Include(s => s.Artist)
                .Include(s => s.Genre)
                .Include(s => s.Album);

            return View(await songs.ToListAsync());
        }

        // =========================
        // 2. DETAILS
        // =========================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var song = await _context.Songs
                .Include(s => s.Artist)
                .Include(s => s.Genre)
                .Include(s => s.Album)
                .FirstOrDefaultAsync(m => m.SongId == id);

            if (song == null) return NotFound();

            return View(song);
        }

        // =========================
        // 3. CREATE (GET)
        // =========================
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        // =========================
        // 4. CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Song song)
        {
            if (ModelState.IsValid)
            {
                _context.Add(song);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns();
            return View(song);
        }

        // =========================
        // 5. EDIT (GET)
        // =========================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var song = await _context.Songs.FindAsync(id);
            if (song == null) return NotFound();

            LoadDropdowns();
            return View(song);
        }

        // =========================
        // 6. EDIT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Song song)
        {
            if (id != song.SongId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(song);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Songs.Any(e => e.SongId == song.SongId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns();
            return View(song);
        }

        // =========================
        // 7. DELETE (GET)
        // =========================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var song = await _context.Songs
                .Include(s => s.Artist)
                .FirstOrDefaultAsync(m => m.SongId == id);

            if (song == null) return NotFound();

            return View(song);
        }

        // =========================
        // 8. DELETE (POST)
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song != null)
            {
                _context.Songs.Remove(song);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // LOAD DROPDOWN
        // =========================
        private void LoadDropdowns()
        {
            ViewBag.GenreId = new SelectList(_context.Genres, "GenreId", "Name");
            ViewBag.ArtistId = new SelectList(_context.Artists, "ArtistId", "Name");
            ViewBag.AlbumId = new SelectList(_context.Albums, "AlbumId", "Title");
        }
    }
}