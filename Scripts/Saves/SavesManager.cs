using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

public class SavesManager : Singleton<SavesManager> {

	private static string saveDirectory = null;
	private static string SaveDirectory {
		get {
			if (saveDirectory == null) {
				saveDirectory = Path.GetFullPath(Application.persistentDataPath);
				if (!Directory.Exists(saveDirectory)) {
					Directory.CreateDirectory(saveDirectory);
				}
			}
			return saveDirectory;
		}
	}
	private static string defPathForSaves = "default";//GC
      
    public static string GetPersistentFilePath(string fileName, string folder = null) {
		var directory = SaveDirectory;
		if (folder != null) {
			directory = Path.Combine(SaveDirectory, folder);
			Directory.CreateDirectory(directory);
		}

		if (string.IsNullOrEmpty(fileName)) {
			return directory;
		}

		return Path.Combine(directory, fileName);
	}


	public readonly char[] TRIM_CHARS = { '\n', '\r' };

	public const string SAVE_ID = "TDS";

	private const int BACKUPS_COUNT = 3;
	private const string FOLDER_FORMAT = "yyyy-MM-dd-HH-mm-ss";

	private JsonSerializerSettings serializationSettings = null;

	// Автоплей-бот (AUTOPLAY): на время серии прогонов профиль живёт только в памяти,
	// SaveProfile() ничего не пишет на диск. Сбрасывается раннером по окончании.
	public static bool SuppressSaving = false;
	private static bool suppressLogged = false;

	[ShowInInspector]
	private SaveDataSettings settings = new SaveDataSettings();
	public SaveDataSettings Settings {
		get { return settings; }
	}

	[ShowInInspector]
	private SaveDataProfile profile = new SaveDataProfile();

	private static bool isProfileLoaded;

	protected override void Awake() {
		base.Awake();
		//временно. Поидее это должно вызываться после в ходу в гугл плей и прочую хуйню
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		LoadSettings();
		LoadProfile();
	}

	public static SaveDataProfile Profile {
		get { return Instance.profile; }
	}

	public SaveDataProfile CreateProfile() {
		var profile = new SaveDataProfile();
		profile.CheckValues();
		return profile;
	}

	public SaveDataSettings CreateSettings() {
		settings = new SaveDataSettings();
		settings.CheckValues();
		return settings;
	}

	public void SetSettings(SaveDataSettings settings) {
		this.settings = settings;
		InGameEventManager.SendEvent(InGameEvents.OnSettingsLoaded);
		Debug.Log("settings loaded");
	}

	public void SetProfile(SaveDataProfile profile) {
		this.profile = profile;
		InGameEventManager.SendEvent(InGameEvents.OnProfileLoaded);
		isProfileLoaded = true;
		Debug.Log("profile loaded");
	}

	public void LoadSettings() {
		string path = GetPersistentFilePath("settings.tds", "Settings");
		var settings = LoadFile<SaveDataSettings>(path, false);

		if (settings == null) {
			settings = new SaveDataSettings();
			settings.language = SaveDataSettings.SelectLanguage();
		}
		settings.CheckValues();
		SetSettings(settings);
	}

	public void LoadProfile() {
		string path = GetPersistentFilePath("profile.tds", GetSaveFolder(true));
		profile = LoadFile<SaveDataProfile>(path, true);

		// Демо: старые сейвы не мигрируем, а сбрасываем — тестеры начинают с нуля и видят весь туториал.
		// Файл не трогаем: новый профиль при первом же сохранении ляжет поверх него или в более свежую папку
		if (profile != null && profile.version < SaveDataProfile.CURRENT_VERSION) {
			Debug.Log("[SavesManager] save version " + profile.version + " < " + SaveDataProfile.CURRENT_VERSION + ", profile reset");
			profile = null;
		}

		if (profile == null) {
			profile = new SaveDataProfile();
		}
		profile.CheckValues();
		SetProfile(profile);
		Debug.Log("profile loaded");
	}

	public void SaveSettings() {
		CheckSerializationSettings();
		string s = JsonConvert.SerializeObject(settings, serializationSettings);
		StreamWriter sav = File.CreateText(GetPersistentFilePath("settings.tds", "Settings"));
		sav.Write(SAVE_ID + "\n" + s);
		sav.Close();
	}

	/// <summary>
	/// Глубокая копия профиля через ту же JSON-сериализацию, что и сейв (ObscuredInt конвертер).
	/// </summary>
	public SaveDataProfile CloneProfile(SaveDataProfile source) {
		CheckSerializationSettings();
		string s = JsonConvert.SerializeObject(source, serializationSettings);
		var res = JsonConvert.DeserializeObject<SaveDataProfile>(s, serializationSettings);
		res.CheckValues();
		return res;
	}

