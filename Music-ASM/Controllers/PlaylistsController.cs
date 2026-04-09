using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music_ASM.Models;
using System.Security.Claims;

public class PlaylistController : Controller
{
    private readonly MusicAsmDbContext _context;

    public PlaylistController(MusicAsmDbContext context)
    {
        _context = context;
    }

    // 🎵 Danh sách playlist của user
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var playlists = await _context.Playlists
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return View(playlists);
    }

    // ➕ Tạo playlist (GET)
    public IActionResult Create()
    {
        return View();
    }

    // ➕ Tạo playlist (POST)
    [HttpPost]
    public async Task<IActionResult> Create(Playlist playlist)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        playlist.UserId = userId;

        _context.Playlists.Add(playlist);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    // 📄 Chi tiết playlist
    public async Task<IActionResult> Details(int id)
    {
        var playlist = await _context.Playlists
            .Include(p => p.PlaylistSongs)
                .ThenInclude(ps => ps.Song)
                    .ThenInclude(s => s.Artist)
            .FirstOrDefaultAsync(p => p.PlaylistId == id);

        return View(playlist);
    }

    // Thêm bài vào playlist (hỗ trợ cả GET và POST)
    [HttpPost]
    public async Task<IActionResult> AddSong(int playlistId, int songId)
    {
        var exist = await _context.PlaylistSongs
            .FirstOrDefaultAsync(x => x.PlaylistId == playlistId && x.SongId == songId);

        if (exist == null)
        {
            _context.PlaylistSongs.Add(new PlaylistSong
            {
                PlaylistId = playlistId,
                SongId = songId,
                AddedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(); // Trả về 200 OK
        }

        return Conflict("Bài hát đã có trong playlist"); // Trả về 409 nếu đã tồn tại
    }
    // Trong PlaylistController.cs
    public async Task<IActionResult> Choose(int songId)
    {
        // Lấy UserId từ người dùng đang đăng nhập
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Lấy danh sách playlist của user
        var playlists = await _context.Playlists
            .Where(p => p.UserId == userId)
            .Include(p => p.PlaylistSongs)
            .ToListAsync();

        ViewBag.SongId = songId;

        return View(playlists);
    }
}