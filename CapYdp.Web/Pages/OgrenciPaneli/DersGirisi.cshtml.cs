using System.Security.Claims;
using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.OgrenciPaneli;

public class DersGirisiModel : PageModel
{
    // Faz 2'de KuralParametre tablosundan okunacak varsayılan dönemlik uyarı eşikleri
    public const int VarsayilanDonemMaxAkts = 30;
    public const int VarsayilanDonemMaxDersSayisi = 6;

    private readonly AppDbContext _db;

    public DersGirisiModel(AppDbContext db)
    {
        _db = db;
    }

    public Ogrenci Ogrenci { get; set; } = null!;
    public AkademikDonem? AktifDonem { get; set; }
    public List<OgrenciDersListesi> ProtokolDerslerim { get; set; } = [];
    public List<Ders> DigerKatalogDersleri { get; set; } = [];
    public List<DersKayit> BuDonemKayitlari { get; set; } = [];
    public HashSet<int> GecilenVeyaMuafDersIdleri { get; set; } = [];
    public HashSet<int> BuDonemSecilenDersIdleri { get; set; } = [];

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
        var ogrenci = await _db.Ogrenciler
            .Include(o => o.DersListesi)
            .FirstOrDefaultAsync(o => o.KullaniciId == kullaniciId);
        var aktifDonem = await _db.AkademikDonemler.FirstOrDefaultAsync(d => d.AktifMi);

        if (ogrenci is null || aktifDonem is null)
        {
            return RedirectToPage();
        }

        // 1) Ders mutlaka aktif katalogda bulunmalıdır (Rapor 4.3.2: Serbest metin ile ders uydurulamaz).
        var ders = await _db.Dersler.FirstOrDefaultAsync(d => d.Id == dersId && d.AktifMi);
        if (ders is null)
        {
            TempData["HataMesaji"] = "Seçilen ders aktif ders kataloğunda bulunamadı.";
            return RedirectToPage();
        }

        // 2) Öğrenci daha önce onaylı olarak geçtiği veya muaf olduğu bir dersi tekrar seçemez.
        var dahaOnceGectiMi = await _db.DersKayitlari.AnyAsync(k =>
            k.OgrenciId == ogrenci.Id
            && k.DersId == dersId
            && k.NotOnayDurumu == OnayDurumu.Onaylandi
            && (k.DersDurumu == DersDurumu.Gecti || k.DersDurumu == DersDurumu.Muaf));

        var listedeMuafMi = ogrenci.DersListesi.Any(dl =>
            dl.DersId == dersId && dl.Durum == DersListesiDurumu.Muaf);

        if (dahaOnceGectiMi || listedeMuafMi)
        {
            TempData["HataMesaji"] = $"{ders.DersKodu} — {ders.DersAdi} dersini daha önce başarıyla tamamladığınız veya muaf olduğunuz için tekrar seçemezsiniz.";
            return RedirectToPage();
        }

        // 3) Aynı dönemde aynı ders iki kez seçilemez.
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
            .CountAsync(k => k.OgrenciId == ogrenci.Id
                             && k.DersId == dersId
                             && k.KayitOnayDurumu != OnayDurumu.IptalEdildi);

        var protokoldeVarMi = ogrenci.DersListesi.Any(dl =>
            dl.DersId == dersId && dl.Durum == DersListesiDurumu.Aktif);

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

        var kaynakEtiketi = protokoldeVarMi ? "protokol listesinden" : "bölüm ders havuzundan";
        await _db.KaydetAsync($"Öğrenci {aktifDonem.DonemAdi} dönemi için {ders.DersKodu} dersini {kaynakEtiketi} onaya gönderdi.");

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
            .Include(o => o.DersListesi.OrderBy(dl => dl.Ders.Yariyil).ThenBy(dl => dl.Ders.DersKodu))
                .ThenInclude(dl => dl.Ders)
            .FirstOrDefaultAsync(o => o.KullaniciId == kullaniciId);

        if (ogrenci is null)
        {
            return RedirectToPage("/Hesap/Giris");
        }

        Ogrenci = ogrenci;
        AktifDonem = await _db.AkademikDonemler.FirstOrDefaultAsync(d => d.AktifMi);

        var onayliGecilenIdler = await _db.DersKayitlari
            .Where(k => k.OgrenciId == ogrenci.Id
                        && k.NotOnayDurumu == OnayDurumu.Onaylandi
                        && (k.DersDurumu == DersDurumu.Gecti || k.DersDurumu == DersDurumu.Muaf))
            .Select(k => k.DersId)
            .ToListAsync();

        GecilenVeyaMuafDersIdleri = onayliGecilenIdler
            .Union(ogrenci.DersListesi.Where(dl => dl.Durum == DersListesiDurumu.Muaf).Select(dl => dl.DersId))
            .ToHashSet();

        if (AktifDonem is not null)
        {
            BuDonemKayitlari = await _db.DersKayitlari
                .Include(k => k.Ders)
                .Where(k => k.OgrenciId == ogrenci.Id
                            && k.DonemId == AktifDonem.Id
                            && k.KayitOnayDurumu != OnayDurumu.IptalEdildi)
                .OrderBy(k => k.Ders.DersKodu)
                .ToListAsync();

            BuDonemSecilenDersIdleri = BuDonemKayitlari
                .Where(k => k.KayitOnayDurumu is OnayDurumu.Bekliyor or OnayDurumu.Onaylandi)
                .Select(k => k.DersId)
                .ToHashSet();
        }

        ProtokolDerslerim = ogrenci.DersListesi
            .Where(dl => dl.Durum != DersListesiDurumu.Cikarildi && dl.Ders.AktifMi)
            .OrderBy(dl => dl.Ders.Yariyil)
            .ThenBy(dl => dl.Ders.DersKodu)
            .ToList();

        var protokolIdSet = ProtokolDerslerim.Select(dl => dl.DersId).ToHashSet();

        var sorgu = _db.Dersler.Where(d => d.AktifMi && !protokolIdSet.Contains(d.Id));
        if (!string.IsNullOrWhiteSpace(Arama))
        {
            var terim = Arama.Trim();
            sorgu = sorgu.Where(d => d.DersKodu.Contains(terim) || d.DersAdi.Contains(terim));
        }

        DigerKatalogDersleri = await sorgu
            .OrderBy(d => d.Yariyil)
            .ThenBy(d => d.DersKodu)
            .ToListAsync();

        return null;
    }
}
