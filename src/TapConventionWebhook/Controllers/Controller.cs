using System.Text.Json;
using k8s.Models;
using Microsoft.AspNetCore.Mvc;
// using Newtonsoft.Json;
using TapConventionWebhook.Models;
// using Newtonsoft.Json.Linq;

namespace TapConventionWebhook.Controllers;
//https://raw.githubusercontent.com/vmware-tanzu/cartographer-conventions/main/api/openapi-spec/conventions-server.yaml

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

    public PodConventionContext Webhook([FromBody] PodConventionContext context)
    {
        context.Status ??= new PodConventionContextStatus();
        context.Spec ??= new PodConventionContextSpec();
        context.Spec!.Template ??= new V1PodTemplateSpec();
        
        context.Status.Template = JsonSerializer.Deserialize<V1PodTemplateSpec>(JsonSerializer.Serialize(context.Spec.Template!))!;
        if (!context.Spec.Template.Metadata.Labels.ContainsKey("kerberos"))
        {
            _log.LogDebug("No kerberos label applied - skipping convention");
            return context;
        }

        var container = context.Status.Template!.Spec?.Containers.First()!;
        container.Ports ??= new List<V1ContainerPort>();
        container.Ports.Add(new V1ContainerPort{ ContainerPort = 8080 });

        container.Env ??= new List<V1EnvVar>();
        container.Env.Add(new V1EnvVar("KRB5_CONFIG", "/krb/krb5.conf"));
        container.Env.Add(new V1EnvVar("KRB5_KTNAME", "/krb/service.keytab"));
        container.Env.Add(new V1EnvVar("KRB5_CLIENT_KTNAME", "/krb/service.keytab"));

        container.VolumeMounts ??= new List<V1VolumeMount>();
        container.VolumeMounts.Add(new V1VolumeMount
        {
            Name = "krb-app",
            MountPath = "/krb",
        });


        var sidecarContainer = new V1Container
        {
            Name = "kdc-sidecar",
            Image = Environment.GetEnvironmentVariable("SIDECAR_IMAGE"), //todo: replace with options,
            Resources = new()
            {
                Limits = new Dictionary<string, ResourceQuantity>
                {
                    { "memory", new ResourceQuantity("100Mi") },
                    { "cpu", new ResourceQuantity("100m") }
                },
                Requests = new Dictionary<string, ResourceQuantity>
                {
                    { "memory", new ResourceQuantity("100Mi") },
                    { "cpu", new ResourceQuantity("100m") }
                },
            
            },
            Env = new List<V1EnvVar>()
            {
                //todo: remove
                // new("KRB_KDC", valueFrom: new V1EnvVarSource(secretKeyRef: new V1SecretKeySelector
                // {
                //     Name = "kerberos-demo-krb-creds",
                //     Key = "ad_host",
                //     Optional = false
                // })),
                // new("KRB_SERVICE_ACCOUNT", valueFrom: new V1EnvVarSource(secretKeyRef: new V1SecretKeySelector
                // {
                //     Name = "kerberos-demo-krb-creds",
                //     Key = "username",
                //     Optional = false
                // })),
                // new("KRB_PASSWORD", valueFrom: new V1EnvVarSource(secretKeyRef: new V1SecretKeySelector
                // {
                //     Name = "kerberos-demo-krb-creds",
                //     Key = "password",
                //     Optional = false
                // })),
                new("KRB5_CONFIG", "/krb/krb5.conf"),
                new("KRB5CCNAME", "/krb/krb5cc"),
                new("KRB5_KTNAME", "/krb/service.keytab"),
                new("KRB5_CLIENT_KTNAME", "/krb/service.keytab"),
            },
            VolumeMounts = new List<V1VolumeMount>()
            {
                new()
                {
                    Name = "krb-app",
                    MountPath = "/krb"
                }
            }
        };
        context.Status.Template!.Spec!.Containers.Add(sidecarContainer);
        context.Status.Template.Spec!.Volumes ??= new List<V1Volume>();
        context.Status.Template.Spec!.Volumes.Add(new V1Volume(
            name: "krb-app", 
            emptyDir: new V1EmptyDirVolumeSource
            {
                Medium = "Memory"
            }));

        context.Status.AppliedConventions ??= new List<string>();
        context.Status.AppliedConventions.Add("kerberos-sidecar-convention");
        _log.LogInformation("Kerberos convention applied");
        
        return context;
    }
}