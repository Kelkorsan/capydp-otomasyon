using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Data;

/// <summary>
/// ASP.NET Core Data Protection anahtarlarının (oturum çerezi ve CSRF token şifreleme anahtarları)
/// canlı ortamda veritabanında kalıcı olarak saklandığı ayrı bağlam.
/// Ana <see cref="AppDbContext"/> modeline dahil edilmemiştir; böylece SQL Server migration geçmişi etkilenmez.
/// </summary>
public class AnahtarDeposuDbContext(DbContextOptions<AnahtarDeposuDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}
