using System;
using UnityEngine;

public class ParticleSystemEffect : MonoBehaviour {

	ParticleSystem s;

	private bool looping;
	private float timer;
	private bool isStopping = false;

	void Awake() {
		s = GetComponent<ParticleSystem>();
	}

	public void Init(bool looping) {
		isStopping = false;
		this.looping = looping;
		var main = s.main;
		main.loop = looping;
		s.Play();
	}

	public void SetLifetimeMultiplier( float multiplier) {
		var main = s.main;
		main.startLifetimeMultiplier = multiplier;
	}

	public void SetDuration(float duration) {
		var main = s.main;
		main.duration = duration;
	}

	public void OnParticleSystemStopped() {
		SimplePool.Despawn(gameObject);
	}

	public void Stop(bool immediate = false) {
		if (immediate) {
			s.Stop();
			SimplePool.Despawn(gameObject);
		} else {
			s.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			isStopping = true;
		}
	}

	void Update() {
		if (isStopping && !s.IsAlive(true)) {
			SimplePool.Despawn(gameObject);
		}
	}
}
