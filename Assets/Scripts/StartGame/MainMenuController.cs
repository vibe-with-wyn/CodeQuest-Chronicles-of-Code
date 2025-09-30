using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{

    [SerializeField] private AudioSource menuSound;

    public void OnNewGameButton()
    {
        StartCoroutine(HandleNewGame());
    }

    private IEnumerator HandleNewGame()
    {
        if (menuSound != null)
        {
            menuSound.Play();
            yield return new WaitForSeconds(menuSound.clip.length);
            Debug.Log($"Sound played for {menuSound.clip.length} seconds");
        }
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ResetPlayerData();
        }
        SceneManager.LoadScene("LanguageSelection");
    }

    public void OnResumeButton()
    {
        StartCoroutine(HandleResume());
    }

    private IEnumerator HandleResume()
    {
        if (menuSound != null)
        {
            menuSound.Play();
            yield return new WaitForSeconds(menuSound.clip.length);
            Debug.Log($"Sound played for {menuSound.clip.length} seconds");
        }

        if (GameDataManager.Instance != null)
        {
            PlayerData data = GameDataManager.Instance.GetPlayerData();
            string targetScene = (data.ProgressLevel >= 1 && data.LastScene == "GameWorldContext")
                ? "OakWoodsOfSyntax" : data.LastScene;
            if (!string.IsNullOrEmpty(targetScene) && data.ProgressLevel >= 1)
            {
                Debug.Log($"Resuming to {targetScene} with progress level {data.ProgressLevel}");
                LoadingScreenController.TargetSceneName = targetScene;
                AsyncOperation loadOp = SceneManager.LoadSceneAsync("LoadingScreen");
                yield return null;
            }
            else
            {
                Debug.LogWarning("No valid saved progress or scene, starting new game");
                SceneManager.LoadScene("LanguageSelection");
            }
        }
        else
        {
            Debug.LogError("GameDataManager not found, starting new game");
            SceneManager.LoadScene("LanguageSelection");
        }
    }

    public void OnExitButton()
    {
        Application.Quit();

        Debug.Log("Game is exiting... (Note: Application.Quit() does not work in the editor)");
    }
}