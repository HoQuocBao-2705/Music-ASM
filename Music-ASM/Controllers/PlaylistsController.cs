using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music_ASM.Models;

namespace Music_ASM.Controllers
{
    public class PlaylistsController : Controller
    {
        private readonly MusicAsmDbContext _context;

        public PlaylistsController(MusicAsmDbContext context)
        {
            _context = context;
        }

        // 🔥 CHI TIẾT PLAYLIST
        public async Task<IActionResult> Details(int id)
        {
            var playlist = await _context.Playlists
                .Include(p => p.User)
                .Include(p => p.PlaylistSongs)
                    .ThenInclude(ps => ps.Song)
                        .ThenInclude(s => s.Artist)
                .FirstOrDefaultAsync(p => p.PlaylistId == id);

            if (playlist == null)
                return NotFound();

            return View(playlist);
        }
    }
}