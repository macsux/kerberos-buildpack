using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KerberosBuildpack.Tests;

public class HealthReportEntry
{
    /// <summary>
    /// Gets additional key-value pairs describing the health of the component.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Gets a human-readable description of the status of the component that was checked.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the health check execution duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets an <see cref="System.Exception"/> representing the exception that was thrown when checking for status (if any).
    /// </summary>
    public string[]? Exception { get; set; }

    /// <summary>
    /// Gets the health status of the component that was checked.
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Gets the tags associated with the health check.
    /// </summary>
    public IEnumerable<string>? Tags { get; set; }
}