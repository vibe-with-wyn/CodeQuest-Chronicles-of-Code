using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CharacterSelectionController : MonoBehaviour
{
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioSource swordsmanSound;
    [SerializeField] private AudioSource mageSound;
    [SerializeField] private AudioSource archerSound;

    private bool isDataManagerReady = false;

    private void Start()
    {
        StartCoroutine(CheckDataManager());

        if (backgroundMusic != null)
        {
            backgroundMusic.Play();
        }
    }

    private IEnumerator CheckDataManager()
    {
        while (GameDataManager.Instance == null)
        {
            yield return null;
        }

        isDataManagerReady = true;

        Debug.Log("GameDataManager is ready in CharacterSelection");
    }

    public void SelectCharacter(string character)
    {
        StartCoroutine(HandleButtonClick(character));
    }

    private IEnumerator HandleButtonClick(string character)
    {
        AudioSource clickSound = GetClickSound(character);

        if (clickSound != null)
        {
            clickSound.Play();

            yield return new WaitForSeconds(clickSound.clip.length); // Wait for sound

            Debug.Log($"Sound played for {clickSound.clip.length} seconds for {character}");
        }

        Debug.Log($"Received character parameter: '{character}'");

        if (isDataManagerReady && GameDataManager.Instance != null && !string.IsNullOrEmpty(character))
        {
            GameDataManager.Instance.SetCharacter(character);
            GameDataManager.Instance.UpdateProgress(1);

            if (GameDataManager.Instance.SelectedLanguage != null && GameDataManager.Instance.SelectedCharacter != null)
            {
                Debug.Log($"Player selected: {GameDataManager.Instance.SelectedLanguage}, " +
                    $"{GameDataManager.Instance.SelectedCharacter}, Progress: {GameDataManager.Instance.progressLevel}");

                Debug.Log("Proceeding to load LoadingScreen and then GameWorldContext");

                StartCoroutine(LoadLoadingScreenAndProceed("GameWorldContext"));
            }
            else
            {
                Debug.LogWarning("Language or character not fully set before proceeding");
            }
        }
        else
        {
            Debug.LogError("GameDataManager not found, not ready, or invalid character selected");
        }
    }

    private AudioSource GetClickSound(string character)
    {
        switch (character.ToLower())
        {
            case "swordsman":
                return swordsmanSound;
            case "mage":
                return mageSound;
            case "archer":
                return archerSound;
            default:
                Debug.LogWarning($"No sound defined for character: {character}");
                return null;
        }
    }

    private IEnumerator LoadLoadingScreenAndProceed(string targetScene)
    {
        Debug.Log("Loading LoadingScreen scene");
        LoadingScreenController.TargetSceneName = targetScene; // Set the target scene

        AsyncOperation loadOp = SceneManager.LoadSceneAsync("LoadingScreen");
        // No need to delay activation, just let LoadingScreenController handle the rest
        yield return null;
    }
}