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
        PlayerInstance.Instance.gameObject.SetActive(false);
        SceneManager.Instance.LoadScene("Cena_Main_Menu");
    }

    public override void Show()
    {
        base.Show();
        EventSystem.current.SetSelectedGameObject(_resumeButton);
    }
}
