using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music_ASM.Models;
using System.Security.Claims;

namespace Music_ASM.Controllers
{
    public class AccountController : Controller
    {
        private readonly MusicAsmDbContext _context;

        public AccountController(MusicAsmDbContext context)
        {
            _context = context;
        }

        // GET: Hiển thị form đăng nhập
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Xử lý dữ liệu đăng nhập
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);

            if (user != null)
            {
                // ⭐ Xử lý FullName: nếu là admin thì hiển thị "Admin", nếu không thì dùng FullName hoặc Username
                string fullName;
                if (user.Role?.RoleName == "Admin")
                {
                    fullName = "Admin";
                }
                else
                {
                    fullName = string.IsNullOrEmpty(user.FullName) ? user.Username : user.FullName;
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("FullName", fullName),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User"),
                    new Claim("Avatar", user.AvatarUrl ?? ""),
                    new Claim("IsPremium", user.IsPremium.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác!";
            return View();
        }

        // Xử lý đăng xuất
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // Trang thông báo không có quyền truy cập
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: Hiển thị form đăng ký
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Xử lý dữ liệu đăng ký
        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string fullname, string email)
        {
            // Kiểm tra xem tài khoản hoặc email đã tồn tại chưa
            bool isExists = await _context.Users.AnyAsync(u => u.Username == username || u.Email == email);
            if (isExists)
            {
                ViewBag.Error = "Tên đăng nhập hoặc Email đã tồn tại trong hệ thống!";
                return View();
            }

            // Tạo user mới (Mặc định gán RoleId = 2 cho User thường)
            var newUser = new User
            {
                Username = username,
                PasswordHash = password, // Lưu ý: Dự án thực tế nên mã hóa MD5/BCrypt
                FullName = fullname,     // ← Lưu họ tên đầy đủ
                Email = email,
                RoleId = 2,              // User role
                IsPremium = false,
                AvatarUrl = null,        // ← Để null để dùng avatar chữ cái đầu
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Dùng TempData để truyền thông báo thành công sang trang Login
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập để nghe nhạc.";
            return RedirectToAction("Login");
        }

        // GET: Hiển thị trang hồ sơ người dùng (ĐÃ ĐƯỢC CẬP NHẬT)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(userIdClaim);

            // Lấy thông tin user cùng với các thống kê
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Playlists) // Lấy thêm danh sách Playlists
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Đếm số bài hát yêu thích
            var favoriteCount = await _context.FavoriteSongs
                .CountAsync(f => f.UserId == userId);

            // Đếm tổng lượt nghe
            var totalListenCount = await _context.Set<ListeningHistory>()
                .CountAsync(h => h.UserId == userId);

            // Tổng thời gian nghe
            var totalListeningTime = await _context.Set<ListeningHistory>()
                .Where(h => h.UserId == userId)
                .SumAsync(h => (long?)h.Duration) ?? 0;

            // Gán dữ liệu vào ViewBag để đẩy sang View
            ViewBag.FavoriteCount = favoriteCount;
            ViewBag.TotalListenCount = totalListenCount;
            ViewBag.TotalListeningTime = totalListeningTime;

            return View(user);
        }

        // POST: Cập nhật thông tin hồ sơ
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string fullName, string email, IFormFile avatar)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin
            if (!string.IsNullOrEmpty(fullName))
            {
                user.FullName = fullName;
            }

            if (!string.IsNullOrEmpty(email))
            {
                user.Email = email;
            }

            // Xử lý upload avatar
            if (avatar != null && avatar.Length > 0)
            {
                // Tạo tên file duy nhất
                var fileName = $"avatar_{userId}_{DateTime.Now.Ticks}{Path.GetExtension(avatar.FileName)}";
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
                var filePath = Path.Combine(folderPath, fileName);

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                // Cập nhật AvatarUrl
                user.AvatarUrl = $"/images/avatars/{fileName}";
            }

            await _context.SaveChangesAsync();

            // Cập nhật lại claim FullName và Avatar
            await UpdateClaim("FullName", user.FullName);
            await UpdateClaim("Avatar", user.AvatarUrl ?? "");

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        // Helper: Cập nhật claim
        private async Task UpdateClaim(string claimType, string newValue)
        {
            var identity = (ClaimsIdentity)User.Identity;
            var oldClaim = identity.FindFirst(claimType);

            if (oldClaim != null)
            {
                identity.RemoveClaim(oldClaim);
            }

            identity.AddClaim(new Claim(claimType, newValue));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );
        }

        // GET: Trang nâng cấp Premium
        [HttpGet]
        [Authorize]
        public IActionResult UpgradePremium()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = _context.Users.Find(userId);

            if (user == null) return RedirectToAction("Login");

            // Tạo mã giao dịch ngẫu nhiên
            var transactionId = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            ViewBag.TransactionId = transactionId;
            ViewBag.Amount = "50,000";
            ViewBag.UserName = user.Username;

            return View(user);
        }

        // POST: Xử lý sau khi thanh toán thành công
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ConfirmUpgrade(string transactionId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            // Cập nhật premium
            user.IsPremium = true;
            await _context.SaveChangesAsync();

            // Cập nhật claim
            await UpdateClaim("IsPremium", "True");

            TempData["SuccessMessage"] = "Chúc mừng! Bạn đã trở thành thành viên Premium. 🎉";
            return RedirectToAction("Profile");
        }
    }
}