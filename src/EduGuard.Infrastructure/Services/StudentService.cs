using EduGuard.Application.DTOs.Common;
using EduGuard.Application.DTOs.Students;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Entities;
using EduGuard.Domain.Enums;

namespace EduGuard.Infrastructure.Services;

public class StudentService : IStudentService
{
    private readonly IEduGuardDataStore _dataStore;

    public StudentService(IEduGuardDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<ApiResponse<StudentDto>> RegisterStudentAsync(RegisterStudentDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AdmissionNumber) || string.IsNullOrWhiteSpace(request.FirstName))
        {
            return ApiResponse<StudentDto>.Fail("Admission number and first name are required.");
        }

        var existing = await _dataStore.GetStudentByAdmissionNumberAsync(request.AdmissionNumber, cancellationToken);
        if (existing != null)
        {
            return ApiResponse<StudentDto>.Fail($"A student with admission number '{request.AdmissionNumber}' already exists.");
        }

        var school = await _dataStore.GetSchoolByIdAsync(request.SchoolId, cancellationToken);
        if (school == null)
        {
            return ApiResponse<StudentDto>.Fail("Specified school was not found.");
        }

        var student = new Student
        {
            Id = Guid.NewGuid(),
            AdmissionNumber = request.AdmissionNumber.Trim().ToUpperInvariant(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender.Trim(),
            MotherTongue = request.MotherTongue?.Trim(),
            SocialCategory = request.SocialCategory?.Trim(),
            IsBPL = request.IsBPL,
            IsFirstGenerationLearner = request.IsFirstGenerationLearner,
            HasSpecialNeeds = request.HasSpecialNeeds,
            IsSingleParentOrOrphan = request.IsSingleParentOrOrphan,
            DistanceToSchoolKm = request.DistanceToSchoolKm,
            TransportMode = request.TransportMode,
            FamilyIncomeTier = request.FamilyIncomeTier,
            GuardianName = request.GuardianName?.Trim(),
            GuardianRelationship = request.GuardianRelationship?.Trim(),
            GuardianPhone = request.GuardianPhone?.Trim(),
            GuardianOccupation = request.GuardianOccupation?.Trim(),
            ResidentialAddress = request.ResidentialAddress?.Trim(),
            SchoolId = request.SchoolId,
            CurrentStatus = StudentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        // If an initial section is provided, assign and enroll
        string className = string.Empty;
        if (request.InitialSectionId.HasValue)
        {
            var section = await _dataStore.GetSectionByIdAsync(request.InitialSectionId.Value, cancellationToken);
            if (section != null)
            {
                student.CurrentSectionId = section.Id;
                var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
                className = $"{grade?.Name}-{section.SectionName}";

                var academicYearId = request.AcademicYearId ?? section.AcademicYearId;
                var enrollment = new Enrollment
                {
                    Id = Guid.NewGuid(),
                    StudentId = student.Id,
                    SectionId = section.Id,
                    AcademicYearId = academicYearId,
                    EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status = StudentStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow
                };
                await _dataStore.AddEnrollmentAsync(enrollment, cancellationToken);
            }
        }

        await _dataStore.AddStudentAsync(student, cancellationToken);

        // Record Initial Timeline Event
        await _dataStore.AddHistoryItemAsync(new StudentHistory
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            EventType = "Registration",
            Title = "Student Registered",
            Description = $"Student enrolled at {school.Name}. Socioeconomic flags: BPL={student.IsBPL}, FirstGen={student.IsFirstGenerationLearner}, Distance={student.DistanceToSchoolKm}km.",
            EventDateUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        var dto = MapToDto(student, school.Name, className);
        return ApiResponse<StudentDto>.Ok(dto, "Student registered successfully.");
    }

    public async Task<ApiResponse<StudentDto>> GetStudentByIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<StudentDto>.Fail("Student not found.");
        }

        var school = await _dataStore.GetSchoolByIdAsync(student.SchoolId, cancellationToken);
        string className = string.Empty;

        if (student.CurrentSectionId.HasValue)
        {
            var section = await _dataStore.GetSectionByIdAsync(student.CurrentSectionId.Value, cancellationToken);
            if (section != null)
            {
                var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
                className = $"{grade?.Name}-{section.SectionName}";
            }
        }

        return ApiResponse<StudentDto>.Ok(MapToDto(student, school?.Name ?? string.Empty, className));
    }

    public async Task<ApiResponse<PagedResult<StudentDto>>> GetStudentsAsync(StudentFilterDto filter, CancellationToken cancellationToken = default)
    {
        int page = Math.Max(1, filter.Page);
        int pageSize = Math.Clamp(filter.PageSize, 1, 100);

        Func<Student, bool> predicate = s =>
        {
            if (filter.SchoolId.HasValue && s.SchoolId != filter.SchoolId.Value)
                return false;

            if (filter.SectionId.HasValue && s.CurrentSectionId != filter.SectionId.Value)
                return false;

            if (filter.Status.HasValue && s.CurrentStatus != filter.Status.Value)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.Gender) &&
                !s.Gender.Equals(filter.Gender, StringComparison.OrdinalIgnoreCase))
                return false;

