using CapYdp.Web.Data;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Pages.Koordinator;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public AkademikDonem? AktifDonem { get; set; }
    public int AktifCapOgrenciSayisi { get; set; }
    public int AktifYdpOgrenciSayisi { get; set; }
    public int BekleyenDersKayitSayisi { get; set; }
    public int BekleyenNotOnaySayisi { get; set; }
    public List<Ogrenci> BuDonemKayitYapmayanlar { get; set; } = [];
    public List<Ogrenci> GanoRiskliOgrenciler { get; set; } = [];
    public List<IslemKaydi> SonIslemler { get; set; } = [];

    public async Task OnGetAsync()
    {
        AktifDonem = await _db.AkademikDonemler.FirstOrDefaultAsync(d => d.AktifMi);

        AktifCapOgrenciSayisi = await _db.Ogrenciler
            .CountAsync(o => o.Durum == OgrenciDurumu.Aktif && o.ProgramTuru == ProgramTuru.CAP);

        AktifYdpOgrenciSayisi = await _db.Ogrenciler
            .CountAsync(o => o.Durum == OgrenciDurumu.Aktif && o.ProgramTuru == ProgramTuru.YDP);

        BekleyenDersKayitSayisi = await _db.DersKayitlari
            .CountAsync(k => k.KayitOnayDurumu == OnayDurumu.Bekliyor);

        BekleyenNotOnaySayisi = await _db.DersKayitlari
            .CountAsync(k => k.NotOnayDurumu == OnayDurumu.Bekliyor);

        if (AktifDonem is not null)
        {
            var aktifDonemId = AktifDonem.Id;
            BuDonemKayitYapmayanlar = await _db.Ogrenciler
                .Include(o => o.Kullanici)
                .Include(o => o.KabulDonemi)
                .Where(o => o.Durum == OgrenciDurumu.Aktif
                            && !o.DersKayitlari.Any(dk => dk.DonemId == aktifDonemId
                                                          && dk.KayitOnayDurumu != OnayDurumu.Reddedildi
                                                          && dk.KayitOnayDurumu != OnayDurumu.IptalEdildi))
                .OrderBy(o => o.OgrenciNo)
                .ToListAsync();
        }

        // Rapor Bölüm 8: ÇAP için 2.72, YDP için 2.30 altındaki anadal GANO'lar uyarı listesinde gösterilir.
        GanoRiskliOgrenciler = await _db.Ogrenciler
            .Include(o => o.Kullanici)
            .Where(o => o.Durum == OgrenciDurumu.Aktif
                        && o.AnadalGano != null
                        && ((o.ProgramTuru == ProgramTuru.CAP && o.AnadalGano < 2.72m)
                            || (o.ProgramTuru == ProgramTuru.YDP && o.AnadalGano < 2.30m)))
            .ToListAsync();

        SonIslemler = await _db.IslemKayitlari
            .OrderByDescending(i => i.Tarih)
            .Take(8)
            .ToListAsync();
    }
}
