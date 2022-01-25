// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.Bitrise;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitLab;
using Nuke.Common.CI.Jenkins;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.CI.TravisCI;
using Nuke.Common.Tools.Git;
using Nuke.Common.ValueInjection;
using Octokit;

namespace Nuke.Common.Git
{
    /// <summary>
    /// Injects an instance of <see cref="GitRepository"/> based on the local repository.
    /// </summary>
    [PublicAPI]
    [UsedImplicitly(ImplicitUseKindFlags.Default)]
    public class GitRepositoryExtAttribute : ValueInjectionAttributeBase
    {
        public override object GetValue(MemberInfo member, object instance)
        {
            try
            {
                var build = (INukeBuild) instance;
                if (LibGit2Sharp.Repository.IsValid(build.RootDirectory))
                    return new LibGit2Sharp.Repository(build.RootDirectory);
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}