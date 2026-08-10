using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DamirLut.DebuggerDump.Models;

internal sealed class ExportNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("call_count")]
    public uint CallCount { get; set; }

    [JsonPropertyName("time_milliseconds")]
    public float TimeMilliseconds { get; set; }

    [JsonPropertyName("step_percent")]
    public float StepPercent { get; set; }

    [JsonPropertyName("children")]
    public List<ExportNode> Children { get; set; }
}
