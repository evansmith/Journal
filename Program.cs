using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<JournalDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllersWithViews().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/journals", async (JournalDb db) =>
    await db.Journals.ToListAsync());

app.MapGet("entries", async (JournalDb db) => 
    await db.Entries.ToListAsync());

app.MapGet("/journals/{id}", async (int id, JournalDb db) =>
    await db.Journals.Include(x => x.Entries).FirstOrDefaultAsync(p => p.Id == id)
        is Journal journal
        ? Results.Ok(new JournalDto{
            Id = journal.Id,
            Name = journal.Name,
            Entries = journal.Entries.Select(x => new EntryDto{
                Id = x.Id,
                Title = x.Title, 
                Content = x.Content,
                JournalId = x.JournalId
            }).ToList()
        }) : Results.NotFound());

    


app.MapGet("/journals/{id}/entries", async (int id, JournalDb db) =>
{
    var bob = await db.Journals.FindAsync(id);
    if (bob is null) return Results.NotFound();
    var entries = await db.Entries.Where(t => t.Journal == bob).Select(x => new EntryDto{
        Id = x.Id,
        Title = x.Title,
        Content = x.Content,
        Journal = x.Journal
    }).ToListAsync();
    return Results.Json(entries);
});

app.MapPost("/journals", async (Journal journal, JournalDb db) =>
{
    db.Journals.Add(journal);
    await db.SaveChangesAsync();

    return Results.Created($"/journals/{journal.Id}", journal);
});


app.MapPost("/journals/{journalId}/entries", async (int journalId, Entry entry, JournalDb db) =>
{
    var journal = await db.Journals.FindAsync(journalId);
    if (journal is null) return Results.NotFound();
    var entryToCreate = new Entry {
        Journal = journal,
        Title = entry.Title,
        Content = entry.Content
    };
    db.Add(entryToCreate);
    await db.SaveChangesAsync();
    entry.Id = entryToCreate.Id;
    entry.JournalId = entryToCreate.JournalId;
    return Results.Created($"/journals/{journalId}/entries/{entry.Id}", entry);
});

app.Run();

class Journal
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public virtual ICollection<Entry>? Entries {get; set;}
}

class Entry
{
    public int Id {get; set; }
    public string? Title {get; set;}
    public string? Content {get; set;}
    public int JournalId {get; set;}
    public virtual Journal Journal {get; set;}

}

class JournalDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public virtual ICollection<EntryDto>? Entries {get; set;}
}

class EntryDto
{
    public int Id {get; set; }
    public string? Title {get; set;}
    public string? Content {get; set;}
    public int JournalId {get; set;}
    public Journal Journal {get; set;}

}

class JournalDb : DbContext
{
    public JournalDb(DbContextOptions<JournalDb> options)
        : base(options) { }

    public DbSet<Journal> Journals => Set<Journal>();
    public DbSet<Entry> Entries => Set<Entry>();

}