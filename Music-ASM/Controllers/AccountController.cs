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
            // Nếu đã đăng nhập rồi thì đá về trang chủ
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
            // Tìm user trong Database và lấy kèm thông tin Role
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);

            if (user != null)
            {
                // Khởi tạo các Claims (Thông tin lưu trữ trong phiên đăng nhập)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role.RoleName), // Phân quyền ít nhất 2 role ở đây
                    new Claim("Avatar", user.AvatarUrl ?? "/images/default-avatar.png")
                };

                // ... (phần code kiểm tra user ở trên giữ nguyên) ...

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // BỎ đoạn check Admin chuyển sang trang Admin.
                // Cho TẤT CẢ đều quay về Trang chủ sau khi đăng nhập thành công.
                return RedirectToAction("Index", "Home");
            }

            // Báo lỗi nếu sai tài khoản
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
            // Yêu cầu Y2: Validate dữ liệu - Kiểm tra xem tài khoản hoặc email đã tồn tại chưa
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
                FullName = fullname,
                Email = email,
                RoleId = 2,
                IsPremium = false,
                AvatarUrl = "/images/default-avatar.png",
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