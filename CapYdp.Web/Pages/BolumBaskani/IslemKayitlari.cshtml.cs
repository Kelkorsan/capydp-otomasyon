using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.BolumBaskani;

public class IslemKayitlariModel : PageModel
{
    private readonly AppDbContext _db;

    public IslemKayitlariModel(AppDbContext db)
    {
        _db = db;
    }

    public List<IslemKaydi> Kayitlar { get; set; } = [];
    public Dictionary<int, string> KullaniciAdlari { get; set; } = [];

    public async Task OnGetAsync()
    {
        Kayitlar = await _db.IslemKayitlari
            .OrderByDescending(x => x.Tarih)
            .ThenByDescending(x => x.Id)
            .Take(100)
            .ToListAsync();

        KullaniciAdlari = await _db.Users
            .ToDictionaryAsync(u => u.Id, u => $"{u.Ad} {u.Soyad}");
    }
}
