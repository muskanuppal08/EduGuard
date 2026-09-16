using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Students;

namespace EduGuard.Application.Interfaces;

public interface IStudentService
{
    Task<ApiResponse<StudentDto>> RegisterStudentAsync(RegisterStudentDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<StudentDto>> GetStudentByIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<StudentDto>>> GetStudentsAsync(StudentFilterDto filter, CancellationToken cancellationToken = default);
    Task<ApiResponse<StudentDto>> UpdateStudentAsync(Guid studentId, UpdateStudentDto request, CancellationToken cancellationToken = default);
}
