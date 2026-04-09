using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.IO;
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
        public async Task<IActionResult> Delete(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null) return NotFound();

            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
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
        public IActionResult CreateArtist(Artist artist)
        {
            if (ModelState.IsValid)
            {
                _context.Artists.Add(artist);
                _context.SaveChanges();
                return RedirectToAction("Artists");
            }
            return View(artist);
        }

        // ===== GENRE =====

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
        public IActionResult CreateGenre(Genre genre)
        {
            if (ModelState.IsValid)
            {
                _context.Genres.Add(genre);
                _context.SaveChanges();
                return RedirectToAction("Genres");
            }
            return View(genre);
        }
    }
}