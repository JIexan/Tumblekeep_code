using System;
using System.Collections.Generic;

namespace Gameplay.RestorationSystem {
	// Айди регионов хаба. Всюду в коде — enum; строкой становится только на границе
	// сейва (SaveDataPlayerLocation) через ToSaveId(). Имя значения = ключ в сейве
	// и суффикс ключа локализации ("RegionTitle" + имя), поэтому переименование значения —
	// это смена ключа. Новые значения добавлять в конец: в сценах сериализуется числом.
	public enum RegionId {
		Dock,
		LightHouse,
		WatchTower,
		Farm,
		Well,
		Trader,
		Village,
		MarketEntrance,
		Market,
		RoadToBridge,
		RoadToCastle,
		VillageNearCastle,
		HousesNextToWall,
		Castle
	}

	public static class RegionIds {
		// Кэш имён: Enum.ToString() на каждый вызов дороговат для циклов по шагам.
		private static readonly Dictionary<RegionId, string> saveIds = BuildSaveIds();

		public static string ToSaveId(this RegionId id) {
			return saveIds.TryGetValue(id, out var saveId) ? saveId : id.ToString();
		}

		private static Dictionary<RegionId, string> BuildSaveIds() {
			var ids = new Dictionary<RegionId, string>();
			foreach (RegionId id in Enum.GetValues(typeof(RegionId)))
				ids[id] = id.ToString();
			return ids;
		}
	}
}
