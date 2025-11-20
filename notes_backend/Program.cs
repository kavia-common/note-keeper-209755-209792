using Microsoft.AspNetCore.Http.HttpResults;
using NotesBackend.Models;
using NotesBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(settings =>
{
    settings.Title = "Notes API";
    settings.Description = "Simple CRUD API for managing notes.";
    settings.Version = "v1";
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Notes repository (JSON-file backed)
builder.Services.AddNotesRepository();

var app = builder.Build();

// Use CORS
app.UseCors("AllowAll");

// Configure OpenAPI/Swagger
app.UseOpenApi();
app.UseSwaggerUi(config =>
{
    config.Path = "/docs";
});

// Health check endpoint
// PUBLIC_INTERFACE
app.MapGet("/", () => new { message = "Healthy" })
   .WithTags("Health")
   .WithSummary("Health check")
   .WithDescription("Returns a simple JSON payload to indicate the service is healthy.");

var notesGroup = app.MapGroup("/api/notes").WithTags("Notes");

// PUBLIC_INTERFACE
/// <summary>
/// Get all notes.
/// </summary>
/// <returns>List of notes.</returns>
notesGroup.MapGet("", async (INoteRepository repo, CancellationToken ct) =>
{
    var items = await repo.GetAllAsync(ct);
    return Results.Ok(items.Select(NoteResponse.From));
})
.WithSummary("List notes")
.WithDescription("Returns all notes ordered by last updated descending.")
.Produces<IEnumerable<NoteResponse>>(StatusCodes.Status200OK);

// PUBLIC_INTERFACE
/// <summary>
/// Get a note by id.
/// </summary>
/// <param name="id">The note id.</param>
/// <returns>The note if found.</returns>
notesGroup.MapGet("{id:guid}", async Task<Results<Ok<NoteResponse>, NotFound>> (Guid id, INoteRepository repo, CancellationToken ct) =>
{
    var note = await repo.GetByIdAsync(id, ct);
    if (note is null) return TypedResults.NotFound();
    return TypedResults.Ok(NoteResponse.From(note));
})
.WithSummary("Get note by id")
.WithDescription("Returns a single note by its unique identifier.")
.Produces<NoteResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// PUBLIC_INTERFACE
/// <summary>
/// Create a new note.
/// </summary>
/// <param name="request">Note payload.</param>
/// <returns>The created note.</returns>
notesGroup.MapPost("", async Task<Results<Created<NoteResponse>, BadRequest<string>>> (CreateNoteRequest request, INoteRepository repo, HttpContext http, CancellationToken ct) =>
{
    // Basic validation leveraging data annotations handled by minimal API binder is limited; we manually check here.
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
    {
        return TypedResults.BadRequest("Title and Content are required.");
    }
    if (request.Title.Length > 200 || request.Content.Length > 20000)
    {
        return TypedResults.BadRequest("Title must be <= 200 chars and Content <= 20000 chars.");
    }

    var entity = new Note
    {
        Title = request.Title.Trim(),
        Content = request.Content.Trim()
    };
    var created = await repo.CreateAsync(entity, ct);

    var response = NoteResponse.From(created);
    var location = $"{http.Request.Scheme}://{http.Request.Host}/api/notes/{response.Id}";
    return TypedResults.Created(location, response);
})
.WithSummary("Create note")
.WithDescription("Creates a new note and returns the created resource.")
.Produces<NoteResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

// PUBLIC_INTERFACE
/// <summary>
/// Update an existing note.
/// </summary>
/// <param name="id">Note id.</param>
/// <param name="request">Update payload.</param>
/// <returns>No content on success.</returns>
notesGroup.MapPut("{id:guid}", async Task<Results<NoContent, NotFound, BadRequest<string>>> (Guid id, UpdateNoteRequest request, INoteRepository repo, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
    {
        return TypedResults.BadRequest("Title and Content are required.");
    }
    if (request.Title.Length > 200 || request.Content.Length > 20000)
    {
        return TypedResults.BadRequest("Title must be <= 200 chars and Content <= 20000 chars.");
    }

    var existing = await repo.GetByIdAsync(id, ct);
    if (existing is null) return TypedResults.NotFound();

    existing.Title = request.Title.Trim();
    existing.Content = request.Content.Trim();
    var ok = await repo.UpdateAsync(existing, ct);
    return ok ? TypedResults.NoContent() : TypedResults.NotFound();
})
.WithSummary("Update note")
.WithDescription("Updates title and content of an existing note by id.")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status400BadRequest);

// PUBLIC_INTERFACE
/// <summary>
/// Delete a note by id.
/// </summary>
/// <param name="id">Note id.</param>
/// <returns>No content on success.</returns>
notesGroup.MapDelete("{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, INoteRepository repo, CancellationToken ct) =>
{
    var ok = await repo.DeleteAsync(id, ct);
    return ok ? TypedResults.NoContent() : TypedResults.NotFound();
})
.WithSummary("Delete note")
.WithDescription("Deletes a note by its id.")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.Run();