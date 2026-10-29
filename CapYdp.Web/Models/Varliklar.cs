using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CapYdp.Web.Models;

/// <summary>
/// Bu arayüzü uygulayan her varlığın değişiklikleri otomatik olarak IslemKaydi tablosuna yazılır
/// (Rapor Bölüm 11). Identity tabloları bilerek dışarıda tutulur: PasswordHash / SecurityStamp
/// gibi gizli alanların denetim izine sızmasını istemeyiz.
/// </summary>
public interface IDenetlenebilir;

/// <summary>
/// Rapor Tablo 1 (Kullanici). Ayrı bir tablo yerine ASP.NET Core Identity'nin kullanıcı tipini
/// genişletiyoruz: şifre hash'leme, kilitleme, güvenlik damgası gibi işleri çatı üstlenir.
/// </summary>
public class Kullanici : IdentityUser<int>
{
    [MaxLength(100)]
    public string Ad { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Soyad { get; set; } = string.Empty;

    public bool AktifMi { get; set; } = true;

    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string AdSoyad => $"{Ad} {Soyad}";
}

/// <summary>
/// Rapor Tablo 2. Ad-soyad ve e-posta, tekrar (ve zamanla tutarsızlık) olmasın diye
/// yalnızca bağlı Kullanici hesabında tutulur.
/// </summary>
public class Ogrenci : IDenetlenebilir
{
    public int Id { get; set; }

    public int KullaniciId { get; set; }
    public Kullanici Kullanici { get; set; } = null!;

    /// <summary>ÇAP/YDP öğrenci numarası — sistem genelinde benzersiz anahtar (OBİS uyumu için).</summary>
    [MaxLength(20)]
    public string OgrenciNo { get; set; } = string.Empty;

    public ProgramTuru ProgramTuru { get; set; }

    [MaxLength(150)]
    public string AnadalFakulte { get; set; } = string.Empty;

    [MaxLength(150)]
    public string AnadalBolum { get; set; } = string.Empty;

    public int KabulDonemiId { get; set; }
    public AkademikDonem KabulDonemi { get; set; } = null!;

    public OgrenciDurumu Durum { get; set; } = OgrenciDurumu.Aktif;

    /// <summary>Yalnızca koordinatör girer; kaynağı zorunlu olarak belirtilir (Rapor 8.1).</summary>
    [Column(TypeName = "decimal(3,2)")]
    public decimal? AnadalGano { get; set; }

    [MaxLength(100)]
    public string? GanoKaynagi { get; set; }

    public DateTime? GanoGuncellemeTarihi { get; set; }

    public ICollection<OgrenciDersListesi> DersListesi { get; set; } = [];
    public ICollection<DersKayit> DersKayitlari { get; set; } = [];
}

/// <summary>Rapor Tablo 3 — Bölüm ders kataloğu.</summary>
public class Ders : IDenetlenebilir
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string DersKodu { get; set; } = string.Empty;

    [MaxLength(200)]
    public string DersAdi { get; set; } = string.Empty;

    public int Akts { get; set; }

    /// <summary>Dersin ait olduğu yarıyıl (1–8).</summary>
    public int Yariyil { get; set; }

    public DersTuru DersTuru { get; set; }

    /// <summary>Ders silinmez, pasife alınır; geçmiş kayıtlar korunur (Rapor 4.2.3).</summary>
    public bool AktifMi { get; set; } = true;
}

/// <summary>Rapor Tablo 5 — Protokol özel şartları / anadal dersi muafiyet kuralları.</summary>
public class MuafiyetKurali : IDenetlenebilir
{
    public int Id { get; set; }

    public int CapDersId { get; set; }
    public Ders CapDers { get; set; } = null!;

    [MaxLength(20)]
    public string AnadalDersKodu { get; set; } = string.Empty;

    [MaxLength(200)]
    public string AnadalDersAdi { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Aciklama { get; set; } = string.Empty;
}


/// <summary>Rapor Tablo 6.</summary>
public class AkademikDonem : IDenetlenebilir
{
    public int Id { get; set; }

    /// <summary>Örn: "2026-2027 Güz"</summary>
    [MaxLength(50)]
    public string DonemAdi { get; set; } = string.Empty;

    /// <summary>Akademik yılın başlangıç yılı (2026-2027 için 2026).</summary>
    public int Yil { get; set; }

    public DonemTipi DonemTipi { get; set; }

    /// <summary>Aynı anda yalnızca bir dönem aktif olabilir (filtreli benzersiz indeksle zorlanır).</summary>
    public bool AktifMi { get; set; }
}

/// <summary>Rapor Tablo 4 — Öğrencinin protokolden atanmış "alacağı dersler" listesi.</summary>
public class OgrenciDersListesi : IDenetlenebilir
{
    public int Id { get; set; }

    public int OgrenciId { get; set; }
    public Ogrenci Ogrenci { get; set; } = null!;

    public int DersId { get; set; }
    public Ders Ders { get; set; } = null!;

    public bool OzelSartliMi { get; set; }

    public DersListesiDurumu Durum { get; set; } = DersListesiDurumu.Aktif;

