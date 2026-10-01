using UnityEngine;

/// <summary>
/// Параметры статусных эффектов на юнитах (яд/огонь) и связанных спеллов.
/// Живёт в Assets/Settings/StatusEffectsSettings.asset, подключён в GameConfigs
/// </summary>
[CreateAssetMenu(fileName = "StatusEffectsSettings", menuName = "Settings/StatusEffectsSettings")]
public class StatusEffectsSettings : ScriptableObject
{
	[Header("Яд")]
	[Tooltip("Яд не стакается параллельно: повторное попадание обновляет длительность и поднимает концентрацию до этого потолка")]
	[Min(1)] public int poisonMaxStacks = 5;

	[Tooltip("Прибавка к урону тика за каждую ступень концентрации сверх первой (0.2 = +20%)")]
	[Min(0f)] public float poisonStackDamageBonus = 0.2f;

	[Tooltip("Замедление отравленного юнита, % от скорости")]
	[Range(0, 90)] public int poisonSlowPercent = 20;

	[Tooltip("Интервал тика яда, сек")]
	[Min(0.1f)] public float poisonTickInterval = 1f;

	[Header("Огонь")]
	[Tooltip("Максимум параллельных стаков поджога от разных попаданий")]
	[Min(1)] public int fireMaxStacks = 3;

	[Header("Трейты у лучевых башен")]
	[Tooltip("Лазер и электро бьют каждый кадр — трейты (яд/огонь) вешаются на цель не чаще, чем раз в столько секунд")]
	[Min(0.1f)] public float continuousTraitInterval = 1f;

	[Header("Спелл «Ядовитое облако»")]
	[Tooltip("Как часто облако вешает яд на тех, кто внутри, сек")]
	[Min(0.1f)] public float cloudTickInterval = 0.5f;

	[Tooltip("Длительность отравления от одного тика облака, сек (обновляется, пока юнит внутри)")]
	[Min(0.5f)] public float cloudPoisonDuration = 3f;

	/// <summary>Множитель урона тика при данной концентрации</summary>
	public float GetPoisonStackMultiplier(int stacks) {
		return 1f + poisonStackDamageBonus * Mathf.Max(0, stacks - 1);
	}
}
