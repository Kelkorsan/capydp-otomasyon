using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.Koordinator;

public class DonemlerModel : PageModel
{
    private readonly AppDbContext _db;

    public DonemlerModel(AppDbContext db)
    {
        _db = db;
    }

    public List<AkademikDonem> Donemler { get; set; } = [];

    public async Task OnGetAsync()
    {
        Donemler = await _db.AkademikDonemler
            .OrderByDescending(d => d.Yil)
            .ThenByDescending(d => d.Id)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostEkleAsync(int yil, DonemTipi donemTipi, bool aktifYap)
    {
        if (await _db.AkademikDonemler.AnyAsync(d => d.Yil == yil && d.DonemTipi == donemTipi))
        {
            TempData["HataMesaji"] = "Bu akademik yıl ve dönem zaten tanımlı.";
            return RedirectToPage();
        }

        if (aktifYap)
        {
            foreach (var d in await _db.AkademikDonemler.Where(x => x.AktifMi).ToListAsync())
            {
                d.AktifMi = false;
            }
        }

        var tipMetin = donemTipi == DonemTipi.Guz ? "Güz" : "Bahar";
        var donem = new AkademikDonem
        {
            Yil = yil,
            DonemTipi = donemTipi,
            DonemAdi = $"{yil}-{yil + 1} {tipMetin}",
            AktifMi = aktifYap
        };

        _db.AkademikDonemler.Add(donem);
        await _db.KaydetAsync($"Yeni akademik dönem eklendi: {donem.DonemAdi}");
        TempData["BasariMesaji"] = $"{donem.DonemAdi} dönemi oluşturuldu.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAktifYapAsync(int id)
    {
        var tumDonemler = await _db.AkademikDonemler.ToListAsync();
        var secilen = tumDonemler.FirstOrDefault(d => d.Id == id);
        if (secilen is null)
        {
            return NotFound();
        }

        foreach (var d in tumDonemler)
        {
            d.AktifMi = d.Id == id;
        }

        await _db.KaydetAsync($"Aktif akademik dönem '{secilen.DonemAdi}' olarak değiştirildi.");
        TempData["BasariMesaji"] = $"Aktif dönem '{secilen.DonemAdi}' olarak ayarlandı.";
        return RedirectToPage();
    }
}
