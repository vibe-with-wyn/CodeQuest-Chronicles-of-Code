using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroSequence : MonoBehaviour
{
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private float walkDistance = 2f;
    
    // NEW: Quest trigger delay
    [SerializeField] private float questTriggerDelay = 1f; // Delay after UI restoration before showing quest

    private PlayerMovement player;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float walkTimer;
    private bool isWalking;
    private float walkDuration;

    // Store UI state for restoration
    private bool originalInteractable;
    private bool originalBlocksRaycasts;
    private float originalAlpha;
    
    // Store original colors for restoration
    private Color[] originalImageColors;
    private Color[] originalTextColors;
    private Color[] originalSpriteColors;
    private Image[] allImages;
    private TextMeshProUGUI[] allTexts;
    private SpriteRenderer[] allSprites;

    void Start()
    {
        Debug.Log("IntroSequence Start() called");

        if (GameDataManager.Instance != null && !GameDataManager.Instance.HasPlayedIntro)
        {
            // HIDE UI IMMEDIATELY using CanvasGroup properties only
            HideUIImmediately();

            StartCoroutine(FindPlayerAndStartIntro());
        }
        else
        {
            CompleteIntro();
        }
    }

    // ENHANCED APPROACH: Hide all visual elements including SpriteRenderers
    private void HideUIImmediately()
    {
        if (uiCanvasGroup != null)
        {
            // Store original CanvasGroup state
            originalAlpha = uiCanvasGroup.alpha;
            originalInteractable = uiCanvasGroup.interactable;
            originalBlocksRaycasts = uiCanvasGroup.blocksRaycasts;

            // Hide UI completely using CanvasGroup
            uiCanvasGroup.alpha = 0f;
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;

            // Store original states and hide all child elements
            StoreOriginalStatesAndHide();

            Debug.Log("UI hidden immediately including SpriteRenderer components");
        }
    }

    private void StoreOriginalStatesAndHide()
    {
        if (uiCanvasGroup != null)
        {
            // Get all UI Images
            allImages = uiCanvasGroup.GetComponentsInChildren<Image>(true);
            originalImageColors = new Color[allImages.Length];
            for (int i = 0; i < allImages.Length; i++)
            {
                originalImageColors[i] = allImages[i].color;
                Color hiddenColor = allImages[i].color;
                hiddenColor.a = 0f;
                allImages[i].color = hiddenColor;
            }

            // Get all UI Texts
            allTexts = uiCanvasGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
            originalTextColors = new Color[allTexts.Length];
            for (int i = 0; i < allTexts.Length; i++)
            {
                originalTextColors[i] = allTexts[i].color;
                Color hiddenColor = allTexts[i].color;
                hiddenColor.a = 0f;
                allTexts[i].color = hiddenColor;
            }

            // Get all SpriteRenderers (2D sprites/icons)
            allSprites = uiCanvasGroup.GetComponentsInChildren<SpriteRenderer>(true);
            originalSpriteColors = new Color[allSprites.Length];
            for (int i = 0; i < allSprites.Length; i++)
            {
                originalSpriteColors[i] = allSprites[i].color;
                Color hiddenColor = allSprites[i].color;
                hiddenColor.a = 0f;
                allSprites[i].color = hiddenColor;
            }

            // Disable interactivity for buttons and sliders
            Button[] buttons = uiCanvasGroup.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.interactable = false;
            }

            Slider[] sliders = uiCanvasGroup.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
            {
                slider.interactable = false;
            }

            Debug.Log($"Stored and hidden: {allImages.Length} images, {allTexts.Length} texts, {allSprites.Length} sprites, {buttons.Length} buttons, {sliders.Length} sliders");
        }
    }

    private IEnumerator FindPlayerAndStartIntro()
    {
        yield return null;

        PlayerMovement[] players = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        if (players.Length > 1)
        {
            Debug.LogWarning($"Found {players.Length} PlayerMovement objects! Using the first active one.");
            player = System.Array.Find(players, p => p.gameObject.activeInHierarchy && p.gameObject.scene == gameObject.scene);
        }
        else if (players.Length == 1)
        {
            player = players[0];
        }

        if (player == null)
        {
            Debug.LogError("PlayerMovement not found!");
            CompleteIntro();
            yield break;
        }

        Debug.Log($"Found player: {player.gameObject.name} at position {player.transform.position}");

        float walkSpeed = player.GetWalkSpeed();
        walkDuration = walkDistance / walkSpeed;

        startPosition = player.transform.position;
        targetPosition = startPosition + new Vector3(walkDistance, 0f, 0f);
        walkTimer = 0f;
        isWalking = true;

        player.SetIntroWalking(true);
        GameDataManager.Instance.HasPlayedIntro = true;

        Debug.Log($"Intro sequence started: Player walking {walkDistance} units at {walkSpeed} speed over {walkDuration} seconds");
    }

    // ENHANCED: Restore all UI elements including SpriteRenderers
    private void ShowAllUIElements()
    {
        if (uiCanvasGroup != null)
        {
            StartCoroutine(RestoreUIGradually());
        }
    }

    private IEnumerator RestoreUIGradually()
    {
        Debug.Log("Starting UI restoration process...");

        // First, restore CanvasGroup properties
        uiCanvasGroup.alpha = originalAlpha;
        uiCanvasGroup.interactable = originalInteractable;
        uiCanvasGroup.blocksRaycasts = originalBlocksRaycasts;

        yield return null; // Wait a frame

        // Restore all child UI elements using stored states
        RestoreOriginalStates();

        yield return null; // Wait another frame

        // Force canvas update
        Canvas.ForceUpdateCanvases();

        yield return null; // Wait one more frame

        // Reinitialize UI Controller if needed
        UIController uiController = Object.FindFirstObjectByType<UIController>();
        if (uiController != null)
        {
            Debug.Log("Found UIController, reinitializing buttons...");
            uiController.ReinitializeButtons();
            Debug.Log("UIController buttons reinitialized successfully");
        }
        else
        {
            Debug.LogError("UIController not found during UI restoration!");
        }

        Debug.Log("UI restoration process completed");
        
        // NEW: Trigger quest after UI is fully restored
        StartCoroutine(TriggerFirstQuestAfterDelay());
    }
    
    // NEW: Trigger the first quest after a delay
    private IEnumerator TriggerFirstQuestAfterDelay()
    {
        yield return new WaitForSeconds(questTriggerDelay);
        
        Debug.Log("Triggering first quest after intro completion");
        
        // Initialize quest system and start first quest
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.StartFirstQuest();
        }
        else
        {
            Debug.LogError("QuestManager not found! Make sure it exists in the scene.");
        }
    }

    private void RestoreOriginalStates()
    {
        if (uiCanvasGroup != null)
        {
            // Restore all Image components with original colors
            if (allImages != null && originalImageColors != null)
            {
                for (int i = 0; i < allImages.Length && i < originalImageColors.Length; i++)
                {
                    if (allImages[i] != null)
                        allImages[i].color = originalImageColors[i];
                }
            }

            // Restore all Text components with original colors
            if (allTexts != null && originalTextColors != null)
            {
                for (int i = 0; i < allTexts.Length && i < originalTextColors.Length; i++)
                {
                    if (allTexts[i] != null)
                        allTexts[i].color = originalTextColors[i];
                }
            }

            // Restore all SpriteRenderer components with original colors
            if (allSprites != null && originalSpriteColors != null)
            {
                for (int i = 0; i < allSprites.Length && i < originalSpriteColors.Length; i++)
                {
                    if (allSprites[i] != null)
                        allSprites[i].color = originalSpriteColors[i];
                }
            }

            // Restore all Button components
            Button[] buttons = uiCanvasGroup.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.interactable = true;
            }

            // Restore all Slider components
            Slider[] sliders = uiCanvasGroup.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
            {
                slider.interactable = true;
            }

            Debug.Log($"Restored: {allImages?.Length ?? 0} images, {allTexts?.Length ?? 0} texts, {allSprites?.Length ?? 0} sprites");
        }
    }

    void Update()
    {
        if (isWalking)
        {
            walkTimer += Time.deltaTime;
            float t = walkTimer / walkDuration;

            player.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            Debug.Log($"Walking progress: {(t * 100f):F1}%");

            if (t >= 1f)
            {
                player.transform.position = targetPosition;
                isWalking = false;
                player.SetIntroWalking(false);

                Debug.Log("Walk complete, showing UI");

                // Show UI after walk completes
                ShowAllUIElements();

                // Destroy after a delay to ensure restoration completes
                StartCoroutine(DestroyAfterDelay());
            }
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        // Wait for UI restoration and quest trigger to complete
        yield return new WaitForSeconds(0.5f + questTriggerDelay + 0.5f); // Extra buffer
        Debug.Log("IntroSequence cleanup complete");
        Destroy(gameObject);
    }

    private void CompleteIntro()
    {
        if (uiCanvasGroup != null)
        {
            // Ensure UI is fully restored when skipping intro
            uiCanvasGroup.alpha = 1f;
            uiCanvasGroup.interactable = true;
            uiCanvasGroup.blocksRaycasts = true;

            // Quick restore without stored states (fallback approach)
            RestoreAllChildUIElementsDirectly();

            // Force canvas update
            Canvas.ForceUpdateCanvases();

            // Reinitialize buttons when intro is skipped
            UIController uiController = Object.FindFirstObjectByType<UIController>();
            if (uiController != null)
            {
                Debug.Log("Intro skipped - reinitializing UIController buttons");
                uiController.ReinitializeButtons();
            }

            Debug.Log("Intro skipped - UI restored immediately");
            
            // NEW: Still trigger quest even when skipping intro
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.StartFirstQuest();
            }
        }

        Destroy(gameObject);
    }

    private void RestoreAllChildUIElementsDirectly()
    {
        if (uiCanvasGroup != null)
        {
            // Restore all Image components to full opacity
            Image[] images = uiCanvasGroup.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                Color color = img.color;
                color.a = 1f;
                img.color = color;
            }

            // Restore all Text components to full opacity
            TextMeshProUGUI[] texts = uiCanvasGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                Color color = text.color;
                color.a = 1f;
                text.color = color;
            }

            // Restore all SpriteRenderer components to full opacity
            SpriteRenderer[] sprites = uiCanvasGroup.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer sprite in sprites)
            {
                Color color = sprite.color;
                color.a = 1f;
                sprite.color = color;
            }

            // Restore all Button components
            Button[] buttons = uiCanvasGroup.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.interactable = true;
            }

            // Restore all Slider components
            Slider[] sliders = uiCanvasGroup.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
            {
                slider.interactable = true;
            }

            Debug.Log($"Directly restored: {images.Length} images, {texts.Length} texts, {sprites.Length} sprites, {buttons.Length} buttons, {sliders.Length} sliders");
        }
    }
}