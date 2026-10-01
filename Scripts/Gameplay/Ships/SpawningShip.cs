using UnityEngine;

public class SpawningShip : BaseShip
{
	public int spawnPointID { get; private set; }

	public void Setup(Transform targetPoint, int enterPath, int exitPath, int spawnPoint) {
		base.Setup(targetPoint, enterPath, exitPath);
		spawnPointID = spawnPoint;
	}
}