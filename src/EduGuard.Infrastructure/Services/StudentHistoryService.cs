using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.History;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;

namespace EduGuard.Infrastructure.Services;

public class StudentHistoryService : IStudentHistoryService
{
    private readonly IEduGuardDataStore _dataStore;

    public StudentHistoryService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<List<StudentTimelineItemDto>>> GetStudentHistoryTimelineAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<List<StudentTimelineItemDto>>.Fail("Student not found.");
        }

        var items = await _dataStore.GetHistoryByStudentAsync(studentId, cancellationToken);
        var dtos = items.Select(h => new StudentTimelineItemDto
        {
            Id = h.Id,
            StudentId = h.StudentId,
            EventType = h.EventType,
            Title = h.Title,
            Description = h.Description,
            OldValue = h.OldValue,
            NewValue = h.NewValue,
            EventDateUtc = h.EventDateUtc,
            RecordedByUserId = h.RecordedByUserId
        }).ToList();

        return ApiResponse<List<StudentTimelineItemDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<StudentTimelineItemDto>> AddTimelineNoteAsync(AddTimelineNoteDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(request.StudentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<StudentTimelineItemDto>.Fail("Student not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            return ApiResponse<StudentTimelineItemDto>.Fail("Title and description are required.");
        }

        var history = new StudentHistory
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            EventType = "StaffNote",
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            RecordedByUserId = operatorUserId,
            EventDateUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddHistoryItemAsync(history, cancellationToken);

        return ApiResponse<StudentTimelineItemDto>.Ok(new StudentTimelineItemDto
        {
            Id = history.Id,
            StudentId = history.StudentId,
            EventType = history.EventType,
            Title = history.Title,
            Description = history.Description,
            EventDateUtc = history.EventDateUtc,
            RecordedByUserId = history.RecordedByUserId
        }, "Timeline note recorded successfully.");
    }
}
