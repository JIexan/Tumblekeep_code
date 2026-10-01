using System.Collections.Generic;
using UnityEngine;

public class GameConfigs : ScriptableObject
{
	// Имена сериализованных полей не менять — к ним привязаны данные в Resources/GameConfigs.asset
	[SerializeField] CharacteristicSettings characteristicSettings;
	[SerializeField] TowerUpgradeRules towerUpgradeRules;
	[SerializeField] RarityConfig rarityConfig;
	[SerializeField] TowerUpgradeSettings towerUpgradeSettings;
	[SerializeField] List<BaseTower> towerPrefabs;
	[SerializeField] ItemSettings itemSettings;
	[SerializeField] Gameplay.LevelUpgrades.LevelUpgradesSettings levelUpgradesSettings;
	[SerializeField] Gameplay.IslandExperience.IslandExperienceSettings islandExperienceSettings;
	[SerializeField] Gameplay.Relics.RelicsSettings relicsSettings;
	[SerializeField] ChestsSettings chestsSettings;
	[SerializeField] BirdsSettings birdsSettings;
	[SerializeField] Gameplay.RoadDamage.RoadDamageSettings roadDamageSettings;
	[SerializeField] Gameplay.Cutscenes.DialogueCharactersSettings dialogueCharactersSettings;
	// Мастер-список префабов юнитов по UnitType (тот же ассет, что у GameController.unitsToSpawn) — для книги в хабе
	[SerializeField] BookUnitDescription unitsCatalog;
	[SerializeField] StatusEffectsSettings statusEffectsSettings;
	// Мета-анлоки контента (реликвии/свитки/фракции/ярусы через хаб) + реестр подсказок книги
	[SerializeField] Gameplay.Unlocks.ContentUnlocksSettings contentUnlocksSettings;
	// SSAO-фича рендерера (саб-ассет Universal Render Pipeline Asset_Renderer) — её включает/выключает окно настроек
	[SerializeField] UnityEngine.Rendering.Universal.ScriptableRendererFeature ambientOcclusionFeature;
	// Пул доп. тасков уровня и кривые их целей от прокачки острова
	[SerializeField] Gameplay.TasksSystem.LevelTasksSettings levelTasksSettings;
	// Рероллы катки: бесплатный пул на старте и рост цены платных (окно апгрейдов, торговец)
	[SerializeField] Gameplay.Rerolls.RerollSettings rerollSettings;
	// Особенности островов: пассивный модификатор уровня на эффекторах реликвий
	[SerializeField] Gameplay.IslandFeatures.IslandFeaturesSettings islandFeaturesSettings;

	public Gameplay.IslandFeatures.IslandFeaturesSettings IslandFeaturesSettings => islandFeaturesSettings;
	public Gameplay.Rerolls.RerollSettings RerollSettings => rerollSettings;
	public Gameplay.TasksSystem.LevelTasksSettings LevelTasksSettings => levelTasksSettings;
	public UnityEngine.Rendering.Universal.ScriptableRendererFeature AmbientOcclusionFeature => ambientOcclusionFeature;
	public CharacteristicSettings CharacteristicSettings => characteristicSettings;
	public TowerUpgradeRules TowerUpgradeRules => towerUpgradeRules;
	public RarityConfig RarityConfig => rarityConfig;
	public TowerUpgradeSettings TowerUpgradeSettings => towerUpgradeSettings;
	public List<BaseTower> TowerPrefabs => towerPrefabs;
	public ItemSettings ItemSettings => itemSettings;
	public Gameplay.Relics.RelicsSettings RelicsSettings => relicsSettings;
	public ChestsSettings ChestsSettings => chestsSettings;
	public BirdsSettings BirdsSettings => birdsSettings;
	public Gameplay.RoadDamage.RoadDamageSettings RoadDamageSettings => roadDamageSettings;
	public Gameplay.Cutscenes.DialogueCharactersSettings DialogueCharactersSettings => dialogueCharactersSettings;
	public Gameplay.LevelUpgrades.LevelUpgradesSettings LevelUpgradesSettings => levelUpgradesSettings;
	public BookUnitDescription UnitsCatalog => unitsCatalog;
	public StatusEffectsSettings StatusEffectsSettings => statusEffectsSettings;
	public Gameplay.Unlocks.ContentUnlocksSettings ContentUnlocksSettings => contentUnlocksSettings;

	private static GameConfigs instance;

	public static GameConfigs Instance {
		get {
			if (instance == null) {
				Load();
			}
			return instance;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap() {
		if (instance == null) {
			Load();
		}
	}

	private static void Load() {
		instance = Resources.Load<GameConfigs>("GameConfigs");
		if (instance == null) {
			Debug.LogError("GameConfigs asset not found in Resources");
			return;
		}
		Gameplay.LevelUpgrades.LevelUpgradesManager.Init(instance.levelUpgradesSettings);
		Gameplay.IslandExperience.IslandExperienceManager.Init(instance.islandExperienceSettings);
		Gameplay.Relics.RelicsManager.Init(instance.relicsSettings);
		Gameplay.IslandFeatures.IslandFeaturesManager.Init(instance.islandFeaturesSettings);
		Gameplay.Rerolls.RerollsManager.Init(instance.rerollSettings);
		Gameplay.Unlocks.ContentUnlocksManager.Init(instance.contentUnlocksSettings);
	}
}
