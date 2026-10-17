using System.Security.Claims;
using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.OgrenciPaneli;

public class DersGirisiModel : PageModel
{
    private readonly AppDbContext _db;

    public DersGirisiModel(AppDbContext db)
    {
        _db = db;
    }

    public Ogrenci Ogrenci { get; set; } = null!;
    public AkademikDonem? AktifDonem { get; set; }
    public List<Ders> SecilebilirDersler { get; set; } = [];
    public List<DersKayit> BuDonemKayitlari { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Arama { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var sonuc = await YukleAsync();
        return sonuc ?? Page();
    }

    public async Task<IActionResult> OnPostSecAsync(int dersId)
    {
        var kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ogrenci = await _db.Ogrenciler.FirstOrDefaultAsync(o => o.KullaniciId == kullaniciId);
        var aktifDonem = await _db.AkademikDonemler.FirstOrDefaultAsync(d => d.AktifMi);

        if (ogrenci is null || aktifDonem is null)
        {
            return RedirectToPage();
        }

        // Ders mutlaka aktif katalogda bulunmalıdır (Rapor 4.3.2: Serbest metin ile ders uydurulamaz).
        var ders = await _db.Dersler.FirstOrDefaultAsync(d => d.Id == dersId && d.AktifMi);
        if (ders is null)
        {
            TempData["HataMesaji"] = "Seçilen ders aktif ders kataloğunda bulunamadı.";
            return RedirectToPage();
        }

        var mevcutDonemKaydi = await _db.DersKayitlari.AnyAsync(k =>
            k.OgrenciId == ogrenci.Id
            && k.DersId == dersId
            && k.DonemId == aktifDonem.Id
            && (k.KayitOnayDurumu == OnayDurumu.Bekliyor || k.KayitOnayDurumu == OnayDurumu.Onaylandi));

        if (mevcutDonemKaydi)
        {
            TempData["HataMesaji"] = $"{ders.DersKodu} dersi bu dönem için zaten seçilmiş.";
            return RedirectToPage();
        }

        var oncekiDeneme = await _db.DersKayitlari
            .CountAsync(k => k.OgrenciId == ogrenci.Id && k.DersId == dersId);

        _db.DersKayitlari.Add(new DersKayit
        {
            OgrenciId = ogrenci.Id,
            DersId = dersId,
            DonemId = aktifDonem.Id,
            KayitOnayDurumu = OnayDurumu.Bekliyor,
            DersDurumu = DersDurumu.Planlandi,
            DenemeSayisi = oncekiDeneme + 1,
            GirenId = kullaniciId,
            GirisTarihi = DateTime.UtcNow
        });

        await _db.KaydetAsync($"Öğrenci {aktifDonem.DonemAdi} dönemi için {ders.DersKodu} dersini onaya gönderdi.");
        TempData["BasariMesaji"] = $"{ders.DersKodu} — {ders.DersAdi} dersi 'Onay Bekliyor' olarak eklendi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGeriCekAsync(int kayitId)
    {
        var kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ogrenci = await _db.Ogrenciler.FirstOrDefaultAsync(o => o.KullaniciId == kullaniciId);
        if (ogrenci is null)
        {
            return RedirectToPage();
        }

        var kayit = await _db.DersKayitlari
            .Include(k => k.Ders)
            .FirstOrDefaultAsync(k => k.Id == kayitId && k.OgrenciId == ogrenci.Id);

        if (kayit is null)
        {
            return NotFound();
        }

        // Rapor 7.1: Onaylanan kayıt kilitlenir; öğrenci yalnızca "Bekliyor" aşamasında değişiklik yapabilir.
        if (kayit.KayitKilitliMi)
        {
            TempData["HataMesaji"] = "Koordinatör tarafından onaylanmış/işlenmiş bir ders kaydı kilitlidir, öğrenci tarafından değiştirilemez.";
            return RedirectToPage();
        }

        // Soft-delete prensibi: Fiziksel silme yerine İptalEdildi durumuna alınır.
        kayit.KayitOnayDurumu = OnayDurumu.IptalEdildi;
        kayit.KayitRedGerekce = "Öğrenci onay öncesinde seçimi geri çekti.";
        kayit.DersDurumu = DersDurumu.Cikarildi;

        await _db.KaydetAsync($"Öğrenci onay bekleyen {kayit.Ders.DersKodu} ders seçimini geri çekti.", "Iptal");
        TempData["BasariMesaji"] = $"{kayit.Ders.DersKodu} ders seçimi geri çekildi.";
        return RedirectToPage();
    }

    private async Task<IActionResult?> YukleAsync()
    {
        var kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ogrenci = await _db.Ogrenciler
            .Include(o => o.Kullanici)
            .Include(o => o.DersListesi).ThenInclude(dl => dl.Ders)
            .FirstOrDefaultAsync(o => o.KullaniciId == kullaniciId);

        if (ogrenci is null)
        {
            return RedirectToPage("/Hesap/Giris");
        }

        Ogrenci = ogrenci;
        AktifDonem = await _db.AkademikDonemler.FirstOrDefaultAsync(d => d.AktifMi);

        if (AktifDonem is not null)
        {
            BuDonemKayitlari = await _db.DersKayitlari
                .Include(k => k.Ders)
                .Where(k => k.OgrenciId == ogrenci.Id
                            && k.DonemId == AktifDonem.Id
                            && k.KayitOnayDurumu != OnayDurumu.IptalEdildi)
                .OrderBy(k => k.Ders.DersKodu)
                .ToListAsync();
        }

        var sorgu = _db.Dersler.Where(d => d.AktifMi);
        if (!string.IsNullOrWhiteSpace(Arama))
        {
            var terim = Arama.Trim();
            sorgu = sorgu.Where(d => d.DersKodu.Contains(terim) || d.DersAdi.Contains(terim));
        }

        SecilebilirDersler = await sorgu
            .OrderBy(d => d.Yariyil)
            .ThenBy(d => d.DersKodu)
            .ToListAsync();

        return null;
    }
}