	public void SaveProfile() {
		if (SuppressSaving) {
			if (!suppressLogged) {
				suppressLogged = true;
				Debug.Log("[SavesManager] SaveProfile suppressed (autoplay session)");
			}
			return;
		}
		suppressLogged = false;
		CheckSerializationSettings();
		string s = JsonConvert.SerializeObject(profile, serializationSettings);
		StreamWriter sav = File.CreateText(GetPersistentFilePath("profile.tds", GetSaveFolder()));
		sav.Write(SAVE_ID + GetHash(s) + "\n" + s);
		sav.Close();
	}

	string GetHash(string data) {
		SHA256Managed crypt = new SHA256Managed();
		byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(data), 0, Encoding.UTF8.GetByteCount(data));
		string res = "";
		foreach (byte b in crypto) {
			res += b.ToString("x2");
		}
		return res;
	}

	private T LoadFile<T>(string path, bool hashCheck = true) where T : SaveDataBase {
		T res = default(T);
		if (File.Exists(path)) {
			try {
				StreamReader sav = File.OpenText(path);
				string header = sav.ReadLine();
				string data = sav.ReadToEnd();
				sav.Close();
				res = Parse<T>(header, data, hashCheck);
			}
			catch (Exception e) {
				Debug.Log("Save file " + path + " broken\n" + e.Message);
			}
		}
		return res;
	}

	private T LoadFileFromResources<T>(string path, bool hashCheck = true) where T : SaveDataBase {
		T res = default(T);
		TextAsset file = Resources.Load(path) as TextAsset;
		if (file != null) {
			try {			
				string header = file.text.Substring(0, file.text.IndexOf('\n'));
				string data = file.text.Substring(header.Length + 1);
				res = Parse<T>(header, data, hashCheck);
			}
			catch (Exception e) {
				Debug.Log("Save file " + path + " broken\n" + e.Message);
			}
		}
		return res;
	}

	private void CheckSerializationSettings() {
		if (serializationSettings != null) {
			return;
		}
		serializationSettings = new JsonSerializerSettings();
		serializationSettings.Converters.Add(new ObscuredIntConverter());
#if UNITY_EDITOR
		serializationSettings.Formatting = Formatting.Indented;
#endif
	}

	private T Parse<T>(string header, string data, bool hashCheck = true) where T : SaveDataBase {
		CheckSerializationSettings();
		T res = default(T);
		if (header.StartsWith(SAVE_ID)) {
			if (hashCheck) {
				string hash = header.Substring(SAVE_ID.Length);
				hash = hash.Trim(TRIM_CHARS);
				string computedHash = GetHash(data);
#if !UNITY_EDITOR
				if (hash.Equals(computedHash))
#endif
				{
					res = JsonConvert.DeserializeObject<T>(data, serializationSettings);
					res.CheckValues();
				}
			} else {
				res = JsonConvert.DeserializeObject<T>(data, serializationSettings);
				res.CheckValues();
			}
		}
		return res;
	}

	private string GetSaveFolder(bool alwaysLatest = false) {
		//эта штука будет отвечать за бэкапы

		List<DateTime> backups = new List<DateTime>(BACKUPS_COUNT);

		string[] directories = Directory.GetDirectories(GetPersistentFilePath(""));

		foreach(var directory in directories) {
			DateTime date = new DateTime();
			if(DateTime.TryParseExact(new DirectoryInfo(directory).Name, FOLDER_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) {
				backups.Add(date);
			}
		}
		backups.Sort((a, b) => b.CompareTo(a));

		if(alwaysLatest) {
			if (backups.Count > 0)
				return backups[0].ToString(FOLDER_FORMAT);

			return DateTime.Now.ToString(FOLDER_FORMAT);
		}

		if(backups.Count > BACKUPS_COUNT) {
			for(int i = backups.Count - 1; i >= BACKUPS_COUNT; i--) {
				Directory.Delete(GetPersistentFilePath(backups[i].ToString(FOLDER_FORMAT)),true);
				backups.RemoveAt(i);
			}
		}

		if (backups.Count > 0) {
			TimeSpan diff = DateTime.Now - backups[0];
			if (diff < TimeSpan.FromHours(1))
				return backups[0].ToString(FOLDER_FORMAT);
		}

		return DateTime.Now.ToString(FOLDER_FORMAT);

	}

	public static void Subscribe(Action<object> onProfileLoaded) {
		InGameEventManager.Subscribe(InGameEvents.OnProfileLoaded, onProfileLoaded);
		// Если кто то подписывается уже после того как загрузился профиль - надо ему скинуть событие
		if (isProfileLoaded) {
			onProfileLoaded(null);
		}
	}

	public static void Unsubscribe(Action<object> onProfileLoaded) {
		InGameEventManager.Unsubscribe(InGameEvents.OnProfileLoaded, onProfileLoaded);
	}

	public void DeleteCurrent() {
		string path = GetPersistentFilePath("profile.tds", GetSaveFolder(true));
		if(File.Exists(path)) {
			File.Delete(path);
		}

		profile = null;
	}
}
