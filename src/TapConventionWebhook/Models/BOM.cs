using System.Text.Json.Serialization;

namespace TapConventionWebhook.Models;

public partial class BOM
{
    /// <summary>
    /// bom-name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// base64 encoded bytes with the encoded content of the BOM.
    /// </summary>
    public string? Raw { get; set; }

    private IDictionary<string, object>? _additionalProperties;

    [JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ??= new Dictionary<string, object>(); }
        set { _additionalProperties = value; }
    }

}