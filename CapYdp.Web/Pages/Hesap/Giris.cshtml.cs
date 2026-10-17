using System.ComponentModel.DataAnnotations;
using CapYdp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CapYdp.Web.Pages.Hesap;

public class GirisModel : PageModel
{
    private readonly SignInManager<Kullanici> _girisYoneticisi;
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;

    public GirisModel(SignInManager<Kullanici> girisYoneticisi, UserManager<Kullanici> kullaniciYoneticisi)
    {
        _girisYoneticisi = girisYoneticisi;
        _kullaniciYoneticisi = kullaniciYoneticisi;
    }

    [BindProperty]
    public GirisGirdisi Girdi { get; set; } = new();

    public string? HataMesaji { get; set; }

    public class GirisGirdisi
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; } = string.Empty;

        public bool BeniHatirla { get; set; }
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var kullanici = await _kullaniciYoneticisi.FindByEmailAsync(Girdi.Email.Trim());
        if (kullanici is null || !kullanici.AktifMi)
        {
            // Güvenlik notu: Hesabın var olup olmadığını dışarıya sızdırmamak için (Account Enumeration önlemi)
            // yanlış e-posta ve yanlış şifrede aynı genel hata mesajı döner.
            HataMesaji = "E-posta adresi veya şifre hatalı.";
            return Page();
        }

        var sonuc = await _girisYoneticisi.PasswordSignInAsync(
            kullanici,
            Girdi.Sifre,
            Girdi.BeniHatirla,
            lockoutOnFailure: true);

        if (sonuc.Succeeded)
        {
            return RedirectToPage("/Index");
        }

        if (sonuc.IsLockedOut)
        {
            HataMesaji = "Çok sayıda başarısız deneme nedeniyle hesabınız geçici olarak kilitlendi. Lütfen 15 dakika sonra tekrar deneyin.";
            return Page();
        }

        HataMesaji = "E-posta adresi veya şifre hatalı.";
        return Page();
    }
}
