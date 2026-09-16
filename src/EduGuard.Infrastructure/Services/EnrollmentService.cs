using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Enrollments;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using EduGuard.Domain.Enums;

namespace EduGuard.Infrastructure.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEduGuardDataStore _dataStore;

    public EnrollmentService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<EnrollmentDto>> EnrollStudentAsync(EnrollStudentDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(request.StudentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<EnrollmentDto>.Fail("Student not found.");
        }

        var section = await _dataStore.GetSectionByIdAsync(request.SectionId, cancellationToken);
        if (section == null)
        {
            return ApiResponse<EnrollmentDto>.Fail("Section not found.");
        }

        var year = await _dataStore.GetAcademicYearByIdAsync(request.AcademicYearId, cancellationToken);
        if (year == null)
        {
            return ApiResponse<EnrollmentDto>.Fail("Academic year not found.");
        }

        // Close any currently active enrollment
        var active = await _dataStore.GetActiveEnrollmentForStudentAsync(student.Id, cancellationToken);
        if (active != null)
        {
            active.Status = StudentStatus.Promoted;
            active.ExitDate = DateOnly.FromDateTime(DateTime.UtcNow);
            await _dataStore.UpdateEnrollmentAsync(active, cancellationToken);
        }

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            SectionId = section.Id,
            AcademicYearId = year.Id,
            EnrollmentDate = request.EnrollmentDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Status = StudentStatus.Active,
            Remarks = request.Remarks,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddEnrollmentAsync(enrollment, cancellationToken);

        student.CurrentSectionId = section.Id;
        student.CurrentStatus = StudentStatus.Active;
        await _dataStore.UpdateStudentAsync(student, cancellationToken);

        var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
        await _dataStore.AddHistoryItemAsync(new StudentHistory
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            EventType = "Enrollment",
            Title = "Enrolled in Class",
            Description = $"Enrolled in {grade?.Name}-{section.SectionName} for academic year {year.Name}.",
            RecordedByUserId = operatorUserId,
            EventDateUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return ApiResponse<EnrollmentDto>.Ok(MapToDto(enrollment, student, section, grade, year), "Student enrolled successfully.");
    }

    public async Task<ApiResponse<int>> BatchPromoteStudentsAsync(BatchPromoteDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default)
    {
        var toSection = await _dataStore.GetSectionByIdAsync(request.ToSectionId, cancellationToken);
        if (toSection == null)
        {
            return ApiResponse<int>.Fail("Destination section not found.");
        }

        var year = await _dataStore.GetAcademicYearByIdAsync(request.NewAcademicYearId, cancellationToken);
        if (year == null)
        {
            return ApiResponse<int>.Fail("Academic year not found.");
        }

        var grade = await _dataStore.GetGradeByIdAsync(toSection.ClassGradeId, cancellationToken);
        int promotedCount = 0;

        foreach (var studentId in request.StudentIds)
        {
            var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
            if (student == null) continue;

            // Close existing active enrollment
            var currentEnrollment = await _dataStore.GetActiveEnrollmentForStudentAsync(student.Id, cancellationToken);
            if (currentEnrollment != null)
            {
                currentEnrollment.Status = StudentStatus.Promoted;
                currentEnrollment.ExitDate = DateOnly.FromDateTime(DateTime.UtcNow);
                await _dataStore.UpdateEnrollmentAsync(currentEnrollment, cancellationToken);
            }

            var newEnrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                SectionId = toSection.Id,
                AcademicYearId = year.Id,
                EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = StudentStatus.Active,
                Remarks = "Batch promotion",
                CreatedAtUtc = DateTime.UtcNow
            };

            await _dataStore.AddEnrollmentAsync(newEnrollment, cancellationToken);

            student.CurrentSectionId = toSection.Id;
            student.CurrentStatus = StudentStatus.Active;
            await _dataStore.UpdateStudentAsync(student, cancellationToken);

            await _dataStore.AddHistoryItemAsync(new StudentHistory
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                EventType = "Promotion",
                Title = "Promoted to Next Grade",
                Description = $"Promoted to {grade?.Name}-{toSection.SectionName} for academic year {year.Name}.",
                RecordedByUserId = operatorUserId,
                EventDateUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            promotedCount++;
        }

        return ApiResponse<int>.Ok(promotedCount, $"Successfully promoted {promotedCount} students.");
    }

    public async Task<ApiResponse> RecordDropoutAsync(RecordDropoutDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(request.StudentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse.FailResult("Student not found.");
        }

        var activeEnrollment = await _dataStore.GetActiveEnrollmentForStudentAsync(student.Id, cancellationToken);
        if (activeEnrollment != null)
        {
            activeEnrollment.Status = StudentStatus.DroppedOut;
            activeEnrollment.ExitDate = request.DropoutDate;
            activeEnrollment.ExitReason = request.Reason;
            activeEnrollment.Remarks = request.Remarks;
            await _dataStore.UpdateEnrollmentAsync(activeEnrollment, cancellationToken);
        }

        student.CurrentStatus = StudentStatus.DroppedOut;
        await _dataStore.UpdateStudentAsync(student, cancellationToken);

        await _dataStore.AddHistoryItemAsync(new StudentHistory
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            EventType = "Dropout",
            Title = "Dropout Recorded",
            Description = $"Student marked as dropped out on {request.DropoutDate:yyyy-MM-dd}. Reason: {request.Reason}. Remarks: {request.Remarks}",
            OldValue = StudentStatus.Active.ToString(),
            NewValue = StudentStatus.DroppedOut.ToString(),
            RecordedByUserId = operatorUserId,
            EventDateUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return ApiResponse.OkResult("Student dropout recorded successfully.");
    }

    public async Task<ApiResponse> TransferStudentAsync(TransferStudentDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(request.StudentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse.FailResult("Student not found.");
        }

        var newSchool = await _dataStore.GetSchoolByIdAsync(request.NewSchoolId, cancellationToken);
        if (newSchool == null)
        {
            return ApiResponse.FailResult("New school not found.");
        }

        var activeEnrollment = await _dataStore.GetActiveEnrollmentForStudentAsync(student.Id, cancellationToken);
        if (activeEnrollment != null)
        {
            activeEnrollment.Status = StudentStatus.Transferred;
            activeEnrollment.ExitDate = DateOnly.FromDateTime(DateTime.UtcNow);
            activeEnrollment.Remarks = request.Reason;
            await _dataStore.UpdateEnrollmentAsync(activeEnrollment, cancellationToken);
        }

        var oldSchool = await _dataStore.GetSchoolByIdAsync(student.SchoolId, cancellationToken);
        student.SchoolId = newSchool.Id;
        student.CurrentSectionId = request.NewSectionId;
        student.CurrentStatus = StudentStatus.Transferred;
        await _dataStore.UpdateStudentAsync(student, cancellationToken);

        await _dataStore.AddHistoryItemAsync(new StudentHistory
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            EventType = "Transfer",
            Title = "Transferred to New School",
            Description = $"Transferred from {oldSchool?.Name} to {newSchool.Name}. Reason: {request.Reason}",
            RecordedByUserId = operatorUserId,
            EventDateUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return ApiResponse.OkResult("Student transfer processed.");
    }

    public async Task<ApiResponse> ReEnrollStudentAsync(ReEnrollDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(request.StudentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse.FailResult("Student not found.");
        }

        var section = await _dataStore.GetSectionByIdAsync(request.SectionId, cancellationToken);
        if (section == null)
        {
            return ApiResponse.FailResult("Section not found.");
        }

        var year = await _dataStore.GetAcademicYearByIdAsync(request.AcademicYearId, cancellationToken);
        if (year == null)
        {
            return ApiResponse.FailResult("Academic year not found.");
        }

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            SectionId = section.Id,
            AcademicYearId = year.Id,
            EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = StudentStatus.Active,
            Remarks = $"Re-enrolled: {request.Remarks}",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddEnrollmentAsync(enrollment, cancellationToken);

        student.CurrentSectionId = section.Id;
        student.CurrentStatus = StudentStatus.Active;
        await _dataStore.UpdateStudentAsync(student, cancellationToken);

        var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
        await _dataStore.AddHistoryItemAsync(new StudentHistory
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            EventType = "ReEnrollment",
            Title = "Student Re-Enrolled",
            Description = $"Student re-enrolled into {grade?.Name}-{section.SectionName} after retention intervention. Remarks: {request.Remarks}",
            OldValue = StudentStatus.DroppedOut.ToString(),
            NewValue = StudentStatus.Active.ToString(),
            RecordedByUserId = operatorUserId,
            EventDateUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return ApiResponse.OkResult("Student successfully re-enrolled.");
    }

    public async Task<ApiResponse<List<EnrollmentDto>>> GetStudentEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<List<EnrollmentDto>>.Fail("Student not found.");
        }

        var enrollments = await _dataStore.GetEnrollmentsByStudentAsync(studentId, cancellationToken);
        var dtos = new List<EnrollmentDto>();

        foreach (var e in enrollments)
        {
            var section = await _dataStore.GetSectionByIdAsync(e.SectionId, cancellationToken);
            var grade = section != null ? await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken) : null;
            var year = await _dataStore.GetAcademicYearByIdAsync(e.AcademicYearId, cancellationToken);
            dtos.Add(MapToDto(e, student, section, grade, year));
        }

        return ApiResponse<List<EnrollmentDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<List<EnrollmentDto>>> GetSectionEnrollmentsAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        var section = await _dataStore.GetSectionByIdAsync(sectionId, cancellationToken);
        if (section == null)
        {
            return ApiResponse<List<EnrollmentDto>>.Fail("Section not found.");
        }

        var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
        var year = await _dataStore.GetAcademicYearByIdAsync(section.AcademicYearId, cancellationToken);
        var enrollments = await _dataStore.GetEnrollmentsBySectionAsync(sectionId, cancellationToken);

        var dtos = new List<EnrollmentDto>();
        foreach (var e in enrollments)
        {
            var student = await _dataStore.GetStudentByIdAsync(e.StudentId, cancellationToken);
            if (student != null)
            {
                dtos.Add(MapToDto(e, student, section, grade, year));
            }
        }

        return ApiResponse<List<EnrollmentDto>>.Ok(dtos);
    }

    private static EnrollmentDto MapToDto(Enrollment e, Student s, Section? sec, ClassGrade? g, AcademicYear? y) => new()
    {
        Id = e.Id,
        StudentId = s.Id,
        StudentName = s.FullName,
        AdmissionNumber = s.AdmissionNumber,
        SectionId = e.SectionId,
        SectionName = sec != null && g != null ? $"{g.Name}-{sec.SectionName}" : "N/A",
        AcademicYearId = e.AcademicYearId,
        AcademicYearName = y?.Name ?? "N/A",
        EnrollmentDate = e.EnrollmentDate,
        Status = e.Status,
        ExitDate = e.ExitDate,
        ExitReason = e.ExitReason,
        Remarks = e.Remarks
    };
}
