using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Sql;
using SqlInputs = Pulumi.AzureNative.Sql.Inputs;
using Pulumi.AzureNative.KeyVault;
using KvInputs = Pulumi.AzureNative.KeyVault.Inputs;
using Pulumi.AzureNative.Web;
using WebInputs = Pulumi.AzureNative.Web.Inputs;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static Task<int> Main() => Pulumi.Deployment.RunAsync(() =>
    {
        var env = Pulumi.Deployment.Instance.StackName;

        var config = new Config();
        var location = config.Get("location") ?? "EastUS";
        var tenantId = config.Require("tenantId");
        var sqlAdminUser = config.Require("sqladminuser");
        var sqlAdminPassword = config.RequireSecret("sqladminpassword");

        var rgName = $"provision41-{env}-rg";
        var storageName = $"provision41{env}sa";
        var sqlServerName = $"provision41-{env}-sql";
        var sqlDbName = $"provision41-{env}-db";
        var kvName = $"provision41-{env}-kv";
        var appPlanName = $"provision41-{env}-sp";
        var webAppName = $"provision41-{env}-webapp";

        var rg = new ResourceGroup(rgName, new ResourceGroupArgs
        {
            ResourceGroupName = rgName,
            Location = location
        });

        var storage = new StorageAccount(storageName, new StorageAccountArgs
        {
            AccountName = storageName,
            ResourceGroupName = rg.Name,
            Location = location,
            Sku = new SkuArgs { Name = Pulumi.AzureNative.Storage.SkuName.Standard_LRS },
            Kind = Kind.StorageV2
        });

        var container = new BlobContainer("uploads", new BlobContainerArgs
        {
            AccountName = storage.Name,
            ResourceGroupName = rg.Name,
            PublicAccess = PublicAccess.None
        });

        var sqlServer = new Server(sqlServerName, new ServerArgs
        {
            ServerName = sqlServerName,
            ResourceGroupName = rg.Name,
            Location = location,
            AdministratorLogin = sqlAdminUser,
            AdministratorLoginPassword = sqlAdminPassword,
            Version = "12.0"
        });

        var sqlDb = new Database(sqlDbName, new DatabaseArgs
        {
            DatabaseName = sqlDbName,
            Location = location,
            ResourceGroupName = rg.Name,
            ServerName = sqlServer.Name,
            Sku = new SqlInputs.SkuArgs { Name = "S0", Tier = "Standard" }
        }, new CustomResourceOptions { DependsOn = { sqlServer } });

        var kv = new Vault(kvName, new VaultArgs
        {
            VaultName = kvName,
            ResourceGroupName = rg.Name,
            Location = location,
            Properties = new KvInputs.VaultPropertiesArgs
            {
                TenantId = tenantId,
                Sku = new KvInputs.SkuArgs { Name = Pulumi.AzureNative.KeyVault.SkuName.Standard, Family = "A" },
                EnabledForDeployment = true,
                EnabledForTemplateDeployment = true,
                EnabledForDiskEncryption = true,
                AccessPolicies =
                {
                    new KvInputs.AccessPolicyEntryArgs
                    {
                        TenantId = tenantId,
                        ObjectId = config.Require("currentUserObjectId"),
                        Permissions = new KvInputs.PermissionsArgs
                        {
                            Keys = { "get", "list", "create", "delete", "update", "import" },
                            Secrets = { "get", "list", "set", "delete", "purge" },
                            Certificates = { "get", "list", "create", "delete", "update" }
                        }
                    }
                }
            }
        });

        var sqlConnectionString = Output.Tuple<string, string, string, string>(sqlServer.FullyQualifiedDomainName, sqlDb.Name, sqlAdminUser, sqlAdminPassword)
            .Apply(t => $"Server=tcp:{t.Item1};Initial Catalog={t.Item2};User ID={t.Item3};Password={t.Item4};Encrypt=True;");

        _ = new Secret("sqladminuser", new Pulumi.AzureNative.KeyVault.SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Name,
            Properties = new KvInputs.SecretPropertiesArgs { Value = sqlAdminUser }
        });

        _ = new Secret("sqladminpassword", new Pulumi.AzureNative.KeyVault.SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Name,
            Properties = new KvInputs.SecretPropertiesArgs { Value = sqlAdminPassword }
        });

        _ = new Secret("sqlconnectionstring", new Pulumi.AzureNative.KeyVault.SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Name,
            Properties = new KvInputs.SecretPropertiesArgs { Value = sqlConnectionString }
        });

        var appServicePlan = new AppServicePlan(appPlanName, new AppServicePlanArgs
        {
            ResourceGroupName = rg.Name,
            Location = "CanadaCentral",
            Name = appPlanName,
            Kind = "app",
            Sku = new WebInputs.SkuDescriptionArgs { Name = "B1", Tier = "Basic", Size = "B1", Family = "B", Capacity = 1 }
        });

        var webApp = new WebApp(webAppName, new WebAppArgs
        {
            ResourceGroupName = rg.Name,
            Location = "CanadaCentral",
            Name = webAppName,
            ServerFarmId = appServicePlan.Id,
            Kind = "app",
            HttpsOnly = true,
            SiteConfig = new WebInputs.SiteConfigArgs
            {
                AlwaysOn = false,
                NetFrameworkVersion = "v8.0",
                FtpsState = FtpsState.FtpsOnly,
                Use32BitWorkerProcess = true,
                MinTlsVersion = "1.2",
                NumberOfWorkers = 1
            },
            StorageAccountRequired = false,
            HostNamesDisabled = false,
            RedundancyMode = RedundancyMode.None,
            PublicNetworkAccess = "Enabled"
        });

        var primaryDomain = env == "prod" ? "provision41.com" : $"dev.provision41.com";

        var managedCert = new Certificate("managed-cert", new CertificateArgs
        {
            ResourceGroupName = rg.Name,
            Location = "CanadaCentral",
            ServerFarmId = appServicePlan.Id,
            HostNames = { primaryDomain },
            CanonicalName = primaryDomain
        }, new CustomResourceOptions { DependsOn = { rg, webApp } });

        _ = new WebAppHostNameBinding($"provision41-{env}-domain", new WebAppHostNameBindingArgs
        {
            Name = webApp.Name,
            ResourceGroupName = rg.Name,
            SiteName = webApp.Name,
            HostName = primaryDomain,
            CustomHostNameDnsRecordType = CustomHostNameDnsRecordType.CName,
            SslState = SslState.SniEnabled,
            Thumbprint = managedCert.Thumbprint
        });

        if (env == "prod")
        {
            var wwwCert = new Certificate("managed-cert-www", new CertificateArgs
            {
                ResourceGroupName = rg.Name,
                Location = "CanadaCentral",
                ServerFarmId = appServicePlan.Id,
                HostNames = { "www.provision41.com" },
                CanonicalName = "www.provision41.com"
            }, new CustomResourceOptions { DependsOn = { rg, webApp } });

            _ = new WebAppHostNameBinding("provision41-www-domain", new WebAppHostNameBindingArgs
            {
                Name = webApp.Name,
                ResourceGroupName = rg.Name,
                SiteName = webApp.Name,
                HostName = "www.provision41.com",
                CustomHostNameDnsRecordType = CustomHostNameDnsRecordType.CName,
                SslState = SslState.SniEnabled,
                Thumbprint = wwwCert.Thumbprint
            });
        }

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
