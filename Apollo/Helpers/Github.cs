using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Octokit;

using Humanizer;

using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Helpers {
    public static class Github {
        static GitHubClient _client = null;
        static GitHubClient Client => _client
            ?? (_client = new GitHubClient(new ProductHeaderValue("mat1jaczyyy-apollo-studio")));

        static RepositoryContent blogpost = null;
        static Release release = null;
        static ReleaseAsset download = null;

        public static bool UpdateChecked = false;

        public static async Task<RepositoryContent> LatestBlogpost() {
            if (blogpost == null) {
                blogpost = (
                    await Client.Repository.Content.GetAllContentsByRef(
                        "mat1jaczyyy", "apollo-studio-blog", 
                        (await Client.Repository.Content.GetAllContents("mat1jaczyyy", "apollo-studio-blog")).Last().Name,
                        "master"
                    )
                ).Last();
            }

            return blogpost;
        }

        public static async Task<Release> LatestRelease() {
            if (release == null) {
                release = (
                    await Client.Repository.Release.GetAll("mat1jaczyyy", "apollo-studio")
                ).First(i => i.Prerelease == false);
                
                download = release.Assets.FirstOrDefault(i => i.Name.Contains(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)? "Win.zip" : "Mac.zip"
                ));
            }

            return release;
        }

        public static async Task<ReleaseAsset> LatestDownload() {
            if (release == null)
                try {
                    await LatestRelease();
                } catch {
                    return null;
                }

            return download;
        }

        public static async Task<bool> ShouldUpdate() {
            if (UpdateChecked) return false;

            if (release == null)
                try {
                    await LatestRelease();
                } catch {
                    return false;
                }
            
            UpdateChecked = true;

            if (Preferences.CheckForUpdates && release.Name != Program.Version && download != null)
                return await MessageWindow.Create(
                    $"A new version of Apollo Studio is available ({release.Name} - {download.Size.Bytes().Humanize("#.##")}).\n\n" +
                    "Do you want to update to the latest version?",
                    new string[] { "Yes", "No" }, null
                ) == "Yes";

            return false;
        }
    }
}