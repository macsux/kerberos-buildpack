
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TapConventionWebhook.Controllers;

public partial class WebHookController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    private readonly ILogger<WebHookController> _log;

    public WebHookController(ILogger<WebHookController> log)
    {
        _log = log;
    }

    /// <remarks>
    /// The path defined above is arbitrary and can be overridden to any value by the ClusterPodConvention resource. 
    /// <br/>The webhook path can be configured in the ClusterPodConvention on either .spec.webhook.clientConfig.url or 
    /// <br/>.spec.webhook.clientConfig.service.path with the later preferred if the convention server is to run on the same cluster as workoads. 
    /// <br/>The webhook request and response both use the PodConventionContext with the request defining 
    /// <br/>the .spec and the response defining the status.
    /// <br/>status
    /// </remarks>
    /// <returns>expected response once all conventions are applied successfully.</returns>
    [HttpPost("webhook")]
#pragma warning disable CS1998
    public async Task<PodConventionContext> Webhook([FromBody] PodConventionContext body)
#pragma warning restore CS1998
    {
        
        _log.LogInformation(JsonConvert.SerializeObject(body));
        return body;
    }

}
