using System.Text.Json.Serialization;

namespace TapConventionWebhook.Models;

/// <summary>
/// a wrapper of the PodTemplateSpec and list of ImageConfigs provided in the request body of the server.
/// </summary>
public partial class PodConventionContextSpec
{
    public k8s.Models.V1PodTemplateSpec? Template { get; set; }

    /// <summary>
    /// an array of imageConfig objects with each image configuration object holding the name of the image, the BOM, and the OCI image
    /// <br/>configuration with image metadata from the repository. Each of the image config array entries have a 1:1 mapping to 
    /// <br/>images referenced in the PodTemplateSpec. 
    /// <br/>
    /// </summary>
    public List<ImageConfig>? ImageConfig { get; set; }

    private IDictionary<string, object>? _additionalProperties;

    [JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ??= new Dictionary<string, object>(); }
        set { _additionalProperties = value; }
    }

}