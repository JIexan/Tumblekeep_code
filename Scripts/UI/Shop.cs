using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop : AnimationWindow
{
	[SerializeField] SmallShopSlot[] diamondSlots;

	private ShopCardSettings diamodCardSettings = new ShopCardSettings() {
		packs = new List<ItemPackSettings>(6) {
			new ItemPackSettings() { id = 0, name = "Diamonds", price = 0.99f, rewards = new List<Reward>() { new Reward() { item = ItemType.Crystal, count = 20 } } },
			new ItemPackSettings() { id = 1, name = "Diamonds", price = 4.99f, rewards = new List<Reward>() { new Reward() { item = ItemType.Crystal, count = 150 } } },
			new ItemPackSettings() { id = 2, name = "Diamonds", price = 9.99f, rewards = new List<Reward>() { new Reward() { item = ItemType.Crystal, count = 350 } } },
			new ItemPackSettings() { id = 3, name = "Diamonds", price = 24.99f, rewards = new List<Reward>() { new Reward() { item = ItemType.Crystal, count = 950 } } },
			new ItemPackSettings() { id = 4, name = "Diamonds", price = 59.99f, rewards = new List<Reward>() { new Reward() { item = ItemType.Crystal, count = 2500 } } },
			new ItemPackSettings() { id = 5, name = "Diamonds", price = 99.99f, rewards = new List<Reward>() { new Reward() { item = ItemType.Crystal, count = 5000 } } },
		}
	};

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);



		for(int i = 0; i < diamondSlots.Length; i++) {
			//врменно хардкодим дичь
			diamondSlots[i].Init(this, diamodCardSettings.packs[i], "", i);
		}
	}
}
