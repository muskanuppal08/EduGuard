using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.History;

namespace EduGuard.Application.Interfaces;

public interface IStudentHistoryService
{
    Task<ApiResponse<List<StudentTimelineItemDto>>> GetStudentHistoryTimelineAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<StudentTimelineItemDto>> AddTimelineNoteAsync(AddTimelineNoteDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default);
}
