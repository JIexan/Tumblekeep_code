namespace Gameplay.Cutscenes {
	// Владелец реплик (Cutscene, TutorialManager) сообщает редактору, какой
	// conversationId подставлять в ключ локализации конкретной DialogueLine.
	// linePropertyPath — SerializedProperty.propertyPath реплики: владельцу с
	// несколькими разговорами (шаги туториала) он нужен, чтобы понять, чья строка.
	public interface IDialogueLineOwner {
		string GetConversationId(string linePropertyPath);
	}
}
