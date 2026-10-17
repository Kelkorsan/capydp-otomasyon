namespace CapYdp.Web.Models;

// Not: Tüm enum'lar veritabanında okunabilir olması için metin (nvarchar) olarak saklanır
// (bkz. AppDbContext.ConfigureConventions). Rapordaki "nvarchar" alan tanımlarıyla uyumludur.

public enum ProgramTuru
{
    CAP,
    YDP
}

public enum OgrenciDurumu
{
    Aktif,
    Dondurulmus,
    Cikarilmis,
    Mezun
}

public enum DersTuru
{
    Zorunlu,
    TeknikSecmeli
}

public enum DonemTipi
{
    Guz,
    Bahar
}

public enum DersListesiDurumu
{
    Aktif,
    Cikarildi,
    Muaf
}

/// <summary>Onay mekanizması durumları (Rapor Bölüm 7.1).</summary>
public enum OnayDurumu
{
    Bekliyor,
    Onaylandi,
    Reddedildi,
    IptalEdildi
}

public enum DersDurumu
{
    Planlandi,
    Alindi,
    Gecti,
    Kaldi,
    Muaf,
    Cikarildi
}
