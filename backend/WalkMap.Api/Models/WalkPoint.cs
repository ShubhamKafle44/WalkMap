namespace WalkMap.Api.Models;

public class WalkPoint
{
    public int Id { get; set; }
    public int WalkId { get; set; }
    public Walk Walk { get; set; } = null!;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int SequenceOrder { get; set; }
}