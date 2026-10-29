using System.Security.Claims;
using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.Koordinator;

public class OgrenciDetayModel : PageModel
{
    private readonly AppDbContext _db;

    public OgrenciDetayModel(AppDbContext db)
    {
        _db = db;
    }

    public Ogrenci Ogrenci { get; set; } = null!;
    public List<Ders> AktifKatalogDersleri { get; set; } = [];
    public List<AkademikDonem> Donemler { get; set; } = [];

    public int ToplamHedefAkts { get; set; }
    public int TamamlananAkts { get; set; }
    public int KalanAkts => Math.Max(0, ToplamHedefAkts - TamamlananAkts);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var ogrenci = await OgrenciYukleAsync(id);
        if (ogrenci is null)
        {
            return NotFound();
        }

        Ogrenci = ogrenci;
        await YardimciListeleriYukleAsync();
        AktsHesapla();
        return Page();
    }

    public async Task<IActionResult> OnPostGanoGuncelleAsync(int id, decimal anadalGano, string ganoKaynagi)
    {
        if (string.IsNullOrWhiteSpace(ganoKaynagi))
        {
            TempData["HataMesaji"] = "Anadal GANO güncellenirken bilginin kaynağı belirtilmelidir.";
            return RedirectToPage(new { id });
        }

        if (anadalGano is < 0m or > 4.00m)
        {
            TempData["HataMesaji"] = "GANO 0.00 ile 4.00 arasında olmalıdır.";
            return RedirectToPage(new { id });
        }

        var ogrenci = await _db.Ogrenciler.FindAsync(id);
        if (ogrenci is null)
        {
            return NotFound();
        }

        ogrenci.AnadalGano = anadalGano;
        ogrenci.GanoKaynagi = ganoKaynagi.Trim();
        ogrenci.GanoGuncellemeTarihi = DateTime.UtcNow;

        await _db.KaydetAsync($"Anadal GANO {anadalGano:0.00} olarak güncellendi. Kaynak: {ganoKaynagi.Trim()}.");
        TempData["BasariMesaji"] = "Anadal GANO bilgisi kaynak açıklamasıyla güncellendi.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDurumGuncelleAsync(int id, OgrenciDurumu yeniDurum, string gerekce)
    {
        if (string.IsNullOrWhiteSpace(gerekce))
        {
            TempData["HataMesaji"] = "Öğrenci durumu değiştirilirken gerekçe girilmesi zorunludur.";
            return RedirectToPage(new { id });
        }

        var ogrenci = await _db.Ogrenciler.FindAsync(id);
        if (ogrenci is null)
        {
            return NotFound();
        }

        var eskiDurum = ogrenci.Durum;
        ogrenci.Durum = yeniDurum;
        await _db.KaydetAsync($"Öğrenci durumu {eskiDurum} -> {yeniDurum} olarak değiştirildi. Gerekçe: {gerekce.Trim()}");

        TempData["BasariMesaji"] = $"Öğrenci durumu '{yeniDurum}' olarak güncellendi.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDersAtaAsync(int id, int dersId, bool ozelSartliMi, string gerekce)
    {
        if (string.IsNullOrWhiteSpace(gerekce))
        {
            TempData["HataMesaji"] = "Öğrencinin ders listesine ders eklerken gerekçe girilmesi zorunludur.";
            return RedirectToPage(new { id });
        }

        var mevcut = await _db.OgrenciDersListeleri
            .FirstOrDefaultAsync(x => x.OgrenciId == id && x.DersId == dersId);

        if (mevcut is not null)
        {
            mevcut.Durum = DersListesiDurumu.Aktif;
            mevcut.OzelSartliMi = ozelSartliMi;
            mevcut.DegisiklikGerekce = gerekce.Trim();
            mevcut.DegisiklikTarihi = DateTime.UtcNow;
            await _db.KaydetAsync($"Öğrenci ders listesindeki ders tekrar aktif edildi: {gerekce.Trim()}");
        }
        else
        {
            _db.OgrenciDersListeleri.Add(new OgrenciDersListesi
            {
                OgrenciId = id,
                DersId = dersId,
                OzelSartliMi = ozelSartliMi,
                Durum = DersListesiDurumu.Aktif,
                DegisiklikGerekce = gerekce.Trim(),
                EklemeTarihi = DateTime.UtcNow
            });
            await _db.KaydetAsync($"Öğrenci ders listesine yeni ders eklendi: {gerekce.Trim()}");
        }

        TempData["BasariMesaji"] = "Ders öğrencinin protokol ders listesine eklendi.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDersListesiDurumAsync(int id, int listeKayitId, DersListesiDurumu yeniDurum, string gerekce)
    {
        if (string.IsNullOrWhiteSpace(gerekce))
        {
            TempData["HataMesaji"] = "Öğrenci ders listesinde değişiklik yaparken gerekçe zorunludur.";
            return RedirectToPage(new { id });
        }

        var kayit = await _db.OgrenciDersListeleri
            .Include(x => x.Ders)
            .FirstOrDefaultAsync(x => x.Id == listeKayitId && x.OgrenciId == id);

        if (kayit is null)
        {
            return NotFound();
        }

        kayit.Durum = yeniDurum;
        kayit.DegisiklikGerekce = gerekce.Trim();
        kayit.DegisiklikTarihi = DateTime.UtcNow;

        await _db.KaydetAsync(
            $"Öğrenci ders listesindeki {kayit.Ders.DersKodu} dersi '{yeniDurum}' yapıldı. Gerekçe: {gerekce.Trim()}");

        TempData["BasariMesaji"] = $"{kayit.Ders.DersKodu} dersinin listedeki durumu '{yeniDurum}' olarak güncellendi.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostGeriyeDonukKayitAsync(
        int id,
        int donemId,
        int dersId,
        string? harfNotu,
        decimal? puanNotu,
        bool muafMi = false)
    {
        var ogrenci = await _db.Ogrenciler.FindAsync(id);
        if (ogrenci is null)
        {
            return NotFound();
        }

        var harf = (harfNotu ?? string.Empty).Trim().ToUpperInvariant();
        if (!OnayMerkeziModel.KoordinatorGecerliHarfNotlari.Contains(harf))
        {
            TempData["HataMesaji"] = "Geçerli bir harf notu seçiniz (AA, BA, BB, CB, CC, DC, DD, FD, FF, DZ, MU).";
            return RedirectToPage(new { id });
        }

        if (muafMi && !OnayMerkeziModel.HarfNotuGectiMi(harf))
        {
            TempData["HataMesaji"] = "Başarısız harf notu (FD, FF, DZ) ile muafiyet kaydı oluşturulamaz.";
            return RedirectToPage(new { id });
        }

        if (harf != "MU" && !muafMi && puanNotu is null)
        {
            TempData["HataMesaji"] = "MU (Muaf) dışındaki ders kayıtları için 0-100 arasında puan girilmelidir.";
            return RedirectToPage(new { id });
        }

        if (puanNotu is < 0m or > 100m)
        {
            TempData["HataMesaji"] = "Puan notu 0 ile 100 arasında olmalıdır.";
            return RedirectToPage(new { id });
        }

        var donemVarMi = await _db.AkademikDonemler.AnyAsync(d => d.Id == donemId);
        var ders = await _db.Dersler.FirstOrDefaultAsync(d => d.Id == dersId && d.AktifMi);
        if (!donemVarMi || ders is null)
        {
            TempData["HataMesaji"] = "Geçerli bir dönem ve aktif katalog dersi seçilmelidir.";
            return RedirectToPage(new { id });
        }

        var ayniDonemdeVarMi = await _db.DersKayitlari.AnyAsync(k =>
            k.OgrenciId == id
            && k.DersId == dersId
            && k.DonemId == donemId
            && k.KayitOnayDurumu != OnayDurumu.Reddedildi
            && k.KayitOnayDurumu != OnayDurumu.IptalEdildi);

        if (ayniDonemdeVarMi)
        {
            TempData["HataMesaji"] = $"{ders.DersKodu} dersi için seçilen dönemde zaten aktif bir kayıt bulunuyor.";
            return RedirectToPage(new { id });
        }

        var koordinatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var oncekiDenemeSayisi = await _db.DersKayitlari
            .CountAsync(k => k.OgrenciId == id && k.DersId == dersId);

        var hesaplananDurum = OnayMerkeziModel.HarfNotundanDurumHesapla(harf, muafMi);

        _db.DersKayitlari.Add(new DersKayit
        {
            OgrenciId = id,
            DersId = dersId,
            DonemId = donemId,
            KayitOnayDurumu = OnayDurumu.Onaylandi,
            KayitOnayTarihi = DateTime.UtcNow,
            KayitOnaylayanId = koordinatorId,
            HarfNotu = harf,
            PuanNotu = puanNotu,
            NotOnayDurumu = OnayDurumu.Onaylandi,
            NotOnayTarihi = DateTime.UtcNow,
            NotOnaylayanId = koordinatorId,
            DersDurumu = hesaplananDurum,
            DenemeSayisi = oncekiDenemeSayisi + 1,
            GirenId = koordinatorId,
            GeriyeDonukMu = true
        });

        await _db.KaydetAsync($"Mevcut öğrenci aktarımı kapsamında geriye dönük ders/not kaydı ({ders.DersKodu}: {harf} -> {hesaplananDurum}) eklendi.");
        TempData["BasariMesaji"] = $"Geriye dönük ders ve not kaydı ({ders.DersKodu} — {harf}, {hesaplananDurum}) onaylı olarak işlendi.";
        return RedirectToPage(new { id });
    }

    private async Task<Ogrenci?> OgrenciYukleAsync(int id)
    {
        return await _db.Ogrenciler
            .Include(o => o.Kullanici)
            .Include(o => o.KabulDonemi)
            .Include(o => o.DersListesi.OrderBy(dl => dl.Ders.Yariyil).ThenBy(dl => dl.Ders.DersKodu))
                .ThenInclude(dl => dl.Ders)
            .Include(o => o.DersKayitlari.OrderByDescending(dk => dk.Donem.Yil).ThenBy(dk => dk.Ders.DersKodu))
                .ThenInclude(dk => dk.Ders)
            .Include(o => o.DersKayitlari)
                .ThenInclude(dk => dk.Donem)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    private async Task YardimciListeleriYukleAsync()
    {
        AktifKatalogDersleri = await _db.Dersler
            .Where(d => d.AktifMi)
            .OrderBy(d => d.Yariyil)
            .ThenBy(d => d.DersKodu)
            .ToListAsync();

        Donemler = await _db.AkademikDonemler
            .OrderByDescending(d => d.Yil)
            .ThenByDescending(d => d.Id)
            .ToListAsync();
    }

    private void AktsHesapla()
    {
        // Yalnızca aktif veya muaf protokol dersleri hedef AKTS'yi oluşturur.
        ToplamHedefAkts = Ogrenci.DersListesi
            .Where(d => d.Durum != DersListesiDurumu.Cikarildi)
            .Sum(d => d.Ders.Akts);

        // Yalnızca ONAYLANMIŞ ve Geçti/Muaf durumundaki dersler tamamlanan AKTS'ye sayılır (Rapor Bölüm 7.2).
        var gecilenDersIdleri = Ogrenci.DersKayitlari
            .Where(dk => dk.NotOnayDurumu == OnayDurumu.Onaylandi
                         && dk.DersDurumu is DersDurumu.Gecti or DersDurumu.Muaf)
            .Select(dk => dk.DersId)
            .Union(Ogrenci.DersListesi.Where(dl => dl.Durum == DersListesiDurumu.Muaf).Select(dl => dl.DersId))
            .ToHashSet();

        TamamlananAkts = Ogrenci.DersListesi
            .Where(dl => dl.Durum != DersListesiDurumu.Cikarildi && gecilenDersIdleri.Contains(dl.DersId))
            .Sum(dl => dl.Ders.Akts);
    }
}
