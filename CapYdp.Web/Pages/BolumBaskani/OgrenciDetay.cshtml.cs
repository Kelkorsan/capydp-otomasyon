using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.BolumBaskani;

public class OgrenciDetayModel : PageModel
{
    private readonly AppDbContext _db;

    public OgrenciDetayModel(AppDbContext db)
    {
        _db = db;
    }

    public Ogrenci Ogrenci { get; set; } = null!;
    public ProtokolAktsOzeti AktsOzeti { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var ogrenci = await _db.Ogrenciler
            .Include(o => o.Kullanici)
            .Include(o => o.KabulDonemi)
            .Include(o => o.DersListesi.OrderBy(dl => dl.Ders.Yariyil)).ThenInclude(dl => dl.Ders)
            .Include(o => o.DersKayitlari.OrderByDescending(dk => dk.Donem.Yil)).ThenInclude(dk => dk.Ders)
            .Include(o => o.DersKayitlari).ThenInclude(dk => dk.Donem)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (ogrenci is null)
        {
            return NotFound();
        }

        Ogrenci = ogrenci;
        AktsOzeti = ProtokolHesaplayici.Hesapla(Ogrenci);
        return Page();
    }
}
