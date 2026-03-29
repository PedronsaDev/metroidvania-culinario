using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void StartGame() => SceneManager.Instance.LoadScene("Cena_Pedro");

    public void QuitGame() => Application.Quit();
}
