using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Noundry.Hydro.Demo.Pages.Invoices;

[Authorize(Policy = "CustomerAccess")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        _logger.LogInformation("Invoices page accessed by user: {User}", 
            User.Identity?.Name ?? "Anonymous");
    }
}