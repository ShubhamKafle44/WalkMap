namespace WalkMapFrontend.Models;

using System.Text.Json.Serialization;
public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
}

public class RegisterRequest
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class StartWalkRequest
{
    public string Title { get; set; } = "";
}

public class EndWalkRequest
{
    public int StepCount { get; set; }
    public List<WalkPointDto> RoutePoints { get; set; } = new();
}

public class WalkPointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public int SequenceOrder { get; set; }
}

public class WalkSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public double TotalDistanceMeters { get; set; }
    public double TotalDistanceKm { get; set; }
    public int StepCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double? DurationMinutes { get; set; }
}

public class WalkDetailDto : WalkSummaryDto
{
    public List<WalkPointDto> RoutePoints { get; set; } = new();
}

public class GenerateRouteRequest
{
    public double StartLat { get; set; }
    public double StartLng { get; set; }
    public double TargetDistanceKm { get; set; }
}

public class GenerateRouteResponse
{
    public List<WalkPointDto> Points { get; set; } = new();
    public double EstimatedDistanceKm { get; set; }
}

public class ApiError
{
    public string Message { get; set; } = "";
}




// Root object
public class ORSResponse
{
    [JsonPropertyName("features")]
    public List<ORSFeature> Features { get; set; } = new();
}

public class ORSFeature
{
    [JsonPropertyName("geometry")]
    public ORSGeometry Geometry { get; set; } = new();

    [JsonPropertyName("properties")]
    public ORSProperties Properties { get; set; } = new();
}

public class ORSGeometry
{
    [JsonPropertyName("coordinates")]
    public List<List<double>> Coordinates { get; set; } = new();

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

public class ORSProperties
{
    [JsonPropertyName("summary")]
    public ORSSummary Summary { get; set; } = new();
}

public class ORSSummary
{
    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }
}