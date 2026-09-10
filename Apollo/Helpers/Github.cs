using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Octokit;

using Apollo.Core;

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
                
                var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows
                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX : OSPlatform.Linux;
                var name = DownloadAssetName(release.Assets.Select(asset => asset.Name), platform, RuntimeInformation.ProcessArchitecture);
                download = release.Assets.FirstOrDefault(asset => asset.Name == name);
            }

            return release;
        }

        internal static string DownloadAssetName(IEnumerable<string> names, OSPlatform platform, Architecture architecture) {
            // Match the running build, including Intel builds running under Rosetta.
            // Never replace an ARM installation with an Intel-only update.
            string suffix = platform == OSPlatform.Windows && architecture == Architecture.X64 ? "-Win.zip"
                : platform == OSPlatform.OSX && architecture == Architecture.X64 ? "-Mac.zip"
                : platform == OSPlatform.OSX && architecture == Architecture.Arm64 ? "-Mac-arm64.zip"
                : null;
            return suffix == null ? null : names.FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
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
            if (release == null)
                try {
                    await LatestRelease();
                } catch {
                    return false;
                }

            return Preferences.CheckForUpdates && release.Name != Program.Version && download != null;
        }

        public static string AvaloniaVersion() =>
            typeof(Avalonia.Application).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion.Split('+')[0]
            ?? typeof(Avalonia.Application).Assembly.GetName().Version.ToString();
    }
}
