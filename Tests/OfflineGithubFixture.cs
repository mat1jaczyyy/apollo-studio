using System;
using System.Reflection;
#if !LEGACY_AVALONIA
using System.Runtime.CompilerServices;
using System.Text;
using Apollo.Helpers;
using Octokit;
#endif

namespace Apollo.Tests {
    internal static partial class Scenarios {
        // Populate both splash caches before Program.Main can start asynchronous
        // network requests. Merely disabling update checks does not disable the
        // splash's release/blog fetches, and seeding only release leaves download
        // from an earlier fetch available to the missing-asset fixture.
        static void ConfigureOfflineGithub() {
#if !LEGACY_AVALONIA
            static void Set(object instance, string name, object value) {
                for (var type = instance.GetType(); type != null; type = type.BaseType) {
                    var field = type.GetField("<" + name + ">k__BackingField",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field == null) continue;
                    field.SetValue(instance, value);
                    return;
                }
                throw new InvalidOperationException("Offline GitHub fixture property missing: " + name);
            }

            var release = (Release)RuntimeHelpers.GetUninitializedObject(typeof(Release));
            Set(release, "Name", "Version 1.8.17");
            Set(release, "Body", "Offline regression fixture");
            Set(release, "PublishedAt", (DateTimeOffset?)DateTimeOffset.Parse("2026-09-12T00:00:00Z"));
            var blog = (RepositoryContent)RuntimeHelpers.GetUninitializedObject(typeof(RepositoryContent));
            Set(blog, "Name", "1789171200.md");
            Set(blog, "Encoding", "base64");
            Set(blog, "EncodedContent", Convert.ToBase64String(Encoding.UTF8.GetBytes("# Offline regression fixture")));

            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            typeof(Github).GetField("release", flags).SetValue(null, release);
            typeof(Github).GetField("download", flags).SetValue(null, null);
            typeof(Github).GetField("blogpost", flags).SetValue(null, blog);
#endif
        }
    }
}
