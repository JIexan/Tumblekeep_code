using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerArrow : MonoBehaviour 
{
	[SerializeField] BallisticMotion motion;
	[SerializeField] BaseProjectileTrigger trigger;

	[SerializeField] Transform arrowShank;
	[SerializeField] Transform tipRoot;
	[SerializeField] GameObject defaultTip;
	[SerializeField] GameObject poisonTip;

	[SerializeField] Vector3 heavyScale;
	[SerializeField] Vector3 doubleScale;

	[SerializeField] Renderer fireArrowRenderer;
	[SerializeField] float fireTipOffset;
	[SerializeField] float defaultShankScale = 1f;

	public BallisticMotion Motion => motion;
	public BaseProjectileTrigger Trigger => trigger;

	public void Init(BaseTower tower) {
		tipRoot.localScale = Vector3.one;

		if (tower.HasTrait(TowerTraitType.Heavy)) {
			tipRoot.localScale = Vector3.Scale(tipRoot.localScale, heavyScale);
		}

		if (tower.HasTrait(TowerTraitType.Double) && !tower.HasTrait(TowerTraitType.Poison)) {
			tipRoot.localScale = Vector3.Scale(tipRoot.localScale, doubleScale);
		}

		if(tower.HasTrait(TowerTraitType.Fire)) {
			fireArrowRenderer.gameObject.SetActive(true);
			float fireEnd = fireArrowRenderer.localBounds.center.z + fireArrowRenderer.localBounds.extents.z + fireTipOffset;
			float shankStart = Mathf.Abs(arrowShank.transform.localPosition.z);
			arrowShank.localScale = new Vector3(1f,1f, (fireEnd + shankStart) / shankStart);
			tipRoot.localPosition = new Vector3(0f, 0f, fireEnd);
		} else {
			fireArrowRenderer.gameObject.SetActive(false);
			tipRoot.localPosition = Vector3.zero;
			arrowShank.localScale = new Vector3(1f,1f, defaultShankScale);
		}

		if (tower.HasTrait(TowerTraitType.Poison)) {
			defaultTip.SetActive(false);
			poisonTip.SetActive(true);
		} else {
			defaultTip.SetActive(true);
			poisonTip.SetActive(false);
		}
	}
}
