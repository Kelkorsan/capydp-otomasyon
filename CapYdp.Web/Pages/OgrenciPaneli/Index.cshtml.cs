using System.Security.Claims;
using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.OgrenciPaneli;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public Ogrenci Ogrenci { get; set; } = null!;
    public int ToplamHedefAkts { get; set; }
    public int TamamlananAkts { get; set; }
    public int KalanAkts => Math.Max(0, ToplamHedefAkts - TamamlananAkts);
    public int TamamlananDersSayisi { get; set; }
    public int BekleyenIslemSayisi { get; set; }
    public HashSet<int> GecilenDersIdleri { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ogrenci = await _db.Ogrenciler
            .Include(o => o.Kullanici)
            .Include(o => o.KabulDonemi)
            .Include(o => o.DersListesi.OrderBy(dl => dl.Ders.Yariyil).ThenBy(dl => dl.Ders.DersKodu))
                .ThenInclude(dl => dl.Ders)
            .Include(o => o.DersKayitlari.OrderByDescending(dk => dk.Donem.Yil).ThenBy(dk => dk.Ders.DersKodu))
                .ThenInclude(dk => dk.Ders)
            .Include(o => o.DersKayitlari)
                .ThenInclude(dk => dk.Donem)
            .FirstOrDefaultAsync(o => o.KullaniciId == kullaniciId);

        if (ogrenci is null)
        {
            return RedirectToPage("/Hesap/Giris");
        }

        Ogrenci = ogrenci;

        ToplamHedefAkts = Ogrenci.DersListesi
            .Where(dl => dl.Durum != DersListesiDurumu.Cikarildi)
            .Sum(dl => dl.Ders.Akts);

        GecilenDersIdleri = Ogrenci.DersKayitlari
            .Where(dk => dk.NotOnayDurumu == OnayDurumu.Onaylandi
                         && dk.DersDurumu is DersDurumu.Gecti or DersDurumu.Muaf)
            .Select(dk => dk.DersId)
            .Union(Ogrenci.DersListesi.Where(dl => dl.Durum == DersListesiDurumu.Muaf).Select(dl => dl.DersId))
            .ToHashSet();

        TamamlananAkts = Ogrenci.DersListesi
            .Where(dl => dl.Durum != DersListesiDurumu.Cikarildi && GecilenDersIdleri.Contains(dl.DersId))
            .Sum(dl => dl.Ders.Akts);

        TamamlananDersSayisi = Ogrenci.DersListesi
            .Count(dl => dl.Durum != DersListesiDurumu.Cikarildi && GecilenDersIdleri.Contains(dl.DersId));

        BekleyenIslemSayisi = Ogrenci.DersKayitlari
            .Count(dk => dk.KayitOnayDurumu == OnayDurumu.Bekliyor || dk.NotOnayDurumu == OnayDurumu.Bekliyor);

        return Page();
    }
}
