using Microsoft.EntityFrameworkCore;
using provision41.web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages
builder.Services.AddRazorPages();

// Add SQL Server EF Core DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Error handling and HTTPS settings
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Optional but recommended
app.UseHttpsRedirection();

// Redirect root domain to www
app.Use(async (context, next) =>
{
    if (context.Request.Host.Host.Equals("provision41.com", StringComparison.OrdinalIgnoreCase))
    {
        var redirectUrl = $"https://www.provision41.com{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(redirectUrl, permanent: true);
        return;
    }

    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
