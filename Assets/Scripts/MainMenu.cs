using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private FadeController fadeController;

    public void OnPlayPressed()
    {
        StartCoroutine(LoadGameWithFade());
    }

    public void OnQuitPressed()
    {
        Application.Quit();
    }

    private IEnumerator LoadGameWithFade()
    {
        yield return StartCoroutine(fadeController.FadeOut());
        SceneManager.LoadScene("SampleScene");
    }
}
