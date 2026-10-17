using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

var builder = WebApplication.CreateBuilder(args);

// 1) Veritabanı sağlayıcı seçimi:
// CAPYDP_DB_PROVIDER ortam değişkeni veya appsettings.json içindeki VeritabaniSaglayici kontrol edilir.
// "Otomatik" modda makinede SQL Server LocalDB kuruluysa MSSQL kullanılır; henüz kurulmamışsa
// geliştirme sürecinin kesintisiz devam etmesi için yerel SQLite veritabanına düşer.
var saglayiciAyari = Environment.GetEnvironmentVariable("CAPYDP_DB_PROVIDER")
                     ?? builder.Configuration["VeritabaniSaglayici"]
                     ?? "Otomatik";

var sqlServerKullan = saglayiciAyari.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
                      || (saglayiciAyari.Equals("Otomatik", StringComparison.OrdinalIgnoreCase) && LocalDbKuruluMu());

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (sqlServerKullan)
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
    else
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite"));
    }
});

// 2) ASP.NET Core Identity ve Güvenlik Politikası (Rapor Bölüm 3 & 12)
// Self-register (öğrencinin kendi kendine kayıt olması) kapalıdır; hesapları yalnızca Koordinatör açar.
builder.Services
    .AddIdentity<Kullanici, IdentityRole<int>>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // Şifre politikası
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Brute-force koruması: 5 hatalı denemede hesap 15 dakika kilitlenir
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// 3) Oturum çerezi (Cookie) güvenliği:
// HttpOnly: XSS ile JS üzerinden document.cookie okunmasını engeller.
// SameSite=Lax: CSRF saldırılarına karşı ek savunma katmanı sağlar.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "CapYdp.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Hesap/Giris";
    options.LogoutPath = "/Hesap/Cikis";
    options.AccessDeniedPath = "/Hesap/ErisimEngellendi";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// 4) Rol bazlı yetkilendirme politikaları (Rapor Bölüm 3.2)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politikalar.KoordinatorPolitikasi, p => p.RequireRole(Roller.Koordinator))
    .AddPolicy(Politikalar.OgrenciPolitikasi, p => p.RequireRole(Roller.Ogrenci))
    .AddPolicy(Politikalar.BolumBaskaniPolitikasi, p => p.RequireRole(Roller.BolumBaskani, Roller.Koordinator));

// 5) Razor Pages klasör bazlı yetkilendirme kuralları:
// Tek tek sayfalarda [Authorize] unutulsa bile klasör seviyesinde yetki zorunlu tutulur (Default-Deny yaklaşımı).
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Koordinator", Politikalar.KoordinatorPolitikasi);
    options.Conventions.AuthorizeFolder("/OgrenciPaneli", Politikalar.OgrenciPolitikasi);
    options.Conventions.AuthorizeFolder("/BolumBaskani", Politikalar.BolumBaskaniPolitikasi);
    options.Conventions.AllowAnonymousToFolder("/Hesap");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Temel güvenlik başlıkları (Rapor Bölüm 12 — XSS / Clickjacking / MIME sniffing önlemleri)
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Veritabanı şemasını hazırla ve demo/protokol verilerini yükle
using (var kapsam = app.Services.CreateScope())
{
    var db = kapsam.ServiceProvider.GetRequiredService<AppDbContext>();
    if (sqlServerKullan)
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    await VeriBaslatici.BaslatAsync(app.Services);
}

app.Run();

static bool LocalDbKuruluMu()
{
    if (!OperatingSystem.IsWindows())
    {
        return false;
    }

    using var anahtar = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions");
    return anahtar is not null && anahtar.GetSubKeyNames().Length > 0;
}
