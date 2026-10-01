using DG.Tweening;
using UnityEngine;

// Ездовой юнит: при смерти «коня» всадник спешивается и идёт дальше сам.
// Конь с аниматором (Horse) играет "Death" и зовёт OnDeathAnimationEnd из анимационного события;
// конь без аниматора (Bull) спешивает всадника сразу и валится обычной DOTween-смертью.
public class RiderUnit : Unit {

	public Unit riderPrefab;
	public UnitType riderType = UnitType.Legionnaire; // тип нужен всаднику для награды/книги после спешивания
	public Vector3 riderPosition;
	public Transform riderParent;

	private Unit rider;
	private bool riderReleased;

	public override float CombinedHP => base.CombinedHP + riderPrefab.CombinedHP;

	public override void Start() {
		base.Start();

		rider = Instantiate(riderPrefab, riderParent);
		rider.transform.localPosition = riderPosition;
		// Init нужен: без него у всадника type = Pirate (награда не та) и включены все варианты внешности разом
		rider.Init(waveId, riderType, ChildHpMultiplier, false, RewardMultiplier);
		rider.AIEnabled = false;
		rider.anim.SetTrigger("Ride");
		rider.GetComponent<Collider>().enabled = false;
	}

	public override void DeathRoutine() {
		rider.anim.SetTrigger("JumpOff");
		if (anim != null) {
			anim.SetTrigger("Death");
		} else {
			OnDeathAnimationEnd();
			base.DeathRoutine();
		}
	}

	public void OnDeathAnimationEnd() {
		if (riderReleased)
			return;
		riderReleased = true;

		rider.SetCurrentWaypoint(currentWaypoint);
		GameController.Instance.AddEnemy(rider);
		GameController.Instance.uiController.AddHealthBar(rider);

		rider.GetComponent<Collider>().enabled = true;
		rider.transform.SetParent(null, true);
		rider.AIEnabled = true;
	}
}
