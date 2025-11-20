using System.Collections.Concurrent;
using System.Text.Json;
using NotesBackend.Models;

namespace NotesBackend.Services
{
    /// <summary>
    /// A lightweight JSON-file based repository for notes, suitable for demos and local dev.
    /// Thread-safe via async lock to avoid concurrent file writes.
    /// </summary>
    public class JsonNoteRepository : INoteRepository
    {
        private readonly string _dataFilePath;
        private readonly SemaphoreSlim _ioLock = new(1, 1);
        private readonly ConcurrentDictionary<Guid, Note> _cache = new();

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonNoteRepository(IHostEnvironment env, IConfiguration config)
        {
            // Allow override via configuration: "Notes:DataFile"
            var configuredPath = config["Notes:DataFile"];
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                _dataFilePath = configuredPath!;
            }
            else
            {
                // Default under app data directory
                var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
                Directory.CreateDirectory(dataDir);
                _dataFilePath = Path.Combine(dataDir, "notes.json");
            }

            // Load on startup
            _ = EnsureLoadedAsync(CancellationToken.None);
        }

        private async Task EnsureLoadedAsync(CancellationToken ct)
        {
            await _ioLock.WaitAsync(ct);
            try
            {
                if (!File.Exists(_dataFilePath))
                {
                    await File.WriteAllTextAsync(_dataFilePath, "[]", ct);
                }
                var content = await File.ReadAllTextAsync(_dataFilePath, ct);
                var list = JsonSerializer.Deserialize<List<Note>>(content, SerializerOptions) ?? new List<Note>();

                _cache.Clear();
                foreach (var n in list)
                {
                    _cache[n.Id] = n;
                }
            }
            finally
            {
                _ioLock.Release();
            }
        }

        private async Task PersistAsync(CancellationToken ct)
        {
            await _ioLock.WaitAsync(ct);
            try
            {
                var list = _cache.Values.OrderBy(n => n.CreatedAtUtc).ToList();
                var json = JsonSerializer.Serialize(list, SerializerOptions);
                var dir = Path.GetDirectoryName(_dataFilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(_dataFilePath, json, ct);
            }
            finally
            {
                _ioLock.Release();
            }
        }

        public async Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken ct = default)
        {
            // ensure loaded at least once
            if (_cache.IsEmpty && File.Exists(_dataFilePath))
            {
                await EnsureLoadedAsync(ct);
            }
            var items = _cache.Values.OrderByDescending(n => n.UpdatedAtUtc).ToList();
            return items;
        }

        public async Task<Note?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (_cache.IsEmpty && File.Exists(_dataFilePath))
            {
                await EnsureLoadedAsync(ct);
            }
            _cache.TryGetValue(id, out var note);
            return note;
        }

        public async Task<Note> CreateAsync(Note note, CancellationToken ct = default)
        {
            note.Id = note.Id == Guid.Empty ? Guid.NewGuid() : note.Id;
            note.CreatedAtUtc = DateTime.UtcNow;
            note.UpdatedAtUtc = note.CreatedAtUtc;

            _cache[note.Id] = note;
            await PersistAsync(ct);
            return note;
        }

        public async Task<bool> UpdateAsync(Note note, CancellationToken ct = default)
        {
            if (!_cache.ContainsKey(note.Id))
            {
                return false;
            }
            note.UpdatedAtUtc = DateTime.UtcNow;
            _cache[note.Id] = note;
            await PersistAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var removed = _cache.TryRemove(id, out _);
            if (removed)
            {
                await PersistAsync(ct);
            }
            return removed;
        }
    }

    /// <summary>
    /// DI registration helpers for notes services.
    /// </summary>
    public static class NotesServiceCollectionExtensions
    {
        // PUBLIC_INTERFACE
        /// <summary>
        /// Registers the Notes repository using a JSON-file persistence.
        /// </summary>
        public static IServiceCollection AddNotesRepository(this IServiceCollection services)
        {
            services.AddSingleton<INoteRepository, JsonNoteRepository>();
            return services;
        }
    }
}
