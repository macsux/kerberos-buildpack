using System.Text.Json.Serialization;

namespace TapConventionWebhook.Models;

/// <summary>
/// A wrapper for the PodConventionContextSpec and the PodConventionContextStatus which is the structure used for both requests 
/// <br/>and responses from the convention server.
/// <br/>
/// </summary>
public partial class PodConventionContext
{
    public string ApiVersion { get; set; } = null!;

    public string Kind { get; set; } = null!;

    public Metadata Metadata { get; set; } = null!;

    public PodConventionContextSpec Spec { get; set; } = null!;

    public PodConventionContextStatus Status { get; set; }  = null!;

    private IDictionary<string, object>? _additionalProperties;

    [JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ??= new Dictionary<string, object>(); }
        set { _additionalProperties = value; }
    }

}