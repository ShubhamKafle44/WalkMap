namespace WalkMap.Api.DTOs;

public class GenerateRouteRequest
{
    public double StartLat { get; set; }
    public double StartLng { get; set; }
    public double TargetDistanceKm { get; set; } = 3.0;
}
