using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    }
}