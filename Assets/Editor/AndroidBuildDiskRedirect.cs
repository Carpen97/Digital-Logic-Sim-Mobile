#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Puts Gradle cache and JVM temp on the project drive (e.g. I:) instead of defaulting to
/// %USERPROFILE%\.gradle and %TEMP% on C:, which avoids "not enough space on the disk" when C: is full.
/// Applies to the Unity editor process and child processes (Gradle) only.
/// </summary>
[InitializeOnLoad]
internal static class AndroidBuildDiskRedirect
{
    const string GradleUserHomeFolderName = ".gradle-user-home";
    const string BuildTempFolderName = ".unity-build-temp";

    static AndroidBuildDiskRedirect()
    {
        Apply();
    }

    static void Apply()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string gradleUserHome = Path.Combine(projectRoot, GradleUserHomeFolderName);
        string buildTemp = Path.Combine(projectRoot, BuildTempFolderName);

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
