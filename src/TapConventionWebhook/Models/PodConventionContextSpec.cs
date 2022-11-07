namespace TapConventionWebhook.Controllers;

/// <summary>
/// a wrapper of the PodTemplateSpec and list of ImageConfigs provided in the request body of the server.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.17.0.0 (NJsonSchema v10.8.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class PodConventionContextSpec
{
    [Newtonsoft.Json.JsonProperty("template", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public PodTemplateSpec? Template { get; set; }

    /// <summary>
    /// an array of imageConfig objects with each image configuration object holding the name of the image, the BOM, and the OCI image
    /// <br/>configuration with image metadata from the repository. Each of the image config array entries have a 1:1 mapping to 
    /// <br/>images referenced in the PodTemplateSpec. 
    /// <br/>
    /// </summary>
    [Newtonsoft.Json.JsonProperty("imageConfig", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public List<ImageConfig>? ImageConfig { get; set; }

    private IDictionary<string, object>? _additionalProperties;

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ??= new Dictionary<string, object>(); }
        set { _additionalProperties = value; }
    }

}