using System.ComponentModel.DataAnnotations;

namespace NotesBackend.Models
{
    /// <summary>
    /// Note entity stored by the service.
    /// </summary>
    public class Note
    {
        /// <summary>
        /// Unique identifier of the note.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Title of the note.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The content/body of the note.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// When the note was created (UTC).
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the note was last updated (UTC).
        /// </summary>
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    // PUBLIC_INTERFACE
    /// <summary>
    /// Request payload for creating a new note.
    /// </summary>
    public class CreateNoteRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(20000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    // PUBLIC_INTERFACE
    /// <summary>
    /// Request payload for updating an existing note.
    /// </summary>
    public class UpdateNoteRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(20000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    // PUBLIC_INTERFACE
    /// <summary>
    /// Response payload for returning a note.
    /// </summary>
    public class NoteResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public static NoteResponse From(Note n) => new NoteResponse
        {
            Id = n.Id,
            Title = n.Title,
            Content = n.Content,
            CreatedAtUtc = n.CreatedAtUtc,
            UpdatedAtUtc = n.UpdatedAtUtc
        };
    }
}
