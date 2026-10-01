using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.RoadDamage {
	/// <summary>
	/// Демо-режим: все башни с оружием в сцене непрерывно стреляют по случайным плиткам дороги
	/// (через BaseTowerWeapon.ForcedTargetPoint), чтобы посмотреть, как дорога ломается.
	/// Кладётся рядом с RoadTileMap, включается галочкой компонента. Башни ставятся как обычно.
	/// </summary>
	public class RoadDamageDemo : MonoBehaviour {
		[Tooltip("Как часто башня выбирает новую плитку-цель, сек.")]
		[SerializeField] float retargetInterval = 2f;
		[Tooltip("Случайный разброс точки прицеливания вокруг центра плитки, м.")]
		[SerializeField] float aimScatter = 1f;
		[Tooltip("Как часто искать новые башни в сцене (они строятся в рантайме), сек.")]
		[SerializeField] float weaponsRefreshInterval = 1f;

		private readonly List<BaseTowerWeapon> weapons = new List<BaseTowerWeapon>();
		private float retargetTimer;
		private float refreshTimer;

		private void OnEnable() {
			retargetTimer = 0f;
			refreshTimer = 0f;
		}

		private void OnDisable() {
			foreach (var weapon in weapons) {
				if (weapon != null)
					weapon.ForcedTargetPoint = null;
			}
			weapons.Clear();
		}

		private void Update() {
			RoadTileMap map = RoadTileMap.Current;
			if (map == null)
				return;

			refreshTimer -= Time.deltaTime;
			if (refreshTimer <= 0f) {
				refreshTimer = weaponsRefreshInterval;
				RefreshWeapons();
			}

			retargetTimer -= Time.deltaTime;
			if (retargetTimer <= 0f) {
				retargetTimer = retargetInterval;
				Retarget(map);
			}
		}

		private void RefreshWeapons() {
			weapons.RemoveAll(w => w == null);
			foreach (var weapon in FindObjectsOfType<BaseTowerWeapon>()) {
				if (!weapons.Contains(weapon))
					weapons.Add(weapon);
			}
		}

		private void Retarget(RoadTileMap map) {
			foreach (var weapon in weapons) {
				if (weapon == null || !weapon.SupportsForcedTarget)
					continue;
				if (map.TryGetRandomTilePosition(weapon.transform.position, weapon.Radius, out Vector3 tilePosition)) {
					Vector2 scatter = Random.insideUnitCircle * aimScatter;
					weapon.ForcedTargetPoint = new Vector3(tilePosition.x + scatter.x, 0f, tilePosition.z + scatter.y);
				} else {
					weapon.ForcedTargetPoint = null;
				}
			}
		}
	}
}
