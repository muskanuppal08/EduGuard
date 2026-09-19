using EduGuard.Application.DTOs.Academics;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;

namespace EduGuard.Infrastructure.Services;

public class MarksService : IMarksService
{
    private readonly IEduGuardDataStore _dataStore;

    public MarksService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<ExamRosterDto>> GetExamRosterAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var assessment = await _dataStore.GetAssessmentByIdAsync(assessmentId, cancellationToken);
        if (assessment == null)
        {
            return ApiResponse<ExamRosterDto>.Fail($"Assessment with ID '{assessmentId}' was not found.");
        }

        var subject = await _dataStore.GetSubjectByIdAsync(assessment.SubjectId, cancellationToken);
        var enrollments = await _dataStore.GetEnrollmentsBySectionAsync(assessment.SectionId, cancellationToken);
        var marks = await _dataStore.GetMarksByAssessmentAsync(assessmentId, cancellationToken);
        var marksMap = marks.ToDictionary(m => m.StudentId);

        var rosterItems = new List<ExamRosterItemDto>();
        int evaluatedCount = 0;
        int passedCount = 0;
        int failedCount = 0;

        foreach (var enrollment in enrollments)
        {
            var student = await _dataStore.GetStudentByIdAsync(enrollment.StudentId, cancellationToken);
            if (student == null) continue;

            marksMap.TryGetValue(student.Id, out var mark);

            if (mark != null)
            {
                evaluatedCount++;
                if (mark.IsPass) passedCount++;
                else failedCount++;
            }

            rosterItems.Add(new ExamRosterItemDto
            {
                StudentId = student.Id,
                AdmissionNumber = student.AdmissionNumber,
                FullName = student.FullName,
                MarkId = mark?.Id,
                MarksObtained = mark?.MarksObtained,
                IsAbsent = mark?.IsAbsent ?? false,
                GradeLetter = mark?.GradeLetter,
                IsPass = mark?.IsPass,
                Remarks = mark?.Remarks
            });
        }

        var roster = new ExamRosterDto
        {
            AssessmentId = assessment.Id,
            AssessmentTitle = assessment.Title,
            SubjectName = subject?.Name ?? "Unknown Subject",
            MaxMarks = assessment.MaxMarks,
            PassingMarks = assessment.PassingMarks,
            TotalStudents = rosterItems.Count,
            EvaluatedCount = evaluatedCount,
            PassedCount = passedCount,
            FailedCount = failedCount,
            Students = rosterItems.OrderBy(r => r.AdmissionNumber).ToList()
        };

