using MigrationCommander.Extensions;
using MigrationCommander.Dashboard.Components;
using MigrationCommander.Dashboard.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add MigrationCommander services with in-memory SQLite database
builder.Services.AddMigrationCommander(options =>
{
    // Use in-memory SQLite - no file system or Docker required
    options.InternalDatabasePath = "Data Source=MigrationCommander;Mode=Memory;Cache=Shared";
    options.EnableRealTimeUpdates = true;
});

// Add SignalR for real-time updates
builder.Services.AddSignalR();
builder.Services.AddMigrationCommanderSignalR();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Initialize MigrationCommander database and seed test data
app.UseMigrationCommander(seedTestData: true);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Map SignalR hub
app.MapMigrationCommanderHub();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
