namespace TapConventionWebhook.Models;

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.17.0.0 (NJsonSchema v10.8.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class ImageConfig
{
    /// <summary>
    /// a string reference to the image name and tag or associated digest.
    /// </summary>
    [Newtonsoft.Json.JsonProperty("image", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public string? Image { get; set; }

    /// <summary>
    /// an array of Bills of Materials (BOMs) describing the software components and their dependencies and may be zero or more per image.
    /// <br/>
    /// </summary>
    [Newtonsoft.Json.JsonProperty("boms", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public List<BOM>? Boms { get; set; }

    /// <summary>
    /// OCI image metadata
    /// </summary>
    [Newtonsoft.Json.JsonProperty("config", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public object? Config { get; set; }

    private IDictionary<string, object>? _additionalProperties;

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ??= new Dictionary<string, object>(); }
        set { _additionalProperties = value; }
    }

}