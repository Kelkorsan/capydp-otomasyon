using System.ComponentModel.DataAnnotations;
using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.Koordinator;

public class DerslerModel : PageModel
{
    private readonly AppDbContext _db;

    public DerslerModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Ders> Dersler { get; set; } = [];
    public List<MuafiyetKurali> MuafiyetKurallari { get; set; } = [];

    [BindProperty]
    public YeniDersGirdisi YeniDers { get; set; } = new();

    public class YeniDersGirdisi
    {
        [Required]
        [MaxLength(20)]
        public string DersKodu { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DersAdi { get; set; } = string.Empty;

        [Range(1, 30)]
        public int Akts { get; set; } = 4;

        [Range(1, 8)]
        public int Yariyil { get; set; } = 3;

        public DersTuru DersTuru { get; set; } = DersTuru.Zorunlu;
    }

    public async Task OnGetAsync()
    {
        await YukleAsync();
    }

    public async Task<IActionResult> OnPostDersEkleAsync()
    {
        if (!ModelState.IsValid)
        {
            await YukleAsync();
            return Page();
        }

        var kod = YeniDers.DersKodu.Trim();
        if (await _db.Dersler.AnyAsync(d => d.DersKodu == kod))
        {
            TempData["HataMesaji"] = $"{kod} kodlu ders katalogda zaten mevcut.";
            return RedirectToPage();
        }

        var ders = new Ders
        {
            DersKodu = kod,
            DersAdi = YeniDers.DersAdi.Trim(),
            Akts = YeniDers.Akts,
            Yariyil = YeniDers.Yariyil,
            DersTuru = YeniDers.DersTuru,
            AktifMi = true
        };

        _db.Dersler.Add(ders);
        await _db.KaydetAsync($"Kataloğa yeni ders eklendi: {ders.DersKodu} - {ders.DersAdi}");
        TempData["BasariMesaji"] = $"{ders.DersKodu} — {ders.DersAdi} kataloğa eklendi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDersGuncelleAsync(int id, string dersAdi, int akts, int yariyil, DersTuru dersTuru)
    {
        var ders = await _db.Dersler.FindAsync(id);
        if (ders is null)
        {
            return NotFound();
        }

        ders.DersAdi = dersAdi.Trim();
        ders.Akts = akts;
        ders.Yariyil = yariyil;
        ders.DersTuru = dersTuru;

        await _db.KaydetAsync($"Katalog dersi güncellendi: {ders.DersKodu}");
        TempData["BasariMesaji"] = $"{ders.DersKodu} ders bilgileri güncellendi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAktiflikDegistirAsync(int id)
    {
        var ders = await _db.Dersler.FindAsync(id);
        if (ders is null)
        {
            return NotFound();
        }

        // Rapor 4.2.3: Ders silinmez, pasife alınır; geçmiş kayıtlar korunur, yeni kayıtlarda görünmez.
        ders.AktifMi = !ders.AktifMi;
        var durumMetni = ders.AktifMi ? "aktif edildi" : "pasife alındı";

        await _db.KaydetAsync($"{ders.DersKodu} dersi {durumMetni} (Soft-delete prensibi).");
        TempData["BasariMesaji"] = $"{ders.DersKodu} — {ders.DersAdi} {durumMetni}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMuafiyetEkleAsync(int capDersId, string anadalDersKodu, string anadalDersAdi, string aciklama)
    {
        _db.MuafiyetKurallari.Add(new MuafiyetKurali
        {
            CapDersId = capDersId,
            AnadalDersKodu = anadalDersKodu.Trim(),
            AnadalDersAdi = anadalDersAdi.Trim(),
            Aciklama = aciklama.Trim()
        });

        await _db.KaydetAsync($"Yeni protokol muafiyet kuralı tanımlandı ({anadalDersKodu}).");
        TempData["BasariMesaji"] = "Protokol muafiyet kuralı eklendi.";
        return RedirectToPage();
    }

    private async Task YukleAsync()
    {
        Dersler = await _db.Dersler
            .OrderBy(d => d.Yariyil)
            .ThenBy(d => d.DersKodu)
            .ToListAsync();

        MuafiyetKurallari = await _db.MuafiyetKurallari
            .Include(m => m.CapDers)
            .OrderBy(m => m.CapDers.DersKodu)
            .ToListAsync();
    }
}
