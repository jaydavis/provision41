using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.Sql.Inputs;
using Pulumi.AzureNative.KeyVault;
using Pulumi.AzureNative.KeyVault.Inputs;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static Task<int> Main() => Pulumi.Deployment.RunAsync(() =>
    {
        var env = Pulumi.Deployment.Instance.StackName; // "dev", "uat", "prod"

        var config = new Config();
        var location = config.Get("location") ?? "EastUS";
        var tenantId = config.Require("tenantId");
        var sqlAdminUser = config.Require("sqladminuser");
        var sqlAdminPassword = config.RequireSecret("sqladminpassword");

        // Names
        var rgName = $"provision41-{env}-rg";
        var storageName = $"provision41{env}sa";
        var sqlServerName = $"provision41-{env}-sql";
        var sqlDbName = $"provision41-{env}-db";
        var kvName = $"provision41{env}kv";
        var appPlanName = $"provision41-{env}-appplan";
        var webAppName = $"provision41-{env}-webapp";

        // Resource Group
        var rg = new ResourceGroup($"provision41-{env}-rg", new ResourceGroupArgs
        {
            Location = location
        });

        // Storage Account
        var storage = new StorageAccount($"provision41{env}sa", new StorageAccountArgs
        {
            Name = storageName,
            ResourceGroupName = rg.Name,
            Location = location,
            Sku = new Pulumi.AzureNative.Storage.Inputs.SkuArgs
            {
                Name = Pulumi.AzureNative.Storage.SkuName.Standard_LRS
            },
            Kind = Kind.StorageV2
        });

        var container = new BlobContainer("uploads", new BlobContainerArgs
        {
            AccountName = storage.Name,
            ResourceGroupName = rg.Name,
            PublicAccess = PublicAccess.None
        });

        // SQL Server
        var sqlServer = new Server($"provision41-{env}-sql", new ServerArgs
        {
            ResourceGroupName = rg.Name,
            Location = location,
            AdministratorLogin = sqlAdminUser,
            AdministratorLoginPassword = sqlAdminPassword,
            Version = "12.0"
        });

        var sqlDb = new Database($"provision41-{env}-db", new DatabaseArgs
        {
            Location = location,
            ResourceGroupName = rg.Name,
            ServerName = sqlServer.Name,
            Sku = new Pulumi.AzureNative.Sql.Inputs.SkuArgs
            {
                Name = "S0",
                Tier = "Standard"
            }
        }, new CustomResourceOptions
        {
            DependsOn = { sqlServer } 
        });


        // Key Vault
        var kv = new Vault($"provision41{env}kv", new VaultArgs
        {
            ResourceGroupName = rg.Name,
            Location = location,
            Properties = new VaultPropertiesArgs
            {
                TenantId = tenantId,
                Sku = new Pulumi.AzureNative.KeyVault.Inputs.SkuArgs
                {
                    Name = Pulumi.AzureNative.KeyVault.SkuName.Standard,
                    Family = "A"
                },
                EnabledForDeployment = true,
                EnabledForTemplateDeployment = true,
                EnabledForDiskEncryption = true,
                AccessPolicies = {}
            }
        });

        // SQL Connection String
        var sqlConnectionString = Output.Tuple<string, string, string, string>(
            sqlServer.FullyQualifiedDomainName,
            sqlDb.Name,
            sqlAdminUser,
            sqlAdminPassword
        ).Apply(t =>
        {
            (string fqdn, string dbName, string user, string pwd) = t;
            return $"Server=tcp:{fqdn};Initial Catalog={dbName};User ID={user};Password={pwd};Encrypt=True;";
        });

        // Key Vault Secrets
        var secretUser = new Secret("sqladminuser", new SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Name,
            Properties = new SecretPropertiesArgs
            {
                Value = sqlAdminUser
            }
        });

        var secretPwd = new Secret($"sqladminpassword", new SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Name,
            Properties = new SecretPropertiesArgs
            {
                Value = sqlAdminPassword
            }
        });

        var secretConnStr = new Secret($"sqlconnectionstring", new SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Name,
            Properties = new SecretPropertiesArgs
            {
                Value = sqlConnectionString
            }
        });

        // App Service Plan
        var appServicePlan = new Pulumi.AzureNative.Web.AppServicePlan($"provision41-{env}-plan", new Pulumi.AzureNative.Web.AppServicePlanArgs
        {
            Name = appPlanName,
            ResourceGroupName = rg.Name,
            Location = location,
            Kind = "App",
            Sku = new Pulumi.AzureNative.Web.Inputs.SkuDescriptionArgs
            {
                Name = "B1",
                Tier = "Basic"
            }
        });

        // Web App
        var webApp = new Pulumi.AzureNative.Web.WebApp($"provision41-{env}-webapp", new Pulumi.AzureNative.Web.WebAppArgs
        {
            Name = webAppName,
            ResourceGroupName = rg.Name,
            Location = location,
            ServerFarmId = appServicePlan.Id,
            SiteConfig = new Pulumi.AzureNative.Web.Inputs.SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new Pulumi.AzureNative.Web.Inputs.NameValuePairArgs
                    {
                        Name = "WEBSITE_RUN_FROM_PACKAGE",
                        Value = "1"
                    }
                }
            }
        });

        // Outputs
        return new Dictionary<string, object?>
        {
            ["resourceGroup"] = rg.Name,
            ["sqlServer"] = sqlServer.Name,
            ["sqlDatabase"] = sqlDb.Name,
            ["blobUrl"] = Output.Format($"https://{storage.Name}.blob.core.windows.net/{container.Name}"),
            ["keyVault"] = kv.Name,
            ["webAppUrl"] = Output.Format($"https://{webApp.DefaultHostName}")
        };
    });
}
