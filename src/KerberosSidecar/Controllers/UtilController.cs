using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KerberosSidecar.Controllers;

[Controller]
public class UtilController : Controller
{
    private readonly IOptionsSnapshot<KerberosOptions> _options;

    public UtilController(IOptionsSnapshot<KerberosOptions> options)
    {
        _options = options;
    }

    [HttpGet("/ticket")]
    public async Task<string> GetTicket(string? spn)
    {
        spn ??= _options.Value.KerberosClient.UserPrincipalName;
        var ticket = await _options.Value.KerberosClient.GetServiceTicket(spn);
        return Convert.ToBase64String(ticket.EncodeGssApi().Span);
    }
}