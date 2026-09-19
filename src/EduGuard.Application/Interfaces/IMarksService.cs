using EduGuard.Application.DTOs.Academics;
using EduGuard.Application.DTOs.Common;

namespace EduGuard.Application.Interfaces;

public interface IMarksService
{
    Task<ApiResponse<ExamRosterDto>> GetExamRosterAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> RecordMarksBatchAsync(RecordMarksBatchDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<StudentMarkDto>> UpdateStudentMarkAsync(Guid markId, UpdateStudentMarkDto request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<StudentMarkDto>>> GetMarksByAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<StudentMarkDto>>> GetMarksByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
}
