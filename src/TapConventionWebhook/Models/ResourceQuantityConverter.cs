using System.Text.Json;
using k8s.Models;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace TapConventionWebhook.Models;

public class ResourceQuantityJsonConverter : JsonConverter<ResourceQuantity>
{
    // public override ResourceQuantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    // {
    //     return new ResourceQuantity(reader.GetString());
    // }
    //
    // public override void Write(Utf8JsonWriter writer, ResourceQuantity value, JsonSerializerOptions options)
    // {
    //     if (writer == null)
    //     {
    //         throw new ArgumentNullException(nameof(writer));
    //     }
    //
    //     writer.WriteStringValue(value?.ToString());
    // }

    public override void WriteJson(JsonWriter writer, ResourceQuantity? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.ToString());
    }

    public override ResourceQuantity? ReadJson(JsonReader reader, Type objectType, ResourceQuantity? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return new ResourceQuantity(reader.ReadAsString());

    }
}