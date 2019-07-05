using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Octokit;

using Humanizer;

using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Helpers {
    public static class Github {
        static Release cache = null;
        static ReleaseAsset download = null;

        public static bool UpdateChecked = false;

        public static async Task<Release> LatestRelease() {
            if (cache == null) {
                cache = (
                    await new Octokit.GitHubClient(
                        new Octokit.ProductHeaderValue("mat1jaczyyy-apollo-studio")
                    ).Repository.Release.GetAll("mat1jaczyyy", "apollo-studio")
                ).First(/* i => i.Prerelease == false */);
                
                download = cache.Assets.FirstOrDefault(i => i.Name.Contains(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)? "Win" : "Mac"
                ));
            }

            return cache;
        }

        public static async Task<bool> ShouldUpdate() {
            if (UpdateChecked) return false;

            if (cache == null)
                try {
                    await LatestRelease();
                } catch {
                    return false;
                }
            
            UpdateChecked = true;

            if (cache.Name != Program.Version && download != null)
                return await MessageWindow.Create(
                    $"A new version of Apollo Studio is available ({cache.Name} - {download.Size.Bytes().Humanize("#.##")}).\n\n" +
                    "Do you want to update to the latest version?",
                    new string[] { "Yes", "No" }, null
                ) == "Yes";

            return false;
        }
    }
}