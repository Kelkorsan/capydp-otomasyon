using CapYdp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CapYdp.Web.Pages.Hesap;

public class CikisModel : PageModel
{
    private readonly SignInManager<Kullanici> _girisYoneticisi;

    public CikisModel(SignInManager<Kullanici> girisYoneticisi)
    {
        _girisYoneticisi = girisYoneticisi;
    }

    public IActionResult OnGet() => RedirectToPage("/Hesap/Giris");

    public async Task<IActionResult> OnPostAsync()
    {
        await _girisYoneticisi.SignOutAsync();
        return RedirectToPage("/Hesap/Giris");
    }
}
