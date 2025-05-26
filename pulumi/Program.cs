using Pulumi;
using Pulumi.AzureNative.Authorization;
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
using System.Linq;

class Program
{
    static Task<int> Main() => Pulumi.Deployment.RunAsync(() =>
    {
        var env = Pulumi.Deployment.Instance.StackName;

        var config = new Config();
        var location = config.Get("location") ?? "EastUS";
        var tenantId = config.Require("tenantId");
        var subscriptionId = config.Require("subscriptionId");
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

        var outboundIps = new[]
        {
            "130.107.160.8","130.107.160.2","130.107.165.174","130.107.167.13","130.107.167.220",
            "130.107.167.229","130.107.164.223","130.107.164.224","130.107.163.158","130.107.163.159",
            "130.107.163.160","130.107.163.161","130.107.164.77","130.107.164.94","130.107.164.95",
            "130.107.164.122","130.107.164.123","130.107.164.144","20.48.204.13"
        };

        var firewallRules = outboundIps.Select((ip, index) =>
            new FirewallRule($"allow-outbound-ip-{index + 1}", new FirewallRuleArgs
            {
                ServerName = sqlServer.Name,
                ResourceGroupName = rg.Name,
                StartIpAddress = ip,
                EndIpAddress = ip
            })).ToList();

        _ = new FirewallRule("allow-local-ip", new FirewallRuleArgs
        {
            ServerName = sqlServer.Name,
            ResourceGroupName = rg.Name,
            StartIpAddress = "65.29.89.193",
            EndIpAddress = "65.29.89.193"
        });

        var sqlDb = new Database(sqlDbName, new DatabaseArgs
        {
            DatabaseName = sqlDbName,
            Location = location,
            ResourceGroupName = rg.Name,
            ServerName = sqlServer.Name,
            Sku = new SqlInputs.SkuArgs { Name = "S0", Tier = "Standard" }
        }, new CustomResourceOptions { DependsOn = { sqlServer } });

        var sqlConnectionString = Output.Tuple<string, string, string, string>(
            sqlServer.FullyQualifiedDomainName,
            sqlDb.Name,
            sqlAdminUser,
            sqlAdminPassword
        ).Apply(t =>
            $"Server=tcp:{t.Item1};Initial Catalog={t.Item2};User ID={t.Item3};Password={t.Item4};Encrypt=True;"
        );

        var appServicePlan = new AppServicePlan(appPlanName, new AppServicePlanArgs
        {
            Name = appPlanName,
            ResourceGroupName = rg.Name,
            Location = "CanadaCentral",
            Kind = "app",
            Sku = new WebInputs.SkuDescriptionArgs { Name = "B1", Tier = "Basic", Size = "B1", Family = "B", Capacity = 1 }
        });

        var webApp = new WebApp(webAppName, new WebAppArgs
        {
            Name = webAppName,
            ResourceGroupName = rg.Name,
            Location = "CanadaCentral",
            ServerFarmId = appServicePlan.Id,
            Kind = "app",
            HttpsOnly = true,
            Identity = new Pulumi.AzureNative.Web.Inputs.ManagedServiceIdentityArgs
            {
                Type = Pulumi.AzureNative.Web.ManagedServiceIdentityType.SystemAssigned
            },
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

        var webAppIdentity = webApp.Identity.Apply(i => i?.PrincipalId);

        var kv = webAppIdentity.Apply(principalId => new Vault(kvName, new VaultArgs
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
                    },
                    new KvInputs.AccessPolicyEntryArgs
                    {
                        TenantId = tenantId,
                        ObjectId = principalId!,
                        Permissions = new KvInputs.PermissionsArgs
                        {
                            Secrets = { "get", "list" }
                        }
                    }
                }
            }
        }));

        _ = webAppIdentity.Apply(principalId => new RoleAssignment("kv-access-role", new RoleAssignmentArgs
        {
            PrincipalId = principalId!,
            PrincipalType = "ServicePrincipal",
            RoleDefinitionId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/4633458b-17de-408a-b874-0445c86b69e6",
            Scope = kv.Apply(v => v.Id)
        }));

        _ = new Secret("sqladminuser", new Pulumi.AzureNative.KeyVault.SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Apply(v => v.Name),
            Properties = new KvInputs.SecretPropertiesArgs { Value = sqlAdminUser }
        });

        _ = new Secret("sqladminpassword", new Pulumi.AzureNative.KeyVault.SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Apply(v => v.Name),
            Properties = new KvInputs.SecretPropertiesArgs { Value = sqlAdminPassword }
        });

        _ = new Secret("sqlconnectionstring", new Pulumi.AzureNative.KeyVault.SecretArgs
        {
            ResourceGroupName = rg.Name,
            VaultName = kv.Apply(v => v.Name),
            Properties = new KvInputs.SecretPropertiesArgs { Value = sqlConnectionString }
        });

        _ = new WebAppApplicationSettings("provision41-app-settings", new WebAppApplicationSettingsArgs
        {
            Name = webApp.Name,
            ResourceGroupName = rg.Name,
            Properties =
            {
                { "KeyVaultName", kv.Apply(v => v.Name) }
            }
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
            ["keyVault"] = kv.Apply(v => v.Name),
            ["webAppUrl"] = Output.Format($"https://{webApp.DefaultHostName}")
        };
    });
}