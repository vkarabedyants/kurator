using Kurator.Core.Entities;

namespace Kurator.Core.Interfaces;

public interface IContactService
{
    Task<(IEnumerable<Contact> Contacts, int Total)> GetContactsAsync(
        int userId,
        bool isAdmin,
        int? blockId = null,
        string? search = null,
        int? influenceStatusId = null,
        int? influenceTypeId = null,
        int? organizationId = null,
        int page = 1,
        int pageSize = 50);

    Task<Contact?> GetContactByIdAsync(int id, int userId, bool isAdmin);

    Task<Contact> CreateContactAsync(
        int blockId,
        string fullName,
        int userId,
        bool isAdmin,
        int? organizationId = null,
        string? position = null,
        int? influenceStatusId = null,
        int? influenceTypeId = null,
        string? usefulnessDescription = null,
        int? communicationChannelId = null,
        int? contactSourceId = null,
        DateTime? nextTouchDate = null,
        string? notes = null);

    Task UpdateContactAsync(
        int id,
        int userId,
        bool isAdmin,
        int? organizationId = null,
        string? position = null,
        int? influenceStatusId = null,
        int? influenceTypeId = null,
        string? usefulnessDescription = null,
        int? communicationChannelId = null,
        int? contactSourceId = null,
        DateTime? nextTouchDate = null,
        string? notes = null);

    Task DeleteContactAsync(int id, int userId);

    Task<IEnumerable<Contact>> GetOverdueContactsAsync(int userId, bool isAdmin);

    Task<bool> HasAccessToContactAsync(int contactId, int userId, bool isAdmin);

    Task<bool> HasAccessToBlockAsync(int blockId, int userId, bool isAdmin);

    Task<string> GenerateContactIdAsync(int blockId);
}
