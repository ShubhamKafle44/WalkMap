using WalkMap.Api.DTOs;

namespace WalkMap.Api.Services;

public interface IWalkService
{
    // Walk CRUD
    Task<WalkSummaryDto> StartWalkAsync(int userId, StartWalkRequest request);

    Task<WalkDetailDto> EndWalkAsync(int userId, int walkId, EndWalkRequest request);

    Task<IEnumerable<WalkSummaryDto>> GetWalkHistoryAsync(int userId);

    Task<WalkDetailDto> GetWalkByIdAsync(int userId, int walkId);

    Task DeleteWalkAsync(int userId, int walkId);

    // Route Generation
    RouteGenerateResponse GenerateRoute(RouteGenerateRequest request);
}