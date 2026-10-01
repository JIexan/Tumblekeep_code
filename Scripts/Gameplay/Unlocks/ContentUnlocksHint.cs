using System.Text;
using Assets.SimpleLocalization;
using Gameplay.RestorationSystem;

namespace Gameplay.Unlocks {
	// Тексты подсказок «что восстановить в хабе, чтобы открыть» для книги.
	// Ключи: Book_Locked_Hint ({0} шаг, {1} зона), Book_Locked_NoPath, Book_Locked_Faction ({0} фракция),
	// Book_Locked_Tier ({0} ярус), FactionName_{FactionId}, UnitTierName_{UnitTier}.
	public static class ContentUnlocksHint {
		// Подсказка по конкретному контенту (реликвия/свиток): «Откроется после восстановления «X» в зоне «Y»»
		public static string Compose(UnlockKind kind, string key) {
			var entry = ContentUnlocksManager.GetHint(kind, key);
			if (entry == null || string.IsNullOrEmpty(entry.stepId))
				return LocalizationManager.Localize("Book_Locked_NoPath");
			string step = LocalizationManager.Localize("ProgressStepTitle" + entry.stepId);
			string region = LocalizationManager.Localize("RegionTitle" + entry.regionId.ToSaveId());
			return LocalizeFormat("Book_Locked_Hint", step, region);
		}

		// null — юнит открыт; иначе строки причин (фракция / ярус) с подсказкой пути
		public static string ForUnit(UnitType type) {
			var reasons = ContentUnlocksManager.GetUnitLockReasons(type);
			if (reasons.Count == 0)
				return null;

			var sb = new StringBuilder();
			foreach (var reason in reasons) {
				if (sb.Length > 0)
					sb.Append('\n');
				switch (reason.Kind) {
					case UnlockKind.Faction:
						sb.Append(LocalizeFormat("Book_Locked_Faction", LocalizationManager.Localize("FactionName_" + reason.Key)));
						break;
					case UnlockKind.Tier:
						sb.Append(LocalizeFormat("Book_Locked_Tier", LocalizationManager.Localize("UnitTierName_" + reason.Key)));
						break;
				}
				sb.Append('\n').Append(Compose(reason.Kind, reason.Key));
			}
			return sb.ToString();
		}

		// Отсутствующий ключ возвращается как "{key}" — string.Format на нём кинул бы FormatException
		private static string LocalizeFormat(string key, params object[] args) {
			string format = LocalizationManager.Localize(key);
			if (!format.Contains("{0}"))
				return format;
			try {
				return string.Format(format, args);
			} catch (System.FormatException) {
				return format;
			}
		}
	}
}
