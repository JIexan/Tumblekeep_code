namespace Gameplay.Cutscenes {
	// Общий словарь эмоций для всех персонажей. Neutral = 0 — дефолт и фолбэк,
	// когда у персонажа не нарисован портрет под конкретную эмоцию. Только добавлять в конец.
	public enum EmotionId {
		Neutral = 0,
		Happy = 1,
		Angry = 2,
		Surprised = 3,
		Sad = 4,
		Thinking = 5,
	}
}
