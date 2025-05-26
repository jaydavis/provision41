using Microsoft.EntityFrameworkCore;
using provision41.web.Data;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

// ✅ Corrected variable name
var keyVaultName = builder.Configuration["KeyVaultName"];
if (string.IsNullOrEmpty(keyVaultName))
{
    throw new InvalidOperationException("KeyVaultName configuration is required.");
}

// ✅ Add Key Vault secrets to config
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());

builder.Services.AddRazorPages();

// ✅ Now sqlconnectionstring can come from Key Vault
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration["sqlconnectionstring"]));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

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
