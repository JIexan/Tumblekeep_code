using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseShopSlot : MonoBehaviour
{
	[Header("Cloud")]
#pragma warning disable 0649
	[SerializeField] protected Image cloud;
	[SerializeField] TMP_Text cloudText;
#pragma warning restore 0649

	public virtual void SetTitle(Sprite background, Color outlineColor) { }
	public virtual void SetBackground(Sprite image) { }

	public virtual void SetCloud(string text, Sprite background) {
		cloud.gameObject.SetActive(true);
		cloudText.text = text;
		cloud.sprite = background;
	}

	public virtual void SetTextColor(Color color) { }
	public virtual void SetBadge(string text, Sprite background, Color outlineColor) { }
}
