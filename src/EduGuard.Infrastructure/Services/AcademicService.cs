using EduGuard.Application.DTOs.Academics;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;

namespace EduGuard.Infrastructure.Services;

public class AcademicService : IAcademicService
{
    private readonly IEduGuardDataStore _dataStore;

    public AcademicService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<SubjectDto>> CreateSubjectAsync(CreateSubjectDto request, CancellationToken cancellationToken = default)
    {
        var school = await _dataStore.GetSchoolByIdAsync(request.SchoolId, cancellationToken);
        if (school == null)
        {
            return ApiResponse<SubjectDto>.Fail($"School with ID '{request.SchoolId}' was not found.");
        }

        var existingSubjects = await _dataStore.GetSubjectsBySchoolAsync(request.SchoolId, cancellationToken);
        if (existingSubjects.Any(s => s.SubjectCode.Equals(request.SubjectCode, StringComparison.OrdinalIgnoreCase)))
        {
            return ApiResponse<SubjectDto>.Fail($"Subject with code '{request.SubjectCode}' already exists in this school.");
        }

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            SchoolId = request.SchoolId,
            SubjectCode = request.SubjectCode.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            IsCoreSubject = request.IsCoreSubject,
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddSubjectAsync(subject, cancellationToken);

        var dto = new SubjectDto
        {
            Id = subject.Id,
            SchoolId = subject.SchoolId,
            SubjectCode = subject.SubjectCode,
            Name = subject.Name,
            IsCoreSubject = subject.IsCoreSubject,
            Description = subject.Description
        };

        return ApiResponse<SubjectDto>.Ok(dto, "Subject created successfully.");
    }

    public async Task<ApiResponse<List<SubjectDto>>> GetSubjectsBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        var subjects = await _dataStore.GetSubjectsBySchoolAsync(schoolId, cancellationToken);
        var dtos = subjects.Select(s => new SubjectDto
        {
            Id = s.Id,
            SchoolId = s.SchoolId,
            SubjectCode = s.SubjectCode,
            Name = s.Name,
            IsCoreSubject = s.IsCoreSubject,
            Description = s.Description
        }).ToList();

        return ApiResponse<List<SubjectDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<SubjectDto>> GetSubjectByIdAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        var subject = await _dataStore.GetSubjectByIdAsync(subjectId, cancellationToken);
        if (subject == null)
        {
            return ApiResponse<SubjectDto>.Fail($"Subject with ID '{subjectId}' was not found.");
        }

        var dto = new SubjectDto
        {
            Id = subject.Id,
            SchoolId = subject.SchoolId,
            SubjectCode = subject.SubjectCode,
            Name = subject.Name,
            IsCoreSubject = subject.IsCoreSubject,
            Description = subject.Description
        };

