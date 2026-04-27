#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Puts Gradle cache and JVM temp on the same volume as the project but off C: (e.g. I:\GradleUserHome),
/// avoiding full C: and avoiding paths nested under the project folder (which can exceed Windows MAX_PATH
/// when Gradle writes deep transform cache paths for Ninja).
/// Applies to the Unity editor process and child processes (Gradle) only.
/// </summary>
[InitializeOnLoad]
internal static class AndroidBuildDiskRedirect
{
    const string GradleDirName = "GradleUserHome";
    const string BuildTempDirName = "UnityAndroidBuildTemp";

    static AndroidBuildDiskRedirect()
    {
        Apply();
    }

    static void Apply()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string driveRoot = Path.GetPathRoot(projectRoot);
        if (string.IsNullOrEmpty(driveRoot))
            return;

        string gradleUserHome = Path.Combine(driveRoot, GradleDirName);
        string buildTemp = Path.Combine(driveRoot, BuildTempDirName);

        try
        {
            Directory.CreateDirectory(gradleUserHome);
            Directory.CreateDirectory(buildTemp);
        }
        catch
        {
            return;
        }

        Environment.SetEnvironmentVariable("GRADLE_USER_HOME", gradleUserHome, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("TEMP", buildTemp, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("TMP", buildTemp, EnvironmentVariableTarget.Process);
    }
}
#endif
