using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.Sql.Inputs;
using Pulumi.AzureNative.KeyVault;
using Pulumi.AzureNative.KeyVault.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;

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
        var appPlanName = $"provision41-{env}-sp";
        var webAppName = $"provision41-{env}-webapp";

        // Resource Group
        var rg = new ResourceGroup($"provision41-{env}-rg", new ResourceGroupArgs
        {
            ResourceGroupName = rgName,
            Location = location
        });

        // Storage Account
        var storage = new StorageAccount($"provision41{env}sa", new StorageAccountArgs
        {
            AccountName = storageName,
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
            ServerName = sqlServerName,
            ResourceGroupName = rg.Name,
            Location = location,
            AdministratorLogin = sqlAdminUser,
            AdministratorLoginPassword = sqlAdminPassword,
            Version = "12.0"
        });

        var sqlDb = new Database($"provision41-{env}-db", new DatabaseArgs
        {
            DatabaseName = sqlDbName,
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
            VaultName = kvName,
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
        var secretUser = new Secret("sqladminuser", new Pulumi.AzureNative.KeyVault.SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Name,
            Properties = new SecretPropertiesArgs
            {
                Value = sqlAdminUser
            }
        });

        var secretPwd = new Secret($"sqladminpassword", new Pulumi.AzureNative.KeyVault.SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Name,
            Properties = new SecretPropertiesArgs
            {
                Value = sqlAdminPassword
            }
        });

        var secretConnStr = new Secret($"sqlconnectionstring", new Pulumi.AzureNative.KeyVault.SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Name,
            Properties = new SecretPropertiesArgs
            {
                Value = sqlConnectionString
            }
        });

        // App Service Plan
        var appServicePlan = new AppServicePlan("existing-service-plan", new AppServicePlanArgs
        {
            ResourceGroupName = rg.Name,
            Location = "Canada Central",
            Name = "provision41-dev-sp",
            Kind = "app",
            Sku = new SkuDescriptionArgs
            {
                Name = "F1",
                Tier = "Free",
                Size = "F1",
                Family = "F",
                Capacity = 0
            },
            IsSpot = false,
            ElasticScaleEnabled = false,
            MaximumElasticWorkerCount = null,
            TargetWorkerCount = 0,
            TargetWorkerSizeId = 0
        }, new CustomResourceOptions
        {
            ImportId = "/subscriptions/2f35adf4-bda0-40da-ab3f-c8aa12a2d1f8/resourceGroups/provision41-dev-rg/providers/Microsoft.Web/serverfarms/provision41-dev-sp"
        });

        // Web App
        var webApp = new WebApp("provision41-dev-webapp", new WebAppArgs
        {
            ResourceGroupName = rg.Name,
            Location = "Canada Central",
            Name = "provision41-dev-webapp",
            ServerFarmId = appServicePlan.Id,
            Kind = "app",
            HttpsOnly = true,
            ClientAffinityEnabled = true,
            ClientCertEnabled = false,
            ClientCertMode = ClientCertMode.Required,
            SiteConfig = new SiteConfigArgs
            {
                AlwaysOn = false,
                NetFrameworkVersion = "v8.0",
                FtpsState = FtpsState.FtpsOnly,
                Use32BitWorkerProcess = true,
                MinTlsVersion = "1.2",
                ScmType = "None",
                NumberOfWorkers = 1,
                WebSocketsEnabled = false,
                Http20Enabled = false,
                HttpLoggingEnabled = false,
                DetailedErrorLoggingEnabled = false,
                RequestTracingEnabled = false,
                RemoteDebuggingEnabled = false,
                VirtualApplications = 
                {
                    new VirtualApplicationArgs
                    {
                        VirtualPath = "/",
                        PhysicalPath = "site\\wwwroot",
                        PreloadEnabled = false
                    }
                }
            },
            KeyVaultReferenceIdentity = "SystemAssigned",
            StorageAccountRequired = false,
            HostNamesDisabled = false,
            RedundancyMode = RedundancyMode.None,
            PublicNetworkAccess = "Enabled"
        }, new CustomResourceOptions
        {
            ImportId = "/subscriptions/2f35adf4-bda0-40da-ab3f-c8aa12a2d1f8/resourceGroups/provision41-dev-rg/providers/Microsoft.Web/sites/provision41-dev-webapp"
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
