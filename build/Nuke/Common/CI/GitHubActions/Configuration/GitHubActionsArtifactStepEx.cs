using Nuke.Common.Utilities;

namespace Nuke.Common.CI.GitHubActions.Configuration;

public class GitHubActionsArtifactStepEx : GitHubActionsArtifactStep
{
    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- uses: actions/upload-artifact@v3");

        using (writer.Indent())
        {
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine($"name: {Name}");
                writer.WriteLine($"path: {Path}");
            }
        }
    }
}