using Microsoft.EntityFrameworkCore;
using WalkMap.Api.Data;
using WalkMap.Api.DTOs;
using WalkMap.Api.Models;

namespace WalkMap.Api.Services;

public class WalkService : IWalkService
{
    private readonly AppDbContext _db;

    public WalkService(AppDbContext db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────────
    // Walk CRUD
    // ──────────────────────────────────────────────

    public async Task<WalkSummaryDto> StartWalkAsync(int userId, StartWalkRequest request)
    {
        var walk = new Walk
        {
            UserId = userId,
            Title = request.Title,
            StartedAt = DateTime.UtcNow
        };

        _db.Walks.Add(walk);
        await _db.SaveChangesAsync();

        return ToSummary(walk);
    }

    public async Task<WalkDetailDto> EndWalkAsync(int userId, int walkId, EndWalkRequest request)
    {
        var walk = await _db.Walks
            .Include(w => w.RoutePoints)
            .FirstOrDefaultAsync(w => w.Id == walkId && w.UserId == userId)
            ?? throw new KeyNotFoundException("Walk not found.");

        // FIX: Save GPS route points to the DB first so they get IDs and are
        //      fully flushed before we read them back for the distance calculation.
        int seq = 0;
        foreach (var pt in request.RoutePoints.OrderBy(p => p.SequenceOrder))
        {
            walk.RoutePoints.Add(new WalkPoint
            {
                Latitude = pt.Latitude,
                Longitude = pt.Longitude,
                Timestamp = pt.Timestamp,
                SequenceOrder = seq++
            });
        }

        await _db.SaveChangesAsync(); // flush points first

        // Now calculate distance against the persisted, ordered points
        walk.TotalDistanceMeters = CalculateTotalDistance(
            walk.RoutePoints.OrderBy(p => p.SequenceOrder).ToList());
        walk.StepCount = request.StepCount;
        walk.EndedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(); // save summary fields
        return ToDetail(walk);
    }

    public async Task<IEnumerable<WalkSummaryDto>> GetWalkHistoryAsync(int userId)
    {
        var walks = await _db.Walks
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.StartedAt)
            .ToListAsync();

        return walks.Select(ToSummary);
    }

    public async Task<WalkDetailDto> GetWalkByIdAsync(int userId, int walkId)
    {
        var walk = await _db.Walks
            .Include(w => w.RoutePoints)
            .FirstOrDefaultAsync(w => w.Id == walkId && w.UserId == userId)
            ?? throw new KeyNotFoundException("Walk not found.");

        return ToDetail(walk);
    }

    public async Task DeleteWalkAsync(int userId, int walkId)
    {
        var walk = await _db.Walks
            .FirstOrDefaultAsync(w => w.Id == walkId && w.UserId == userId)
            ?? throw new KeyNotFoundException("Walk not found.");

        _db.Walks.Remove(walk);
        await _db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    // Route Generation
    // ──────────────────────────────────────────────

    /// <summary>
    /// Generates a simple circular walking route around the starting point.
    /// In production, replace with an external routing API (e.g., Google Maps, OpenRouteService).
    /// </summary>
    public RouteGenerateResponse GenerateRoute(RouteGenerateRequest request)
    {
        const int numPoints = 20;
        var points = new List<WalkPointDto>();
        double radiusKm = request.TargetDistanceKm / (2 * Math.PI);
        double radiusDeg = radiusKm / 111.0;

        for (int i = 0; i <= numPoints; i++)
        {
            double angle = 2 * Math.PI * i / numPoints;
            double lat = request.StartLat + radiusDeg * Math.Cos(angle);
            double lng = request.StartLng + radiusDeg * Math.Sin(angle)
                         / Math.Cos(request.StartLat * Math.PI / 180);

            points.Add(new WalkPointDto(lat, lng, DateTime.UtcNow, i));
        }

        return new RouteGenerateResponse(points, request.TargetDistanceKm);
    }

    // ──────────────────────────────────────────────
    // Distance Calculation (Haversine Formula)
    // ──────────────────────────────────────────────

    public static double CalculateTotalDistance(IList<WalkPoint> points)
    {
        double total = 0;
        for (int i = 0; i < points.Count - 1; i++)
            total += HaversineMeters(points[i].Latitude, points[i].Longitude,
                                     points[i + 1].Latitude, points[i + 1].Longitude);
        return total;
    }

    public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double phi1 = lat1 * Math.PI / 180;
        double phi2 = lat2 * Math.PI / 180;
        double dPhi = (lat2 - lat1) * Math.PI / 180;
        double dLambda = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2)
                 + Math.Cos(phi1) * Math.Cos(phi2)
                 * Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    // ──────────────────────────────────────────────
    // Mapping helpers
    // ──────────────────────────────────────────────

    private static WalkSummaryDto ToSummary(Walk w) => new(
        w.Id,
        w.Title,
        w.TotalDistanceMeters,
        Math.Round(w.TotalDistanceMeters / 1000, 2),
        w.StepCount,
        w.StartedAt,
        w.EndedAt,
        w.Duration.HasValue ? Math.Round(w.Duration.Value.TotalMinutes, 1) : null
    );

    private static WalkDetailDto ToDetail(Walk w) => new(
        w.Id,
        w.Title,
        w.TotalDistanceMeters,
        Math.Round(w.TotalDistanceMeters / 1000, 2),
        w.StepCount,
        w.StartedAt,
        w.EndedAt,
        w.Duration.HasValue ? Math.Round(w.Duration.Value.TotalMinutes, 1) : null,
        w.RoutePoints
            .OrderBy(p => p.SequenceOrder)
            .Select(p => new WalkPointDto(p.Latitude, p.Longitude, p.Timestamp, p.SequenceOrder))
            .ToList()
    );
}