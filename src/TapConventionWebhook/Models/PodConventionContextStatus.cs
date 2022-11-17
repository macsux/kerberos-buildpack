using System.Text.Json.Serialization;

namespace TapConventionWebhook.Models;

/// <summary>
/// status type used to represent the current status of the context retrieved by the request.
/// </summary>
public partial class PodConventionContextStatus
{
    public k8s.Models.V1PodTemplateSpec Template { get; set; } = null!;

    /// <summary>
    /// a list of string with names of conventions to be applied
    /// </summary>
    public List<string>? AppliedConventions { get; set; }

    private IDictionary<string, object>? _additionalProperties;

    [JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ??= new Dictionary<string, object>(); }
        set { _additionalProperties = value; }
    }

}