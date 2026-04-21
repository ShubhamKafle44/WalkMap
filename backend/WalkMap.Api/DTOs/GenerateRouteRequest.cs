using System.Text.Json.Serialization;

namespace WalkMap.Api.DTOs;

public record GenerateRouteRequest(
    [property: JsonPropertyName("startLat")] double StartLat,
    [property: JsonPropertyName("startLng")] double StartLng,
    [property: JsonPropertyName("targetDistanceKm")] double TargetDistanceKm
);