using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.BolumBaskani;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Ogrenci> Ogrenciler { get; set; } = [];

    public async Task OnGetAsync()
    {
        Ogrenciler = await _db.Ogrenciler
            .Include(o => o.Kullanici)
            .Include(o => o.KabulDonemi)
            .OrderBy(o => o.OgrenciNo)
            .ToListAsync();
    }
}
