using Kurator.Core.Entities;

namespace Kurator.Core.Interfaces;

public interface IInteractionService
{
    Task<(IEnumerable<Interaction> Interactions, int Total)> GetInteractionsAsync(
        int userId,
        bool isAdmin,
        int? contactId = null,
        int? blockId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? interactionTypeId = null,
        int? resultId = null,
        int page = 1,
        int pageSize = 50);

    Task<Interaction?> GetInteractionByIdAsync(int id, int userId, bool isAdmin);

    Task<Interaction> CreateInteractionAsync(
        int contactId,
        int userId,
        bool isAdmin,
        DateTime? interactionDate = null,
        int? interactionTypeId = null,
        int? resultId = null,
        string? comment = null,
        string? statusChangeJson = null,
        string? attachmentsJson = null,
        DateTime? nextTouchDate = null);

    Task UpdateInteractionAsync(
        int id,
        int userId,
        bool isAdmin,
        DateTime? interactionDate = null,
        int? interactionTypeId = null,
        int? resultId = null,
        string? comment = null,
        string? statusChangeJson = null,
        DateTime? nextTouchDate = null);

    Task DeactivateInteractionAsync(int id, int userId);

    Task<IEnumerable<Interaction>> GetRecentInteractionsAsync(int userId, bool isAdmin, int count = 5);

    Task RecordStatusChangeAsync(int contactId, int userId, string statusChangeJson);
}
