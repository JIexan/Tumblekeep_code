using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveDataAnalitics : SaveDataBase {

	private static string[] OPTIONAL_PARAMS = new [] {
		"num_battles_completed",
		"num_artefact_geted",
		"num_reset",
		"morale_balance",
		"gold_balance",
		"current_ship",
		"map_id",
		"num_battles_failed",
		"num_battles_escaped",
		"session_num",
		"simplified_tutor",
		"short_tutor_end",
	};

	public float purchaseSum = 0.0f;
	public string purchaseCurrency = "";

	public int artefactsCount = 0;
	public int battlesStarted = 0;
	public int battlesLost = 0;
	public int resetsMade = 0;
	public int timesPurchased = 0;
	public int sessionNumber = 0;

	/*public void IncArtefacts(string artefact_id) {
		artefactsCount++;
		Analytics.singletone.EventArtefactGet(artefact_id);
	}

	//create standart parametrs
	public Dictionary<string, object> StandartEvent(Dictionary<string, object> data = null, int parametersLimit = -1) {
		var parameters = new Dictionary<string, object>();
		if (data != null) {
			foreach (string key in data.Keys) {
				parameters[key] = data[key];
			}
		}

		var profile = SavesManager.Profile;

		parameters["num_battles_started"] = battlesStarted;
		parameters["num_battles_failed"] = battlesLost;
		parameters["num_battles_escaped"] = profile.statistics.timesEscaped.Get();
		parameters["map_id"] = profile.map.currentMapNumber;
		parameters["current_ship"] = T_Core.MyShip.CaseIndex;
		parameters["gold_balance"] = profile.piastresAmount.Get();
		parameters["num_quests"] = profile.statistics.questsDone.Get();
		parameters["session_num"] = sessionNumber;
		parameters["purchase_count"] = timesPurchased;
		parameters["purchase_sum"] = purchaseSum;
		parameters["num_battles_completed"] = battlesStarted - battlesLost - profile.statistics.timesEscaped.Get();
		parameters["morale_balance"] = NewControlling.singletone.Morale.value;
		parameters["num_artefact_geted"] = artefactsCount;
		parameters["num_reset"] = resetsMade;
		parameters["currency"] = purchaseCurrency;
		parameters["simplified_tutor"] = UnityRemoteSettings.simplifiedMobileTutorial; 
		parameters["short_tutor_end"] = UnityRemoteSettings.fixedMobileTutorial;

		if (parametersLimit != -1) {
			for (int i = 0; i < OPTIONAL_PARAMS.Length && parameters.Count > parametersLimit; i++) {
				parameters.Remove(OPTIONAL_PARAMS[i]);
			}
		}

		return parameters;
	}*/
}
