using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TapConventionWebhook.Models;
using Newtonsoft.Json.Linq;

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
#pragma warning disable CS1998
    public async Task<String> Webhook()
#pragma warning restore CS1998
    {
        var body = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var bodyObject = JObject.Parse(body);

        // Copy spec.template to status.template
        bodyObject!["status"]!["template"] = bodyObject!["spec"]!["template"]!.DeepClone(); 
        
        // Add ports to primary container
        if (bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["ports"] == null || bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["ports"]!.Type == JTokenType.Null)
        {
            bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["ports"] = new JArray();    
        }
        ((JArray)bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["ports"]!).Add(JObject.Parse("{'containerPort': 8080}"));

        // Add environment variables to primary container
        if (bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["env"] == null || bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["env"]!.Type == JTokenType.Null)
        {
            bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["env"] = new JArray();    
        }
        ((JArray)bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["env"]!).Add(JObject.Parse("{'name': 'KRB5_CONFIG', 'value': '/krb/krb5.conf'}"));
        ((JArray)bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["env"]!).Add(JObject.Parse("{'name': 'KRB5CCNAME', 'value': '/krb/krb5cc'}"));
        ((JArray)bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["env"]!).Add(JObject.Parse("{'name': 'KRB5_KTNAME', 'value': '/krb/service.keytab'}"));
        ((JArray)bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["env"]!).Add(JObject.Parse("{'name': 'KRB5_CLIENT_KTNAME', 'value': '/krb/service.keytab'}"));
        
        // Add VolumeMonnts to primary container 
        if (bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["volumeMounts"] == null || bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["volumeMounts"]!.Type == JTokenType.Null)
        {
            bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["volumeMounts"] = new JArray();    
        }
        ((JArray)bodyObject!["status"]!["template"]!["spec"]!["containers"]![0]!["volumeMounts"]!).Add(JObject.Parse("{'name': 'krb-app', 'mountPath': '/krb'}"));
        
        // Add Volumes
        string containerBlock = @"{
    'name': 'kdc-sidecar',
    'image': '<<SIDECAR_IMAGE>>',
    'resources': {
    'limits': {
        'memory': '100Mi',
        'cpu': '100m'
    },
    'requests': {
        'memory': '100Mi',
        'cpu': '100m'
    }},
    'env': [
        {
            'name': 'KRB_KDC',
            'valueFrom': {
                'secretKeyRef': {
                    'name': 'kerberos-demo-tbs-krb-creds',
                    'key': 'ad_host',
                    'optional': false
                }
           }
         },
        {
            'name': 'KRB_SERVICE_ACCOUNT',
            'valueFrom': {
                'secretKeyRef': {
                    'name': 'kerberos-demo-tbs-krb-creds',
                    'key': 'username',
                    'optional': false
                }
            }
        },
        {
            'name': 'KRB_PASSWORD',
            'valueFrom': {
                'secretKeyRef': {
                    'name': 'kerberos-demo-tbs-krb-creds',
                    'key': 'password',
                    'optional': false
                }
            }
        },
        {
            'name': 'KRB5_CONFIG',
            'value': '/krb/krb5.conf'
        },
        {
            'name': 'KRB5CCNAME',
            'value': '/krb/krb5cc'
        },
        {
            'name': 'KRB5_KTNAME',
            'value': '/krb/service.keytab'
        },
        {
            'name': 'KRB5_CLIENT_KTNAME',
            'value': '/krb/service.keytab'
        }
    ],
    'volumeMounts': [
        {
            'name': 'krb-app',
            'mountPath': '/krb'
        }
    ]
}";
        var sidecarContainer = JObject.Parse(containerBlock.Replace("<<SIDECAR_IMAGE>>", Environment.GetEnvironmentVariable("SIDECAR_IMAGE")));

        
        ((JArray)bodyObject!["status"]!["template"]!["spec"]!["containers"]!).Add(sidecarContainer);

        
        // Add Volumes

        string volumes = @"{
                'name': 'krb-app',
                'emptyDir': {
                    'medium': 'Memory'
                }
            }";
       
        if (bodyObject!["status"]!["template"]!["spec"]!["volumes"] == null || bodyObject!["status"]!["template"]!["spec"]!["volumes"]!.Type == JTokenType.Null)
        {
            bodyObject!["status"]!["template"]!["spec"]!["volumes"] = new JArray();    
        }
        ((JArray)bodyObject!["status"]!["template"]!["spec"]!["volumes"]!).Add(JObject.Parse(volumes));

        
        
        if (bodyObject!["status"]!["appliedConventions"] == null || bodyObject!["status"]!["appliedConventions"]!.Type == JTokenType.Null)
        {
            bodyObject!["status"]!["appliedConventions"] = new JArray();    
        }

        ((JArray)bodyObject?["status"]?["appliedConventions"]!).Add("kerberos-sidecar-convention");

        _log.LogInformation(bodyObject?.ToString());
        return bodyObject?.ToString() ?? String.Empty;
    }

}
