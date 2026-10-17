using System.Security.Claims;
using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.OgrenciPaneli;

public class NotGirisiModel : PageModel
{
    private readonly AppDbContext _db;

    public NotGirisiModel(AppDbContext db)
    {
        _db = db;
    }

    public List<DersKayit> OnayliDersKayitlari { get; set; } = [];

    public static readonly string[] GecerliHarfNotlari =
        ["AA", "BA", "BB", "CB", "CC", "DC", "DD", "FD", "FF", "DZ"];

    public async Task<IActionResult> OnGetAsync()
    {
        var kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ogrenci = await _db.Ogrenciler.FirstOrDefaultAsync(o => o.KullaniciId == kullaniciId);
        if (ogrenci is null)
        {
            return RedirectToPage("/Hesap/Giris");
        }

        // Yalnızca ders kaydı koordinatör tarafından ONAYLANMIŞ dersler için not girilebilir.
        OnayliDersKayitlari = await _db.DersKayitlari
            .Include(k => k.Ders)
            .Include(k => k.Donem)
            .Where(k => k.OgrenciId == ogrenci.Id && k.KayitOnayDurumu == OnayDurumu.Onaylandi)
            .OrderByDescending(k => k.Donem.Yil)
            .ThenBy(k => k.Ders.DersKodu)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostNotGirAsync(int kayitId, string harfNotu, decimal puanNotu)
    {
        var kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ogrenci = await _db.Ogrenciler.FirstOrDefaultAsync(o => o.KullaniciId == kullaniciId);
        if (ogrenci is null)
        {
            return RedirectToPage();
        }

        var kayit = await _db.DersKayitlari
            .Include(k => k.Ders)
            .FirstOrDefaultAsync(k => k.Id == kayitId
                                      && k.OgrenciId == ogrenci.Id
                                      && k.KayitOnayDurumu == OnayDurumu.Onaylandi);

        if (kayit is null)
        {
            return NotFound();
        }

        // Rapor 7.1: Not onaylandıktan sonra kilitlenir; öğrenci değiştiremez.
        if (kayit.NotOnayDurumu == OnayDurumu.Onaylandi)
        {
            TempData["HataMesaji"] = "Koordinatör tarafından onaylanmış bir not kilitlidir; düzeltme yalnızca koordinatör tarafından yapılabilir.";
            return RedirectToPage();
        }

        var harf = harfNotu.Trim().ToUpperInvariant();
        if (!GecerliHarfNotlari.Contains(harf) || puanNotu is < 0m or > 100m)
        {
            TempData["HataMesaji"] = "Geçerli bir harf notu ve 0-100 arasında puan giriniz.";
            return RedirectToPage();
        }

        kayit.HarfNotu = harf;
        kayit.PuanNotu = puanNotu;
        kayit.NotOnayDurumu = OnayDurumu.Bekliyor;
        kayit.NotRedGerekce = null;

        await _db.KaydetAsync($"Öğrenci {kayit.Ders.DersKodu} dersi için dönem sonu notunu ({harf} / {puanNotu}) onaya gönderdi.");
        TempData["BasariMesaji"] = $"{kayit.Ders.DersKodu} dersi notu ({harf}) 'Onay Bekliyor' olarak kaydedildi.";
        return RedirectToPage();
    }
}
