using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using NuGet.Configuration;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using Octokit;
using LibGit2Sharp;
using Nuke.Common.Utilities;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using FileMode = System.IO.FileMode;
using ZipFile = System.IO.Compression.ZipFile;
using Credentials = Octokit.Credentials;
using Project = Octokit.Project;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

[assembly: InternalsVisibleTo("KerberosBuildpackTests")]
[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    [Flags]
    public enum StackType
    {
        Windows = 1,
        Linux = 2
    }
    public static int Main () => Execute<Build>(x => x.Publish);
    const string BuildpackProjectName = "KerberosBuildpack";
    [GitRepositoryExt] LibGit2Sharp.Repository GitRepository;
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = "Debug";
    readonly string Runtime = "linux-x64";
    readonly string Framework = "net6.0";
    
    [Parameter("GitHub personal access token with access to the repo")]
    string GitHubToken;

    [Parameter("Application directory against which buildpack will be applied")]
    readonly string ApplicationDirectory;
    
    [Solution] readonly Solution Solution;
    string PackageZipName => $"{BuildpackProjectName}-{Runtime}-{ReleaseName}.zip";
    string SampleZipName => $"sampleapp-{Runtime}-{ReleaseName}.zip";
    [NerdbankGitVersioning(UpdateBuildNumber = true)] readonly NerdbankGitVersioning GitVersion;
    public string ReleaseName => IsCurrentBranchCommitted() ? $"v{GitVersion.NuGetPackageVersion}" : "WIP";
    
    public AbsolutePath GetPublishDirectory(Nuke.Common.ProjectModel.Project project) => project.Directory / "bin" / Configuration / Framework / Runtime / "publish";
    public bool IsGitHubRepository 
        => GitRepository.Network.Remotes
            .Where(x => x.Name == "origin")
            .Select(x => x.Url.Contains("github.com"))
            .FirstOrDefault();
    bool IsCurrentBranchCommitted() => !GitRepository.RetrieveStatus().IsDirty;


    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    
    string[] LifecycleHooks = {"detect", "supply", "release", "finalize"};

    Target Clean => _ => _
        .Description("Cleans up **/bin and **/obj folders")
        .Executes(() =>
        {
            (RootDirectory / "sample").GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
        });
    
    Target Publish => _ => _
        .Description("Packages buildpack in Cloud Foundry expected format into /artifacts directory")
        .After(Clean)
        .DependsOn(PublishSample, Restore)
        .Executes(() =>
        {
            var workDirectory = TemporaryDirectory / "pack";
            EnsureCleanDirectory(TemporaryDirectory);
            var buildpackProject = Solution.GetProject(BuildpackProjectName) ?? throw new Exception($"Unable to find project called {BuildpackProjectName} in solution {Solution.Name}");
            var sidecarProject = Solution.GetProject("KerberosSidecar") ?? throw new Exception($"Unable to find project called KerberosSidecar in solution {Solution.Name}");
            var buildpackPublishDirectory = GetPublishDirectory(buildpackProject);
            var sidecarPublishDirectory = GetPublishDirectory(sidecarProject);
            var workBinDirectory = workDirectory / "bin";
            var workDeps = workDirectory / "deps";


            DotNetPublish(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetFramework(Framework)
                .SetRuntime(Runtime)
                .EnableSelfContained()
                .EnableNoRestore()
                .SetAssemblyVersion(GitVersion.AssemblyVersion)
                .SetFileVersion(GitVersion.AssemblyFileVersion)
                .SetInformationalVersion(GitVersion.AssemblyInformationalVersion)
            );
            
            var lifecycleBinaries = Solution.GetProjects("Lifecycle*")
                .Select(x => x.Directory / "bin" / Configuration / Framework / Runtime / "publish")
                .SelectMany(x => Directory.GetFiles(x).Where(path => LifecycleHooks.Any(hook => Path.GetFileName(path).StartsWith(hook))));

            foreach (var lifecycleBinary in lifecycleBinaries)
            {
                CopyFileToDirectory(lifecycleBinary, workBinDirectory, FileExistsPolicy.OverwriteIfNewer);
            }

            CopyDirectoryRecursively(buildpackPublishDirectory, workBinDirectory, DirectoryExistsPolicy.Merge);
            
            CopyDirectoryRecursively(sidecarPublishDirectory, workDeps / "sidecar" , DirectoryExistsPolicy.Merge);

            
            var tempZipFile = TemporaryDirectory / PackageZipName;

            ZipFile.CreateFromDirectory(workDirectory, tempZipFile, CompressionLevel.NoCompression, false);
            MakeFilesInZipUnixExecutable(tempZipFile);
            CopyFileToDirectory(tempZipFile, ArtifactsDirectory, FileExistsPolicy.Overwrite);
            Serilog.Log.Information(ArtifactsDirectory / PackageZipName);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(c => c
                .SetProjectFile(Solution));
        });

    Target PublishSample => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            EnsureExistingDirectory(ArtifactsDirectory);
            var demoProjectDirectory = RootDirectory / "sample" / "KerberosDemo";
            DotNetPublish(c => c
                .SetProject(demoProjectDirectory / "KerberosDemo.csproj")
                .SetConfiguration("DEBUG"));
            var publishFolder = demoProjectDirectory / "bin" / "Debug" / "net6.0" / "publish";
            var manifestFile = publishFolder / "manifest.yml";
            var manifest = File.ReadAllText(manifestFile);
            manifest = manifest.ReplaceRegex(@"\r?\n\s*path:.+", match => match.Result(""));
            File.WriteAllText(manifestFile, manifest);
            var artifactZip = ArtifactsDirectory / $"sampleapp-{Runtime}-{ReleaseName}.zip";
            DeleteFile(artifactZip);
            ZipFile.CreateFromDirectory(publishFolder, artifactZip, CompressionLevel.NoCompression, false);
        });
    
    Target Release => _ => _
        .Description("Creates a GitHub release (or amends existing) and uploads buildpack artifact")
        .DependsOn(Publish)
        .Requires(() => GitHubToken)
        .Requires(() => IsGitHubRepository)
        .Executes(async () =>
        {
            var client = new GitHubClient(new ProductHeaderValue(BuildpackProjectName))
            {
                Credentials = new Credentials(GitHubToken, AuthenticationType.Bearer)
            };
            var pushUrl = new Uri(GitRepository.Network.Remotes.Where(x => x.Name == "origin").Select(x => x.Url.TrimEnd(".git")).First());
            
            var owner = pushUrl.Segments[1].Trim('/');
            var repoName = pushUrl.Segments[2].Trim('/');

            
            Release release;
            try
            {
                release = await client.Repository.Release.Get(owner, repoName, ReleaseName);
            }
            catch (Octokit.NotFoundException)
            {
                var newRelease = new NewRelease(ReleaseName)
                {
                    Name = ReleaseName,
                    Draft = false,
                    Prerelease = false
                };
                release = await client.Repository.Release.Create(owner, repoName, newRelease);
            }

            var artifactsToRelease = new[] { PackageZipName, SampleZipName }.ToHashSet();

            foreach (var existingAsset in release.Assets.Where(x => artifactsToRelease.Contains(x.Name)))
            {
                await client.Repository.Release.DeleteAsset(owner, repoName, existingAsset.Id);
            }

            var downloadLinks = new List<string>();
            foreach (var artifact in artifactsToRelease)
            {
                var zipPackageLocation = ArtifactsDirectory / artifact;
                var stream = File.OpenRead(zipPackageLocation);
                var releaseAssetUpload = new ReleaseAssetUpload(artifact, "application/zip", stream, TimeSpan.FromHours(1));
                var releaseAsset = await client.Repository.Release.UploadAsset(release, releaseAssetUpload);

                downloadLinks.Add(releaseAsset.BrowserDownloadUrl);
            }

            Serilog.Log.Information(string.Join("\n", downloadLinks));
        });
    
    Target Detect => _ => _
        .Description("Invokes buildpack 'detect' lifecycle event")
        .Requires(() => ApplicationDirectory)
        .Executes(() =>
        {
            try
            {
                DotNetRun(s => s
                    .SetProjectFile(Solution.GetProject("Lifecycle.Detect").Path)
                    .SetApplicationArguments(ApplicationDirectory)
                    .SetConfiguration(Configuration)
                    .SetFramework("netcoreapp3.1"));
                Serilog.Log.Information("Detect returned 'true'");
            }
            catch (ProcessException)
            {
                Serilog.Log.Information("Detect returned 'false'");
            }
        });

    Target Supply => _ => _
        .Description("Invokes buildpack 'supply' lifecycle event")
        .Requires(() => ApplicationDirectory)
        .Executes(() =>
        {
            var home = (AbsolutePath)Path.GetTempPath() / Guid.NewGuid().ToString();
            var app = home / "app";
            var deps = home / "deps";
            var index = 0;
            var cache = home / "cache";
            CopyDirectoryRecursively(ApplicationDirectory, app);

            DotNetRun(s => s
                .SetProjectFile(Solution.GetProject("Lifecycle.Supply").Path)
                .SetApplicationArguments($"{app} {cache} {app} {deps} {index}")
                .SetConfiguration(Configuration)
                .SetFramework("netcoreapp3.1"));
            Serilog.Log.Information($"Buildpack applied. Droplet is available in {home}");

        });

    public void MakeFilesInZipUnixExecutable(AbsolutePath zipFile)
    {
        var tmpFileName = zipFile + ".tmp";
        using (var input = new ZipInputStream(File.Open(zipFile, FileMode.Open)))
        using (var output = new ZipOutputStream(File.Open(tmpFileName, FileMode.Create)))
        {
            output.SetLevel(9);
            ZipEntry entry;
		
            while ((entry = input.GetNextEntry()) != null)
            {
                var outEntry = new ZipEntry(entry.Name) {HostSystem = (int) HostSystemID.Unix};
                var entryAttributes =  
                    ZipEntryAttributes.ReadOwner | 
                    ZipEntryAttributes.ReadOther | 
                    ZipEntryAttributes.ReadGroup |
                    ZipEntryAttributes.ExecuteOwner | 
                    ZipEntryAttributes.ExecuteOther | 
                    ZipEntryAttributes.ExecuteGroup;
                entryAttributes = entryAttributes | (entry.IsDirectory ? ZipEntryAttributes.Directory : ZipEntryAttributes.Regular);
                outEntry.ExternalFileAttributes = (int) (entryAttributes) << 16; // https://unix.stackexchange.com/questions/14705/the-zip-formats-external-file-attribute
                output.PutNextEntry(outEntry);
                input.CopyTo(output);
            }
            output.Finish();
            output.Flush();
        }

        DeleteFile(zipFile);
        RenameFile(tmpFileName,zipFile, FileExistsPolicy.Overwrite);
    }
    
    [Flags]
    enum ZipEntryAttributes
    {
        ExecuteOther = 1,
        WriteOther = 2,
        ReadOther = 4,
	
        ExecuteGroup = 8,
        WriteGroup = 16,
        ReadGroup = 32,

        ExecuteOwner = 64,
        WriteOwner = 128,
        ReadOwner = 256,

        Sticky = 512, // S_ISVTX
        SetGroupIdOnExecution = 1024,
        SetUserIdOnExecution = 2048,

        //This is the file type constant of a block-oriented device file.
        NamedPipe = 4096,
        CharacterSpecial = 8192,
        Directory = 16384,
        Block = 24576,
        Regular = 32768,
        SymbolicLink = 40960,
        Socket = 49152
	
    }
    class PublishTarget
    {
        public string Framework { get; set; }
        public string Runtime { get; set; }
    }
}
