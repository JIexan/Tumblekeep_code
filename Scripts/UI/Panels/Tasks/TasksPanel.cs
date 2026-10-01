using Gameplay.TasksSystem;
using Gameplay.TasksSystem.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TasksPanel : MonoBehaviour
{
	[SerializeField] bool showProgress = true;
	[SerializeField] TaskPanel taskPanelPrefab;
	// Основной таск ([0]) — своей карточкой в отдельном контейнере; без этих полей все строки идут подряд
	[SerializeField] TaskPanel primaryTaskPanelPrefab;
	[SerializeField] Transform primaryContainer;
	// Куда класть доп. таски; пусто — в собственный transform
	[SerializeField] Transform secondaryContainer;

	private List<TaskPanel> tasks = new List<TaskPanel>();

	private void Start() {
		if (showProgress && LevelTasksManager.Instance != null) {
			var tasksCollection = LevelTasksManager.Instance.GetCurrentTasksList();
			if (tasksCollection != null) {
				Init(tasksCollection);
			} else {
				// Рестарт уровня (ResetLevel чистит таски) или прямой запуск боевой сцены в редакторе.
				// SetLevelTasks сам стартует таски, если матч уже начался
				Debug.LogWarning("TasksPanel: no tasks collection found, generating tasks via LevelTasksGenerator");
				var generatedTasks = LevelTasksGenerator.Generate(CurrentLevelContext.Level);
				LevelTasksManager.Instance.SetLevelTasks(generatedTasks);
				Init(generatedTasks);
			}
		}
	}

	public void Init(List<AbstractTask> tasksCollection) {
		for (int i = 0; i < tasksCollection.Count; i++) {
			if (i < tasks.Count) {
				tasks[i].gameObject.SetActive(true);
				tasks[i].Init(tasksCollection[i], showProgress);
			} else {
				bool primary = i == 0 && primaryTaskPanelPrefab != null && primaryContainer != null;
				var secondaryParent = secondaryContainer != null ? secondaryContainer : transform;
				var taskPanel = Instantiate(primary ? primaryTaskPanelPrefab : taskPanelPrefab, primary ? primaryContainer : secondaryParent);
				taskPanel.Init(tasksCollection[i], showProgress);
				tasks.Add(taskPanel);
			}
		}

		if(tasksCollection.Count < tasks.Count) {
			for (int i = tasksCollection.Count; i < tasks.Count; i++) {
				tasks[i].gameObject.SetActive(false);
			}
		}
	}
}
