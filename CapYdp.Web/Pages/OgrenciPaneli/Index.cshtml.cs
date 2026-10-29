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
    public ProtokolAktsOzeti AktsOzeti { get; set; } = null!;
    public int ToplamHedefAkts => AktsOzeti.ToplamHedefAkts;
    public int TamamlananAkts => AktsOzeti.TamamlananAkts;
    public int KalanAkts => AktsOzeti.KalanAkts;
    public int TamamlananDersSayisi => AktsOzeti.TamamlananDersSayisi;
    public int BekleyenIslemSayisi { get; set; }
    public HashSet<int> GecilenDersIdleri => AktsOzeti.GecilenDersIdleri;

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
        AktsOzeti = ProtokolHesaplayici.Hesapla(Ogrenci);

        BekleyenIslemSayisi = Ogrenci.DersKayitlari
            .Count(dk => dk.KayitOnayDurumu == OnayDurumu.Bekliyor || dk.NotOnayDurumu == OnayDurumu.Bekliyor);

        return Page();
    }
}
