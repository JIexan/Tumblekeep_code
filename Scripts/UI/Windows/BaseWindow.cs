using System;
using UnityEngine;

public class BaseWindow : MonoBehaviour
{
    private Action onBeforeShow;
    private Action onAfterHide;

	// Start is called before the first frame update
	protected virtual void Start()
    {
        
    }

	public virtual void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
        this.onBeforeShow = onBeforeShow;
        this.onAfterHide = onAfterHide;

        onBeforeShow?.Invoke();
	}

    public virtual void Hide() {
        onAfterHide?.Invoke();
	}

    // Update is called once per frame
    protected virtual void Update()
    {
        
    }
}
