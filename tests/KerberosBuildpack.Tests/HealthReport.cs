using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KerberosBuildpack.Tests;

public class HealthReport
{
    /// <summary>
    /// A <see cref="IReadOnlyDictionary{TKey, T}"/> containing the results from each health check.
    /// </summary>
    /// <remarks>
    /// The keys in this dictionary map the name of each executed health check to a <see cref="HealthReportEntry"/> for the
    /// result data returned from the corresponding health check.
    /// </remarks>
    public Dictionary<string, HealthReportEntry>? Results { get; set; }

    /// <summary>
    /// Gets a <see cref="HealthStatus"/> representing the aggregate status of all the health checks. The value of <see cref="Status"/>
    /// will be the most severe status reported by a health check. If no checks were executed, the value is always <see cref="HealthStatus.Healthy"/>.
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Gets the time the health check service took to execute.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }
}