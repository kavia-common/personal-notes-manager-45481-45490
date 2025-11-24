using System.ComponentModel.DataAnnotations;

namespace NotesBackend;

// Models
public record Note(
    Guid Id,
    string Title,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

// Request models with validation
public record CreateNoteRequest(
    [property: Required, MinLength(1)] string Title,
    [property: Required, MinLength(1)] string Content
);

public record UpdateNoteRequest(
    [property: Required, MinLength(1)] string Title,
    [property: Required, MinLength(1)] string Content
);

// Repository Abstraction
public interface INotesRepository
{
    // PUBLIC_INTERFACE
    IEnumerable<Note> GetAll();

    // PUBLIC_INTERFACE
    Note? GetById(Guid id);

    // PUBLIC_INTERFACE
    Note Create(string title, string content);

    // PUBLIC_INTERFACE
    Note? Update(Guid id, string title, string content);

    // PUBLIC_INTERFACE
    bool Delete(Guid id);
}

// In-memory repository implementation
public sealed class InMemoryNotesRepository : INotesRepository
{
    private readonly Dictionary<Guid, Note> _store = new();

    public IEnumerable<Note> GetAll()
    {
        return _store.Values.OrderByDescending(n => n.UpdatedAt);
    }

    public Note? GetById(Guid id)
    {
        return _store.TryGetValue(id, out var note) ? note : null;
    }

    public Note Create(string title, string content)
    {
        var now = DateTimeOffset.UtcNow;
        var note = new Note(Guid.NewGuid(), title.Trim(), content.Trim(), now, now);
        _store[note.Id] = note;
        return note;
    }

    public Note? Update(Guid id, string title, string content)
    {
        if (!_store.TryGetValue(id, out var existing))
        {
            return null;
        }

        var updated = existing with
        {
            Title = title.Trim(),
            Content = content.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _store[id] = updated;
        return updated;
    }

    public bool Delete(Guid id)
    {
        return _store.Remove(id);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApiDocument(settings =>
        {
            settings.Title = "Personal Notes API";
            settings.Description = "RESTful API for managing personal notes (CRUD).";
            settings.Version = "1.0.0";
            settings.DocumentName = "v1";
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

        // Add a simple in-memory repository as a singleton so notes persist while the app runs
        builder.Services.AddSingleton<INotesRepository, InMemoryNotesRepository>();

        var app = builder.Build();

        // Use CORS
        app.UseCors("AllowAll");

        // Configure OpenAPI/Swagger
        app.UseOpenApi();
        app.UseSwaggerUi(config =>
        {
            config.Path = "/docs";
            config.DocumentTitle = "Personal Notes API Docs";
        });

        // Health check endpoint
        // PUBLIC_INTERFACE
        app.MapGet("/", () => Results.Ok(new { message = "Healthy" }))
           .WithName("HealthCheck")
           .WithSummary("Health check")
           .WithDescription("Returns a basic message to indicate the API is running.");

        // Notes endpoints
        var notes = app.MapGroup("/api/notes").WithTags("Notes");

        // PUBLIC_INTERFACE
        notes.MapGet("/", (INotesRepository repo) =>
        {
            // Returns all notes
            var all = repo.GetAll();
            return Results.Ok(all);
        })
        .WithName("ListNotes")
        .WithSummary("List all notes")
        .WithDescription("Returns an array of all notes ordered by last update.")
        .Produces<IEnumerable<Note>>(StatusCodes.Status200OK);

        // PUBLIC_INTERFACE
        notes.MapGet("/{id:guid}", (Guid id, INotesRepository repo) =>
        {
            var note = repo.GetById(id);
            return note is not null ? Results.Ok(note) : Results.NotFound();
        })
        .WithName("GetNoteById")
        .WithSummary("Get note by id")
        .WithDescription("Returns a single note by its GUID identifier.")
        .Produces<Note>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // PUBLIC_INTERFACE
        notes.MapPost("/", (CreateNoteRequest req, INotesRepository repo) =>
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Content))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["title"] = string.IsNullOrWhiteSpace(req.Title) ? new[] { "Title is required." } : Array.Empty<string>(),
                    ["content"] = string.IsNullOrWhiteSpace(req.Content) ? new[] { "Content is required." } : Array.Empty<string>()
                });
            }

            var created = repo.Create(req.Title, req.Content);
            return Results.Created($"/api/notes/{created.Id}", created);
        })
        .WithName("CreateNote")
        .WithSummary("Create a new note")
        .WithDescription("Creates a new note with title and content. Returns the created resource.")
        .Produces<Note>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // PUBLIC_INTERFACE
        notes.MapPut("/{id:guid}", (Guid id, UpdateNoteRequest req, INotesRepository repo) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Content))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["title"] = string.IsNullOrWhiteSpace(req.Title) ? new[] { "Title is required." } : Array.Empty<string>(),
                    ["content"] = string.IsNullOrWhiteSpace(req.Content) ? new[] { "Content is required." } : Array.Empty<string>()
                });
            }

            var updated = repo.Update(id, req.Title, req.Content);
            return updated is not null ? Results.Ok(updated) : Results.NotFound();
        })
        .WithName("UpdateNote")
        .WithSummary("Update an existing note")
        .WithDescription("Updates the title and content of an existing note.")
        .Produces<Note>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);

        // PUBLIC_INTERFACE
        notes.MapDelete("/{id:guid}", (Guid id, INotesRepository repo) =>
        {
            var deleted = repo.Delete(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteNote")
        .WithSummary("Delete a note")
        .WithDescription("Deletes a note by its identifier. Returns 204 on success or 404 if not found.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        app.Run();
    }
}