using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenuUI : BaseUIWindow
{

    [SerializeField] private GameObject _resumeButton;

    public void ResumeGame()
    {
        Hide();
    }

    public void QuitToMainMenu()
    {
        //GameManager.Instance.QuitToMainMenu();
    }

    public override void Show()
    {
        base.Show();
        EventSystem.current.SetSelectedGameObject(_resumeButton);
    }
}
