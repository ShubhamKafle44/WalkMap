namespace WalkMapMobile.Models;

public class WalkSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public double TotalDistanceMeters { get; set; }
    public int StepCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    // Computed display helpers
    public string DistanceDisplay =>
        TotalDistanceMeters >= 1000
            ? $"{TotalDistanceMeters / 1000:F2} km"
            : $"{TotalDistanceMeters:F0} m";

    public string DurationDisplay
    {
        get
        {
            if (EndedAt == null) return "In progress";
            var duration = EndedAt.Value - StartedAt;
            return duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours}h {duration.Minutes}m"
                : $"{duration.Minutes}m {duration.Seconds}s";
        }
    }

    public string DateDisplay => StartedAt.ToString("MMM dd, yyyy · h:mm tt");
}

public class WalkDetail : WalkSummary
{
    public List<WalkPoint> RoutePoints { get; set; } = new();
}

public class WalkPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public int SequenceOrder { get; set; }
}

public class StartWalkRequest
{
    public string Title { get; set; } = string.Empty;
}

public class EndWalkRequest
{
    public int StepCount { get; set; }
    public List<WalkPoint> RoutePoints { get; set; } = new();
}

public class GenerateRouteRequest
{
    public double StartLat { get; set; }
    public double StartLng { get; set; }
    public double TargetDistanceKm { get; set; }
}

public class GenerateRouteResponse
{
    public List<WalkPoint> RoutePoints { get; set; } = new();
    public double EstimatedDistanceKm { get; set; }
}