        return ApiResponse<ExamRosterDto>.Ok(roster);
    }

    public async Task<ApiResponse<int>> RecordMarksBatchAsync(RecordMarksBatchDto request, Guid? operatorUserId = null, CancellationToken cancellationToken = default)
    {
        var assessment = await _dataStore.GetAssessmentByIdAsync(request.AssessmentId, cancellationToken);
        if (assessment == null)
        {
            return ApiResponse<int>.Fail($"Assessment with ID '{request.AssessmentId}' was not found.");
        }

        int count = 0;
        var existingMarks = await _dataStore.GetMarksByAssessmentAsync(request.AssessmentId, cancellationToken);
        var marksMap = existingMarks.ToDictionary(m => m.StudentId);

        foreach (var item in request.Marks)
        {
            if (item.MarksObtained > assessment.MaxMarks && !item.IsAbsent)
            {
                return ApiResponse<int>.Fail($"Marks obtained ({item.MarksObtained}) cannot exceed maximum marks ({assessment.MaxMarks}).");
            }

            marksMap.TryGetValue(item.StudentId, out var mark);

            decimal marksObtained = item.IsAbsent ? 0.0m : item.MarksObtained;
            decimal percentage = assessment.MaxMarks > 0 ? (marksObtained / assessment.MaxMarks * 100m) : 0m;
            var (gradeLetter, gradePoint) = item.IsAbsent ? ("F", 0.0m) : CalculateGrade(percentage);
            bool isPass = !item.IsAbsent && marksObtained >= assessment.PassingMarks;

            if (mark == null)
            {
                mark = new StudentExamMark
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = assessment.Id,
                    StudentId = item.StudentId,
                    MarksObtained = marksObtained,
                    GradeLetter = gradeLetter,
                    GradePoint = gradePoint,
                    IsPass = isPass,
                    IsAbsent = item.IsAbsent,
                    Remarks = item.Remarks,
                    RecordedByUserId = operatorUserId,
                    CreatedAtUtc = DateTime.UtcNow
                };
            }
            else
            {
                mark.MarksObtained = marksObtained;
                mark.GradeLetter = gradeLetter;
                mark.GradePoint = gradePoint;
                mark.IsPass = isPass;
                mark.IsAbsent = item.IsAbsent;
                mark.Remarks = item.Remarks;
                mark.RecordedByUserId = operatorUserId;
                mark.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _dataStore.AddOrUpdateMarkAsync(mark, cancellationToken);
            count++;
        }

        return ApiResponse<int>.Ok(count, $"Successfully recorded marks for {count} students.");
    }

    public async Task<ApiResponse<StudentMarkDto>> UpdateStudentMarkAsync(Guid markId, UpdateStudentMarkDto request, CancellationToken cancellationToken = default)
    {
        var mark = await _dataStore.GetMarkByIdAsync(markId, cancellationToken);
        if (mark == null)
        {
            return ApiResponse<StudentMarkDto>.Fail($"Student mark with ID '{markId}' was not found.");
        }

        var assessment = await _dataStore.GetAssessmentByIdAsync(mark.AssessmentId, cancellationToken);
        if (assessment == null)
        {
            return ApiResponse<StudentMarkDto>.Fail("Associated assessment not found.");
        }

        if (request.MarksObtained > assessment.MaxMarks && !request.IsAbsent)
        {
            return ApiResponse<StudentMarkDto>.Fail($"Marks obtained ({request.MarksObtained}) cannot exceed maximum marks ({assessment.MaxMarks}).");
        }

        var student = await _dataStore.GetStudentByIdAsync(mark.StudentId, cancellationToken);
        var subject = await _dataStore.GetSubjectByIdAsync(assessment.SubjectId, cancellationToken);

        decimal marksObtained = request.IsAbsent ? 0.0m : request.MarksObtained;
        decimal percentage = assessment.MaxMarks > 0 ? (marksObtained / assessment.MaxMarks * 100m) : 0m;
        var (gradeLetter, gradePoint) = request.IsAbsent ? ("F", 0.0m) : CalculateGrade(percentage);

        mark.MarksObtained = marksObtained;
        mark.GradeLetter = gradeLetter;
        mark.GradePoint = gradePoint;
        mark.IsPass = !request.IsAbsent && marksObtained >= assessment.PassingMarks;
        mark.IsAbsent = request.IsAbsent;
        mark.Remarks = request.Remarks;
        mark.UpdatedAtUtc = DateTime.UtcNow;

        await _dataStore.AddOrUpdateMarkAsync(mark, cancellationToken);

        var dto = new StudentMarkDto
        {
            Id = mark.Id,
            AssessmentId = assessment.Id,
            AssessmentTitle = assessment.Title,
            SubjectId = assessment.SubjectId,
            SubjectName = subject?.Name ?? "Unknown Subject",
            IsCoreSubject = subject?.IsCoreSubject ?? false,
            StudentId = mark.StudentId,
            StudentName = student?.FullName ?? "Unknown Student",
            AdmissionNumber = student?.AdmissionNumber ?? string.Empty,
            MarksObtained = mark.MarksObtained,
            MaxMarks = assessment.MaxMarks,
            PassingMarks = assessment.PassingMarks,
            GradeLetter = mark.GradeLetter,
            GradePoint = mark.GradePoint,
            IsPass = mark.IsPass,
            IsAbsent = mark.IsAbsent,
            Remarks = mark.Remarks
        };

        return ApiResponse<StudentMarkDto>.Ok(dto, "Student mark updated successfully.");
    }

    public async Task<ApiResponse<List<StudentMarkDto>>> GetMarksByAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var assessment = await _dataStore.GetAssessmentByIdAsync(assessmentId, cancellationToken);
        if (assessment == null)
        {
            return ApiResponse<List<StudentMarkDto>>.Fail($"Assessment with ID '{assessmentId}' was not found.");
        }

        var subject = await _dataStore.GetSubjectByIdAsync(assessment.SubjectId, cancellationToken);
        var marks = await _dataStore.GetMarksByAssessmentAsync(assessmentId, cancellationToken);
        var dtos = new List<StudentMarkDto>();

        foreach (var m in marks)
        {
            var student = await _dataStore.GetStudentByIdAsync(m.StudentId, cancellationToken);
            dtos.Add(new StudentMarkDto
            {
                Id = m.Id,
                AssessmentId = assessment.Id,
                AssessmentTitle = assessment.Title,
                SubjectId = assessment.SubjectId,
                SubjectName = subject?.Name ?? "Unknown Subject",
                IsCoreSubject = subject?.IsCoreSubject ?? false,
                StudentId = m.StudentId,
                StudentName = student?.FullName ?? "Unknown Student",
                AdmissionNumber = student?.AdmissionNumber ?? string.Empty,
                MarksObtained = m.MarksObtained,
                MaxMarks = assessment.MaxMarks,
                PassingMarks = assessment.PassingMarks,
                GradeLetter = m.GradeLetter,
                GradePoint = m.GradePoint,
                IsPass = m.IsPass,
                IsAbsent = m.IsAbsent,
                Remarks = m.Remarks
            });
        }

        return ApiResponse<List<StudentMarkDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<List<StudentMarkDto>>> GetMarksByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<List<StudentMarkDto>>.Fail($"Student with ID '{studentId}' was not found.");
        }

        var marks = await _dataStore.GetMarksByStudentAsync(studentId, cancellationToken);
        var dtos = new List<StudentMarkDto>();

        foreach (var m in marks)
        {
            var assessment = await _dataStore.GetAssessmentByIdAsync(m.AssessmentId, cancellationToken);
            var subject = assessment != null ? await _dataStore.GetSubjectByIdAsync(assessment.SubjectId, cancellationToken) : null;

            dtos.Add(new StudentMarkDto
            {
                Id = m.Id,
                AssessmentId = m.AssessmentId,
                AssessmentTitle = assessment?.Title ?? "Unknown Assessment",
                SubjectId = assessment?.SubjectId ?? Guid.Empty,
                SubjectName = subject?.Name ?? "Unknown Subject",
                IsCoreSubject = subject?.IsCoreSubject ?? false,
                StudentId = student.Id,
                StudentName = student.FullName,
                AdmissionNumber = student.AdmissionNumber,
                MarksObtained = m.MarksObtained,
                MaxMarks = assessment?.MaxMarks ?? 100m,
                PassingMarks = assessment?.PassingMarks ?? 35m,
                GradeLetter = m.GradeLetter,
                GradePoint = m.GradePoint,
                IsPass = m.IsPass,
                IsAbsent = m.IsAbsent,
                Remarks = m.Remarks
            });
        }

        return ApiResponse<List<StudentMarkDto>>.Ok(dtos.OrderByDescending(d => d.AssessmentTitle).ToList());
    }

    public static (string GradeLetter, decimal GradePoint) CalculateGrade(decimal percentage)
    {
        if (percentage >= 90m) return ("A+", 4.0m);
        if (percentage >= 80m) return ("A", 3.7m);
        if (percentage >= 70m) return ("B+", 3.3m);
        if (percentage >= 60m) return ("B", 3.0m);
        if (percentage >= 50m) return ("C", 2.0m);
        if (percentage >= 35m) return ("D", 1.0m);
        return ("F", 0.0m);
    }
}
