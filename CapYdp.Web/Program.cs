using System.Globalization;
using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Npgsql;

// Render/Railway gibi PaaS servisleri dinlenecek portu PORT ortam değişkeniyle bildirir.
// WebApplication.CreateBuilder çağrılmadan önce ASPNETCORE_HTTP_PORTS'a aktarılır.
var platformPortu = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(platformPortu))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", platformPortu);
}

// HTML5 <input type="number" step="0.01"> alanları (GANO, yüzlük puan notu) ondalık ayırıcı olarak her zaman '.' gönderir.
// Türkçe metin/tarih davranışı korunurken sayısal biçimlendirme '.' (Invariant) olarak sabitlenir.
var kultur = new CultureInfo("tr-TR")
{
    NumberFormat = CultureInfo.InvariantCulture.NumberFormat
};
CultureInfo.DefaultThreadCurrentCulture = kultur;
CultureInfo.DefaultThreadCurrentUICulture = kultur;

var builder = WebApplication.CreateBuilder(args);

// 1) Veritabanı sağlayıcı seçimi:
// CAPYDP_DB_PROVIDER ortam değişkeni veya appsettings.json içindeki VeritabaniSaglayici kontrol edilir.
// "Otomatik" modda makinede SQL Server LocalDB kuruluysa MSSQL kullanılır; henüz kurulmamışsa
// geliştirme sürecinin kesintisiz devam etmesi için yerel SQLite veritabanına düşer.
// Canlı ortam (Render/Railway + Neon): DATABASE_URL veya ConnectionStrings:PostgreSql tanımlıysa
// PostgreSQL kullanılır. Bağlantı bilgisi koda/depoya yazılmaz, yalnızca ortam değişkeninden okunur.
var postgresBaglanti = PostgresBaglantisiniCoz(
    Environment.GetEnvironmentVariable("DATABASE_URL") ?? builder.Configuration.GetConnectionString("PostgreSql"));

var saglayiciAyari = Environment.GetEnvironmentVariable("CAPYDP_DB_PROVIDER")
                     ?? (postgresBaglanti is not null ? "PostgreSql" : null)
                     ?? builder.Configuration["VeritabaniSaglayici"]
                     ?? "Otomatik";

var postgresKullan = saglayiciAyari.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase);
var sqlServerKullan = !postgresKullan
                      && (saglayiciAyari.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
                          || (saglayiciAyari.Equals("Otomatik", StringComparison.OrdinalIgnoreCase) && LocalDbKuruluMu()));

if (postgresKullan && postgresBaglanti is null)
{
    throw new InvalidOperationException("PostgreSql seçildi ancak DATABASE_URL / ConnectionStrings:PostgreSql tanımlı değil.");
}

// Npgsql 6+ yalnızca Kind=Utc DateTime kabul eder; mevcut kod tabanıyla birebir uyum için
// tarih alanları 'timestamp without time zone' olarak eşlenir (değerler zaten UTC tutuluyor).
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (postgresKullan)
    {
        options.UseNpgsql(postgresBaglanti);
    }
    else if (sqlServerKullan)
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
    else
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite"));
    }
});

// Canlı ortamda konteyner her yeniden başladığında (deploy / uyku modundan uyanma) oturum çerezlerini
// ve CSRF token'larını şifreleyen Data Protection anahtarları kaybolmasın diye veritabanında saklanır.
if (postgresKullan)
{
    builder.Services.AddDbContext<AnahtarDeposuDbContext>(options => options.UseNpgsql(postgresBaglanti));
    builder.Services.AddDataProtection()
        .SetApplicationName("CapYdp.Web")
        .PersistKeysToDbContext<AnahtarDeposuDbContext>();
}

// Ters vekil sunucu (Render/Railway yük dengeleyicisi) arkasında gerçek istemci IP'si ve HTTPS
// bilgisinin korunması için: denetim izindeki IP adresi ve HTTPS yönlendirmesi doğru çalışır.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
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

// Vekil sunucu başlıkları diğer tüm middleware'lerden önce işlenmelidir.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// İç ağdan gelen düz HTTP sağlık kontrollerini (/saglik) yönlendirmeden muaf tut.
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/saglik"),
    altUygulama => altUygulama.UseHttpsRedirection());

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

// Sağlık kontrolü: PaaS servisinin health check'i ve uyku modunu engelleyen ping servisleri için.
// Yalnızca veritabanı erişilebilirliğini döndürür; hassas bilgi içermez.
app.MapGet("/saglik", async (AppDbContext db) =>
        await db.Database.CanConnectAsync()
            ? Results.Ok(new { durum = "calisiyor", veritabani = "bagli" })
            : Results.Json(new { durum = "hata", veritabani = "baglanamadi" }, statusCode: 503))
   .AllowAnonymous();

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
        // SQLite ve PostgreSQL (Neon): mevcut migration'lar SQL Server'a özgü olduğundan şema modelden üretilir.
        await db.Database.EnsureCreatedAsync();
    }

    if (postgresKullan)
    {
        var anahtarDb = kapsam.ServiceProvider.GetRequiredService<AnahtarDeposuDbContext>();
        await anahtarDb.Database.ExecuteSqlRawAsync(
            """CREATE TABLE IF NOT EXISTS "DataProtectionKeys" ("Id" serial PRIMARY KEY, "FriendlyName" text NULL, "Xml" text NULL);""");
    }

    await VeriBaslatici.BaslatAsync(app.Services);
}

app.Run();

// Neon/Railway "postgresql://kullanici:sifre@host/veritabani?sslmode=require" biçiminde URI verir;
// Npgsql ise anahtar=değer biçimi beklediği için dönüştürülür.
static string? PostgresBaglantisiniCoz(string? ham)
{
    if (string.IsNullOrWhiteSpace(ham))
    {
        return null;
    }

    if (!ham.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        && !ham.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return ham;
    }

    var uri = new Uri(ham);
    var kullaniciBilgisi = uri.UserInfo.Split(':', 2);
    var csb = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = Uri.UnescapeDataString(kullaniciBilgisi[0]),
        Password = kullaniciBilgisi.Length > 1 ? Uri.UnescapeDataString(kullaniciBilgisi[1]) : null,
        SslMode = SslMode.Require,
        // Neon sunucusuz (auto-suspend) yapısı için: soğuk uyanma toleransı ve 5 dk uyku öncesi boşta bağlantı temizliği
        Timeout = 30,
        CommandTimeout = 30,
        KeepAlive = 30,
        ConnectionIdleLifetime = 60,
        ConnectionPruningInterval = 10
    };

    foreach (var parca in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var kv = parca.Split('=', 2);
        if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<SslMode>(kv[1], ignoreCase: true, out var sslMode))
        {
            csb.SslMode = sslMode;
        }
    }

    return csb.ConnectionString;
}

static bool LocalDbKuruluMu()
{
    if (!OperatingSystem.IsWindows())
    {
        return false;
    }

    using var anahtar = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions");
    return anahtar is not null && anahtar.GetSubKeyNames().Length > 0;
}
