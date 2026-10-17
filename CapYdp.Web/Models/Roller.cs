namespace CapYdp.Web.Models;

/// <summary>
/// Sistemdeki kullanıcı rolleri (Rapor Bölüm 3.1).
/// Rol adları kod içinde sabit metin olarak dağılmasın diye tek yerde tutulur.
/// </summary>
public static class Roller
{
    public const string Koordinator = "Koordinator";
    public const string Ogrenci = "Ogrenci";
    public const string BolumBaskani = "BolumBaskani";

    public static readonly string[] Tumu = [Koordinator, Ogrenci, BolumBaskani];
}

/// <summary>
/// Yetkilendirme politika adları. Sayfa klasörleri bu politikalarla korunur (Program.cs).
/// </summary>
public static class Politikalar
{
    public const string KoordinatorPolitikasi = "KoordinatorPolitikasi";
    public const string OgrenciPolitikasi = "OgrenciPolitikasi";
    public const string BolumBaskaniPolitikasi = "BolumBaskaniPolitikasi";
}
