using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SaveDataAchievements : SaveDataBase {

	public ObscuredInt[] achievementValues = null;
	public bool[] landGameplayTasksTypesCompletedForAchieve = null;

	public override void CheckValues() {
		base.CheckValues();

		int count = 30;
		achievementValues = CheckArray(achievementValues, count, 0);
		
		if (landGameplayTasksTypesCompletedForAchieve == null) {
			landGameplayTasksTypesCompletedForAchieve = new bool[9];
		}
	}

	/*public void UpdateAchievement(Achievement ach, int amount) {
		if (H_Achieves.singletone.IsAchUnlocked(ach)) {
			return;
		}
		var a = (int)ach;
		if (achievementValues[a] + amount >= H_Achieves.requirements[ach]) {
			H_Achieves.singletone.UnlockAchieve(ach);
		} else {
			achievementValues[a] += amount;
			H_Core.GLOBAL_SAVE();
		}
	}*/

	public int[] MakeBackup() {
		int[] res = new int[achievementValues.Length];
		for(int i = 0; i < res.Length;i++) {
			res[i] = achievementValues[i];
		}
		return res;
	}

	public void RestoreBackup(int[] backup) {
		achievementValues = CheckArray(achievementValues, achievementValues.Length, backup, 0);
	}
}
