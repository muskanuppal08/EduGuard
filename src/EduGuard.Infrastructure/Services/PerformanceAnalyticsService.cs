using EduGuard.Application.DTOs.Academics;
using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;

namespace EduGuard.Infrastructure.Services;

public class PerformanceAnalyticsService : IPerformanceAnalyticsService
{
    private readonly IEduGuardDataStore _dataStore;

    public PerformanceAnalyticsService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<StudentReportCardDto>> GenerateReportCardAsync(Guid studentId, Guid? academicYearId = null, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<StudentReportCardDto>.Fail($"Student with ID '{studentId}' was not found.");
        }

        var school = await _dataStore.GetSchoolByIdAsync(student.SchoolId, cancellationToken);
        var currentSection = student.CurrentSectionId.HasValue
            ? await _dataStore.GetSectionByIdAsync(student.CurrentSectionId.Value, cancellationToken)
            : null;

        var academicYear = academicYearId.HasValue
            ? await _dataStore.GetAcademicYearByIdAsync(academicYearId.Value, cancellationToken)
            : (currentSection != null ? await _dataStore.GetAcademicYearByIdAsync(currentSection.AcademicYearId, cancellationToken) : null);

        var allMarks = await _dataStore.GetMarksByStudentAsync(studentId, cancellationToken);

        // Filter marks by academic year if specified
        var marksToProcess = new List<(Domain.Entities.StudentExamMark Mark, Domain.Entities.Assessment Assessment, Domain.Entities.Subject Subject)>();
        foreach (var mark in allMarks)
        {
            var assessment = await _dataStore.GetAssessmentByIdAsync(mark.AssessmentId, cancellationToken);
            if (assessment == null) continue;

            if (academicYear != null && assessment.AcademicYearId != academicYear.Id)
            {
                continue;
            }

            var subject = await _dataStore.GetSubjectByIdAsync(assessment.SubjectId, cancellationToken);
            if (subject == null) continue;

            marksToProcess.Add((mark, assessment, subject));
        }

        // Group by subject
        var subjectGroups = marksToProcess.GroupBy(m => m.Subject.Id);
        var subjectSummaries = new List<SubjectScoreSummaryDto>();

        decimal cumulativeMarksObtained = 0.0m;
        decimal cumulativeMaxMarks = 0.0m;
        decimal totalGradePoints = 0.0m;
        int subjectsCount = 0;
        int coreFailedCount = 0;
        int totalFailedCount = 0;

        foreach (var group in subjectGroups)
        {
            var subject = group.First().Subject;
            decimal subjObtained = group.Sum(x => x.Mark.MarksObtained);
            decimal subjMax = group.Sum(x => x.Assessment.MaxMarks);
            decimal avgPercentage = subjMax > 0 ? Math.Round(subjObtained / subjMax * 100m, 2) : 0m;
            var (gradeLetter, gradePoint) = MarksService.CalculateGrade(avgPercentage);
            bool isPass = avgPercentage >= 35.0m;

            if (!isPass)
            {
                totalFailedCount++;
                if (subject.IsCoreSubject)
                {
                    coreFailedCount++;
                }
            }

            cumulativeMarksObtained += subjObtained;
            cumulativeMaxMarks += subjMax;
            totalGradePoints += gradePoint;
            subjectsCount++;

            subjectSummaries.Add(new SubjectScoreSummaryDto
            {
                SubjectId = subject.Id,
                SubjectName = subject.Name,
                SubjectCode = subject.SubjectCode,
                IsCoreSubject = subject.IsCoreSubject,
                TotalMarksObtained = subjObtained,
                TotalMaxMarks = subjMax,
                OverallGrade = gradeLetter,
                IsPass = isPass,
                AssessmentsCount = group.Count()
            });
        }

        decimal cumulativePercentage = cumulativeMaxMarks > 0 ? Math.Round(cumulativeMarksObtained / cumulativeMaxMarks * 100m, 2) : 0m;
        decimal gpa = subjectsCount > 0 ? Math.Round(totalGradePoints / subjectsCount, 2) : 0m;
        var (overallGrade, _) = MarksService.CalculateGrade(cumulativePercentage);

        var reportCard = new StudentReportCardDto
        {
            StudentId = student.Id,
            FullName = student.FullName,
            AdmissionNumber = student.AdmissionNumber,
            SchoolName = school?.Name ?? "Unknown School",
            ClassName = currentSection?.SectionName ?? "Unassigned Section",
            AcademicYearName = academicYear?.Name ?? "All Academic Years",
            SubjectSummaries = subjectSummaries.OrderBy(s => s.SubjectName).ToList(),
            TotalMarksObtained = cumulativeMarksObtained,
            TotalMaxMarks = cumulativeMaxMarks,
            GPA = gpa,
            OverallGrade = overallGrade,
            CoreSubjectsFailedCount = coreFailedCount,
            TotalSubjectsFailedCount = totalFailedCount
        };

