using EduGuard.Application.DTOs.Common;

namespace EduGuard.Application.Interfaces;

public interface IStudentRiskService
{
    Task<DropoutAnalyticsSummaryDto> GetRiskSummaryAsync(CancellationToken cancellationToken = default);
    Task<StudentRiskDto?> GetStudentRiskAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<List<InterventionDto>> GetInterventionsAsync(CancellationToken cancellationToken = default);
    Task<InterventionDto> CreateInterventionAsync(CreateInterventionDto request, Guid? createdByUserId = null, CancellationToken cancellationToken = default);
    Task<InterventionDto?> UpdateInterventionAsync(Guid id, UpdateInterventionDto request, CancellationToken cancellationToken = default);
    Task<List<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto request, CancellationToken cancellationToken = default);
    Task<ModelTrainingDto> TrainModelAsync(CancellationToken cancellationToken = default);
    Task<ModelTrainingDto?> GetModelStatusAsync(CancellationToken cancellationToken = default);
    Task<List<SupportRequestDto>> GetSupportRequestsAsync(CancellationToken cancellationToken = default);
    Task<SupportRequestDto?> CreateSupportRequestAsync(CreateSupportRequestDto request, CancellationToken cancellationToken = default);
    Task<SupportRequestDto?> UpdateSupportRequestAsync(Guid id, UpdateSupportRequestDto request, CancellationToken cancellationToken = default);
}