    [MaxLength(500)]
    public string? DegisiklikGerekce { get; set; }

    public DateTime EklemeTarihi { get; set; } = DateTime.UtcNow;

    public DateTime? DegisiklikTarihi { get; set; }
}

/// <summary>
/// Rapor Tablo 7 — Dönemlik ders kaydı ve notu. Bir ders birden çok kez alınabilir; her deneme ayrı satırdır.
///
/// TASARIM DÜZELTMESİ: Raporda tek bir "OnayDurumu" alanı vardı. Ancak aynı satır iki ayrı onaydan geçer:
///   1) Dönem başı ders kaydı onayı, 2) Dönem sonu not onayı.
/// Tek alanla, kaydı onaylanıp kilitlenen bir derse not girilemez hale gelirdi. Bu yüzden iki onay
/// ayrı alanlarla takip edilir. Kilit durumu da bu alanlardan türetilir (ayrı bir KilitliMi alanı yok).
/// </summary>
public class DersKayit : IDenetlenebilir
{
    public int Id { get; set; }

    public int OgrenciId { get; set; }
    public Ogrenci Ogrenci { get; set; } = null!;

    public int DersId { get; set; }
    public Ders Ders { get; set; } = null!;

    public int DonemId { get; set; }
    public AkademikDonem Donem { get; set; } = null!;

    // --- 1) Ders kaydı onayı ---
    public OnayDurumu KayitOnayDurumu { get; set; } = OnayDurumu.Bekliyor;

    [MaxLength(500)]
    public string? KayitRedGerekce { get; set; }

    public DateTime? KayitOnayTarihi { get; set; }

    public int? KayitOnaylayanId { get; set; }
    public Kullanici? KayitOnaylayan { get; set; }

    // --- 2) Not onayı (not girilene kadar null) ---
    [MaxLength(2)]
    public string? HarfNotu { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? PuanNotu { get; set; }

    public OnayDurumu? NotOnayDurumu { get; set; }

    [MaxLength(500)]
    public string? NotRedGerekce { get; set; }

    public DateTime? NotOnayTarihi { get; set; }

    public int? NotOnaylayanId { get; set; }
    public Kullanici? NotOnaylayan { get; set; }

    // --- Genel ---
    public DersDurumu DersDurumu { get; set; } = DersDurumu.Planlandi;

    /// <summary>Bu dersin kaçıncı denemesi olduğu (1, 2, ...).</summary>
    public int DenemeSayisi { get; set; } = 1;

    public int GirenId { get; set; }
    public Kullanici Giren { get; set; } = null!;

    public DateTime GirisTarihi { get; set; } = DateTime.UtcNow;

    /// <summary>Mevcut öğrenci aktarımında koordinatörün geriye dönük girdiği kayıt (onay kilidine takılmaz).</summary>
    public bool GeriyeDonukMu { get; set; }

    [NotMapped]
    public bool KayitKilitliMi => KayitOnayDurumu != OnayDurumu.Bekliyor;

    [NotMapped]
    public bool NotKilitliMi => NotOnayDurumu is not null and not OnayDurumu.Bekliyor;
}

/// <summary>
/// Rapor Tablo 15 — Denetim izi. Uygulama katmanında değiştirilemez/silinemez
/// (AppDbContext bu tabloda Modified/Deleted durumunu reddeder).
/// </summary>
public class IslemKaydi
{
    public long Id { get; set; }

    /// <summary>null = sistem işlemi (örn. başlangıç verisi).</summary>
    public int? KullaniciId { get; set; }

    [MaxLength(30)]
    public string IslemTuru { get; set; } = string.Empty;

    [MaxLength(100)]
    public string TabloAdi { get; set; } = string.Empty;

    [MaxLength(50)]
    public string KayitId { get; set; } = string.Empty;

    public string? EskiDeger { get; set; }

    public string? YeniDeger { get; set; }

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public DateTime Tarih { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IpAdresi { get; set; }
}

public record ProtokolAktsOzeti(
    int ToplamHedefAkts,
    int TamamlananAkts,
    int KalanAkts,
    int TamamlananDersSayisi,
    int ToplamHedefDersSayisi,
    int ZorunluHedefAkts,
    int TeknikSecmeliHedefAkts,
    int TamamlananTeknikSecmeliAkts,
    HashSet<int> GecilenDersIdleri);

/// <summary>
/// ÇAP ve YDP mezuniyet/sertifika AKTS yükümlülüklerini resmi yönerge ve protokole göre tek merkezden hesaplar.
/// </summary>
public static class ProtokolHesaplayici
{
    // Selçuk Üni. EEM -> Bilgisayar Müh. ÇAP Kabul ve Ders Protokol Formu:
    // 20 zorunlu ders (103 AKTS) + 8 teknik seçmeli ders (32 AKTS: 5.yy 3x4, 6.yy 3x4, 7.yy 2x4) = 135 AKTS.
    public const int CapTeknikSecmeliHedefAkts = 32;
    public const int CapTeknikSecmeliHedefDersSayisi = 8;

