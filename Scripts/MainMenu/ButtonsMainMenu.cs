using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsMainMenu : Singleton<ButtonsMainMenu>
{
    public GameObject windowShop;

    private float saveButtonPosition, saveLevelButtonRightPanel;
    
    void Start()
    {
        saveButtonPosition = GetComponent<RectTransform>().anchoredPosition.x;
    }

    // Показываем уровни
    public void PlayLvl()
    {
        MainMenu.Instance.SwitchToMap();

		PressButonMainMenu();        
    }

    public void ShowBook ()
    {
        PlayerLocationUIController.Instance.ShowWindow(WindowType.Book);
    }

    public void ShowShop()
    {
        windowShop.SetActive(true);
    }

    // Прокачка
    public void ShowUgradeMenu()
    {
        PressButonMainMenu();
        DisableButtonSrollCamera();

    }

    // Ежедневные награды
    public void ShowDailyReward()
    {
        PressButonMainMenu();
        DisableButtonSrollCamera();
    }

    // Настройки
    public void ShowSettings()
    {
        PlayerLocationUIController.Instance.ShowWindow(WindowType.Settings);
    }

    // Скрываем кнопки поворачиваем камеру
    private void DisableButtonSrollCamera()
    {
        ShowOrDisableButtons(false);

    }

    // Кнопки главного меню
    public void ShowOrDisableButtons(bool show)
    {
        if (show)
			(transform as RectTransform).DOMoveX(saveButtonPosition, 1f).SetEase(Ease.InQuad).SetDelay(0.5f);      
        else
        {
            float positionScrollButton = GetComponent<RectTransform>().anchoredPosition.x - GetComponent<RectTransform>().sizeDelta.x;
			(transform as RectTransform).DOMoveX(positionScrollButton, 1f).SetEase(Ease.InQuad).SetDelay(0.5f);
		}
    }
    
    // Закрываем ежедневные награды
    public void CloseDaily()
    {
        CloseWindow();
        ShowOrDisableButtons(true);		
     
    }

   
    public void CloseSettings()
    {  
    }

    // Закрываем дерево технологий
    public void CloseTree()
    {
        CloseWindow();
        ShowOrDisableButtons(true);

        ManagerContenetTechTree.Instance.ResetWindow();        
    }

    private void rotateXonComplete()
    {
        //cameraMainMenu.stopRorate = false;
       // ManagerLevel.Instance.Rotation(false);
    }

	public void TestPlay() {
        PressButonMainMenu();
        SceneManager.LoadScene("SampleScene");
    }

    private void PressButonMainMenu()
    {
        // ButtonClickSound handles the click on the button itself.
    }

    private void CloseWindow()
    {
        // ButtonClickSound handles the click on the button itself.
    }
}
