#if AUTOPLAY
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Autoplay {
	/// <summary>
	/// Подхватывает конфиг серии, оставленный окном Tools/Autoplay в EditorPrefs, при входе в play mode.
	/// Ключ удаляется сразу: обычный Play после отменённой серии не должен запускать бота.
	/// </summary>
	public static class AutoplayBootstrap {
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Boot() {
#if UNITY_EDITOR
			string json = UnityEditor.EditorPrefs.GetString(AutoplayEditorBridge.PendingConfigKey, "");
			if (string.IsNullOrEmpty(json))
				return;
			UnityEditor.EditorPrefs.DeleteKey(AutoplayEditorBridge.PendingConfigKey);

			AutoplayConfig cfg;
			try {
				cfg = Newtonsoft.Json.JsonConvert.DeserializeObject<AutoplayConfig>(json);
			} catch (System.Exception e) {
				Debug.LogError("[Autoplay] pending config is broken: " + e.Message);
				return;
			}
			if (cfg == null)
				return;
			AutoplayRunner.StartSeries(cfg);
#endif
		}

		// Для первой сцены sceneLoaded может не прийти подписчику из BeforeSceneLoad —
		// дублируем вызов (OnSceneReady идемпотентен по handle сцены)
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void AfterFirstScene() {
			if (AutoplayRunner.Current != null)
				AutoplayRunner.Current.OnSceneReady(SceneManager.GetActiveScene());
		}
	}
}
#endif
