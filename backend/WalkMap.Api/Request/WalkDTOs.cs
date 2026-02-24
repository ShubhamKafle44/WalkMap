namespace WalkMap.Api.DTOs;

public record WalkPointDto(double Latitude, double Longitude, DateTime Timestamp, int SequenceOrder);

public record StartWalkRequest(string Title);

public record EndWalkRequest(int StepCount, List<WalkPointDto> RoutePoints);

public record WalkSummaryDto(
    int Id,
    string Title,
    double TotalDistanceMeters,
    double TotalDistanceKm,
    int StepCount,
    DateTime StartedAt,
    DateTime? EndedAt,
    double? DurationMinutes
);

public record WalkDetailDto(
    int Id,
    string Title,
    double TotalDistanceMeters,
    double TotalDistanceKm,
    int StepCount,
    DateTime StartedAt,
    DateTime? EndedAt,
    double? DurationMinutes,
    List<WalkPointDto> RoutePoints
);

public record RouteGenerateRequest(
    double StartLat,
    double StartLng,
    double TargetDistanceKm
);

public record RouteGenerateResponse(
    List<WalkPointDto> Points,
    double EstimatedDistanceKm
);