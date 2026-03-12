using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransactionsIngest.Data;
using TransactionsIngest.Services;

var configuration = 
    new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json",
                    optional: false).Build();

var services = new ServiceCollection();

//Register database

services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

//Register services
var useMockFeed = configuration.GetValue<bool>("AppSettings:UseMockFeed");

if (useMockFeed)
    services.AddScoped<ITransactionService, MockTransactionService>();

services.AddScoped<IngestionService>();

var serviceProvider = services.BuildServiceProvider();

//Run database migrations
using (var scope = serviceProvider.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Run ingestion
using (var scope = serviceProvider.CreateScope())
{
    var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
    await ingestionService.RunAsync();
}