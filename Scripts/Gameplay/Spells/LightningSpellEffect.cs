using DG.Tweening;
using DigitalRuby.ThunderAndLightning;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningSpellEffect : MonoBehaviour
{
	public LightningBoltPrefabScript lightning;
	public Color cloudyColor;
	public float flashDuration;
	public float cloudUpDuration;

	public float minHitDelay;
	public float maxHitDelay;

	private bool isPlaying;
	public bool IsPlaying {
		get { return isPlaying; }
	}

	private float hitTimer;
	private int nextEnemyID;

	List<Unit> enemies;
	float damage;

	// Затемняем основной источник света сцены (RenderSettings.sun), а не ambient:
	// запечённая в лайтмапу земля на ambient не реагирует, а прямой свет у неё реалтаймовый.
	Light sun;
	float sunBaseIntensity;
	Tween fadeTween;

	public void StartSequence(float value1) {
		damage = value1;
		isPlaying = true;
		enemies = GameController.Instance.GetAllEnemies();
		hitTimer = Random.Range(minHitDelay, maxHitDelay) + cloudUpDuration;
		nextEnemyID = 0;
		// Исходную интенсивность запоминаем один раз: повторный каст может прийти, пока свет ещё возвращается
		if(sun == null) {
			sun = FindSun();
			if(sun != null)
				sunBaseIntensity = sun.intensity;
		}
		FadeSun(0f);
	}

	// Не во всех сценах уровней солнце назначено в Lighting — тогда берём самый яркий directional
	static Light FindSun() {
		if(RenderSettings.sun != null)
			return RenderSettings.sun;
		Light brightest = null;
		foreach(var light in FindObjectsOfType<Light>()) {
			if(light.type == LightType.Directional && (brightest == null || light.intensity > brightest.intensity))
				brightest = light;
		}
		return brightest;
	}

	public void HitUnit(Unit u) {
		lightning.gameObject.SetActive(true);
		lightning.Destination = u.gameObject;
		// Вспышка: свет подскакивает до исходного значения и падает обратно в темноту
		DOTween.To(() => sun != null ? sun.intensity : 0f, (x) => { if(sun != null) sun.intensity = x; }, sunBaseIntensity, flashDuration).SetEase(Ease.Flash,2,-1).SetLink(gameObject).OnComplete(() => {
			u.Damage(null, damage, DamageType.Electric);
			lightning.Destination = null;
			lightning.gameObject.SetActive(false);
		});
	}

	void EndSequence () {
		FadeSun(sunBaseIntensity);
		isPlaying = false;
	}

	void FadeSun(float intensity) {
		fadeTween?.Kill();
		if(sun == null)
			return;
		fadeTween = DOTween.To(() => sun.intensity, (x) => sun.intensity = x, intensity, cloudUpDuration).SetEase(Ease.InOutCubic).SetLink(gameObject);
	}

	private void Update() {
		if(isPlaying) {
			hitTimer -= Time.deltaTime;
			if(hitTimer <= 0) {
				while (nextEnemyID < enemies.Count && enemies[nextEnemyID] == null)
					nextEnemyID++;

				if(nextEnemyID >= enemies.Count) {
					EndSequence();
					return;
				}

				HitUnit(enemies[nextEnemyID]);
				nextEnemyID++;
				hitTimer = Random.Range(minHitDelay, maxHitDelay);
			}
		}
	}
}
