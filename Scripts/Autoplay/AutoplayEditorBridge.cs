#if AUTOPLAY
using System.IO;
using UnityEngine;

namespace Autoplay {
	/// <summary>
	/// Всё, что рантайм-коду бота нужно от редактора. Бот живёт только в редакторе, но с дефайном
	/// код компилируется и для Standalone — поэтому UnityEditor под #if UNITY_EDITOR.
	/// </summary>
	public static class AutoplayEditorBridge {
		public const string PendingConfigKey = "Autoplay.PendingConfig";
		public const string LastConfigKey = "Autoplay.LastConfig";
		public const string LastSessionKey = "Autoplay.LastSession";
		public const string LevelSettingsPath = "Assets/Settings/Hub/LevelSettings.asset";

		public static string DefaultOutputRoot =>
			Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AutoplayStats"));

		public static Level FindLevel(string levelId) {
#if UNITY_EDITOR
			var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelSettings>(LevelSettingsPath);
			if (settings == null || settings.Levels == null)
				return null;
			return settings.Levels.Find(l => l != null && l.Id == levelId);
#else
			return null;
#endif
		}

		public static void ExitPlayMode() {
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#endif
		}

		public static void SetLastSession(string folder) {
#if UNITY_EDITOR
			UnityEditor.EditorPrefs.SetString(LastSessionKey, folder);
#endif
		}
	}
}
#endif
