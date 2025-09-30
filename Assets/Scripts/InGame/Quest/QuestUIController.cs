using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class QuestUIController : MonoBehaviour
{
    [Header("Quest Display UI")]
    [SerializeField] private GameObject questDisplayPanel;
    [SerializeField] private Image questScrollImage;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button closeButton;
    
    [Header("NEW: Objective Display")]
    [SerializeField] private Transform objectivesContainer; // Parent for objective list
    [SerializeField] private GameObject objectivePrefab; // Prefab for each objective
    
    [Header("NEW: Progress Display")]
    [SerializeField] private Slider questProgressBar; // Overall progress
    [SerializeField] private TextMeshProUGUI progressText; // "2/3 completed"
    
    [Header("Decoration Settings")]
    [SerializeField] private GameObject[] decorationObjects;
    [SerializeField] private SpriteRenderer[] decorationSpriteRenderers;
    [SerializeField] private Image[] decorationImages;
    
    [Header("Display Settings")]
    [SerializeField] private float autoDisplayDuration = 5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    [Header("Quest Scroll Sprite")]
    [SerializeField] private Sprite defaultScrollSprite;
    
    private CanvasGroup questPanelCanvasGroup;
    private bool isDisplaying = false;
    private Coroutine autoHideCoroutine;
    private List<GameObject> activeObjectiveItems = new List<GameObject>(); // NEW: Track objective UI
    
    // Store original decoration colors for proper restoration
    private Color[] originalDecorationSpriteColors;
    private Color[] originalDecorationImageColors;
    
    void Start()
    {
        InitializeQuestUI();
        SubscribeToQuestEvents();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromQuestEvents();
    }
    
    private void InitializeQuestUI()
    {
        // Get or add CanvasGroup for fading
        if (questDisplayPanel != null)
        {
            questPanelCanvasGroup = questDisplayPanel.GetComponent<CanvasGroup>();
            if (questPanelCanvasGroup == null)
            {
                questPanelCanvasGroup = questDisplayPanel.AddComponent<CanvasGroup>();
            }
            
            // Initially hide the panel
            questDisplayPanel.SetActive(false);
        }
        
        // Initialize decoration handling
        InitializeDecorations();
        
        // Setup button listeners
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
        
        // Set default scroll sprite if available
        if (questScrollImage != null && defaultScrollSprite != null)
        {
            questScrollImage.sprite = defaultScrollSprite;
        }
        
        Debug.Log("QuestUIController initialized with objectives support");
    }
    
    private void InitializeDecorations()
    {
        // Auto-find decoration objects if not assigned
        if ((decorationObjects == null || decorationObjects.Length == 0) && 
            (decorationSpriteRenderers == null || decorationSpriteRenderers.Length == 0) &&
            (decorationImages == null || decorationImages.Length == 0))
        {
            AutoFindDecorations();
        }
        
        // Store original colors for SpriteRenderers
        if (decorationSpriteRenderers != null && decorationSpriteRenderers.Length > 0)
        {
            originalDecorationSpriteColors = new Color[decorationSpriteRenderers.Length];
            for (int i = 0; i < decorationSpriteRenderers.Length; i++)
            {
                if (decorationSpriteRenderers[i] != null)
                {
                    originalDecorationSpriteColors[i] = decorationSpriteRenderers[i].color;
                }
            }
        }
        
        // Store original colors for Images
        if (decorationImages != null && decorationImages.Length > 0)
        {
            originalDecorationImageColors = new Color[decorationImages.Length];
            for (int i = 0; i < decorationImages.Length; i++)
            {
                if (decorationImages[i] != null)
                {
                    originalDecorationImageColors[i] = decorationImages[i].color;
                }
            }
        }
        
        // Initially hide all decorations
        SetDecorationsVisibility(false);
        
        Debug.Log($"Initialized decorations: {decorationObjects?.Length ?? 0} objects, {decorationSpriteRenderers?.Length ?? 0} sprites, {decorationImages?.Length ?? 0} images");
    }
    
    private void AutoFindDecorations()
    {
        if (questDisplayPanel == null) return;
        
        // Find all SpriteRenderers in children (including inactive)
        SpriteRenderer[] foundSprites = questDisplayPanel.GetComponentsInChildren<SpriteRenderer>(true);
        if (foundSprites.Length > 0)
        {
            decorationSpriteRenderers = foundSprites;
            Debug.Log($"Auto-found {foundSprites.Length} SpriteRenderer decorations");
        }
        
        // Find all Images in children that are not the main quest UI elements
        Image[] allImages = questDisplayPanel.GetComponentsInChildren<Image>(true);
        System.Collections.Generic.List<Image> decorationImagesList = new System.Collections.Generic.List<Image>();
        
        foreach (Image img in allImages)
        {
            // Skip the main quest scroll image
            if (img != questScrollImage)
            {
                decorationImagesList.Add(img);
            }
        }
        
        if (decorationImagesList.Count > 0)
        {
            decorationImages = decorationImagesList.ToArray();
            Debug.Log($"Auto-found {decorationImages.Length} Image decorations");
        }
    }
    
    private void SetDecorationsVisibility(bool visible)
    {
        // Handle decoration GameObjects
        if (decorationObjects != null)
        {
            foreach (GameObject decoration in decorationObjects)
            {
                if (decoration != null)
                {
                    decoration.SetActive(visible);
                }
            }
        }
        
        // Handle decoration SpriteRenderers
        if (decorationSpriteRenderers != null)
        {
            foreach (SpriteRenderer spriteRenderer in decorationSpriteRenderers)
            {
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = visible ? 1f : 0f;
                    spriteRenderer.color = color;
                }
            }
        }
        
        // Handle decoration Images
        if (decorationImages != null)
        {
            foreach (Image image in decorationImages)
            {
                if (image != null)
                {
                    Color color = image.color;
                    color.a = visible ? 1f : 0f;
                    image.color = color;
                }
            }
        }
    }
    
    private void SetDecorationsAlpha(float alpha)
    {
        // Handle decoration SpriteRenderers
        if (decorationSpriteRenderers != null && originalDecorationSpriteColors != null)
        {
            for (int i = 0; i < decorationSpriteRenderers.Length && i < originalDecorationSpriteColors.Length; i++)
            {
                if (decorationSpriteRenderers[i] != null)
                {
                    Color color = originalDecorationSpriteColors[i];
                    color.a = alpha;
                    decorationSpriteRenderers[i].color = color;
                }
            }
        }
        
        // Handle decoration Images
        if (decorationImages != null && originalDecorationImageColors != null)
        {
            for (int i = 0; i < decorationImages.Length && i < originalDecorationImageColors.Length; i++)
            {
                if (decorationImages[i] != null)
                {
                    Color color = originalDecorationImageColors[i];
                    color.a = alpha;
                    decorationImages[i].color = color;
                }
            }
        }
    }
    
    private void SubscribeToQuestEvents()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnNewQuestStarted += OnNewQuestReceived;
            QuestManager.Instance.OnQuestUpdated += OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted; // NEW
            QuestManager.Instance.OnObjectiveCompleted += OnObjectiveCompleted; // NEW
        }
    }
    
    private void UnsubscribeFromQuestEvents()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnNewQuestStarted -= OnNewQuestReceived;
            QuestManager.Instance.OnQuestUpdated -= OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted; // NEW
            QuestManager.Instance.OnObjectiveCompleted -= OnObjectiveCompleted; // NEW
        }
    }
    
    // Called when a new quest starts (automatically shows for a few seconds)
    private void OnNewQuestReceived(QuestData quest)
    {
        Debug.Log($"New quest received: {quest.questTitle}");
        ShowQuestDisplay(quest, true); // true = auto-hide after duration
    }
    
    // Called when quest is updated
    private void OnQuestUpdated(QuestData quest)
    {
        Debug.Log($"Quest updated: {quest.questTitle}");
        if (isDisplaying)
        {
            UpdateQuestDisplay(quest);
        }
    }
    
    // NEW: Called when quest is completed
    private void OnQuestCompleted(QuestData quest)
    {
        Debug.Log($"Quest completed: {quest.questTitle}");
        // You can add special completion effects here
    }
    
    // NEW: Called when objective is completed
    private void OnObjectiveCompleted(QuestObjective objective, QuestData quest)
    {
        Debug.Log($"Objective completed: {objective.objectiveTitle}");
        // Refresh display if showing
        if (isDisplaying)
        {
            UpdateObjectiveDisplay(quest);
        }
    }
    
    // Show quest display (called by quest log button or automatically)
    public void ShowQuestDisplay(QuestData quest, bool autoHide = false)
    {
        if (quest == null || questDisplayPanel == null) return;
        
        StartCoroutine(ShowQuestCoroutine(quest, autoHide));
    }
    
    private IEnumerator ShowQuestCoroutine(QuestData quest, bool autoHide)
    {
        isDisplaying = true;
        
        // Update quest content
        UpdateQuestDisplay(quest);
        
        // Show continue button only for auto-display, close button for manual access
        if (continueButton != null)
            continueButton.gameObject.SetActive(autoHide);
        
        if (closeButton != null)
            closeButton.gameObject.SetActive(!autoHide);
        
        // Show panel and decorations
        questDisplayPanel.SetActive(true);
        
        // Activate decoration GameObjects
        if (decorationObjects != null)
        {
            foreach (GameObject decoration in decorationObjects)
            {
                if (decoration != null)
                {
                    decoration.SetActive(true);
                }
            }
        }
        
        // Fade in both panel and decorations simultaneously
        yield return StartCoroutine(FadeQuestDisplayAndDecorations(0f, 1f, fadeInDuration));
        
        // Auto-hide after duration if requested
        if (autoHide)
        {
            autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
        }
    }
    
    // ENHANCED: Update quest display with objectives
    private void UpdateQuestDisplay(QuestData quest)
    {
        if (questTitleText != null)
            questTitleText.text = quest.questTitle;
        
        if (questDescriptionText != null)
            questDescriptionText.text = quest.questDescription;
        
        // Update quest image if provided
        if (questScrollImage != null && quest.questImage != null)
            questScrollImage.sprite = quest.questImage;
        else if (questScrollImage != null && defaultScrollSprite != null)
            questScrollImage.sprite = defaultScrollSprite;
        
        // NEW: Update progress bar
        if (questProgressBar != null)
        {
            questProgressBar.value = quest.GetOverallProgress();
        }
        
        // NEW: Update progress text
        if (progressText != null)
        {
            int completed = quest.objectives.FindAll(o => o.isCompleted).Count;
            int total = quest.objectives.Count;
            progressText.text = $"{completed}/{total} objectives completed";
        }
        
        // NEW: Update objectives display
        UpdateObjectiveDisplay(quest);
    }
    
    // NEW: Create/update objective list
    private void UpdateObjectiveDisplay(QuestData quest)
    {
        if (objectivesContainer == null) return;
        
        // Clear existing objective UI
        ClearObjectiveItems();
        
        // Create UI for each objective
        foreach (var objective in quest.objectives)
        {
            CreateObjectiveItem(objective);
        }
    }
    
    // NEW: Create individual objective UI item
    private void CreateObjectiveItem(QuestObjective objective)
    {
        if (objectivePrefab != null)
        {
            GameObject objectiveItem = Instantiate(objectivePrefab, objectivesContainer);
            activeObjectiveItems.Add(objectiveItem);
            
            // Setup objective UI
            ObjectiveItemUI objectiveUI = objectiveItem.GetComponent<ObjectiveItemUI>();
            if (objectiveUI != null)
            {
                objectiveUI.SetupObjective(objective);
            }
        }
        else
        {
            // Fallback: create simple text display
            GameObject textObj = new GameObject("ObjectiveText");
            textObj.transform.SetParent(objectivesContainer);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            string status = objective.isCompleted ? "✓" : (objective.isActive ? "▶" : "○");
            text.text = $"{status} {objective.objectiveTitle}";
            
            // Color coding
            if (objective.isCompleted)
                text.color = Color.green;
            else if (objective.isActive)
                text.color = Color.white;
            else
                text.color = Color.gray;
            
            activeObjectiveItems.Add(textObj);
        }
    }
    
    // NEW: Clear all objective UI items
    private void ClearObjectiveItems()
    {
        foreach (GameObject item in activeObjectiveItems)
        {
            if (item != null)
                Destroy(item);
        }
        activeObjectiveItems.Clear();
    }
    
    private IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSeconds(autoDisplayDuration);
        
        if (isDisplaying)
        {
            HideQuestDisplay();
        }
    }
    
    public void HideQuestDisplay()
    {
        if (!isDisplaying) return;
        
        StartCoroutine(HideQuestCoroutine());
    }
    
    private IEnumerator HideQuestCoroutine()
    {
        // Stop auto-hide coroutine if running
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        
        // Fade out both panel and decorations simultaneously
        yield return StartCoroutine(FadeQuestDisplayAndDecorations(1f, 0f, fadeOutDuration));
        
        // Hide panel and decorations
        questDisplayPanel.SetActive(false);
        SetDecorationsVisibility(false);
        ClearObjectiveItems(); // NEW: Clear objectives
        isDisplaying = false;
        
        Debug.Log("Quest display and decorations hidden");
    }
    
    // Combined fade method for panel and decorations
    private IEnumerator FadeQuestDisplayAndDecorations(float startAlpha, float endAlpha, float duration)
    {
        if (questPanelCanvasGroup == null) yield break;
        
        float elapsed = 0f;
        
        // Set starting alpha for both panel and decorations
        questPanelCanvasGroup.alpha = startAlpha;
        SetDecorationsAlpha(startAlpha);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            
            // Apply fade to both panel and decorations simultaneously
            questPanelCanvasGroup.alpha = currentAlpha;
            SetDecorationsAlpha(currentAlpha);
            
            yield return null;
        }
        
        // Ensure final alpha values are set
        questPanelCanvasGroup.alpha = endAlpha;
        SetDecorationsAlpha(endAlpha);
        
        Debug.Log($"Fade completed: Panel and decorations alpha set to {endAlpha}");
    }
    
    // Button event handlers
    private void OnContinueButtonClicked()
    {
        Debug.Log("Continue button clicked");
        HideQuestDisplay();
    }
    
    private void OnCloseButtonClicked()
    {
        Debug.Log("Close button clicked");
        HideQuestDisplay();
    }
    
    // Public method to show current quest (called by quest log button)
    public void ShowCurrentQuest()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.HasActiveQuest())
        {
            QuestData currentQuest = QuestManager.Instance.GetCurrentQuest();
            ShowQuestDisplay(currentQuest, false); // false = manual access, show close button
        }
        else
        {
            Debug.Log("No active quest to display");
        }
    }
}
