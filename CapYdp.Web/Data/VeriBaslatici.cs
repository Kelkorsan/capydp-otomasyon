using CapYdp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CapYdp.Web.Data;

/// <summary>
/// Geliştirme ve demo ortamı için sentetik başlangıç verilerini (roller, örnek hesaplar,
/// Selçuk Üniversitesi Bilgisayar Mühendisliği ÇAP/YDP protokol dersleri ve muafiyet kuralları) yükler.
/// </summary>
public static class VeriBaslatici
{
    public const string DemoSifre = "CapYdp!2026";

    public static async Task BaslatAsync(IServiceProvider servisler)
    {
        using var kapsam = servisler.CreateScope();
        var db = kapsam.ServiceProvider.GetRequiredService<AppDbContext>();
        var rolYoneticisi = kapsam.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var kullaniciYoneticisi = kapsam.ServiceProvider.GetRequiredService<UserManager<Kullanici>>();

        // 1) Rolleri oluştur
        foreach (var rolAdi in Roller.Tumu)
        {
            if (!await rolYoneticisi.RoleExistsAsync(rolAdi))
            {
                await rolYoneticisi.CreateAsync(new IdentityRole<int>(rolAdi));
            }
        }

        // 2) Koordinatör ve Bölüm Başkanı hesapları
        var koordinator = await KullaniciYoksaOlusturAsync(
            kullaniciYoneticisi,
            email: "koordinator@selcuk.edu.tr",
            ad: "Doç. Dr. ÇAP/YDP",
            soyad: "Koordinatörü",
            rol: Roller.Koordinator);

        await KullaniciYoksaOlusturAsync(
            kullaniciYoneticisi,
            email: "bolumbaskani@selcuk.edu.tr",
            ad: "Prof. Dr. Bölüm",
            soyad: "Başkanı",
            rol: Roller.BolumBaskani);

        // 3) Akademik dönemler
        if (!await db.AkademikDonemler.AnyAsync())
        {
            db.AkademikDonemler.AddRange(
                new AkademikDonem { DonemAdi = "2025-2026 Güz", Yil = 2025, DonemTipi = DonemTipi.Guz, AktifMi = false },
                new AkademikDonem { DonemAdi = "2025-2026 Bahar", Yil = 2025, DonemTipi = DonemTipi.Bahar, AktifMi = false },
                new AkademikDonem { DonemAdi = "2026-2027 Güz", Yil = 2026, DonemTipi = DonemTipi.Guz, AktifMi = true }
            );
            await db.KaydetAsync("Başlangıç akademik dönem tanımları oluşturuldu.");
        }

        // 4) Selçuk Üni. Bilgisayar Mühendisliği gerçek protokol ders kataloğu
        if (!await db.Dersler.AnyAsync())
        {
            var dersler = new List<Ders>
            {
                new() { DersKodu = "3301156", DersAdi = "Bilgisayar Mühendisliğine Giriş", Akts = 4, Yariyil = 1, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301351", DersAdi = "Nesneye Yönelik Prog. Dili", Akts = 4, Yariyil = 3, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301352", DersAdi = "İşletim Sistemleri", Akts = 4, Yariyil = 3, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301354", DersAdi = "Veri Yapıları ve Uygulamaları", Akts = 4, Yariyil = 3, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301452", DersAdi = "Programlama Dili-2", Akts = 4, Yariyil = 4, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301454", DersAdi = "Veri Tabanı Yönetim Sistemleri", Akts = 5, Yariyil = 4, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301455", DersAdi = "Yazılım Mühendisliği", Akts = 4, Yariyil = 4, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301456", DersAdi = "Mobil Programlama", Akts = 4, Yariyil = 4, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301552", DersAdi = "Programlama Dili-3", Akts = 5, Yariyil = 5, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301553", DersAdi = "Veri Haberleşmesi ve Ağlar", Akts = 5, Yariyil = 5, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301561", DersAdi = "Sinyal İşleme", Akts = 4, Yariyil = 5, DersTuru = DersTuru.TeknikSecmeli },
                new() { DersKodu = "3301568", DersAdi = "Veri Madenciliği", Akts = 4, Yariyil = 5, DersTuru = DersTuru.TeknikSecmeli },
                new() { DersKodu = "3301651", DersAdi = "Yapay Zeka", Akts = 4, Yariyil = 6, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301652", DersAdi = "Otomata Teorisi ve Biçimsel Diller", Akts = 4, Yariyil = 6, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301653", DersAdi = "Görüntü İşleme", Akts = 4, Yariyil = 6, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301654", DersAdi = "Mühendislik Tasarımı", Akts = 6, Yariyil = 6, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301663", DersAdi = "Gömülü Sistemler", Akts = 4, Yariyil = 6, DersTuru = DersTuru.TeknikSecmeli },
                new() { DersKodu = "3301665", DersAdi = "Robotik", Akts = 4, Yariyil = 6, DersTuru = DersTuru.TeknikSecmeli },
                new() { DersKodu = "3301751", DersAdi = "Bilgisayar Mühendisliği Uygulamaları", Akts = 4, Yariyil = 7, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301752", DersAdi = "Bilgisayar Mimarisi ve Organizasyonu", Akts = 4, Yariyil = 7, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301753", DersAdi = "Veri Bilimi", Akts = 4, Yariyil = 7, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301770", DersAdi = "Veri İletişim Teknikleri", Akts = 4, Yariyil = 7, DersTuru = DersTuru.TeknikSecmeli },
                new() { DersKodu = "3301851", DersAdi = "İşletmede Mesleki Eğitim", Akts = 20, Yariyil = 8, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301852", DersAdi = "Staj 1", Akts = 5, Yariyil = 8, DersTuru = DersTuru.Zorunlu },
                new() { DersKodu = "3301853", DersAdi = "Staj 2", Akts = 5, Yariyil = 8, DersTuru = DersTuru.Zorunlu }
            };

            db.Dersler.AddRange(dersler);
            await db.KaydetAsync("Bölüm ders kataloğu protokol belgesinden yüklendi.");

            var dersSozluk = await db.Dersler.ToDictionaryAsync(d => d.DersKodu, d => d.Id);
            db.MuafiyetKurallari.AddRange(
                new MuafiyetKurali { CapDersId = dersSozluk["3301354"], AnadalDersKodu = "3302458", AnadalDersAdi = "Veri Yapıları", Aciklama = "EEM 4. Yarıyıl Teknik Seçmeli alınırsa muaf sayılır." },
                new MuafiyetKurali { CapDersId = dersSozluk["3301561"], AnadalDersKodu = "3302758", AnadalDersAdi = "Sayısal Sinyal İşleme", Aciklama = "EEM 7. Yarıyıl Teknik Seçmeli alınırsa muaf sayılır." },
                new MuafiyetKurali { CapDersId = dersSozluk["3301568"], AnadalDersKodu = "3302459", AnadalDersAdi = "Mühendislik Veri Madenciliği", Aciklama = "EEM 4. Yarıyıl Teknik Seçmeli alınırsa muaf sayılır." },
                new MuafiyetKurali { CapDersId = dersSozluk["3301651"], AnadalDersKodu = "3302561", AnadalDersAdi = "Makine Öğrenmesi ve Yapay Zekâ", Aciklama = "EEM 5. Yarıyıl Teknik Seçmeli alınırsa muaf sayılır." },
                new MuafiyetKurali { CapDersId = dersSozluk["3301653"], AnadalDersKodu = "3302659", AnadalDersAdi = "Görüntü İşleme", Aciklama = "EEM 6. Yarıyıl Teknik Seçmeli alınırsa muaf sayılır." },
                new MuafiyetKurali { CapDersId = dersSozluk["3301663"], AnadalDersKodu = "3302656", AnadalDersAdi = "Gömülü Sistemler", Aciklama = "EEM 6. Yarıyıl Teknik Seçmeli alınırsa muaf sayılır." },
                new MuafiyetKurali { CapDersId = dersSozluk["3301665"], AnadalDersKodu = "3302563", AnadalDersAdi = "Robotik Sistemlere Giriş", Aciklama = "EEM 5. Yarıyıl Teknik Seçmeli alınırsa muaf sayılır." },
                new MuafiyetKurali { CapDersId = dersSozluk["3301770"], AnadalDersKodu = "3302556", AnadalDersAdi = "Veri İletişim Teknikleri", Aciklama = "EEM 5. Yarıyıl Teknik Seçmeli alınırsa muaf sayılır." }
            );
            await db.KaydetAsync("Protokol özel muafiyet kuralları yüklendi.");
        }

        // 5) Demo öğrenciler ve örnek ders/onay kayıtları
        if (!await db.Ogrenciler.AnyAsync())
        {
            var guz25 = await db.AkademikDonemler.FirstAsync(d => d.DonemAdi == "2025-2026 Güz");
            var bahar26 = await db.AkademikDonemler.FirstAsync(d => d.DonemAdi == "2025-2026 Bahar");
            var guz26 = await db.AkademikDonemler.FirstAsync(d => d.DonemAdi == "2026-2027 Güz");
            var dersler = await db.Dersler.ToDictionaryAsync(d => d.DersKodu);

            var ahmetKullanici = await KullaniciYoksaOlusturAsync(
                kullaniciYoneticisi,
                email: "ahmet.yilmaz@ogr.selcuk.edu.tr",
                ad: "Ahmet",
                soyad: "Yılmaz",
                rol: Roller.Ogrenci);

            var zeynepKullanici = await KullaniciYoksaOlusturAsync(
                kullaniciYoneticisi,
                email: "zeynep.kaya@ogr.selcuk.edu.tr",
                ad: "Zeynep",
                soyad: "Kaya",
                rol: Roller.Ogrenci);

            var ahmet = new Ogrenci
            {
                KullaniciId = ahmetKullanici.Id,
                OgrenciNo = "223301045",
                ProgramTuru = ProgramTuru.CAP,
                AnadalFakulte = "Teknoloji Fakültesi",
                AnadalBolum = "Elektrik-Elektronik Mühendisliği",
                KabulDonemiId = guz25.Id,
                Durum = OgrenciDurumu.Aktif,
                AnadalGano = 3.18m,
                GanoKaynagi = "2025-2026 Bahar Resmi Transkript",
                GanoGuncellemeTarihi = DateTime.UtcNow.AddDays(-15)
            };

            var zeynep = new Ogrenci
            {
                KullaniciId = zeynepKullanici.Id,
                OgrenciNo = "233301088",
                ProgramTuru = ProgramTuru.YDP,
                AnadalFakulte = "Teknoloji Fakültesi",
                AnadalBolum = "Elektrik-Elektronik Mühendisliği",
                KabulDonemiId = guz26.Id,
                Durum = OgrenciDurumu.Aktif,
                AnadalGano = 2.84m,
                GanoKaynagi = "Kabul Protokolü Başvuru Transkripti",
                GanoGuncellemeTarihi = DateTime.UtcNow.AddDays(-10)
            };

            db.Ogrenciler.AddRange(ahmet, zeynep);
            await db.KaydetAsync("Demo ÇAP ve YDP öğrencileri eklendi.");

            // Ahmet'in protokol ders listesi
            string[] ahmetProtokolKodlari = ["3301156", "3301351", "3301352", "3301354", "3301452", "3301454", "3301455", "3301553", "3301651"];
            foreach (var kod in ahmetProtokolKodlari)
            {
                db.OgrenciDersListeleri.Add(new OgrenciDersListesi
                {
                    OgrenciId = ahmet.Id,
                    DersId = dersler[kod].Id,
                    OzelSartliMi = kod is "3301354" or "3301651",
                    Durum = DersListesiDurumu.Aktif,
                    DegisiklikGerekce = "İlk protokol ataması"
                });
            }

            // Zeynep'in YDP ders listesi (en az 6 ders, >=30 AKTS)
            string[] zeynepProtokolKodlari = ["3301156", "3301351", "3301352", "3301354", "3301454", "3301455", "3301553"];
            foreach (var kod in zeynepProtokolKodlari)
            {
                db.OgrenciDersListeleri.Add(new OgrenciDersListesi
                {
                    OgrenciId = zeynep.Id,
                    DersId = dersler[kod].Id,
                    OzelSartliMi = kod == "3301354",
                    Durum = DersListesiDurumu.Aktif,
                    DegisiklikGerekce = "YDP protokol ataması"
                });
            }

            // Ahmet'in geçmiş dönem onaylanmış dersleri + aktif dönemde onay bekleyen kayıtları
            db.DersKayitlari.AddRange(
                new DersKayit
                {
                    OgrenciId = ahmet.Id,
                    DersId = dersler["3301156"].Id,
                    DonemId = guz25.Id,
                    KayitOnayDurumu = OnayDurumu.Onaylandi,
                    KayitOnayTarihi = DateTime.UtcNow.AddMonths(-10),
                    KayitOnaylayanId = koordinator.Id,
                    HarfNotu = "AA",
                    PuanNotu = 92,
                    NotOnayDurumu = OnayDurumu.Onaylandi,
                    NotOnayTarihi = DateTime.UtcNow.AddMonths(-7),
                    NotOnaylayanId = koordinator.Id,
                    DersDurumu = DersDurumu.Gecti,
                    DenemeSayisi = 1,
                    GirenId = ahmetKullanici.Id
                },
                new DersKayit
                {
                    OgrenciId = ahmet.Id,
                    DersId = dersler["3301351"].Id,
                    DonemId = guz25.Id,
                    KayitOnayDurumu = OnayDurumu.Onaylandi,
                    KayitOnayTarihi = DateTime.UtcNow.AddMonths(-10),
                    KayitOnaylayanId = koordinator.Id,
                    HarfNotu = "BA",
                    PuanNotu = 85,
                    NotOnayDurumu = OnayDurumu.Onaylandi,
                    NotOnayTarihi = DateTime.UtcNow.AddMonths(-7),
                    NotOnaylayanId = koordinator.Id,
                    DersDurumu = DersDurumu.Gecti,
                    DenemeSayisi = 1,
                    GirenId = ahmetKullanici.Id
                },
                // 2025-2026 Bahar: Notu girilmiş ve koordinatör onayı bekleyen bir ders
                new DersKayit
                {
                    OgrenciId = ahmet.Id,
                    DersId = dersler["3301454"].Id,
                    DonemId = bahar26.Id,
                    KayitOnayDurumu = OnayDurumu.Onaylandi,
                    KayitOnayTarihi = DateTime.UtcNow.AddMonths(-5),
                    KayitOnaylayanId = koordinator.Id,
                    HarfNotu = "BB",
                    PuanNotu = 80,
                    NotOnayDurumu = OnayDurumu.Bekliyor,
                    DersDurumu = DersDurumu.Alindi,
                    DenemeSayisi = 1,
                    GirenId = ahmetKullanici.Id
                },
                // 2026-2027 Güz (Aktif dönem): Ders kaydı onayı bekleyen dersler
                new DersKayit
                {
                    OgrenciId = ahmet.Id,
                    DersId = dersler["3301352"].Id,
                    DonemId = guz26.Id,
                    KayitOnayDurumu = OnayDurumu.Bekliyor,
                    DersDurumu = DersDurumu.Planlandi,
                    DenemeSayisi = 1,
                    GirenId = ahmetKullanici.Id
                },
                new DersKayit
                {
                    OgrenciId = ahmet.Id,
                    DersId = dersler["3301553"].Id,
                    DonemId = guz26.Id,
                    KayitOnayDurumu = OnayDurumu.Bekliyor,
                    DersDurumu = DersDurumu.Planlandi,
                    DenemeSayisi = 1,
                    GirenId = ahmetKullanici.Id
                }
            );

            await db.KaydetAsync("Demo öğrenci ders listeleri ve örnek dönem kayıtları oluşturuldu.");
        }
    }

    private static async Task<Kullanici> KullaniciYoksaOlusturAsync(
        UserManager<Kullanici> kullaniciYoneticisi,
        string email,
        string ad,
        string soyad,
        string rol)
    {
        var mevcut = await kullaniciYoneticisi.FindByEmailAsync(email);
        if (mevcut is not null)
        {
            return mevcut;
        }

        var yeni = new Kullanici
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Ad = ad,
            Soyad = soyad,
            AktifMi = true
        };

        var sonuc = await kullaniciYoneticisi.CreateAsync(yeni, DemoSifre);
        if (!sonuc.Succeeded)
        {
            var hatalar = string.Join(", ", sonuc.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Demo kullanıcısı oluşturulamadı ({email}): {hatalar}");
        }

        await kullaniciYoneticisi.AddToRoleAsync(yeni, rol);
        return yeni;
    }
}
