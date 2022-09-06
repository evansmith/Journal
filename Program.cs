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
     Console.WriteLine($"this is the journal: {journal.Id}");
    var entryToCreate = new Entry {
        Journal = journal,
        Title = entry.Title,
        Content = entry.Content
    };
    Console.WriteLine($"this is the journal: {journal}");
    Console.WriteLine($"entry to create journal id {entryToCreate.Journal.Id}");
    db.Add(entryToCreate);
    await db.SaveChangesAsync();
    entry.Id = entryToCreate.Id;
    entry.JournalId = entryToCreate.JournalId;
    Console.WriteLine($"entry id {entry.Id}");
     //Console.WriteLine($"entry to create journal id {entry.JournalId}");
    //Console.WriteLine($"entry journal id {entry.Journal.Id}");
    return Results.Created($"/journals/{journalId}/entries/{entry.Id}", entry);
});

app.Run();

class Journal
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<Entry>? Entries {get; set;}
}

class Entry
{
    public int Id {get; set; }
    public string? Title {get; set;}
    public string? Content {get; set;}
    public int JournalId {get; set;}
    public Journal Journal {get; set;}

}

class JournalDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<EntryDto>? Entries {get; set;}
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