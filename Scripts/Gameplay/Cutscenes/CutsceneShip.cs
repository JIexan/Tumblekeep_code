using DG.Tweening;
using UnityEngine;

namespace Gameplay.Cutscenes {
	// Корабль катсцены: визуал MerchantShip без боевой логики (BaseShip завязан на
	// маршруты LevelDesriptor и в хабе неприменим). Владеет пеной, покачиванием и
	// «вечным плаваньем»-конвейером: корабль и камера СТОЯТ в мире неподвижно (нечему
	// прыгать), а течение изображают вода (WaterTiler.SetDrift — фазовый дрейф назад,
	// обёрнутый по модулю шага решётки) и живые world-space частицы пены, дрейфующие
	// тем же вектором. Реальное движение корабля появляется только на подходе к острову
	// (ShipArriveAction), когда дрейф затухает.
	public class CutsceneShip : MonoBehaviour {
		[Tooltip("Контейнер визуала — покачивание крутит его, не root (root двигают экшены).")]
		[SerializeField] Transform visual;
		[SerializeField] ParticleSystem foam;
		[Header("Покачивание")]
		[SerializeField] float bobAmplitude = 0.12f;
		[SerializeField] float bobPeriod = 2.6f;
		[SerializeField] float rollAmplitude = 1.5f;

		private Tween sailTween;
		private Tween driftFadeTween;
		private float sailSpeed;
		private Vector3 driftDirection;
		private WaterTiler waterTiler;
		private ParticleSystem[] particleSystems;
		private ParticleSystem.Particle[] particleBuffer;

		// Шаг решётки тайлов воды — ShipArriveAction квантует по нему вектор телепорта,
		// чтобы фаза воды в кадре сохранилась точно (0 — тайлера в сцене нет)
		public float WaterTileStep {
			get {
				EnsureTiler();
				return waterTiler != null ? waterTiler.WorldTileStep : 0f;
			}
		}

		private void Awake() {
			particleSystems = GetComponentsInChildren<ParticleSystem>(true);
		}

		private void OnEnable() {
			if (visual == null)
				return;
			// Независимые лупы (не TweenId катсцены): пришвартованный корабль
			// продолжает покачиваться и после катсцены/скипа
			visual.DOLocalMoveY(visual.localPosition.y + bobAmplitude, bobPeriod * 0.5f)
				.SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(this);
			visual.DOLocalRotate(new Vector3(0f, 0f, rollAmplitude), bobPeriod, RotateMode.LocalAxisAdd)
				.From(new Vector3(0f, 0f, -rollAmplitude * 0.5f), true)
				.SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(this);
		}

		private readonly GameAudioLoop idleAudio = new GameAudioLoop();

		private void Update() => idleAudio.Update(GameAudioManager.Instance?.GetId(GameSound.ShipIdle),
			transform.position, true);

		private void OnDisable() {
			idleAudio.Stop();
			DOTween.Kill(this);
			StopSailLoop();
		}

		public void SetFoam(bool on) {
			if (foam == null)
				return;
			if (on)
				foam.Play();
			else
				foam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		}

		// Мгновенный перенос корабля: живые world-space частицы (пена) и узор ряби
		// воды едут тем же вектором — ни след, ни рябь не выдают телепорт.
		// (Камера переносится этим же вектором в ShipArriveAction.)
		public void TeleportTo(Vector3 position, Quaternion rotation) {
			Vector3 delta = position - transform.position;
			transform.SetPositionAndRotation(position, rotation);
			ShiftWorldParticles(delta);
			EnsureTiler();
			if (waterTiler != null)
				waterTiler.AddVisualOffset(delta);
		}

		// «Вечное плаванье» на месте: включает течение воды и дрейф пены назад.
		// Твины помечены TweenId — Skip катсцены убивает их вместе с остальными.
		public void StartSailLoop(float speed) {
			StopSailLoop();
			sailSpeed = speed;
			driftDirection = -transform.forward;
			EnsureTiler();
			if (waterTiler != null)
				waterTiler.SetDrift(driftDirection * sailSpeed);
			SetFoam(true);

			sailTween = DOVirtual.Float(0f, 1f, 1f, _ => { })
				.SetLoops(-1, LoopType.Restart)
				.SetId(Actions.BaseCutsceneAction.TweenId)
				.SetLink(gameObject)
				.OnUpdate(() => ShiftWorldParticles(driftDirection * (sailSpeed * Time.deltaTime)));
		}

		// fadeTime > 0 — течение затухает плавно (корабль «набирает ход» на подходе),
		// 0 — мгновенный стоп (скип/восстановление состояния).
		public void StopSailLoop(float fadeTime = 0f) {
			if (sailTween != null && sailTween.IsActive())
				sailTween.Kill();
			sailTween = null;
			if (driftFadeTween != null && driftFadeTween.IsActive())
				driftFadeTween.Kill();
			driftFadeTween = null;

			EnsureTiler();
			if (fadeTime > 0f && sailSpeed > 0f && waterTiler != null) {
				driftFadeTween = DOVirtual.Float(sailSpeed, 0f, fadeTime, speed => {
						waterTiler.SetDrift(driftDirection * speed);
						ShiftWorldParticles(driftDirection * (speed * Time.deltaTime));
					})
					.SetEase(Ease.OutQuad)
					.SetId(Actions.BaseCutsceneAction.TweenId)
					.SetLink(gameObject);
			} else if (waterTiler != null) {
				waterTiler.SetDrift(Vector3.zero);
			}
		}

		private void EnsureTiler() {
			if (waterTiler == null)
				waterTiler = FindObjectOfType<WaterTiler>();
		}

		// Перенос живых частиц всех world-space систем корабля (телепорт и дрейф пены)
		private void ShiftWorldParticles(Vector3 delta) {
			if (particleSystems == null || delta == Vector3.zero)
				return;
			foreach (var ps in particleSystems) {
				if (ps == null || ps.main.simulationSpace != ParticleSystemSimulationSpace.World)
					continue;
				int maxParticles = ps.main.maxParticles;
				if (particleBuffer == null || particleBuffer.Length < maxParticles)
					particleBuffer = new ParticleSystem.Particle[maxParticles];
				int count = ps.GetParticles(particleBuffer);
				if (count == 0)
					continue;
				for (int i = 0; i < count; i++)
					particleBuffer[i].position += delta;
				ps.SetParticles(particleBuffer, count);
			}
		}
	}
}
