namespace TapConventionWebhook.Models;

/// <summary>
/// status type used to represent the current status of the context retrieved by the request.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.17.0.0 (NJsonSchema v10.8.0.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class PodConventionContextStatus
{
    [Newtonsoft.Json.JsonProperty("template", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public k8s.Models.V1PodTemplateSpec? Template { get; set; }

    /// <summary>
    /// a list of string with names of conventions to be applied
    /// </summary>
    [Newtonsoft.Json.JsonProperty("appliedConventions", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public List<string>? AppliedConventions { get; set; }

    private IDictionary<string, object>? _additionalProperties;

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ??= new Dictionary<string, object>(); }
        set { _additionalProperties = value; }
    }

}