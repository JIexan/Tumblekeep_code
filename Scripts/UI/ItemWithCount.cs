using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemWithCount : MonoBehaviour {
	[SerializeField] Image image;
	[SerializeField] TMPro.TMP_Text text;
	[SerializeField] string addtitionalText = "";

	public void Init(Reward r) {
		Init(ItemIconsManager.Instance.GetIcon(r.item), r.count);
	}

	public void Init(ItemType item, int count) {
		Init(ItemIconsManager.Instance.GetIcon(item), count);
	}

	public void Init(Sprite icon, int count) {
		image.gameObject.SetActive(true);
		if(text != null)
			text.gameObject.SetActive(true);
		image.sprite = icon;
		SetCount(count);
	}

	public void SetCount(int count) {
		if (text != null)
			text.text = addtitionalText + count.ToString();
	}

	public void Hide() {
		image.gameObject.SetActive(false);
		if (text != null)
			text.gameObject.SetActive(false);
	}
}
