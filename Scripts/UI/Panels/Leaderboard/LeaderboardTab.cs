using System.Collections.Generic;
using UnityEngine;

// Вкладка рейтинга в LevelInfoWindow: сверху скролл с топом, снизу закреплён блок «ваша позиция»
// (игрок + соседи по таблице). Пока макет: строки с примером запечены в префаб билдером
// (Tools/UI/Build Level Info Window) и видны как есть, пока никто не вызвал Show.
public class LeaderboardTab : MonoBehaviour {
	[SerializeField] RectTransform topContent;
	[SerializeField] RectTransform aroundContent;
	[SerializeField] LeaderboardRow rowPrefab;

	private List<LeaderboardRow> topRows;
	private List<LeaderboardRow> aroundRows;

	public void Show(IList<LeaderboardEntry> top, IList<LeaderboardEntry> around) {
		if (topRows == null) {
			// Запечённые строки макета идут в пул.
			topRows = new List<LeaderboardRow>(topContent.GetComponentsInChildren<LeaderboardRow>(true));
			aroundRows = new List<LeaderboardRow>(aroundContent.GetComponentsInChildren<LeaderboardRow>(true));
		}
		Fill(topRows, topContent, top);
		Fill(aroundRows, aroundContent, around);
	}

	private void Fill(List<LeaderboardRow> rows, RectTransform content, IList<LeaderboardEntry> entries) {
		for (int i = 0; i < entries.Count; i++) {
			if (i >= rows.Count)
				rows.Add(Instantiate(rowPrefab, content));
			rows[i].gameObject.SetActive(true);
			rows[i].Set(entries[i]);
		}
		for (int i = entries.Count; i < rows.Count; i++)
			rows[i].gameObject.SetActive(false);
	}
}
