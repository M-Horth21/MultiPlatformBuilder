using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OutputEnable.MultiPlatformBuilder
{
  public class MultiPlatformBuilder : EditorWindow
  {
    enum Platforms
    {
      None = 0,
      Windows = 0b001,
      Linux = 0b010,
      MacOs = 0b100
    }

    Platforms _selectedPlatforms;

    const string PreferencesKey = "MultiBuildPrefs";

    [MenuItem("Tools/Multi-Platform Builder ^#M")]
    static void OpenWindow()
    {
      GetWindow<MultiPlatformBuilder>(utility: false, title: "Multi-Platform Builder", focus: true);
    }

    void OnEnable()
    {
      _selectedPlatforms = (Platforms)EditorPrefs.GetInt(PreferencesKey, 0);
    }

    void OnDisable()
    {
      EditorPrefs.SetInt(PreferencesKey, (int)_selectedPlatforms);
    }

    void OnGUI()
    {
      _selectedPlatforms = (Platforms)EditorGUILayout.EnumFlagsField("Platforms", _selectedPlatforms);

      if (GUILayout.Button("Build all"))
      {
        foreach (var item in GetFlags(_selectedPlatforms))
        {
          var summary = RunBuild((Platforms)item);
          if (summary.result != BuildResult.Succeeded) break;
        }
      }
    }

    BuildSummary RunBuild(Platforms platform)
    {
      BuildOptions buildOptions = BuildOptions.None;

#if UNITY_EDITOR_WIN
      if (platform == Platforms.Windows) buildOptions |= BuildOptions.ShowBuiltPlayer;
#endif

      BuildTarget target = platform switch
      {
        Platforms.Windows => BuildTarget.StandaloneWindows,
        Platforms.Linux => BuildTarget.StandaloneLinux64,
        Platforms.MacOs => BuildTarget.StandaloneOSX,
        _ => BuildTarget.NoTarget
      };

      string buildPath = Path.Combine(Application.dataPath, "..",
        "Builds", platform.ToString());

      string fileName = Application.productName + GetPlatformFileExtension(platform);

      BuildPlayerOptions options = new()
      {
        scenes = EditorBuildSettings.scenes.Select(x => x.path).ToArray(),
        locationPathName = Path.Combine(buildPath, fileName),
        options = buildOptions,
        target = target,
      };

      Stopwatch stopwatch = Stopwatch.StartNew();

      var report = BuildPipeline.BuildPlayer(options);
      var summary = report.summary;

      if (summary.result == BuildResult.Succeeded)
      {
        UnityEngine.Debug.Log(
          $"Build for {platform} completed in " +
          $"{stopwatch.ElapsedMilliseconds / 1000f}seconds.");
      }
      else
      {
        UnityEngine.Debug.LogError(
          $"Build for {platform} failed in " +
          $"{stopwatch.ElapsedMilliseconds / 1000f}seconds.");

      }

      return summary;
    }

    string GetPlatformFileExtension(Platforms platform)
    {
      return platform switch
      {
        Platforms.Windows => ".exe",
        Platforms.Linux => ".x86",
        _ => ""
      };
    }

    public IEnumerable<Enum> GetFlags(Enum input)
    {
      foreach (Enum value in Enum.GetValues(input.GetType()))
      {
        if (Convert.ToInt32(value) == 0)
          continue;

        if (input.HasFlag(value))
          yield return value;
      }
    }
  }
}