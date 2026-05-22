using System.Text.Json.Serialization;

namespace ET.CompleteAgent.Host.Models;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AgentInvokeRequest))]
[JsonSerializable(typeof(AgentInvokeResponse))]
[JsonSerializable(typeof(AgentErrorResponse))]
[JsonSerializable(typeof(SentimentClassification))]
[JsonSerializable(typeof(string))]
internal sealed partial class AgentJsonContext : JsonSerializerContext;
