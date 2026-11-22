using EduGuard.Application.DTOs.Common;
using EduGuard.Application.Interfaces;
using EduGuard.Domain.Enums;
using Microsoft.Data.Sqlite;

namespace EduGuard.Infrastructure.Services;

public class StudentRiskService : IStudentRiskService
{
    private readonly IEduGuardDataStore _store;
    private readonly string _databasePath;
    private readonly string _connectionString;

    public StudentRiskService(IEduGuardDataStore store)
    {
        _store = store;
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EduGuard");
        Directory.CreateDirectory(appData);
        _databasePath = Path.Combine(appData, "eduguard.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath }.ToString();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Interventions (
                Id TEXT PRIMARY KEY,
                StudentId TEXT NOT NULL,
                StudentName TEXT NOT NULL,
                Plan TEXT NOT NULL,
                AssignedTo TEXT NOT NULL,
                Status TEXT NOT NULL,
                Priority TEXT NOT NULL,
                FollowUpNotes TEXT,
                CreatedAtUtc TEXT NOT NULL,
                FollowUpAtUtc TEXT
            );
            CREATE TABLE IF NOT EXISTS Notifications (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Message TEXT NOT NULL,
                Audience TEXT NOT NULL,
                Read INTEGER NOT NULL DEFAULT 0,
                CreatedAtUtc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS SupportRequests (
                Id TEXT PRIMARY KEY,
                StudentId TEXT NOT NULL,
                StudentName TEXT NOT NULL,
                AdmissionNumber TEXT NOT NULL,
                ClassName TEXT,
                Category TEXT NOT NULL,
                CategoryLabel TEXT NOT NULL,
                Urgency TEXT NOT NULL,
                Description TEXT NOT NULL,
                ReportedBy TEXT NOT NULL,
                Status TEXT NOT NULL,
                ResolutionNotes TEXT,
                CreatedAtUtc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ModelMetrics (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                TrainedAtUtc TEXT NOT NULL,
                TrainingRecords INTEGER NOT NULL,
                Accuracy REAL NOT NULL,
                Recall REAL NOT NULL,
                Precision REAL NOT NULL,
                F1Score REAL NOT NULL,
                Algorithm TEXT NOT NULL DEFAULT 'legacy'
            );
            """;
        command.ExecuteNonQuery();

        command.CommandText = "PRAGMA table_info(ModelMetrics);";
        using (var columns = command.ExecuteReader())
        {
            var hasAlgorithm = false;
            while (columns.Read())
                hasAlgorithm |= columns.GetString(1).Equals("Algorithm", StringComparison.OrdinalIgnoreCase);
            if (!hasAlgorithm)
            {
                columns.Close();
                command.CommandText = "ALTER TABLE ModelMetrics ADD COLUMN Algorithm TEXT NOT NULL DEFAULT 'legacy';";
                command.ExecuteNonQuery();
            }
        }
        command.CommandText = "DELETE FROM ModelMetrics WHERE Algorithm <> 'logistic-regression-v1';";
        command.ExecuteNonQuery();

        command.CommandText = "SELECT COUNT(*) FROM Notifications;";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            command.CommandText = """
                INSERT INTO Notifications (Id, Title, Message, Audience, Read, CreatedAtUtc)
                VALUES ($id, 'Monthly risk review', 'Review high-risk students and update intervention follow-ups.', 'Counselors', 0, $date);
                """;
            command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$date", DateTime.UtcNow.ToString("O"));
            command.ExecuteNonQuery();
        }
    }

    public async Task<DropoutAnalyticsSummaryDto> GetRiskSummaryAsync(CancellationToken cancellationToken = default)
    {
        var students = await _store.QueryStudentsAsync(student => student.CurrentStatus != StudentStatus.Archived, 1, 1000, cancellationToken);
        var risks = new List<StudentRiskDto>();

        foreach (var student in students)
        {
            var risk = await CalculateRiskAsync(student.Id, student, cancellationToken);
            if (risk != null) risks.Add(risk);
        }

        risks = risks.OrderByDescending(r => r.RiskScore).ToList();
        var assessedRisks = risks.Where(r => r.RiskLevel != "Unassessed").ToList();
        return new DropoutAnalyticsSummaryDto
        {
            TotalStudents = risks.Count,
            CriticalRisk = risks.Count(r => r.RiskLevel == "Critical"),
            HighRisk = risks.Count(r => r.RiskLevel == "High"),
            ModerateRisk = risks.Count(r => r.RiskLevel == "Moderate"),
            LowRisk = risks.Count(r => r.RiskLevel == "Low"),
            Unassessed = risks.Count(r => r.RiskLevel == "Unassessed"),
            AverageRiskScore = assessedRisks.Count == 0 ? 0 : decimal.Round(assessedRisks.Average(r => r.RiskScore), 1),
            Students = risks
        };
    }

    public async Task<StudentRiskDto?> GetStudentRiskAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _store.GetStudentByIdAsync(studentId, cancellationToken);
        return student == null ? null : await CalculateRiskAsync(studentId, student, cancellationToken);
    }

    private async Task<StudentRiskDto?> CalculateRiskAsync(Guid studentId, Domain.Entities.Student student, CancellationToken cancellationToken)
    {
        var attendance = await _store.GetAttendanceByStudentAsync(studentId, null, null, cancellationToken);
        var attendancePercentage = attendance.Count == 0
            ? 0m
            : decimal.Round(attendance.Count(a => a.Status == Domain.Enums.AttendanceStatus.Present) * 100m / attendance.Count, 1);

        var marks = await _store.GetMarksByStudentAsync(studentId, cancellationToken);
        var academicAverage = marks.Count == 0
            ? 0m
            : decimal.Round(marks.Where(m => !m.IsAbsent).Select(m => m.MarksObtained).DefaultIfEmpty(0).Average(), 1);

        decimal score = 0;
        var factors = new List<string>();

        if (attendance.Count > 0 && attendancePercentage < 85)
        {
            score += attendancePercentage < 75 ? 30 : 20;
            factors.Add("Chronic or declining attendance");
        }
        if (marks.Count > 0 && academicAverage < 40)
        {
            score += 28;
            factors.Add("Severe academic underperformance");
        }
        else if (marks.Count > 0 && academicAverage < 60)
        {
            score += 15;
            factors.Add("Low academic performance");
        }
        if (student.IsBPL) { score += 10; factors.Add("Below Poverty Line"); }
        if (student.IsFirstGenerationLearner) { score += 8; factors.Add("First-generation learner"); }
        if (student.IsSingleParentOrOrphan) { score += 8; factors.Add("Single-parent or orphan household"); }
        if (student.DistanceToSchoolKm > 5) { score += 8; factors.Add("Long commute distance"); }
        if (student.HasSpecialNeeds) { score += 5; factors.Add("Accessibility support needs"); }

        if (attendance.Count == 0) factors.Add("Attendance history not recorded");
        if (marks.Count == 0) factors.Add("Academic marks not recorded");
        var hasObservedSignals = attendance.Count > 0 || marks.Count > 0 ||
            student.IsBPL || student.IsFirstGenerationLearner || student.IsSingleParentOrOrphan ||
            student.DistanceToSchoolKm > 5 || student.HasSpecialNeeds;
        var level = !hasObservedSignals ? "Unassessed"
            : score >= 75 ? "Critical" : score >= 55 ? "High" : score >= 35 ? "Moderate" : "Low";
        if (factors.Count == 0) factors.Add("No major risk factors detected");

        var section = student.CurrentSectionId.HasValue
            ? await _store.GetSectionByIdAsync(student.CurrentSectionId.Value, cancellationToken)
            : null;
        var grade = section == null ? null : await _store.GetGradeByIdAsync(section.ClassGradeId, cancellationToken);

        return new StudentRiskDto
        {
            StudentId = student.Id,
            AdmissionNumber = student.AdmissionNumber,
            StudentName = student.FullName,
            ClassName = section == null ? null : $"{grade?.Name}-{section.SectionName}",
            RiskScore = Math.Min(score, 100),
            RiskLevel = level,
            AttendancePercentage = attendancePercentage,
            AcademicAverage = academicAverage,
            AttendanceDataAvailable = attendance.Count > 0,
            AcademicDataAvailable = marks.Count > 0,
            Factors = factors
        };
    }

    public async Task<List<InterventionDto>> GetInterventionsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<InterventionDto>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Interventions ORDER BY CreatedAtUtc DESC;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadIntervention(reader));
        }
        return items;
    }

    public async Task<InterventionDto> CreateInterventionAsync(CreateInterventionDto request, Guid? createdByUserId = null, CancellationToken cancellationToken = default)
    {
        var student = await _store.GetStudentByIdAsync(request.StudentId, cancellationToken);
        var item = new InterventionDto
        {
            Id = Guid.NewGuid(),
            StudentId = request.StudentId,
            StudentName = student?.FullName ?? "Unknown student",
            Plan = request.Plan,
            AssignedTo = request.AssignedTo,
            Priority = request.Priority,
            Status = "Open",
            FollowUpAtUtc = request.FollowUpAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Interventions
            (Id, StudentId, StudentName, Plan, AssignedTo, Status, Priority, FollowUpNotes, CreatedAtUtc, FollowUpAtUtc)
            VALUES ($id, $studentId, $studentName, $plan, $assignedTo, $status, $priority, NULL, $createdAt, $followUp);
            """;
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$studentId", item.StudentId.ToString());
        command.Parameters.AddWithValue("$studentName", item.StudentName);
        command.Parameters.AddWithValue("$plan", item.Plan);
        command.Parameters.AddWithValue("$assignedTo", item.AssignedTo);
        command.Parameters.AddWithValue("$status", item.Status);
        command.Parameters.AddWithValue("$priority", item.Priority);
        command.Parameters.AddWithValue("$createdAt", item.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$followUp", item.FollowUpAtUtc?.ToString("O") ?? (object)DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return item;
    }

    public async Task<InterventionDto?> UpdateInterventionAsync(Guid id, UpdateInterventionDto request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Interventions
            SET Status = $status, FollowUpNotes = $notes, FollowUpAtUtc = $followUp
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$status", request.Status);
        command.Parameters.AddWithValue("$notes", request.FollowUpNotes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$followUp", request.FollowUpAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);

        command.CommandText = "SELECT * FROM Interventions WHERE Id = $id;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadIntervention(reader) : null;
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<NotificationDto>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Notifications ORDER BY CreatedAtUtc DESC;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new NotificationDto
            {
                Id = Guid.Parse(reader.GetString(0)),
                Title = reader.GetString(1),
                Message = reader.GetString(2),
                Audience = reader.GetString(3),
                Read = reader.GetInt32(4) == 1,
                CreatedAtUtc = DateTime.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind)
            });
        }
        return items;
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto request, CancellationToken cancellationToken = default)
    {
        var item = new NotificationDto
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Message = request.Message,
            Audience = request.Audience,
            CreatedAtUtc = DateTime.UtcNow
        };
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Notifications (Id, Title, Message, Audience, Read, CreatedAtUtc) VALUES ($id, $title, $message, $audience, 0, $date);";
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$title", item.Title);
        command.Parameters.AddWithValue("$message", item.Message);
        command.Parameters.AddWithValue("$audience", item.Audience);
        command.Parameters.AddWithValue("$date", item.CreatedAtUtc.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
        return item;
    }

    public async Task<ModelTrainingDto> TrainModelAsync(CancellationToken cancellationToken = default)
    {
        var students = await _store.QueryStudentsAsync(
            student => student.CurrentStatus is StudentStatus.DroppedOut or StudentStatus.Graduated,
            1, 1000, cancellationToken);
        var labeled = new List<(double Feature, int Label, string Key)>();
        foreach (var student in students)
        {
            var risk = await CalculateRiskAsync(student.Id, student, cancellationToken);
            if (risk != null)
                labeled.Add((Convert.ToDouble(risk.RiskScore) / 100d,
                    student.CurrentStatus == StudentStatus.DroppedOut ? 1 : 0,
                    student.AdmissionNumber));
        }

        if (labeled.Count < 10 || labeled.Select(item => item.Label).Distinct().Count() < 2)
        {
            return new ModelTrainingDto
            {
                TrainingRecords = labeled.Count,
                Status = "Insufficient labeled history: need at least 10 graduated/dropped-out records with both outcomes."
            };
        }

        var ordered = labeled.OrderBy(item => item.Key, StringComparer.Ordinal).ToList();
        var training = ordered.Where((_, index) => index % 5 != 0).ToList();
        var testing = ordered.Where((_, index) => index % 5 == 0).ToList();
        if (training.Select(item => item.Label).Distinct().Count() < 2 ||
            testing.Count == 0 || testing.Select(item => item.Label).Distinct().Count() < 2)
        {
            return new ModelTrainingDto
            {
                TrainingRecords = labeled.Count,
                Status = "Insufficient outcome balance for a reliable holdout evaluation."
            };
        }

        var weight = 0d;
        var bias = 0d;
        const double learningRate = 0.1d;
        for (var epoch = 0; epoch < 2000; epoch++)
        {
            foreach (var sample in training)
            {
                var probability = Sigmoid(weight * sample.Feature + bias);
                var error = probability - sample.Label;
                weight -= learningRate * error * sample.Feature;
                bias -= learningRate * error;
            }
        }

        var truePositive = 0;
        var falsePositive = 0;
        var falseNegative = 0;
        var correct = 0;
        foreach (var sample in testing)
        {
            var predicted = Sigmoid(weight * sample.Feature + bias) >= 0.5d ? 1 : 0;
            if (predicted == sample.Label) correct++;
            if (predicted == 1 && sample.Label == 1) truePositive++;
            if (predicted == 1 && sample.Label == 0) falsePositive++;
            if (predicted == 0 && sample.Label == 1) falseNegative++;
        }
        var precision = truePositive + falsePositive == 0 ? 0m : (decimal)truePositive / (truePositive + falsePositive) * 100m;
        var recall = truePositive + falseNegative == 0 ? 0m : (decimal)truePositive / (truePositive + falseNegative) * 100m;
        var f1 = precision + recall == 0 ? 0m : 2m * precision * recall / (precision + recall);
        var result = new ModelTrainingDto
        {
            TrainedAtUtc = DateTime.UtcNow,
            TrainingRecords = labeled.Count,
            Accuracy = decimal.Round((decimal)correct * 100m / testing.Count, 1),
            Recall = decimal.Round(recall, 1),
            Precision = decimal.Round(precision, 1),
            F1Score = decimal.Round(f1, 1),
            Status = $"Evaluated logistic regression on {testing.Count} held-out labeled students."
        };

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ModelMetrics (Id, TrainedAtUtc, TrainingRecords, Accuracy, Recall, Precision, F1Score, Algorithm)
            VALUES (1, $trained, $records, $accuracy, $recall, $precision, $f1, 'logistic-regression-v1')
            ON CONFLICT(Id) DO UPDATE SET
                TrainedAtUtc = excluded.TrainedAtUtc,
                TrainingRecords = excluded.TrainingRecords,
                Accuracy = excluded.Accuracy,
                Recall = excluded.Recall,
                Precision = excluded.Precision,
                F1Score = excluded.F1Score,
                Algorithm = excluded.Algorithm;
            """;
        command.Parameters.AddWithValue("$trained", result.TrainedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$records", result.TrainingRecords);
        command.Parameters.AddWithValue("$accuracy", (double)result.Accuracy);
        command.Parameters.AddWithValue("$recall", (double)result.Recall);
        command.Parameters.AddWithValue("$precision", (double)result.Precision);
        command.Parameters.AddWithValue("$f1", (double)result.F1Score);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return result;
    }

    public async Task<ModelTrainingDto?> GetModelStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM ModelMetrics WHERE Id = 1 AND Algorithm = 'logistic-regression-v1';";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new ModelTrainingDto
        {
            TrainedAtUtc = DateTime.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
            TrainingRecords = reader.GetInt32(2),
            Accuracy = (decimal)reader.GetDouble(3),
            Recall = (decimal)reader.GetDouble(4),
            Precision = (decimal)reader.GetDouble(5),
            F1Score = (decimal)reader.GetDouble(6),
            Status = "Evaluated logistic regression on labeled historical outcomes."
        };
    }

    public async Task<List<SupportRequestDto>> GetSupportRequestsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<SupportRequestDto>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM SupportRequests ORDER BY CreatedAtUtc DESC;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(ReadSupportRequest(reader));
        return items;
    }

    public async Task<SupportRequestDto?> CreateSupportRequestAsync(CreateSupportRequestDto request, CancellationToken cancellationToken = default)
    {
        var risk = await GetStudentRiskAsync(request.StudentId, cancellationToken);
        var student = await _store.GetStudentByIdAsync(request.StudentId, cancellationToken);
        if (student == null || risk == null) return null;

        var item = new SupportRequestDto
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            StudentName = student.FullName,
            AdmissionNumber = student.AdmissionNumber,
            ClassName = risk.ClassName,
            Category = request.Category,
            CategoryLabel = request.CategoryLabel,
            Urgency = request.Urgency,
            Description = request.Description,
            ReportedBy = request.ReportedBy,
            Status = "Under Review",
            CreatedAtUtc = DateTime.UtcNow
        };
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO SupportRequests
            (Id, StudentId, StudentName, AdmissionNumber, ClassName, Category, CategoryLabel,
             Urgency, Description, ReportedBy, Status, ResolutionNotes, CreatedAtUtc)
            VALUES ($id, $studentId, $name, $admission, $class, $category, $label,
                    $urgency, $description, $reportedBy, $status, NULL, $createdAt);
            """;
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$studentId", item.StudentId.ToString());
        command.Parameters.AddWithValue("$name", item.StudentName);
        command.Parameters.AddWithValue("$admission", item.AdmissionNumber);
        command.Parameters.AddWithValue("$class", item.ClassName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$category", item.Category);
        command.Parameters.AddWithValue("$label", item.CategoryLabel);
        command.Parameters.AddWithValue("$urgency", item.Urgency);
        command.Parameters.AddWithValue("$description", item.Description);
        command.Parameters.AddWithValue("$reportedBy", item.ReportedBy);
        command.Parameters.AddWithValue("$status", item.Status);
        command.Parameters.AddWithValue("$createdAt", item.CreatedAtUtc.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
        return item;
    }

    public async Task<SupportRequestDto?> UpdateSupportRequestAsync(Guid id, UpdateSupportRequestDto request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE SupportRequests SET Status = $status, ResolutionNotes = $notes WHERE Id = $id;";
        command.Parameters.AddWithValue("$status", request.Status);
        command.Parameters.AddWithValue("$notes", request.ResolutionNotes);
        command.Parameters.AddWithValue("$id", id.ToString());
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) return null;
        command.CommandText = "SELECT * FROM SupportRequests WHERE Id = $id;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadSupportRequest(reader) : null;
    }

    private static SupportRequestDto ReadSupportRequest(SqliteDataReader reader)
    {
        return new SupportRequestDto
        {
            Id = Guid.Parse(reader.GetString(0)),
            StudentId = Guid.Parse(reader.GetString(1)),
            StudentName = reader.GetString(2),
            AdmissionNumber = reader.GetString(3),
            ClassName = reader.IsDBNull(4) ? null : reader.GetString(4),
            Category = reader.GetString(5),
            CategoryLabel = reader.GetString(6),
            Urgency = reader.GetString(7),
            Description = reader.GetString(8),
            ReportedBy = reader.GetString(9),
            Status = reader.GetString(10),
            ResolutionNotes = reader.IsDBNull(11) ? null : reader.GetString(11),
            CreatedAtUtc = DateTime.Parse(reader.GetString(12), null, System.Globalization.DateTimeStyles.RoundtripKind)
        };
    }

    private static double Sigmoid(double value)
        => 1d / (1d + Math.Exp(-Math.Clamp(value, -30d, 30d)));

    private static InterventionDto ReadIntervention(SqliteDataReader reader)
    {
        return new InterventionDto
        {
            Id = Guid.Parse(reader.GetString(0)),
            StudentId = Guid.Parse(reader.GetString(1)),
            StudentName = reader.GetString(2),
            Plan = reader.GetString(3),
            AssignedTo = reader.GetString(4),
            Status = reader.GetString(5),
            Priority = reader.GetString(6),
            FollowUpNotes = reader.IsDBNull(7) ? null : reader.GetString(7),
            CreatedAtUtc = DateTime.Parse(reader.GetString(8), null, System.Globalization.DateTimeStyles.RoundtripKind),
            FollowUpAtUtc = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9), null, System.Globalization.DateTimeStyles.RoundtripKind)
        };
    }
}
