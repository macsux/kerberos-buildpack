namespace TapConventionWebhook.Models;

/// <summary>
/// A wrapper for the PodConventionContextSpec and the PodConventionContextStatus which is the structure used for both requests 
/// <br/>and responses from the convention server.
/// <br/>
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.17.0.0 (NJsonSchema v10.8.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class PodConventionContext
{
    [Newtonsoft.Json.JsonProperty("apiVersion", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public string? ApiVersion { get; set; }

    [Newtonsoft.Json.JsonProperty("kind", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public string? Kind { get; set; }

    [Newtonsoft.Json.JsonProperty("metadata", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public Metadata? Metadata { get; set; }

    [Newtonsoft.Json.JsonProperty("spec", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public PodConventionContextSpec? Spec { get; set; }

    [Newtonsoft.Json.JsonProperty("status", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public PodConventionContextStatus? Status { get; set; }

    private IDictionary<string, object>? _additionalProperties;

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ??= new Dictionary<string, object>(); }
        set { _additionalProperties = value; }
    }

}