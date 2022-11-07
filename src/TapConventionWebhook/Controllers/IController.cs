namespace TapConventionWebhook.Controllers;

[System.CodeDom.Compiler.GeneratedCode("NSwag", "13.17.0.0 (NJsonSchema v10.8.0.0 (Newtonsoft.Json v13.0.0.0))")]
public interface IController
{

    /// <remarks>
    /// The path defined above is arbitrary and can be overridden to any value by the ClusterPodConvention resource. 
    /// <br/>The webhook path can be configured in the ClusterPodConvention on either .spec.webhook.clientConfig.url or 
    /// <br/>.spec.webhook.clientConfig.service.path with the later preferred if the convention server is to run on the same cluster as workoads. 
    /// <br/>The webhook request and response both use the PodConventionContext with the request defining 
    /// <br/>the .spec and the response defining the status.
    /// <br/>status
    /// </remarks>

    /// <returns>expected response once all conventions are applied successfully.</returns>

    Task<PodConventionContext> WebhookAsync(PodConventionContext body);

}