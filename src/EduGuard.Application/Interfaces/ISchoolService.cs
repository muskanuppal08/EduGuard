using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Schools;

namespace EduGuard.Application.Interfaces;

public interface ISchoolService
{
    Task<ApiResponse<SchoolDto>> CreateSchoolAsync(CreateSchoolDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<SchoolDto>> GetSchoolByIdAsync(Guid schoolId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<SchoolDto>>> GetSchoolsAsync(SchoolFilterDto filter, CancellationToken cancellationToken = default);
    Task<ApiResponse<SchoolDto>> UpdateSchoolAsync(Guid schoolId, UpdateSchoolDto request, CancellationToken cancellationToken = default);
}
