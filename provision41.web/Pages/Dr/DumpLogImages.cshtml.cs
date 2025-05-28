using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Storage.Blobs;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Sas;

namespace Provision41Web.Pages.Dr;

public class DumplogImagesModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public List<string> ImageUrls { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id <= 0)
            return BadRequest("Missing or invalid log ID.");

        try
        {
            var kvName = Environment.GetEnvironmentVariable("KeyVaultName");
            if (string.IsNullOrEmpty(kvName))
                throw new InvalidOperationException("KeyVaultName environment variable is not set.");

            var secretClient = new SecretClient(new Uri($"https://{kvName}.vault.azure.net/"), new DefaultAzureCredential());

            var accountName = (await secretClient.GetSecretAsync("blobstorageaccountname")).Value.Value;
            var accountKey = (await secretClient.GetSecretAsync("blobstorageaccountkey")).Value.Value;

            var credential = new StorageSharedKeyCredential(accountName, accountKey);
            var blobServiceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), credential);
            var containerClient = blobServiceClient.GetBlobContainerClient("uploads");

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: $"log-{Id}/"))
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerClient.Name,
                    BlobName = blobItem.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasToken = sasBuilder.ToSasQueryParameters(credential).ToString();
                var uri = $"https://{accountName}.blob.core.windows.net/{containerClient.Name}/{blobItem.Name}?{sasToken}";

                ImageUrls.Add(uri);
            }

            return Page();
        }
        catch (Exception ex)
        {
            Console.WriteLine("🔥 Error generating SAS URLs for blob images:");
            Console.WriteLine(ex.ToString());
            return StatusCode(500, "Error retrieving image URLs.");
        }
    }
}
