using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KerberosSidecar.HealthChecks;

public static class HealthCheckOptionsExtensions
{
    public static HealthCheckOptions WithJsonDetails(this HealthCheckOptions options)
    {
        options.ResponseWriter = WriteResponse;
        return options;
    }
    private static Task WriteResponse(HttpContext context, HealthReport result)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonWriterOptions
        {
            Indented = true
        };

        using (var stream = new MemoryStream())
        {
            using (var writer = new Utf8JsonWriter(stream, options))
            {
                writer.WriteStartObject();
                writer.WriteString("status", result.Status.ToString());
                writer.WriteStartObject("results");
                foreach (var entry in result.Entries)
                {
                    writer.WriteStartObject(entry.Key);
                    writer.WriteString("status", entry.Value.Status.ToString());
                    writer.WriteString("description", entry.Value.Description);
                    writer.WriteStartArray("exception");
                    foreach (var line in entry.Value.Exception?.ToString().Split("\r\n") ?? Enumerable.Empty<string>())
                    {
                        writer.WriteStringValue(line);
                    }
                    writer.WriteEndArray();
                    // writer.WriteString("exception", entry.Value.Exception?.ToString());
                    writer.WriteStartObject("data");
                    foreach (var item in entry.Value.Data)
                    {
                        writer.WritePropertyName(item.Key);
                        JsonSerializer.Serialize(
                            writer, item.Value, item.Value?.GetType() ??
                                                typeof(object));
                    }
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            var json = Encoding.UTF8.GetString(stream.ToArray());

            return context.Response.WriteAsync(json);
        }
    }
}