            if (filter.IsBPL.HasValue && s.IsBPL != filter.IsBPL.Value)
                return false;

            if (filter.IsFirstGenerationLearner.HasValue && s.IsFirstGenerationLearner != filter.IsFirstGenerationLearner.Value)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLowerInvariant();
                bool matches = s.AdmissionNumber.ToLowerInvariant().Contains(term) ||
                                s.FirstName.ToLowerInvariant().Contains(term) ||
                                s.LastName.ToLowerInvariant().Contains(term);
                if (!matches) return false;
            }

            return true;
        };

        var students = await _dataStore.QueryStudentsAsync(predicate, page, pageSize, cancellationToken);
        var total = await _dataStore.CountStudentsAsync(predicate, cancellationToken);

        var dtos = new List<StudentDto>();
        foreach (var s in students)
        {
            var school = await _dataStore.GetSchoolByIdAsync(s.SchoolId, cancellationToken);
            string className = string.Empty;
            if (s.CurrentSectionId.HasValue)
            {
                var section = await _dataStore.GetSectionByIdAsync(s.CurrentSectionId.Value, cancellationToken);
                if (section != null)
                {
                    var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
                    className = $"{grade?.Name}-{section.SectionName}";
                }
            }
            dtos.Add(MapToDto(s, school?.Name ?? string.Empty, className));
        }

        var result = new PagedResult<StudentDto>(dtos, total, page, pageSize);
        return ApiResponse<PagedResult<StudentDto>>.Ok(result);
    }

    public async Task<ApiResponse<StudentDto>> UpdateStudentAsync(Guid studentId, UpdateStudentDto request, CancellationToken cancellationToken = default)
    {
        var student = await _dataStore.GetStudentByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            return ApiResponse<StudentDto>.Fail("Student not found.");
        }

        student.FirstName = request.FirstName.Trim();
        student.LastName = request.LastName.Trim();
        student.DateOfBirth = request.DateOfBirth;
        student.Gender = request.Gender.Trim();
        student.MotherTongue = request.MotherTongue?.Trim();
        student.SocialCategory = request.SocialCategory?.Trim();
        student.IsBPL = request.IsBPL;
        student.IsFirstGenerationLearner = request.IsFirstGenerationLearner;
        student.HasSpecialNeeds = request.HasSpecialNeeds;
        student.IsSingleParentOrOrphan = request.IsSingleParentOrOrphan;
        student.DistanceToSchoolKm = request.DistanceToSchoolKm;
        student.TransportMode = request.TransportMode;
        student.FamilyIncomeTier = request.FamilyIncomeTier;
        student.GuardianName = request.GuardianName?.Trim();
        student.GuardianRelationship = request.GuardianRelationship?.Trim();
        student.GuardianPhone = request.GuardianPhone?.Trim();
        student.GuardianOccupation = request.GuardianOccupation?.Trim();
        student.ResidentialAddress = request.ResidentialAddress?.Trim();

        await _dataStore.UpdateStudentAsync(student, cancellationToken);

        var school = await _dataStore.GetSchoolByIdAsync(student.SchoolId, cancellationToken);
        string className = string.Empty;
        if (student.CurrentSectionId.HasValue)
        {
            var section = await _dataStore.GetSectionByIdAsync(student.CurrentSectionId.Value, cancellationToken);
            if (section != null)
            {
                var grade = await _dataStore.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);
                className = $"{grade?.Name}-{section.SectionName}";
            }
        }

        return ApiResponse<StudentDto>.Ok(MapToDto(student, school?.Name ?? string.Empty, className), "Student details updated.");
    }

    private static StudentDto MapToDto(Student s, string schoolName, string className) => new()
    {
        Id = s.Id,
        AdmissionNumber = s.AdmissionNumber,
        FirstName = s.FirstName,
        LastName = s.LastName,
        DateOfBirth = s.DateOfBirth,
        Gender = s.Gender,
        MotherTongue = s.MotherTongue,
        SocialCategory = s.SocialCategory,
        IsBPL = s.IsBPL,
        IsFirstGenerationLearner = s.IsFirstGenerationLearner,
        HasSpecialNeeds = s.HasSpecialNeeds,
        IsSingleParentOrOrphan = s.IsSingleParentOrOrphan,
        DistanceToSchoolKm = s.DistanceToSchoolKm,
        TransportMode = s.TransportMode,
        FamilyIncomeTier = s.FamilyIncomeTier,
        GuardianName = s.GuardianName,
        GuardianRelationship = s.GuardianRelationship,
        GuardianPhone = s.GuardianPhone,
        GuardianOccupation = s.GuardianOccupation,
        ResidentialAddress = s.ResidentialAddress,
        SchoolId = s.SchoolId,
        SchoolName = schoolName,
        CurrentSectionId = s.CurrentSectionId,
        CurrentClassName = className,
        CurrentStatus = s.CurrentStatus,
        CreatedAtUtc = s.CreatedAtUtc
    };
}
