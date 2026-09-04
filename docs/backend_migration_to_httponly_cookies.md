# Hướng dẫn chuyển đổi lưu trữ Token sang HttpOnly Cookie cho Backend

Tài liệu này mô tả chi tiết các bước cần thực hiện ở phía Backend (.NET Core) để chuyển đổi cơ chế lưu trữ `accessToken` và `refreshToken` từ phía Client (Local Storage) sang **HttpOnly Cookie**.

Mục đích của việc này là để chống lại các cuộc tấn công XSS (Cross-Site Scripting), bảo vệ an toàn tuyệt đối cho token xác thực của người dùng.

## 1. Cấu hình JWT Middleware để đọc Token từ Cookie

Trong file cấu hình dịch vụ (`ServiceConfiguration.cs` hoặc `Program.cs`), khi đăng ký `AddJwtBearer`, cần thêm logic để middleware lấy Token từ Cookie thay vì chỉ lấy từ header `Authorization: Bearer`.

**File cần sửa:** `ServiceConfiguration.cs` (hoặc nơi cấu hình `AddJwtBearer`)

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        // 1. Đọc Access Token từ Cookie
        if (context.Request.Cookies.TryGetValue("accessToken", out var cookieToken))
        {
            context.Token = cookieToken;
        }

        // 2. Giữ nguyên logic cũ cho SignalR nếu có (đọc từ query string)
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/notifications"))
        {
            context.Token = accessToken;
        }
        
        return Task.CompletedTask;
    }
};
```

## 2. Thay đổi API Login và các API trả về Token (`AuthController.cs`)

Thay vì chỉ trả về `accessToken` và `refreshToken` trong Body của API `LoginResponse`, Backend phải trực tiếp set các token này vào **Http Cookie** của trình duyệt.

**File cần sửa:** `AuthController.cs` (Cập nhật hàm `HandleResult` hoặc các API `Login`, `Verify2Fa`, v.v.)

```csharp
private IActionResult HandleResult(AuthResult result)
{
    if (result.IsSuccess && result.Response != null)
    {
        // Set Access Token Cookie
        if (!string.IsNullOrEmpty(result.Response.AccessToken))
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Quan trọng: JS không thể đọc được
                Secure = Request.IsHttps, // Yêu cầu HTTPS (khi dev có thể false nếu chạy HTTP)
                SameSite = SameSiteMode.Lax, // Hoặc Strict tuỳ thuộc kiến trúc FE/BE
                Expires = DateTime.UtcNow.AddMinutes(180) // Khớp với thời gian sống của JWT
            };
            Response.Cookies.Append("accessToken", result.Response.AccessToken, cookieOptions);
        }
        
        // Set Refresh Token Cookie
        if (!string.IsNullOrEmpty(result.Response.RefreshToken))
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", result.Response.RefreshToken, cookieOptions);
        }
        
        return Ok(result.Response);
    }
    
    // ... logic xử lý lỗi giữ nguyên
}
```

*Lưu ý:* Các API `Enable2Fa` và `Verify2Fa` đang đọc Token tạm thời từ header `Authorization`. Bạn cần bổ sung thêm logic đọc từ Cookie nếu header trống:

```csharp
var authHeader = Request.Headers["Authorization"].ToString();
if (string.IsNullOrEmpty(authHeader) && Request.Cookies.TryGetValue("accessToken", out var cookieToken))
{
    authHeader = $"Bearer {cookieToken}";
}
var result = await authService.Enable2FaAsync(request, authHeader);
```

## 3. Cập nhật API Refresh Token

API Refresh Token hiện tại có thể đang nhận token từ Body. Bạn cần cập nhật để API đọc `refreshToken` từ Cookie.

```csharp
[HttpPost("refresh")]
public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request)
{
    // Ưu tiên đọc từ request body (để tương thích ngược), sau đó đọc từ Cookie
    var token = request?.RefreshToken;
    if (string.IsNullOrEmpty(token))
    {
        token = Request.Cookies["refreshToken"];
    }

    if (string.IsNullOrEmpty(token))
    {
        return Unauthorized(new { Message = "Refresh token is missing" });
    }

    var result = await authService.RefreshAsync(new RefreshRequest { RefreshToken = token });
    return HandleResult(result); // HandleResult sẽ tự động ghi đè Cookie mới
}
```

## 4. Xử lý API Logout

Khi người dùng đăng xuất, bên cạnh việc xoá Refresh Token trong Database, bạn **phải xoá** 2 cookie này trên trình duyệt của người dùng.

```csharp
[Microsoft.AspNetCore.Authorization.Authorize]
[HttpPost("logout")]
public async Task<IActionResult> Logout()
{
    var username = User.Identity?.Name;
    if (string.IsNullOrEmpty(username)) return Unauthorized();

    var result = await authService.LogoutAsync(username);
    
    // Xoá cookie
    Response.Cookies.Delete("accessToken");
    Response.Cookies.Delete("refreshToken");

    return HandleResult(result);
}
```

## 5. Cấu hình CORS (Cross-Origin Resource Sharing)

Để trình duyệt (Frontend) có thể gửi Cookie đính kèm với các request API, CORS ở Backend phải cho phép thông qua `AllowCredentials()`.

**Kiểm tra lại `ServiceConfiguration.cs`:**
```csharp
services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://your-frontend-domain.com") // KHÔNG ĐƯỢC dùng AllowAnyOrigin() khi có AllowCredentials
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Bắt buộc phải có
    });
});
```

---
**Tổng kết:** Sau khi Backend thực hiện các thay đổi này, Frontend chỉ cần cấu hình Axios thêm `withCredentials: true` và xoá toàn bộ code xử lý/lưu trữ token trong LocalStorage là hoàn tất chuyển đổi.
