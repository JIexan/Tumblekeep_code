using UnityEngine;

public class BuildingPlaceholder : BaseTower
{
    public GameObject ExclamationPoint;
	public MeshRenderer[] renderers;

    public void Start()
    {
        GlobalEventManager.Subsribe(GlobalEvents.StopAnimationExclamationPoint, StopAnimationExclamationPoint);
    }

    public void OnDestroy()
    {
        GlobalEventManager.Unsubsribe(GlobalEvents.StopAnimationExclamationPoint, StopAnimationExclamationPoint);
    }

    private void StopAnimationExclamationPoint()
    {
        if (ExclamationPoint != null)
            ExclamationPoint.SetActive(false);
    }

	public void SetVisualsActive(bool active) {
		foreach(var r in renderers) {
			r.enabled = active;
		}
	}

}
