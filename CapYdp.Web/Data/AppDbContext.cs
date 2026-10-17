using System.Security.Claims;
using System.Text.Json;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CapYdp.Web.Data;

/// <summary>
/// Ana veritabanı bağlamı.
/// - ASP.NET Core Identity tablolarını ve alan varlıklarını barındırır.
/// - Soft-delete prensibini zorlar (IDenetlenebilir varlıkların silinmesini engeller).
/// - SaveChangesAsync sırasında tüm değişiklikleri otomatik olarak IslemKaydi tablosuna yazar (Rapor Bölüm 11).
/// </summary>
public class AppDbContext : IdentityDbContext<Kullanici, IdentityRole<int>, int>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private string? _bekleyenAciklama;
    private string? _bekleyenIslemTuru;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Ogrenci> Ogrenciler => Set<Ogrenci>();
    public DbSet<Ders> Dersler => Set<Ders>();
    public DbSet<MuafiyetKurali> MuafiyetKurallari => Set<MuafiyetKurali>();
    public DbSet<AkademikDonem> AkademikDonemler => Set<AkademikDonem>();
    public DbSet<OgrenciDersListesi> OgrenciDersListeleri => Set<OgrenciDersListesi>();
    public DbSet<DersKayit> DersKayitlari => Set<DersKayit>();
    public DbSet<IslemKaydi> IslemKayitlari => Set<IslemKaydi>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Rapordaki veri tasarımıyla uyumlu olarak tüm enum'lar veritabanında metin (nvarchar) olarak saklanır.
        configurationBuilder.Properties<ProgramTuru>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<OgrenciDurumu>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<DersTuru>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<DonemTipi>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<DersListesiDurumu>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<OnayDurumu>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<DersDurumu>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Benzersiz anahtarlar (Rapor Bölüm 5.1)
        builder.Entity<Ogrenci>(b =>
        {
            b.HasIndex(o => o.OgrenciNo).IsUnique();
            b.HasIndex(o => o.KullaniciId).IsUnique();
        });

        builder.Entity<Ders>(b =>
        {
            b.HasIndex(d => d.DersKodu).IsUnique();
        });

        builder.Entity<AkademikDonem>(b =>
        {
            b.HasIndex(d => new { d.Yil, d.DonemTipi }).IsUnique();
        });

        builder.Entity<OgrenciDersListesi>(b =>
        {
            b.HasIndex(x => new { x.OgrenciId, x.DersId }).IsUnique();
        });

        builder.Entity<IslemKaydi>(b =>
        {
            b.HasIndex(i => i.Tarih);
            b.HasIndex(i => new { i.TabloAdi, i.KayitId });
        });

        // Rapor Bölüm 5.2: Cascade delete kullanılmaz; ilişkisel bütünlük Restrict ile korunur.
        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    /// <summary>
    /// İşlem gerekçesi ve özel işlem türü (Onay, Red, Iptal vb.) belirterek kaydetmek için yardımcı metot.
    /// </summary>
    public async Task<int> KaydetAsync(string? aciklama = null, string? islemTuru = null, CancellationToken cancellationToken = default)
    {
        _bekleyenAciklama = aciklama;
        _bekleyenIslemTuru = islemTuru;
        try
        {
            return await SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _bekleyenAciklama = null;
            _bekleyenIslemTuru = null;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        // 1) Denetim izi kayıtlarının değiştirilmesi veya silinmesi kesinlikle yasaktır.
        var bozukDenetimKaydi = ChangeTracker.Entries<IslemKaydi>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);
        if (bozukDenetimKaydi)
        {
            throw new InvalidOperationException("Denetim izi (IslemKaydi) kayıtları değiştirilemez ve silinemez.");
        }

        // 2) Soft-delete prensibi: IDenetlenebilir varlıklar fiziksel olarak silinemez.
        var silinenVarlik = ChangeTracker.Entries<IDenetlenebilir>()
            .FirstOrDefault(e => e.State == EntityState.Deleted);
        if (silinenVarlik is not null)
        {
            throw new InvalidOperationException(
                $"Soft-delete kuralı gereği '{silinenVarlik.Entity.GetType().Name}' tablosundan fiziksel kayıt silinemez. Kaydı pasife alın veya iptal edin.");
        }

        // 3) Denetlenecek değişiklikleri topla
        var bekleyenDenetimler = new List<(EntityEntry Giris, IslemKaydi Kayit)>();
        var kullaniciId = MevcutKullaniciIdGetir();
        var ipAdresi = MevcutIpAdresiGetir();
        var simdi = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IDenetlenebilir>().ToList())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var islemTuru = _bekleyenIslemTuru ?? (entry.State == EntityState.Added ? "Ekleme" : "Guncelleme");
            var eskiDeger = entry.State == EntityState.Modified
                ? OzellikleriJsonYap(entry.OriginalValues, entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToHashSet())
                : null;

            var kayit = new IslemKaydi
            {
                KullaniciId = kullaniciId,
                IslemTuru = islemTuru,
                TabloAdi = entry.Entity.GetType().Name,
                KayitId = AnahtarDegeriGetir(entry),
                EskiDeger = eskiDeger,
                YeniDeger = entry.State == EntityState.Modified
                    ? OzellikleriJsonYap(entry.CurrentValues, entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToHashSet())
                    : null,
                Aciklama = _bekleyenAciklama,
                Tarih = simdi,
                IpAdresi = ipAdresi
            };

            bekleyenDenetimler.Add((entry, kayit));
        }

        // Önce asıl değişiklikleri kaydet (yeni eklenen kayıtların DB ID'leri oluşsun)
        var sonuc = await base.SaveChangesAsync(cancellationToken);

        if (bekleyenDenetimler.Count > 0)
        {
            foreach (var (entry, kayit) in bekleyenDenetimler)
            {
                kayit.KayitId = AnahtarDegeriGetir(entry);
                kayit.YeniDeger ??= OzellikleriJsonYap(entry.CurrentValues, null);
                IslemKayitlari.Add(kayit);
            }

            await base.SaveChangesAsync(cancellationToken);
        }

        return sonuc;
    }

    private int? MevcutKullaniciIdGetir()
    {
        var idStr = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : null;
    }

    private string? MevcutIpAdresiGetir()
    {
        return _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
    }

    private static string AnahtarDegeriGetir(EntityEntry entry)
    {
        var pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return pk?.CurrentValue?.ToString() ?? string.Empty;
    }

    private static string OzellikleriJsonYap(PropertyValues degerler, HashSet<string>? filtre)
    {
        var sozluk = new Dictionary<string, object?>();
        foreach (var prop in degerler.Properties)
        {
            if (filtre is not null && !filtre.Contains(prop.Name))
            {
                continue;
            }

            var val = degerler[prop];
            if (val is Enum enumVal)
            {
                sozluk[prop.Name] = enumVal.ToString();
            }
            else
            {
                sozluk[prop.Name] = val;
            }
        }

        return JsonSerializer.Serialize(sozluk);
    }
}
