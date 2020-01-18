using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Newtonsoft.Json;
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
        static string avalonia = "";

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
            if (release == null)
                try {
                    await LatestRelease();
                } catch {
                    return false;
                }

            return Preferences.CheckForUpdates && release.Name != Program.Version && download != null;
        }

        static readonly string DepsPath = $"{AppDomain.CurrentDomain.BaseDirectory}Apollo.deps.json";

        public static string AvaloniaVersion() {
            if (avalonia == "" && File.Exists(DepsPath)) {
                try {
                    using (StreamReader file = File.OpenText(DepsPath))
                        using (JsonTextReader reader = new JsonTextReader(file))
                            while (reader.Read())
                                if (reader.TokenType == JsonToken.String && (string)reader.Path == "targets['.NETCoreApp,Version=v3.0']['Apollo/1.0.0'].dependencies.Avalonia") {
                                    avalonia = (string)reader.Value;
                                    break;
                                }
                } catch {
                    avalonia = "";
                }
            }

            return avalonia;
        }
    }
}