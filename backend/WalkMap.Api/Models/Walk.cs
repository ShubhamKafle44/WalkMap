namespace WalkMap.Api.Models;

public class Walk
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = "My Walk";

    public double TotalDistanceMeters { get; set; }

    public int StepCount { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public TimeSpan? Duration => EndedAt.HasValue ? EndedAt - StartedAt : null;

    public double? DurationMinutes =>
        EndedAt.HasValue ? (EndedAt.Value - StartedAt).TotalMinutes : null;

    public ICollection<WalkPoint> RoutePoints { get; set; } = new List<WalkPoint>();
}

