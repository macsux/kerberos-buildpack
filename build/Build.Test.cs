using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using Nuke.Common;
using static Nuke.Common.Tools.CloudFoundry.CloudFoundryTasks;
using Newtonsoft.Json.Linq;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.CloudFoundry;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("Name of the installed buildpack that is used for integration tests")]
    readonly string TestBuildpackName = "kerberos-buildpack-test";
    [Parameter("Cloud foundry username")]
    readonly string CfUsername;
    [Parameter("Cloud Foundry Password")]
    readonly string CfPassword;
    [Parameter("Cloud Foundry Endpoint")]
    readonly string CfApiEndpoint;
    [Parameter("Cloud foundry org in which to deploy integration tests")]
    readonly string CfOrg;
    [Parameter("Cloud foundry space in which to deploy integration tests")]
    readonly string CfSpace = "KerberosIntegrationTests";
    [Parameter("KDC server used in integration tests")]
    readonly string IntegrationTestKerbKdc;
    [Parameter("User principal used in integration tests")]
    readonly string IntegrationTestKerbUser;
    [Parameter("Kerberos password used in integration tests")]
    readonly string IntegrationTestKerbPassword;
    [Parameter("SQL server connection string used in integration tests")]
    readonly string IntegrationTestSqlConnectionString;
    [Parameter("User current cf login")]
    readonly bool UseCurrentCfLogin;
    [Parameter("User current cf target (org/space)")]
    readonly bool UseCurrentCfTarget;
    readonly string TestAppName = "KerberosDemo";

    string SampleAppUrl = "http://localhost:8080";

    Target Env => _ => _
        .Executes(() =>
        {
            foreach (var entry in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>())
            {
                Serilog.Log.Information($"{entry.Key}:{entry.Value}");
            }
        });
    Target CfLogin => _ => _
        .Requires(() => CfUsername, () => CfPassword, () => CfApiEndpoint )
        .OnlyWhenStatic(() => !UseCurrentCfLogin)
        .After(Publish, PublishSample)
        .Unlisted()
        .Executes(() =>
        {
            CloudFoundryApi(c => c.SetUrl(CfApiEndpoint));
            CloudFoundryAuth(c => c
                .SetUsername(CfUsername)
                .SetPassword(CfPassword));
        });
    Target SetCfTargetSpace => _ => _
        .OnlyWhenStatic(() => !UseCurrentCfTarget)
        .After(CfLogin, Publish, PublishSample)
        .Executes(() =>
        {
            CloudFoundryTarget(c => c
                .SetOrg(CfOrg));
            CloudFoundryCreateSpace(c => c
                .SetSpace(CfSpace));
            CloudFoundryTarget(c => c
                .SetOrg(CfOrg)
                .SetSpace(CfSpace));
        });

    Target EnsureCfTarget => _ => _
        .After(Publish)
        .DependsOn(CfLogin, SetCfTargetSpace);
    Target InstallBuildpack => _ => _
        .DependsOn(Publish, EnsureCfTarget)
        .Executes(() =>
        {
            var isExisting = CloudFoundryCurl(o => o
                    .SetProcessLogOutput(false)
                    .SetPath("/v3/buildpacks"))
                .StdToJson()
                .SelectTokens("$..name")
                .Select(x => x.Value<string>())
                .Any(buildpackName => buildpackName == TestBuildpackName);
            if (isExisting)
            {
                CloudFoundryUpdateBuildpack(c => c
                    .SetBuildpackName(TestBuildpackName)
                    .SetPath(ArtifactsDirectory / PackageZipName));
            }
            else
            {
                CloudFoundryCreateBuildpack(c => c
                    .SetBuildpackName(TestBuildpackName)
                    .SetPath(ArtifactsDirectory / PackageZipName)
                    .SetPosition(1000));
            }
        });


    Target DeploySampleApp => _ => _
        .DependsOn(SetCfTargetSpace, EnsureCfTarget)
        .After(PublishSample, InstallBuildpack)
        .Requires(
            () => IntegrationTestKerbKdc, 
            () => IntegrationTestKerbUser, 
            () => IntegrationTestKerbPassword, 
            () => IntegrationTestSqlConnectionString)
        .Executes(() =>
        {
            CloudFoundryCreateApp(c => c
                .SetName(TestAppName));
            CloudFoundrySetEnv(c => c
                .SetAppName(TestAppName)
                .CombineWith(new Dictionary<string,string>()
                {
                    {"KRB_KDC", IntegrationTestKerbKdc},
                    {"KRB_SERVICE_ACCOUNT", IntegrationTestKerbUser},
                    {"KRB_PASSWORD", IntegrationTestKerbPassword},
                    {"ConnectionStrings__SqlServer", IntegrationTestSqlConnectionString},
                }, (oo, env) =>
                {
                    
                    return oo
                        .SetEnvVarName(env.Key)
                        .SetEnvVarValue(env.Value);
                }));
            CloudFoundryPush(c => c
                .SetAppName(TestAppName)
                .SetPath(ArtifactsDirectory / SampleZipName)
                .SetBuildpack(TestBuildpackName, "dotnet_core_buildpack")
                .SetHealthCheckType(HealthCheckType.Port)
                .SetMemory("512M")
                .EnableRandomRoute());
        });
    Target DetermineSampleAppUrl => _ => _
        .DependsOn(EnsureCfTarget)
        .Before(Test)
        .After(DeploySampleApp)
        .Executes(() =>
        {
            
            var appId = CloudFoundry($"app {TestAppName} --guid").First().Text;
            var routesJson = CloudFoundryCurl(c => c
                .SetPath($"/v3/apps/{appId}/routes")
                .DisableProcessLogOutput())
                .StdToJson();
            var appUrl = routesJson.SelectToken("$..url").Value<string>();
            SampleAppUrl = $"https://{appUrl}";
        });

    Target Test => _ => _
        .After(Publish)
        .After(InstallBuildpack)
        .After(DeploySampleApp)
        .After(DockerComposeUp)
        .Before(DockerComposeDown)
        .Executes(() =>
        {
            DotNetTest(c => c
                .SetProjectFile(Solution.GetProject("KerberosBuildpack.Tests").Path)
                .AddProcessEnvironmentVariable("SampleAppUrl", SampleAppUrl)
            );
        });

    Target DockerComposeUp => _ => _
        .Unlisted()
        .Executes(() =>
        {
            using var process = ProcessTasks.StartProcess("docker-compose", "up -d", RootDirectory / "sample" / "KerberosDemo");
            process.AssertZeroExitCode();
        });

    Target DockerComposeDown => _ => _
        .Unlisted()
        .Executes(() =>
        {
            using var process = ProcessTasks.StartProcess("docker-compose", "down -v", RootDirectory / "sample" / "KerberosDemo");
            process.AssertZeroExitCode();
        });

    Target IntegrationTestCf => _ => _
        .DependsOn(Publish, InstallBuildpack, DeploySampleApp, DetermineSampleAppUrl, Test);

    Target IntegrationTestDocker => _ => _
        .DependsOn(DockerComposeUp, Test, DockerComposeDown);


}