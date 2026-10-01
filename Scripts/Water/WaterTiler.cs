using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterTiler : MonoBehaviour
{
	[SerializeField] GameObject waterTile;
	[SerializeField] int tileSize;
	[SerializeField] int tilesLayers;
	[SerializeField] float waterLayerY = -0.5f;
	// Start is called before the first frame update

	private GameObject objectToTrack;
	private float tileStep;
	private Vector3 driftVelocity;
	private Vector3 driftPhase;
	private Vector3 visualOffset;

	// Мировой сдвиг визуального узора воды (рябь/волны) — вычитается в шейдере при
	// сэмплинге нойза (_LPWDriftOffset_). Без него узор ряби заякорен в мире: едущий
	// меш пробегает по полю, и анимация ряби визуально ускоряется на скорость дрейфа.
	private static readonly int DriftOffsetShaderId = Shader.PropertyToID("_LPWDriftOffset_");

	// Мировой шаг решётки тайлов (валиден после Start). Катсценам — для решёточно-кратных телепортов.
	public float WorldTileStep => tileStep;

	// Катсценное «течение»: вода визуально едет с этой скоростью при неподвижной камере
	// (корабль стоит на месте — вода бежит назад). Реализовано фазовым смещением root'а:
	// фаза оборачивается по модулю шага решётки (сдвиг идентичных тайлов на целый шаг
	// невидим), так что покрытие никогда не уезжает от камеры дальше полутайла.
	// Vector3.zero — выключить; остаточная фаза безвредна (доля тайла на однородной воде).
	public void SetDrift(Vector3 velocity) {
		driftVelocity = velocity;
		driftVelocity.y = 0f;
	}

	// Мгновенный сдвиг узора воды (решёточно-кратный телепорт камеры/корабля в катсцене):
	// узор ряби переезжает тем же вектором и в кадре не меняется. Остаточное значение
	// безвредно — постоянный сдвиг нойз-поля неотличим от исходного.
	public void AddVisualOffset(Vector3 delta) {
		visualOffset += delta;
		visualOffset.y = 0f;
	}

    void Start()
    {
		tileStep = tileSize * 2 * Constants.sin60;
		Vector3 tilePos = new Vector3((tileStep / 2 + (tileStep * (tilesLayers - 1))) * -1, 0f, (tileStep / 2 + (tileStep * (tilesLayers - 1))) * -1);
        for(int i = -tilesLayers; i < tilesLayers; i++) {
			for(int j = -tilesLayers; j < tilesLayers; j++) {
				GameObject tile = Instantiate(waterTile, transform);
				tile.transform.localPosition = tilePos;
				tilePos += Vector3.right * tileStep;
			}
			tilePos += Vector3.forward * tileStep;
			tilePos.x = (tileStep / 2 + (tileStep * (tilesLayers - 1))) * -1;

		}
		tileStep *= transform.localScale.x;
		objectToTrack = CameraController.Instance.CreateTrackingObject(transform.position.y);
    }

    // LateUpdate: позиция камеры к этому моменту финальна (драг, инерция и катсценные
    // твины DOTween бегут в Update) — вода не отстаёт от телепортов камеры на кадр
    void LateUpdate()
    {
		if (driftVelocity != Vector3.zero) {
			Vector3 driftStep = driftVelocity * Time.deltaTime;
			driftPhase += driftStep;
			driftPhase.x = Mathf.Repeat(driftPhase.x + tileStep * 0.5f, tileStep) - tileStep * 0.5f;
			driftPhase.z = Mathf.Repeat(driftPhase.z + tileStep * 0.5f, tileStep) - tileStep * 0.5f;
			// Узор ряби едет вместе с мешем — скорость её анимации не зависит от дрейфа
			visualOffset += driftStep;
		}

		Shader.SetGlobalVector(DriftOffsetShaderId, new Vector4(visualOffset.x, visualOffset.z, 0f, 0f));

		Vector3 waterPos = objectToTrack.transform.position;
		waterPos.x = Mathf.FloorToInt(waterPos.x / tileStep) * tileStep;
		waterPos.y = waterLayerY;
		waterPos.z = Mathf.FloorToInt(waterPos.z / tileStep) * tileStep;
		transform.position = waterPos + driftPhase;
	}

	private void OnDestroy() {
		// Не тащить сдвиг узора в следующую сцену (глобальный шейдерный стейт)
		Shader.SetGlobalVector(DriftOffsetShaderId, Vector4.zero);
	}
}