        return ApiResponse<SubjectDto>.Ok(dto);
    }

    public async Task<ApiResponse<AssessmentDto>> CreateAssessmentAsync(CreateAssessmentDto request, CancellationToken cancellationToken = default)
    {
        var school = await _dataStore.GetSchoolByIdAsync(request.SchoolId, cancellationToken);
        if (school == null)
        {
            return ApiResponse<AssessmentDto>.Fail($"School with ID '{request.SchoolId}' was not found.");
        }

        var section = await _dataStore.GetSectionByIdAsync(request.SectionId, cancellationToken);
        if (section == null)
        {
            return ApiResponse<AssessmentDto>.Fail($"Section with ID '{request.SectionId}' was not found.");
        }

        var subject = await _dataStore.GetSubjectByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null)
        {
            return ApiResponse<AssessmentDto>.Fail($"Subject with ID '{request.SubjectId}' was not found.");
        }

        var academicYear = await _dataStore.GetAcademicYearByIdAsync(request.AcademicYearId, cancellationToken);
        if (academicYear == null)
        {
            return ApiResponse<AssessmentDto>.Fail($"Academic year with ID '{request.AcademicYearId}' was not found.");
        }

        if (request.PassingMarks > request.MaxMarks)
        {
            return ApiResponse<AssessmentDto>.Fail("Passing marks cannot exceed maximum marks.");
        }

        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            SchoolId = request.SchoolId,
            SectionId = request.SectionId,
            SubjectId = request.SubjectId,
            AcademicYearId = request.AcademicYearId,
            Title = request.Title.Trim(),
            Category = request.Category,
            ExamDate = request.ExamDate,
            MaxMarks = request.MaxMarks,
            PassingMarks = request.PassingMarks,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddAssessmentAsync(assessment, cancellationToken);

        var dto = new AssessmentDto
        {
            Id = assessment.Id,
            SchoolId = assessment.SchoolId,
            SectionId = assessment.SectionId,
            SectionName = section.SectionName,
            SubjectId = assessment.SubjectId,
            SubjectName = subject.Name,
            IsCoreSubject = subject.IsCoreSubject,
            AcademicYearId = assessment.AcademicYearId,
            AcademicYearName = academicYear.Name,
            Title = assessment.Title,
            Category = assessment.Category,
            ExamDate = assessment.ExamDate,
            MaxMarks = assessment.MaxMarks,
            PassingMarks = assessment.PassingMarks,
            EvaluatedStudentsCount = 0
        };

        return ApiResponse<AssessmentDto>.Ok(dto, "Assessment created successfully.");
    }

    public async Task<ApiResponse<List<AssessmentDto>>> GetAssessmentsAsync(AssessmentFilterDto filter, CancellationToken cancellationToken = default)
    {
        var assessments = await _dataStore.QueryAssessmentsAsync(a =>
            (!filter.SchoolId.HasValue || a.SchoolId == filter.SchoolId.Value) &&
            (!filter.SectionId.HasValue || a.SectionId == filter.SectionId.Value) &&
            (!filter.SubjectId.HasValue || a.SubjectId == filter.SubjectId.Value) &&
            (!filter.AcademicYearId.HasValue || a.AcademicYearId == filter.AcademicYearId.Value) &&
            (!filter.Category.HasValue || a.Category == filter.Category.Value),
            cancellationToken);

        var dtos = new List<AssessmentDto>();
        foreach (var a in assessments)
        {
            var section = await _dataStore.GetSectionByIdAsync(a.SectionId, cancellationToken);
            var subject = await _dataStore.GetSubjectByIdAsync(a.SubjectId, cancellationToken);
            var academicYear = await _dataStore.GetAcademicYearByIdAsync(a.AcademicYearId, cancellationToken);
            var marks = await _dataStore.GetMarksByAssessmentAsync(a.Id, cancellationToken);

            dtos.Add(new AssessmentDto
            {
                Id = a.Id,
                SchoolId = a.SchoolId,
                SectionId = a.SectionId,
                SectionName = section?.SectionName ?? "Unknown Section",
                SubjectId = a.SubjectId,
                SubjectName = subject?.Name ?? "Unknown Subject",
                IsCoreSubject = subject?.IsCoreSubject ?? false,
                AcademicYearId = a.AcademicYearId,
                AcademicYearName = academicYear?.Name ?? "Unknown Academic Year",
                Title = a.Title,
                Category = a.Category,
                ExamDate = a.ExamDate,
                MaxMarks = a.MaxMarks,
                PassingMarks = a.PassingMarks,
                EvaluatedStudentsCount = marks.Count
            });
        }

        return ApiResponse<List<AssessmentDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<AssessmentDto>> GetAssessmentByIdAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var a = await _dataStore.GetAssessmentByIdAsync(assessmentId, cancellationToken);
        if (a == null)
        {
            return ApiResponse<AssessmentDto>.Fail($"Assessment with ID '{assessmentId}' was not found.");
        }

        var section = await _dataStore.GetSectionByIdAsync(a.SectionId, cancellationToken);
        var subject = await _dataStore.GetSubjectByIdAsync(a.SubjectId, cancellationToken);
        var academicYear = await _dataStore.GetAcademicYearByIdAsync(a.AcademicYearId, cancellationToken);
        var marks = await _dataStore.GetMarksByAssessmentAsync(a.Id, cancellationToken);

        var dto = new AssessmentDto
        {
            Id = a.Id,
            SchoolId = a.SchoolId,
            SectionId = a.SectionId,
            SectionName = section?.SectionName ?? "Unknown Section",
            SubjectId = a.SubjectId,
            SubjectName = subject?.Name ?? "Unknown Subject",
            IsCoreSubject = subject?.IsCoreSubject ?? false,
            AcademicYearId = a.AcademicYearId,
            AcademicYearName = academicYear?.Name ?? "Unknown Academic Year",
            Title = a.Title,
            Category = a.Category,
            ExamDate = a.ExamDate,
            MaxMarks = a.MaxMarks,
            PassingMarks = a.PassingMarks,
            EvaluatedStudentsCount = marks.Count
        };

        return ApiResponse<AssessmentDto>.Ok(dto);
    }
}
