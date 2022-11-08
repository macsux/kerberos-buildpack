namespace TapConventionWebhook.Models;

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.17.0.0 (NJsonSchema v10.8.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class BOM
{
    /// <summary>
    /// bom-name
    /// </summary>
    [Newtonsoft.Json.JsonProperty("name", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public string? Name { get; set; }

    /// <summary>
    /// base64 encoded bytes with the encoded content of the BOM.
    /// </summary>
    [Newtonsoft.Json.JsonProperty("raw", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public string? Raw { get; set; }

    private IDictionary<string, object>? _additionalProperties;

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ??= new Dictionary<string, object>(); }
        set { _additionalProperties = value; }
    }

}