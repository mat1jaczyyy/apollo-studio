using System.Linq;
using System.Threading.Tasks;

using Octokit;

using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Helpers {
    public static class Github {
        static Release cache = null;

        public static async Task<Release> LatestRelease() {
            if (cache == null) {
                cache = (
                    await new Octokit.GitHubClient(
                        new Octokit.ProductHeaderValue("mat1jaczyyy-apollo-studio")
                    ).Repository.Release.GetAll("mat1jaczyyy", "apollo-studio")
                ).First(/* i => i.Prerelease == false */);

                if (cache.Name != Program.Version) {
                    await MessageWindow.Create(
                        $"A new version of Apollo Studio is available ({cache.Name}).\n\n" +
                        "Do you want to update to the latest version?",
                        new string[] { "Yes", "No" }, null
                    );
                }
            }

            return cache;
        }
    }
}