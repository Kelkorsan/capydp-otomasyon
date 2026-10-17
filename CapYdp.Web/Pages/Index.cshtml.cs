using CapYdp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CapYdp.Web.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Hesap/Giris");
        }

        if (User.IsInRole(Roller.Koordinator))
        {
            return RedirectToPage("/Koordinator/Index");
        }

        if (User.IsInRole(Roller.Ogrenci))
        {
            return RedirectToPage("/OgrenciPaneli/Index");
        }

        if (User.IsInRole(Roller.BolumBaskani))
        {
            return RedirectToPage("/BolumBaskani/Index");
        }

        return RedirectToPage("/Hesap/Giris");
    }
}
