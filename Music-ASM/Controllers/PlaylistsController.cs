using Microsoft.AspNetCore.Authorization; // Cần thêm thư viện này
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music_ASM.Models;
using System.Security.Claims;

// Thêm [Authorize] để bắt buộc người dùng phải đăng nhập mới được vào các trang này
// Nếu không đăng nhập, hệ thống sẽ tự động đá về trang Login thay vì báo lỗi đỏ trang web
[Authorize]
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
            // 👇 THÊM 2 DÒNG NÀY ĐỂ FIX LỖI ẢNH BỊ VỠ 👇
            .Include(p => p.PlaylistSongs)
                .ThenInclude(ps => ps.Song)
            // Thêm AsNoTracking() giúp web chạy nhanh hơn vì chỉ đọc data mà không cần lưu lịch sử thay đổi
            .AsNoTracking()
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

        TempData["SuccessMessage"] = "Tạo playlist thành công!";
        return RedirectToAction("Index");
    }

    // 📄 Chi tiết playlist
    public async Task<IActionResult> Details(int id)
    {
        var playlist = await _context.Playlists
            .Include(p => p.PlaylistSongs)
                .ThenInclude(ps => ps.Song)
                    .ThenInclude(s => s.Artist)
            .AsNoTracking() // Tối ưu tốc độ đọc
            .FirstOrDefaultAsync(p => p.PlaylistId == id);

        if (playlist == null)
        {
            return NotFound(); // Trả về trang 404 nếu không tìm thấy playlist
        }

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

        // Lấy danh sách playlist của user (kèm theo bài hát)
        var playlists = await _context.Playlists
            .Where(p => p.UserId == userId)
            .Include(p => p.PlaylistSongs)
                .ThenInclude(ps => ps.Song)  // ← THÊM DÒNG NÀY để lấy thông tin bài hát
            .ToListAsync();  // Bỏ .AsNoTracking() nếu không cần

        ViewBag.SongId = songId;

        return View(playlists);
    }
    // 🗑️ Xóa bài hát khỏi playlist
    [HttpPost]
    public async Task<IActionResult> RemoveSong(int playlistId, int songId)
    {
        var playlistSong = await _context.PlaylistSongs
            .FirstOrDefaultAsync(ps => ps.PlaylistId == playlistId && ps.SongId == songId);

        if (playlistSong != null)
        {
            _context.PlaylistSongs.Remove(playlistSong);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa bài hát khỏi playlist!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không tìm thấy bài hát trong playlist!";
        }

        return RedirectToAction("Details", new { id = playlistId });
    }

    // 🗑️ Xóa toàn bộ playlist
    [HttpPost]
    public async Task<IActionResult> DeletePlaylist(int id)
    {
        // Lấy playlist cần xóa
        var playlist = await _context.Playlists
            .Include(p => p.PlaylistSongs)
            .FirstOrDefaultAsync(p => p.PlaylistId == id);

        if (playlist == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy playlist!";
            return RedirectToAction("Index");
        }

        // Xóa tất cả bài hát trong playlist trước
        _context.PlaylistSongs.RemoveRange(playlist.PlaylistSongs);

        // Xóa playlist
        _context.Playlists.Remove(playlist);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã xóa playlist thành công!";
        return RedirectToAction("Index");
    }
}