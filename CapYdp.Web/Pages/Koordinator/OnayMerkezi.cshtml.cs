using System.Security.Claims;
using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.Koordinator;

public class OnayMerkeziModel : PageModel
{
    private readonly AppDbContext _db;

    public OnayMerkeziModel(AppDbContext db)
    {
        _db = db;
    }

    public List<DersKayit> BekleyenDersKayitlari { get; set; } = [];
    public List<DersKayit> BekleyenNotKayitlari { get; set; } = [];
    public List<DersKayit> SonIslenenKayitlar { get; set; } = [];

    public async Task OnGetAsync()
    {
        BekleyenDersKayitlari = await _db.DersKayitlari
            .Include(k => k.Ogrenci).ThenInclude(o => o.Kullanici)
            .Include(k => k.Ders)
            .Include(k => k.Donem)
            .Where(k => k.KayitOnayDurumu == OnayDurumu.Bekliyor)
            .OrderBy(k => k.GirisTarihi)
            .ToListAsync();

        BekleyenNotKayitlari = await _db.DersKayitlari
            .Include(k => k.Ogrenci).ThenInclude(o => o.Kullanici)
            .Include(k => k.Ders)
            .Include(k => k.Donem)
            .Where(k => k.NotOnayDurumu == OnayDurumu.Bekliyor)
            .OrderBy(k => k.GirisTarihi)
            .ToListAsync();

        SonIslenenKayitlar = await _db.DersKayitlari
            .Include(k => k.Ogrenci).ThenInclude(o => o.Kullanici)
            .Include(k => k.Ders)
            .Include(k => k.Donem)
            .Where(k => k.KayitOnayDurumu != OnayDurumu.Bekliyor)
            .OrderByDescending(k => k.NotOnayTarihi ?? k.KayitOnayTarihi ?? k.GirisTarihi)
            .Take(15)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDersKaydiOnaylaAsync(int id)
    {
        var kayit = await _db.DersKayitlari
            .Include(k => k.Ders)
            .Include(k => k.Ogrenci)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (kayit is null)
        {
            return NotFound();
        }

        var koordinatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        kayit.KayitOnayDurumu = OnayDurumu.Onaylandi;
        kayit.KayitOnayTarihi = DateTime.UtcNow;
        kayit.KayitOnaylayanId = koordinatorId;
        kayit.DersDurumu = DersDurumu.Alindi;

        await _db.KaydetAsync(
            aciklama: $"{kayit.Ogrenci.OgrenciNo} numaralı öğrencinin {kayit.Ders.DersKodu} ders kaydı onaylandı ve kilitlendi.",
            islemTuru: "Onay");

        TempData["BasariMesaji"] = $"{kayit.Ders.DersKodu} ders kaydı onaylandı ve kilitlendi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTumDersKayitlariniOnaylaAsync()
    {
        var bekleyenler = await _db.DersKayitlari
            .Where(k => k.KayitOnayDurumu == OnayDurumu.Bekliyor)
            .ToListAsync();

        if (bekleyenler.Count == 0)
        {
            return RedirectToPage();
        }

        var koordinatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var simdi = DateTime.UtcNow;

        foreach (var k in bekleyenler)
        {
            k.KayitOnayDurumu = OnayDurumu.Onaylandi;
            k.KayitOnayTarihi = simdi;
            k.KayitOnaylayanId = koordinatorId;
            k.DersDurumu = DersDurumu.Alindi;
        }

        await _db.KaydetAsync(
            aciklama: $"Toplu onay ile {bekleyenler.Count} adet dönemlik ders kaydı onaylandı.",
            islemTuru: "Onay");

        TempData["BasariMesaji"] = $"{bekleyenler.Count} adet bekleyen ders kaydı topluca onaylandı.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDersKaydiReddetAsync(int id, string redGerekce)
    {
        if (string.IsNullOrWhiteSpace(redGerekce))
        {
            TempData["HataMesaji"] = "Red işleminde gerekçe belirtilmesi zorunludur.";
            return RedirectToPage();
        }

        var kayit = await _db.DersKayitlari
            .Include(k => k.Ders)
            .Include(k => k.Ogrenci)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (kayit is null)
        {
            return NotFound();
        }

        var koordinatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // Rapor 7.1: Reddedilen kayıt silinmez, "Reddedildi" durumu ile saklanır.
        kayit.KayitOnayDurumu = OnayDurumu.Reddedildi;
        kayit.KayitRedGerekce = redGerekce.Trim();
        kayit.KayitOnayTarihi = DateTime.UtcNow;
        kayit.KayitOnaylayanId = koordinatorId;

        await _db.KaydetAsync(
            aciklama: $"{kayit.Ogrenci.OgrenciNo} - {kayit.Ders.DersKodu} ders kaydı reddedildi: {redGerekce.Trim()}",
            islemTuru: "Red");

        TempData["BasariMesaji"] = $"{kayit.Ders.DersKodu} ders kaydı gerekçesiyle reddedildi (kayıt silinmeden korundu).";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostNotOnaylaAsync(int id)
    {
        var kayit = await _db.DersKayitlari
            .Include(k => k.Ders)
            .Include(k => k.Ogrenci)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (kayit is null)
        {
            return NotFound();
        }

        var koordinatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        kayit.NotOnayDurumu = OnayDurumu.Onaylandi;
        kayit.NotOnayTarihi = DateTime.UtcNow;
        kayit.NotOnaylayanId = koordinatorId;
        kayit.DersDurumu = HarfNotuGectiMi(kayit.HarfNotu) ? DersDurumu.Gecti : DersDurumu.Kaldi;

        await _db.KaydetAsync(
            aciklama: $"{kayit.Ogrenci.OgrenciNo} - {kayit.Ders.DersKodu} not girişi ({kayit.HarfNotu} / {kayit.PuanNotu}) onaylandı.",
            islemTuru: "Onay");

        TempData["BasariMesaji"] = $"{kayit.Ders.DersKodu} notu ({kayit.HarfNotu}) onaylandı ve kural hesaplarına dahil edildi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostNotReddetAsync(int id, string redGerekce)
    {
        if (string.IsNullOrWhiteSpace(redGerekce))
        {
            TempData["HataMesaji"] = "Not red işleminde gerekçe belirtilmesi zorunludur.";
            return RedirectToPage();
        }

        var kayit = await _db.DersKayitlari
            .Include(k => k.Ders)
            .Include(k => k.Ogrenci)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (kayit is null)
        {
            return NotFound();
        }

        var koordinatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        kayit.NotOnayDurumu = OnayDurumu.Reddedildi;
        kayit.NotRedGerekce = redGerekce.Trim();
        kayit.NotOnayTarihi = DateTime.UtcNow;
        kayit.NotOnaylayanId = koordinatorId;

        await _db.KaydetAsync(
            aciklama: $"{kayit.Ogrenci.OgrenciNo} - {kayit.Ders.DersKodu} not girişi reddedildi: {redGerekce.Trim()}",
            islemTuru: "Red");

        TempData["BasariMesaji"] = $"{kayit.Ders.DersKodu} not girişi gerekçesiyle reddedildi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostOnayliDuzeltAsync(int id, string yeniHarfNotu, decimal yeniPuanNotu, string gerekce)
    {
        if (string.IsNullOrWhiteSpace(gerekce))
        {
            TempData["HataMesaji"] = "Onaylanmış bir kaydı düzeltirken gerekçe zorunludur (Rapor Bölüm 7.1).";
            return RedirectToPage();
        }

        var kayit = await _db.DersKayitlari
            .Include(k => k.Ders)
            .Include(k => k.Ogrenci)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (kayit is null)
        {
            return NotFound();
        }

        var koordinatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var harf = yeniHarfNotu.Trim().ToUpperInvariant();
        kayit.HarfNotu = harf;
        kayit.PuanNotu = yeniPuanNotu;
        kayit.NotOnayDurumu = OnayDurumu.Onaylandi;
        kayit.NotOnayTarihi = DateTime.UtcNow;
        kayit.NotOnaylayanId = koordinatorId;
        kayit.DersDurumu = HarfNotuGectiMi(harf) ? DersDurumu.Gecti : DersDurumu.Kaldi;

        await _db.KaydetAsync(
            aciklama: $"Onaylı kayıt koordinatör tarafından düzeltildi ({harf} / {yeniPuanNotu}). Gerekçe: {gerekce.Trim()}",
            islemTuru: "Guncelleme");

        TempData["BasariMesaji"] = "Onaylı kayıt gerekçesiyle düzeltildi ve denetim izine kaydedildi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostOnayliIptalAsync(int id, string gerekce)
    {
        if (string.IsNullOrWhiteSpace(gerekce))
        {
            TempData["HataMesaji"] = "Onaylanmış bir kaydı iptal ederken gerekçe zorunludur.";
            return RedirectToPage();
        }

        var kayit = await _db.DersKayitlari
            .Include(k => k.Ders)
            .Include(k => k.Ogrenci)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (kayit is null)
        {
            return NotFound();
        }

        kayit.KayitOnayDurumu = OnayDurumu.IptalEdildi;
        kayit.KayitRedGerekce = gerekce.Trim();
        kayit.DersDurumu = DersDurumu.Cikarildi;

        await _db.KaydetAsync(
            aciklama: $"{kayit.Ogrenci.OgrenciNo} - {kayit.Ders.DersKodu} onaylı kaydı iptal edildi: {gerekce.Trim()}",
            islemTuru: "Iptal");

        TempData["BasariMesaji"] = "Kayıt silinmeden 'İptal Edildi' durumuna alındı.";
        return RedirectToPage();
    }

    public static bool HarfNotuGectiMi(string? harfNotu)
    {
        return harfNotu?.ToUpperInvariant() is "AA" or "BA" or "BB" or "CB" or "CC" or "DC" or "DD" or "MU";
    }
}
