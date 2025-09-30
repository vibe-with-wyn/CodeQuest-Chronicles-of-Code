using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameWorldContextController : MonoBehaviour
{
    [SerializeField] private AudioSource narrationAudio;
    [SerializeField] private Button skipButton;

    private bool isSkipping = false;

    void Start()
    {
        if (narrationAudio != null)
        {
            narrationAudio.Play();
            Debug.Log("Narration audio started");
            StartCoroutine(CheckNarrationCompletion());
        }
        else
        {
            Debug.LogWarning("Narration audio not assigned!");
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButton);
        }
        else
        {
            Debug.LogWarning("Skip button not assigned!");
        }
    }

    private IEnumerator CheckNarrationCompletion()
    {
        while (narrationAudio != null && narrationAudio.isPlaying && !isSkipping)
        {
            yield return null;
        }

        if (!isSkipping)
        {
            yield return new WaitForSeconds(2f); // 2-second delay after narration ends
            LoadNextScene();
        }
    }

    public void OnSkipButton()
    {
        isSkipping = true;
        if (narrationAudio != null && narrationAudio.isPlaying)
        {
            narrationAudio.Stop();
            Debug.Log("Narration skipped by player");
        }
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (LoadingScreenController.Instance != null)
        {
            LoadingScreenController.Instance.LoadScene("OakWoodsOfSyntax");
            Debug.Log("Directly using existing LoadingScreenController to load OakWoodsOfSyntax");
        }
        else
        {
            // fallback if somehow missing
            LoadingScreenController.TargetSceneName = "OakWoodsOfSyntax";
            SceneManager.LoadScene("LoadingScreen");
            Debug.Log("Transitioning to LoadingScreen for OakWoodsOfSyntax");
        }
    }

}