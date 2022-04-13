// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Common.CI.GitHubActions
{
    /// <summary>
    /// Interface according to the <a href="https://help.github.com/en/articles/workflow-syntax-for-github-actions">official website</a>.
    /// </summary>
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GitHubActionsExAttribute : GitHubActionsAttribute
    {
        public GitHubActionsExAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images) : base(name, image, images)
        {
        }

        protected override GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
        {
            return new GitHubActionsJob
                   {
                       Name = image.GetValue().Replace(".", "_"),
                       Steps = GetSteps(image, relevantTargets).ToArray(),
                       Image = image
                   };
        }

        private IEnumerable<GitHubActionsStep> GetSteps(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
        {
            yield return new GitHubActionsUsingStep
                         {
                             Using = "actions/checkout@v1"
                         };

            if (CacheKeyFiles.Any())
            {
                yield return new GitHubActionsCacheStep
                             {
                                 IncludePatterns = CacheIncludePatterns,
                                 ExcludePatterns = CacheExcludePatterns,
                                 KeyFiles = CacheKeyFiles
                             };
            }

            yield return new GitHubActionsRunStep
                         {
                             Command = $"./{BuildCmdPath} {InvokedTargets.JoinSpace()}",
                             Imports = GetImports().ToDictionary(x => x.Key, x => x.Value)
                         };

            if (PublishArtifacts)
            {
                var artifacts = relevantTargets
                    .SelectMany(x => x.ArtifactProducts)
                    .Select(x => (AbsolutePath) x)
                    .Select(x => x.DescendantsAndSelf(y => y.Parent).FirstOrDefault())
                    .Distinct().ToList();

                foreach (var artifact in artifacts)
                {
                    yield return new GitHubActionsArtifactStepEx
                                 {
                                     Name = GetArtifactName(artifact),
                                     Path = NukeBuild.RootDirectory.GetUnixRelativePathTo(artifact)
                                 };
                }

                string GetArtifactName(AbsolutePath artifact)
                {
                    var result = artifact.ToString().TrimStart(artifact.Parent.ToString()).TrimStart('/', '\\');
                    return result.Contains("*") ? "artifacts" : result;
                }
            }
        }
    }
}