        return ApiResponse<StudentReportCardDto>.Ok(reportCard);
    }

    public async Task<ApiResponse<AcademicTrajectoryDto>> GetAcademicTrajectoryAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<AcademicTrajectoryDto>.Fail($"Student with ID '{studentId}' was not found.");
        }

        var marks = await _dataStore.GetMarksByStudentAsync(studentId, cancellationToken);
        var assessmentMarks = new List<(Guid SubjectId, DateTime ExamDate, DateOnly DateOnlyVal, string Title, decimal Percentage)>();

        foreach (var m in marks)
        {
            var assessment = await _dataStore.GetAssessmentByIdAsync(m.AssessmentId, cancellationToken);
            if (assessment == null) continue;

            decimal percentage = assessment.MaxMarks > 0 ? Math.Round(m.MarksObtained / assessment.MaxMarks * 100m, 2) : 0m;
            assessmentMarks.Add((assessment.SubjectId, assessment.ExamDate.ToDateTime(TimeOnly.MinValue), assessment.ExamDate, assessment.Title, percentage));
        }

        var sortedPoints = assessmentMarks
            .OrderBy(p => p.ExamDate)
            .Select(p => new TermScorePointDto
            {
                AssessmentTitle = p.Title,
                ExamDate = p.DateOnlyVal,
                Percentage = p.Percentage
            })
            .ToList();

        decimal maxDrop = 0m;

        // 1. Evaluate chronological drops within the same subject
        foreach (var subjectGroup in assessmentMarks.GroupBy(x => x.SubjectId))
        {
            var chronList = subjectGroup.OrderBy(x => x.ExamDate).ToList();
            for (int i = 1; i < chronList.Count; i++)
            {
                decimal drop = chronList[i - 1].Percentage - chronList[i].Percentage;
                if (drop > maxDrop)
                {
                    maxDrop = drop;
                }
            }
        }

        // 2. Evaluate overall consecutive chronological drop
        for (int i = 1; i < sortedPoints.Count; i++)
        {
            decimal drop = sortedPoints[i - 1].Percentage - sortedPoints[i].Percentage;
            if (drop > maxDrop)
            {
                maxDrop = drop;
            }
        }

        string trajectory = "Stable";
        if (maxDrop >= 15.0m)
        {
            trajectory = "AcademicShock";
        }
        else if (sortedPoints.Count >= 2)
        {
            decimal diff = sortedPoints.Last().Percentage - sortedPoints.First().Percentage;
            if (diff >= 5.0m) trajectory = "Improving";
            else if (diff <= -5.0m) trajectory = "Declining";
            else trajectory = "Stable";
        }

        var dto = new AcademicTrajectoryDto
        {
            StudentId = student.Id,
            FullName = student.FullName,
            TermPoints = sortedPoints,
            Trajectory = trajectory,
            MaxDropPercentage = maxDrop
        };

        return ApiResponse<AcademicTrajectoryDto>.Ok(dto);
    }

    public async Task<ApiResponse<FailingStudentsSummaryDto>> GetFailingStudentsForAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var assessment = await _dataStore.GetAssessmentByIdAsync(assessmentId, cancellationToken);
        if (assessment == null)
        {
            return ApiResponse<FailingStudentsSummaryDto>.Fail($"Assessment with ID '{assessmentId}' was not found.");
        }

        var subject = await _dataStore.GetSubjectByIdAsync(assessment.SubjectId, cancellationToken);
        var marks = await _dataStore.GetMarksByAssessmentAsync(assessmentId, cancellationToken);
        var failingMarks = marks.Where(m => !m.IsPass).ToList();

        var failingDtos = new List<StudentMarkDto>();
        foreach (var m in failingMarks)
        {
            var student = await _dataStore.GetStudentByIdAsync(m.StudentId, cancellationToken);
            failingDtos.Add(new StudentMarkDto
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

        var summary = new FailingStudentsSummaryDto
        {
            AssessmentId = assessment.Id,
            AssessmentTitle = assessment.Title,
            SubjectName = subject?.Name ?? "Unknown Subject",
            IsCoreSubject = subject?.IsCoreSubject ?? false,
            PassingMarks = assessment.PassingMarks,
            FailingStudents = failingDtos.OrderBy(f => f.MarksObtained).ToList()
        };

        return ApiResponse<FailingStudentsSummaryDto>.Ok(summary);
    }

    public async Task<ApiResponse<List<StudentReportCardDto>>> GetAtRiskAcademicStudentsBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        var students = await _dataStore.QueryStudentsAsync(s => s.SchoolId == schoolId, 1, 1000, cancellationToken);
        var atRiskReportCards = new List<StudentReportCardDto>();

        foreach (var s in students)
        {
            var res = await GenerateReportCardAsync(s.Id, null, cancellationToken);
            if (res.Success && res.Data != null && res.Data.IsAtAcademicRisk)
            {
                atRiskReportCards.Add(res.Data);
            }
        }

        var ordered = atRiskReportCards
            .OrderByDescending(r => r.CoreSubjectsFailedCount)
            .ThenBy(r => r.CumulativePercentage)
            .ToList();

        return ApiResponse<List<StudentReportCardDto>>.Ok(ordered);
    }
}
