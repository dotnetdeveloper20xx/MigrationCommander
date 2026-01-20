using MigrationCommander.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add MigrationCommander services
builder.Services.AddMigrationCommander(options =>
{
    options.InternalDatabasePath = builder.Configuration.GetConnectionString("MigrationCommander")
        ?? "Data Source=migration_commander.db";
    options.RequireApprovalForProduction = true;
    options.AutoBackupBeforeDestructive = true;
    options.EnableRealTimeUpdates = true;
});

// Add Razor components (for Blazor integration)
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Initialize MigrationCommander (creates database if needed)
app.UseMigrationCommander();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Map MigrationCommander endpoints (SignalR hub, etc.)
app.MapMigrationCommander();

app.Run();
