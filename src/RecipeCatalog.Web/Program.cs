using RecipeCatalog.Core.Data;
using RecipeCatalog.Core.Services;
using RecipeCatalog.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var dataFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataFolder);
var connectionString = builder.Configuration.GetConnectionString("Recipes")
                       ?? $"Data Source={Path.Combine(dataFolder, "recipes.db")}";

builder.Services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(connectionString));
builder.Services.AddSingleton<IRecipeRepository, SqliteRecipeRepository>();
builder.Services.AddSingleton<RecipeService>();
builder.Services.AddSingleton<ImageStorage>();
builder.Services.AddRazorPages().AddMvcOptions(options =>
{
    // Валидацията е в RecipeValidator (Core), за да не се дублират правилата (DRY).
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    var messages = options.ModelBindingMessageProvider;
    messages.SetAttemptedValueIsInvalidAccessor((_, _) => "Въведете валидно число.");
    messages.SetValueMustBeANumberAccessor(_ => "Въведете валидно число.");
    messages.SetValueMustNotBeNullAccessor(_ => "Полето е задължително.");
});

var app = builder.Build();

InitializeDatabase(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/NotFound");
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();

static void InitializeDatabase(IServiceProvider services)
{
    var factory = services.GetRequiredService<ISqliteConnectionFactory>();
    DatabaseInitializer.EnsureCreated(factory);
    SeedData.SeedIfEmpty(services.GetRequiredService<IRecipeRepository>());
}

/// <summary>Нужен за интеграционни тестове с WebApplicationFactory.</summary>
public partial class Program;
