using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct LeaderboardEntry {
	public int Rank;
	public string Name;
	public int Score;
	public Sprite Avatar; // null — заглушка-силуэт на цветной подложке
	public bool IsMe;
}

// Строка лидерборда: место (у топ-3 — кубок), аватар, имя, счёт. Строка игрока подсвечена.
public class LeaderboardRow : MonoBehaviour {
	[SerializeField] Image back;
	[SerializeField] TMP_Text rank;
	[SerializeField] Image trophy;
	[SerializeField] Image avatarBack;
	[SerializeField] Image avatar;
	[SerializeField] TMP_Text playerName;
	[SerializeField] TMP_Text score;
	[SerializeField] Sprite avatarPlaceholder;

	[Header("Цвета")]
	[SerializeField] Color normalBack = new Color(1f, 1f, 1f, 0.07f);
	[SerializeField] Color meBack = new Color(0.8745f, 0.7176f, 0.2078f, 1f);
	[SerializeField] Color normalText = Color.white;
	[SerializeField] Color meText = new Color(0.3412f, 0.2824f, 0.0784f, 1f);
	[SerializeField] Color scoreText = new Color(1f, 0.8275f, 0.2706f, 1f);
	[SerializeField] Color[] topColors = {
		new Color(1f, 0.8f, 0.2f, 1f),      // золото
		new Color(0.78f, 0.82f, 0.87f, 1f), // серебро
		new Color(0.8f, 0.5f, 0.25f, 1f),   // бронза
	};
	[SerializeField] float topBackAlpha = 0.28f;
	[SerializeField] Color[] avatarColors = {
		new Color(0.41f, 0.58f, 0f, 1f),
		new Color(0.58f, 0.16f, 0f, 1f),
		new Color(0.16f, 0.42f, 0.66f, 1f),
		new Color(0.5f, 0.3f, 0.62f, 1f),
		new Color(0.75f, 0.5f, 0.1f, 1f),
	};

	public void Set(LeaderboardEntry entry) {
		bool isTop = entry.Rank >= 1 && entry.Rank <= topColors.Length;

		trophy.gameObject.SetActive(isTop);
		rank.gameObject.SetActive(!isTop);
		rank.text = entry.Rank.ToString();
		if (isTop)
			trophy.color = topColors[entry.Rank - 1];

		if (entry.IsMe) {
			back.color = meBack;
		} else if (isTop) {
			var color = topColors[entry.Rank - 1];
			color.a = topBackAlpha;
			back.color = color;
		} else {
			back.color = normalBack;
		}

		rank.color = entry.IsMe ? meText : normalText;
		playerName.color = entry.IsMe ? meText : normalText;
		score.color = entry.IsMe ? meText : scoreText;

		playerName.text = entry.Name;
		score.text = entry.Score.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');

		bool hasAvatar = entry.Avatar != null;
		avatar.sprite = hasAvatar ? entry.Avatar : avatarPlaceholder;
		avatarBack.color = hasAvatar || avatarColors.Length == 0
			? Color.white
			: avatarColors[Mathf.Abs((entry.Name ?? "").GetHashCode()) % avatarColors.Length];
	}
}
