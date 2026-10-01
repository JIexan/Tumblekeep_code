using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class SpellsManager : Singleton<SpellsManager>
{
    public TowerRadius radiusEffect;
    public MinMaxGradient normalRadiusColor;
    public MinMaxGradient appliedRadiusColor;
    public SplashDamageProjectileTrigger meteor;
    [Tooltip("Эффект взрыва для SpellExplode (тот же, что импакт метеора). Скейлится под радиус спелла")]
    public GameObject explosionEffect;
    [Tooltip("Радиус спелла, при котором эффект взрыва показывается в масштабе префаба. Больше радиус — больше эффект")]
    [Min(0.1f)] public float explosionEffectBaseRadius = 4f;
    [Tooltip("Эффект спелла «Ядовитые шипы» (AcidExplosion из Polygon Arsenal). Скейлится под радиус")]
    public GameObject poisonEffect;
    [Tooltip("Радиус спелла, при котором эффект шипов показывается в масштабе префаба")]
    [Min(0.1f)] public float poisonEffectBaseRadius = 3f;
    [Tooltip("Префаб зоны «Ядовитое облако»")]
    public PoisonCloudZone poisonCloud;
    public LightningSpellEffect lighnningEffect;
    public MoneySpellEffect moneySpellEffect;
    public SpeedUpSpellEffect speedUpSpellEffect;
    [Tooltip("Эффект спелла «Защита». Если не назначен — создаётся компонентом на этом объекте")]
    public ProtectSpellEffect protectSpellEffect;

    /// <summary>Множитель характеристики Speed всех башен от SpellSpeedUp (1 = без бонуса)</summary>
    public static float TowerSpeedFactor { get; set; } = 1f;
    /// <summary>Плоский бонус к Resistance замка от SpellProtect (0 = без бонуса)</summary>
    public static float CastleResistanceBonus { get; set; } = 0f;

    private ProtectSpellEffect ProtectEffect {
        get {
            if (protectSpellEffect == null)
                protectSpellEffect = gameObject.AddComponent<ProtectSpellEffect>();
            return protectSpellEffect;
        }
    }

	public Material spellMaterial;
    public TowerRadius currentRadius = null;

    private MeteorDamageDealer meteorDamageDealer = new MeteorDamageDealer();

    private ItemSettings itemSettings => GameConfigs.Instance.ItemSettings;

    protected override void Awake() {
        base.Awake();
        // Статики переживают смену сцены — на старте катки бонусы всегда сняты
        TowerSpeedFactor = 1f;
        CastleResistanceBonus = 0f;
    }

    // Смысл value1/value2 для каждого спелла (дублируется подписями в BookSpellPresenter):
    //  SpellFireball: value1 = радиус, value2 = урон каждого метеора
    //  SpellExplode:  value1 = радиус, value2 = урон каждому юниту в радиусе
    //  SpellFreeze:   value1 = длительность
    //  SpellLightning: value1 = урон
    //  SpellMoney:    value1 = множитель, value2 = длительность
    //  SpellSpeedUp:  value1 = +% к характеристике Speed башен, value2 = длительность
    //  SpellPoison:   value1 = радиус, value2 = урон яда в секунду, value3 = длительность отравления
    //  SpellPoisonCloud: value1 = радиус, value2 = урон яда в секунду, value3 = время жизни облака
    //  SpellHeal:     value1 = % от максимального HP замка
    //  SpellProtect:  value1 = +Resistance замка, value2 = длительность

	public void InitRadius(Vector2 screenPosition, ItemType spellType) {
        var s = itemSettings.GetSpellData(spellType);
        float radius = 1f;
        bool useRadius = false;
        switch(spellType) {
            case ItemType.SpellFireball:
            case ItemType.SpellExplode:
            case ItemType.SpellPoison:
            case ItemType.SpellPoisonCloud:
                useRadius = true;
                radius = s.value1;
                break;
		}

        if (useRadius) {
            currentRadius = Instantiate(radiusEffect);
            currentRadius.gameObject.SetActive(true);
            currentRadius.Init(radius);
            currentRadius.Play();
            currentRadius.transform.position = CameraController.Instance.GetPointOnLand(screenPosition);
        }

    }

    public void UpdateRadius(Vector2 screenPosition) {
        if (currentRadius != null) {
            currentRadius.transform.position = CameraController.Instance.GetPointOnLand(screenPosition);
        }
    }

    public void HideRadius() {
        if (currentRadius != null) {
            currentRadius.Stop();
            Destroy(currentRadius.gameObject);
        }
    }

    public void ApplySpell(Vector2 screenPosition, ItemType spellType) {
        if(currentRadius != null) {
            currentRadius.SetColor(normalRadiusColor);
            var destroyTimer = currentRadius.gameObject.AddComponent<DestroyTimer>();
            destroyTimer.Init(5f);

		}
        Vector3 applyPosition = CameraController.Instance.GetPointOnLand(screenPosition);
        ApplySpellAtPoint(applyPosition, spellType);
    }

    /// <summary>
    /// Применить спелл в мировой точке (для глобальных спеллов точка не важна).
    /// Свиток НЕ списывается — это делает вызывающий (SpellPanel / автоплей-бот)
    /// </summary>
    public void ApplySpellAtPoint(Vector3 applyPosition, ItemType spellType) {
        var s = itemSettings.GetSpellData(spellType);
        switch (spellType) {
            case ItemType.SpellFireball:
                Vector3 initPosition = applyPosition + (new Vector3(0f, 1f, 1f)).normalized * 40f;
                //заспавнить шары с рандомными задержками. Пусть пока будет 3 шара
                for (int i = 0; i < 3; i++) {
                    var m = Instantiate(meteor);
                    m.transform.position = initPosition + Random.onUnitSphere * (s.value1 / 2);
                    m.transform.forward = (new Vector3(0f, -1f, -1f)).normalized;
                    m.GetComponent<ForwardMotion>().Init(20f,i == 0 ? 0 : Random.Range(0.2f,0.5f));
                    // Init(damage, radius): value1 — радиус, value2 — урон
                    meteorDamageDealer.Init(s.value2, s.value1);
					m.Init(meteorDamageDealer);
                }
                break;
            case ItemType.SpellExplode:
                Explode(applyPosition, s.value1, s.value2);
                break;
            case ItemType.SpellPoison:
                PoisonBurst(applyPosition, s.value1, s.value2, s.value3);
                break;
            case ItemType.SpellPoisonCloud:
                if (poisonCloud != null) {
                    var cloud = Instantiate(poisonCloud, applyPosition, Quaternion.identity);
                    cloud.Init(s.value1, s.value2, s.value3);
                }
                break;
            case ItemType.SpellFreeze:
                foreach(var unit in GameController.Instance.GetAllEnemies()) {
                    unit.AddTemporaryEffect(s.value1, UnitEffect.Freeze, () => { unit.Freeze = true; }, () => { unit.Freeze = false; }, 1);
				}
                break;

            case ItemType.SpellLightning:
                if(!lighnningEffect.IsPlaying)
                    lighnningEffect.StartSequence(s.value1);
                break;
			case ItemType.SpellMoney:
				if (!moneySpellEffect.IsPlaying) {
					CoinsManager.Instance.SetMultiplier(s.value1);
					moneySpellEffect.StartEffect(s.value2);
					GameController.Instance.uiController.spellTimerPanel.Show(ItemType.SpellMoney, moneySpellEffect);
				}
				break;
			case ItemType.SpellSpeedUp:
				if (!speedUpSpellEffect.IsPlaying) {
					speedUpSpellEffect.StartEffect(s.value1, s.value2);
					GameController.Instance.uiController.spellTimerPanel.Show(ItemType.SpellSpeedUp, speedUpSpellEffect);
				}
				break;
			case ItemType.SpellHeal:
				Castle.Instance.HealPercent(s.value1);
				break;
			case ItemType.SpellProtect:
				if (!ProtectEffect.IsPlaying) {
					ProtectEffect.StartEffect(s.value1, s.value2);
					GameController.Instance.uiController.spellTimerPanel.Show(ItemType.SpellProtect, ProtectEffect);
				}
				break;
		}
	}

    /// <summary>
    /// Мгновенный взрыв в точке: эффект импакта метеора, отмасштабированный под радиус,
    /// и урон всем врагам в радиусе (и наземным, и летающим — маска башен не учитывается)
    /// </summary>
    private void Explode(Vector3 position, float radius, float damage) {
        if (explosionEffect != null) {
            var fx = Instantiate(explosionEffect, position, Quaternion.identity);
            // У частиц scalingMode = Hierarchy, поэтому масштаб корня растягивает весь эффект
            fx.transform.localScale = explosionEffect.transform.localScale * (radius / explosionEffectBaseRadius);
        }

        foreach (var u in GameController.Instance.GetAllEnemiesInRadius(position, radius)) {
            u.Damage(null, damage, DamageType.Fire);
        }
    }

    /// <summary>«Ядовитые шипы»: всплеск кислоты в точке, все враги в радиусе получают отравление</summary>
    private void PoisonBurst(Vector3 position, float radius, float damagePerSecond, float duration) {
        if (poisonEffect != null) {
            var fx = Instantiate(poisonEffect, position, Quaternion.identity);
            fx.transform.localScale = poisonEffect.transform.localScale * (radius / poisonEffectBaseRadius);
        }

        foreach (var u in GameController.Instance.GetAllEnemiesInRadius(position, radius)) {
            u.ApplyPoison(null, damagePerSecond, duration);
        }
    }
}
