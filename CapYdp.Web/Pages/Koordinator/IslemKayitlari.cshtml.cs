using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.Koordinator;

public class IslemKayitlariModel : PageModel
{
    private readonly AppDbContext _db;

    public IslemKayitlariModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? TabloAdi { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? IslemTuru { get; set; }

    public List<IslemKaydi> Kayitlar { get; set; } = [];
    public Dictionary<int, string> KullaniciAdlari { get; set; } = [];

    public async Task OnGetAsync()
    {
        var sorgu = _db.IslemKayitlari.AsQueryable();

        if (!string.IsNullOrWhiteSpace(TabloAdi))
        {
            sorgu = sorgu.Where(x => x.TabloAdi == TabloAdi);
        }

        if (!string.IsNullOrWhiteSpace(IslemTuru))
        {
            sorgu = sorgu.Where(x => x.IslemTuru == IslemTuru);
        }

        Kayitlar = await sorgu
            .OrderByDescending(x => x.Tarih)
            .ThenByDescending(x => x.Id)
            .Take(100)
            .ToListAsync();

        KullaniciAdlari = await _db.Users
            .ToDictionaryAsync(u => u.Id, u => $"{u.Ad} {u.Soyad} ({u.Email})");
    }
}
