using NotesBackend.Models;

namespace NotesBackend.Services
{
    // PUBLIC_INTERFACE
    /// <summary>
    /// Abstraction for storing and retrieving notes.
    /// </summary>
    public interface INoteRepository
    {
        Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken ct = default);
        Task<Note?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Note> CreateAsync(Note note, CancellationToken ct = default);
        Task<bool> UpdateAsync(Note note, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
