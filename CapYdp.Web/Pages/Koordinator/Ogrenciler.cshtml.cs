using System.ComponentModel.DataAnnotations;
using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.Koordinator;

public class OgrencilerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;

    public OgrencilerModel(AppDbContext db, UserManager<Kullanici> kullaniciYoneticisi)
    {
        _db = db;
        _kullaniciYoneticisi = kullaniciYoneticisi;
    }

    public List<Ogrenci> Ogrenciler { get; set; } = [];
    public List<AkademikDonem> Donemler { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public ProgramTuru? FiltreProgram { get; set; }

    [BindProperty(SupportsGet = true)]
    public OgrenciDurumu? FiltreDurum { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FiltreDonemId { get; set; }

    [BindProperty]
    public YeniOgrenciGirdisi YeniOgrenci { get; set; } = new();

    public class YeniOgrenciGirdisi
    {
        [Required(ErrorMessage = "Öğrenci numarası zorunludur.")]
        [MaxLength(20)]
        public string OgrenciNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad zorunludur.")]
        [MaxLength(100)]
        public string Ad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [MaxLength(100)]
        public string Soyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Okul e-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Başlangıç şifresi zorunludur.")]
        [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
        public string BaslangicSifresi { get; set; } = "CapYdp!2026";

        public ProgramTuru ProgramTuru { get; set; } = ProgramTuru.CAP;

        [Required(ErrorMessage = "Anadal fakültesi zorunludur.")]
        public string AnadalFakulte { get; set; } = "Teknoloji Fakültesi";

        [Required(ErrorMessage = "Anadal bölümü zorunludur.")]
        public string AnadalBolum { get; set; } = "Elektrik-Elektronik Mühendisliği";

        [Required]
        public int KabulDonemiId { get; set; }

        [Range(0.0, 4.0, ErrorMessage = "GANO 0.00 ile 4.00 arasında olmalıdır.")]
        public decimal? AnadalGano { get; set; }

        [MaxLength(100)]
        public string? GanoKaynagi { get; set; }

        public bool ProtokolZorunluDersleriniAta { get; set; } = true;
    }

    public async Task OnGetAsync()
    {
        await ListeleriYukleAsync();
    }

    public async Task<IActionResult> OnPostEkleAsync()
    {
        if (!ModelState.IsValid)
        {
            await ListeleriYukleAsync();
            return Page();
        }

        var ogrenciNo = YeniOgrenci.OgrenciNo.Trim();
        var email = YeniOgrenci.Email.Trim();

        if (await _db.Ogrenciler.AnyAsync(o => o.OgrenciNo == ogrenciNo))
        {
            TempData["HataMesaji"] = $"{ogrenciNo} numaralı öğrenci sistemde zaten kayıtlı.";
            return RedirectToPage();
        }

        if (await _kullaniciYoneticisi.FindByEmailAsync(email) is not null)
        {
            TempData["HataMesaji"] = $"{email} e-posta adresiyle kayıtlı bir kullanıcı zaten mevcut.";
            return RedirectToPage();
        }

        if (YeniOgrenci.AnadalGano.HasValue && string.IsNullOrWhiteSpace(YeniOgrenci.GanoKaynagi))
        {
            TempData["HataMesaji"] = "Anadal GANO girildiğinde bilginin kaynağı belirtilmelidir.";
            return RedirectToPage();
        }

        var kullanici = new Kullanici
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Ad = YeniOgrenci.Ad.Trim(),
            Soyad = YeniOgrenci.Soyad.Trim(),
            AktifMi = true
        };

        var hesapSonuc = await _kullaniciYoneticisi.CreateAsync(kullanici, YeniOgrenci.BaslangicSifresi);
        if (!hesapSonuc.Succeeded)
        {
            TempData["HataMesaji"] = "Kullanıcı hesabı oluşturulamadı: " +
                                     string.Join(" ", hesapSonuc.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        await _kullaniciYoneticisi.AddToRoleAsync(kullanici, Roller.Ogrenci);

        var ogrenci = new Ogrenci
        {
            KullaniciId = kullanici.Id,
            OgrenciNo = ogrenciNo,
            ProgramTuru = YeniOgrenci.ProgramTuru,
            AnadalFakulte = YeniOgrenci.AnadalFakulte.Trim(),
            AnadalBolum = YeniOgrenci.AnadalBolum.Trim(),
            KabulDonemiId = YeniOgrenci.KabulDonemiId,
            Durum = OgrenciDurumu.Aktif,
            AnadalGano = YeniOgrenci.AnadalGano,
            GanoKaynagi = string.IsNullOrWhiteSpace(YeniOgrenci.GanoKaynagi) ? null : YeniOgrenci.GanoKaynagi.Trim(),
            GanoGuncellemeTarihi = YeniOgrenci.AnadalGano.HasValue ? DateTime.UtcNow : null
        };

        _db.Ogrenciler.Add(ogrenci);
        await _db.KaydetAsync($"Yeni {ogrenci.ProgramTuru} öğrencisi kaydedildi: {ogrenci.OgrenciNo} - {kullanici.AdSoyad}.");

        if (YeniOgrenci.ProtokolZorunluDersleriniAta && YeniOgrenci.ProgramTuru == ProgramTuru.CAP)
        {
            var zorunluDersler = await _db.Dersler.Where(d => d.AktifMi && d.DersTuru == DersTuru.Zorunlu).ToListAsync();
            var muafiyetliDersIdleri = (await _db.MuafiyetKurallari.Select(m => m.CapDersId).ToListAsync()).ToHashSet();

            foreach (var ders in zorunluDersler)
            {
                _db.OgrenciDersListeleri.Add(new OgrenciDersListesi
                {
                    OgrenciId = ogrenci.Id,
                    DersId = ders.Id,
                    OzelSartliMi = muafiyetliDersIdleri.Contains(ders.Id),
                    Durum = DersListesiDurumu.Aktif,
                    DegisiklikGerekce = "Kabul protokolü standart zorunlu ders ataması"
                });
            }

            await _db.KaydetAsync($"{ogrenci.OgrenciNo} için protokol zorunlu dersleri otomatik atandı.");
        }

        TempData["BasariMesaji"] = $"{kullanici.AdSoyad} sisteme eklendi ve öğrenci hesabı açıldı.";
        return RedirectToPage("/Koordinator/OgrenciDetay", new { id = ogrenci.Id });
    }

    private async Task ListeleriYukleAsync()
    {
        Donemler = await _db.AkademikDonemler
            .OrderByDescending(d => d.Yil)
            .ThenByDescending(d => d.Id)
            .ToListAsync();

        if (YeniOgrenci.KabulDonemiId == 0)
        {
            YeniOgrenci.KabulDonemiId = Donemler.FirstOrDefault(d => d.AktifMi)?.Id
                                        ?? Donemler.FirstOrDefault()?.Id
                                        ?? 0;
        }

        var sorgu = _db.Ogrenciler
            .Include(o => o.Kullanici)
            .Include(o => o.KabulDonemi)
            .Include(o => o.DersListesi)
            .AsQueryable();

        if (FiltreProgram.HasValue)
        {
            sorgu = sorgu.Where(o => o.ProgramTuru == FiltreProgram.Value);
        }

        if (FiltreDurum.HasValue)
        {
            sorgu = sorgu.Where(o => o.Durum == FiltreDurum.Value);
        }

        if (FiltreDonemId.HasValue)
        {
            sorgu = sorgu.Where(o => o.KabulDonemiId == FiltreDonemId.Value);
        }

        Ogrenciler = await sorgu.OrderBy(o => o.OgrenciNo).ToListAsync();
    }
}
