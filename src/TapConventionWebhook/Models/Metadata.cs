using System.Text.Json.Serialization;

namespace TapConventionWebhook.Models;

public partial class Metadata
{
    public string? Name { get; set; }

    private IDictionary<string, object>? _additionalProperties;

    [JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ??= new Dictionary<string, object>(); }
        set { _additionalProperties = value; }
    }

}