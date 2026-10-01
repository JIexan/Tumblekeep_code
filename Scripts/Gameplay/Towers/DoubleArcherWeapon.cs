using DG.Tweening;
using UnityEngine;

public class DoubleArcherWeapon : ArcherWeapon
{
	[SerializeField] TowerArrow reloadingArrow2;
	[SerializeField] MeshRenderer[] reloadingArrowParts2;

	float secondArrowTimer = 0f;

	// За цикл вылетают ДВЕ полные стрелы (OnTentionComplete1/2) — DPS должен это отражать,
	// иначе бюджет волн получает вчетверо заниженную оценку этой башни
	public override float DPS => (ProjectileDamage * 2f) / (ReloadTime + (Radius / ArrowSpeed));

	public override void UpdateState() {
		base.UpdateState();

		reloadingArrow2.Init(tower);
	}

    // Update is called once per frame
    protected override void Update()
    {
		if(secondArrowTimer > 0) {
			secondArrowTimer -= Time.deltaTime;
			if(secondArrowTimer <= 0) {
				animator.SetTrigger("Start2");
				secondArrowTimer = 0;
			}
		}

		base.Update();


	}

	public void OnTentionComplete1() {
		state = State.Reload;
		secondArrowTimer = ReloadTime / 2f;
		ShootProjectile(reloadingArrow.transform);
		AnimateArrow(reloadingArrowParts);
	}

	public void OnTentionComplete2() {
		ShootProjectile(reloadingArrow2.transform);
		AnimateArrow(reloadingArrowParts2);
	}

}
