using System.IO;
using UnityEditor;
using UnityEditor.Android;

public class CopyGoogleServices : IPostGenerateGradleAndroidProject
{
    // Runs after Unity generates the Gradle project (both Export and Build & Run)
    public int callbackOrder => 999;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        // Where Unity generates the Gradle project
        // e.g. <project>/Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary
        var gradleRoot = path.Replace('\\', '/');
        
        // The launcher module directory - go up one level from unityLibrary to reach Gradle root
        var gradleParent = Path.GetDirectoryName(gradleRoot);
        var launcherDir = Path.Combine(gradleParent, "launcher");

        // Where we keep our config inside the Unity project (choose one)
        var candidates = new[]
        {
            "Assets/google-services.json",  // Standard Firebase location
            "Assets/Plugins/Android/launcher/google-services.json",
            "Assets/Plugins/Android/google-services.json",
            "Assets/StreamingAssets/google-services.json"
        };

        string src = null;
        foreach (var c in candidates)
        {
            if (File.Exists(c)) { src = c; break; }
        }

        if (src == null)
        {
            UnityEngine.Debug.LogWarning(
                "[Firebase] google-services.json not found in project. " +
                "Looked in Assets/Plugins/Android[/launcher] and StreamingAssets.");
            return;
        }

        // Copy to multiple locations where Gradle might look for it
        var destinations = new[]
        {
            Path.Combine(launcherDir, "src", "release", "google-services.json"),
            Path.Combine(launcherDir, "src", "debug", "google-services.json"),
            Path.Combine(launcherDir, "google-services.json")
        };

        foreach (var dst in destinations)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            File.Copy(src, dst, overwrite: true);
            UnityEngine.Debug.Log($"[Firebase] Copied google-services.json -> {dst}");
        }
    }
}
