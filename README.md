# ÇAP / YDP Öğrenci Takip Otomasyonu

Selçuk Üniversitesi Bilgisayar Mühendisliği Bölümü'ne kabul edilen **Çift Anadal (ÇAP)** ve **Yan Dal (YDP)** öğrencilerinin
protokol ders listesi, ders kaydı, not girişi ve koordinatör onay süreçlerini yöneten web uygulaması (Bitirme Projesi).

> Canlı ortamda yalnızca **sentetik** (gerçek kişilere ait olmayan) test verileri kullanılmaktadır.

## Teknolojiler

| Katman | Teknoloji |
|---|---|
| Uygulama | ASP.NET Core 10 Razor Pages (C#) |
| Kimlik / Yetki | ASP.NET Core Identity, rol + klasör bazlı politika (Default-Deny) |
| Veri erişimi | Entity Framework Core 10 |
| Veritabanı | PostgreSQL (Render / Neon, canlı) · SQL Server LocalDB / SQLite (yerel geliştirme) |
| Barındırma | Docker imajı · Render (ücretsiz web servisi) |
| Arayüz | Bootstrap 5 |

## Roller

| Rol | Yetki özeti |
|---|---|
| **Koordinatör** | Öğrenci hesabı açma, ders kataloğu ve muafiyet kuralları, dönem yönetimi, protokol ders ataması, onay merkezi, denetim izi |
| **Öğrenci** | Protokol listesinden ders seçimi (onaya gönderme / geri çekme), not girişi, durum özeti |
| **Bölüm Başkanı** | Salt okunur: öğrenci listesi ve detayları, işlem kayıtları |

Öğrenciler kendi kendine kayıt olamaz; hesapları yalnızca koordinatör açar.

## Öne çıkan iş kuralları

- **Onay tabanlı akış:** Öğrencinin girdiği ders kaydı ve not, koordinatör onaylayana kadar `Onay Bekliyor` durumundadır; onaylanan kayıt kilitlenir.
- **Gerekçeli red / düzeltme / iptal:** Hiçbir kayıt fiziksel olarak silinmez (soft-delete); her değişiklik gerekçesiyle saklanır.
- **Denetim izi (Audit Trail):** Tüm ekleme/güncellemeler kim, ne zaman, hangi IP'den, eski → yeni değer bilgisiyle `IslemKaydi` tablosuna otomatik yazılır ve değiştirilemez.
- **Protokol / katalog ayrımı** ve **geçilen ders koruması.**
- **Güvenlik:** Parola politikası, 5 hatalı denemede 15 dk hesap kilidi, HttpOnly + SameSite çerez, CSRF token, güvenlik başlıkları (nosniff, X-Frame-Options), konteynerde root olmayan kullanıcı.

## Yerel çalıştırma

```bash
cd CapYdp.Web
dotnet run
```

Veritabanı sağlayıcısı otomatik seçilir: `DATABASE_URL` tanımlıysa PostgreSQL, Windows'ta LocalDB kuruluysa SQL Server, aksi halde SQLite.
İlk açılışta şema oluşturulur ve sentetik demo verisi yüklenir.

## Canlı ortam (Render + PostgreSQL)

- **Canlı adres:** https://capydp-otomasyon.onrender.com
- **Sağlık kontrolü:** `GET /saglik` → `{"durum":"calisiyor","veritabani":"bagli"}`
- Render üzerinde Docker web servisi olarak çalışır; `DATABASE_URL` ortam değişkeniyle PostgreSQL veritabanına bağlanır.

## Proje durumu

**Ara sınav (MVP) — tamamlandı:** giriş ve rol yetkilendirmesi, koordinatör paneli (öğrenci, ders kataloğu, dönem, onay merkezi, denetim izi),
öğrenci paneli (ders girişi, not girişi), bölüm başkanı salt okunur paneli.

**Sonraki fazlar:** belge yönetimi (transkript yükleme/onay), yapılandırılabilir yönetmelik kural motoru, mesajlaşma, raporlar,
şifre değiştirme/sıfırlama, güvenlik testleri.
