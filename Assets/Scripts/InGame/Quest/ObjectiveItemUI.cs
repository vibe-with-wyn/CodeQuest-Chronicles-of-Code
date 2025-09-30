using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectiveItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private Image statusIcon;
    [SerializeField] private Slider progressBar; // For objectives with counts
    [SerializeField] private TextMeshProUGUI progressText;
    
    [Header("Status Icons")]
    [SerializeField] private Sprite completedIcon;
    [SerializeField] private Sprite activeIcon;
    [SerializeField] private Sprite inactiveIcon;
    
    [Header("Colors")]
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;
    
    public void SetupObjective(QuestObjective objective)
    {
        if (objective == null) return;
        
        // Set objective text
        if (objectiveText != null)
        {
            objectiveText.text = objective.objectiveTitle;
        }
        
        // Set status icon and color
        UpdateStatus(objective);
        
        // Set progress bar (if objective has multiple targets)
        if (progressBar != null)
        {
            if (objective.targetCount > 1)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.maxValue = objective.targetCount;
                progressBar.value = objective.currentCount;
            }
            else
            {
                progressBar.gameObject.SetActive(false);
            }
        }
        
        // Set progress text
        if (progressText != null)
        {
            if (objective.targetCount > 1)
            {
                progressText.text = $"{objective.currentCount}/{objective.targetCount}";
            }
            else
            {
                progressText.text = objective.isCompleted ? "Completed" : "";
            }
        }
    }
    
    private void UpdateStatus(QuestObjective objective)
    {
        Color textColor;
        Sprite icon;
        
        if (objective.isCompleted)
        {
            textColor = completedColor;
            icon = completedIcon;
        }
        else if (objective.isActive)
        {
            textColor = activeColor;
            icon = activeIcon;
        }
        else
        {
            textColor = inactiveColor;
            icon = inactiveIcon;
        }
        
        if (objectiveText != null)
            objectiveText.color = textColor;
        
        if (statusIcon != null && icon != null)
            statusIcon.sprite = icon;
    }
}