    // Selçuk Üni. Yan Dal Yönergesi Md. 5(g) (YDP-AKTS-01): en az 30 AKTS ve en az 6 ders.
    public const int YdpAsgariHedefAkts = 30;
    public const int YdpAsgariHedefDersSayisi = 6;

    public static ProtokolAktsOzeti Hesapla(Ogrenci ogrenci)
    {
        var aktifListe = ogrenci.DersListesi
            .Where(dl => dl.Durum != DersListesiDurumu.Cikarildi)
            .ToList();

        var onayliGecilenKayitlar = ogrenci.DersKayitlari
            .Where(dk => dk.NotOnayDurumu == OnayDurumu.Onaylandi
                         && dk.DersDurumu is DersDurumu.Gecti or DersDurumu.Muaf)
            .ToList();

        var gecilenDersIdleri = onayliGecilenKayitlar
            .Select(dk => dk.DersId)
            .Union(aktifListe.Where(dl => dl.Durum == DersListesiDurumu.Muaf).Select(dl => dl.DersId))
            .ToHashSet();

        var listedenTamamlanan = aktifListe
            .Where(dl => gecilenDersIdleri.Contains(dl.DersId))
            .ToList();

        var tumListeDersIdleri = ogrenci.DersListesi
            .Select(dl => dl.DersId)
            .ToHashSet();

        // Protokol listesi dışında ders havuzundan alınıp onaylı olarak geçilen/muaf olunan dersler
        var havuzdanTamamlananDersler = onayliGecilenKayitlar
            .Where(dk => !tumListeDersIdleri.Contains(dk.DersId))
            .Select(dk => dk.Ders)
            .DistinctBy(d => d.Id)
            .ToList();

        var tamamlananAkts = listedenTamamlanan.Sum(dl => dl.Ders.Akts)
                             + havuzdanTamamlananDersler.Sum(d => d.Akts);

        var tamamlananDersSayisi = listedenTamamlanan.Count + havuzdanTamamlananDersler.Count;

        var tamamlananTeknikSecmeliAkts =
            listedenTamamlanan.Where(dl => dl.Ders.DersTuru == DersTuru.TeknikSecmeli).Sum(dl => dl.Ders.Akts)
            + havuzdanTamamlananDersler.Where(d => d.DersTuru == DersTuru.TeknikSecmeli).Sum(d => d.Akts);

        if (ogrenci.ProgramTuru == ProgramTuru.CAP)
        {
            var zorunluListeAkts = aktifListe
                .Where(dl => dl.Ders.DersTuru == DersTuru.Zorunlu)
                .Sum(dl => dl.Ders.Akts);
            var listedekiSecmeliAkts = aktifListe
                .Where(dl => dl.Ders.DersTuru == DersTuru.TeknikSecmeli)
                .Sum(dl => dl.Ders.Akts);
            var listedekiSecmeliAdet = aktifListe
                .Count(dl => dl.Ders.DersTuru == DersTuru.TeknikSecmeli);

            var ekSecmeliAktsKotasi = Math.Max(0, CapTeknikSecmeliHedefAkts - listedekiSecmeliAkts);
            var ekSecmeliDersKotasi = Math.Max(0, CapTeknikSecmeliHedefDersSayisi - listedekiSecmeliAdet);

            var toplamHedefAkts = aktifListe.Sum(dl => dl.Ders.Akts) + ekSecmeliAktsKotasi;
            var toplamHedefDers = aktifListe.Count + ekSecmeliDersKotasi;
            var teknikHedefAkts = Math.Max(CapTeknikSecmeliHedefAkts, listedekiSecmeliAkts);

            return new ProtokolAktsOzeti(
                ToplamHedefAkts: toplamHedefAkts,
                TamamlananAkts: tamamlananAkts,
                KalanAkts: Math.Max(0, toplamHedefAkts - tamamlananAkts),
                TamamlananDersSayisi: tamamlananDersSayisi,
                ToplamHedefDersSayisi: toplamHedefDers,
                ZorunluHedefAkts: zorunluListeAkts,
                TeknikSecmeliHedefAkts: teknikHedefAkts,
                TamamlananTeknikSecmeliAkts: tamamlananTeknikSecmeliAkts,
                GecilenDersIdleri: gecilenDersIdleri);
        }
        else
        {
            var planlananAkts = aktifListe.Sum(dl => dl.Ders.Akts);
            var planlananDers = aktifListe.Count;
            var toplamHedefAkts = Math.Max(YdpAsgariHedefAkts, planlananAkts);
            var toplamHedefDers = Math.Max(YdpAsgariHedefDersSayisi, planlananDers);

            return new ProtokolAktsOzeti(
                ToplamHedefAkts: toplamHedefAkts,
                TamamlananAkts: tamamlananAkts,
                KalanAkts: Math.Max(0, toplamHedefAkts - tamamlananAkts),
                TamamlananDersSayisi: tamamlananDersSayisi,
                ToplamHedefDersSayisi: toplamHedefDers,
                ZorunluHedefAkts: planlananAkts,
                TeknikSecmeliHedefAkts: 0,
                TamamlananTeknikSecmeliAkts: tamamlananTeknikSecmeliAkts,
                GecilenDersIdleri: gecilenDersIdleri);
        }
    }
}
