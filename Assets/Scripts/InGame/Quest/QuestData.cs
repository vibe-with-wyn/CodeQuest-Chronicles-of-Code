using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum QuestType
{
    MainQuest,
    SubQuest,
    SideQuest
}

[System.Serializable]
public enum QuestStatus
{
    NotStarted,
    Active,
    Completed,
    Failed,
    Locked
}

[System.Serializable]
public class QuestObjective
{
    [Header("Objective Information")]
    public string objectiveTitle;
    public string objectiveDescription;
    public bool isCompleted;
    public bool isActive;
    
    [Header("Objective Settings")]
    public bool isOptional = false;
    public int targetCount = 1; // For "collect 5 items" type objectives
    public int currentCount = 0;
    
    public QuestObjective(string title, string description, bool optional = false, int target = 1)
    {
        objectiveTitle = title;
        objectiveDescription = description;
        isOptional = optional;
        targetCount = target;
        currentCount = 0;
        isCompleted = false;
        isActive = false;
    }
    
    public float GetProgress()
    {
        return targetCount > 0 ? (float)currentCount / targetCount : (isCompleted ? 1f : 0f);
    }
    
    public void UpdateProgress(int amount = 1)
    {
        currentCount = Mathf.Min(currentCount + amount, targetCount);
        if (currentCount >= targetCount)
        {
            isCompleted = true;
        }
    }
    
    public void CompleteObjective()
    {
        isCompleted = true;
        currentCount = targetCount;
    }
}

[System.Serializable]
public class QuestData
{
    [Header("Quest Information")]
    public string questId; // NEW: Add unique ID
    public string questTitle = "Welcome to the Adventure";
    public string questDescription = "Complete all objectives to finish this quest.";
    public Sprite questImage; // Optional image to show with the quest
    public bool isCompleted = false;
    public bool isActive = false;
    
    [Header("Quest Objectives")] // NEW: Add objectives list
    public List<QuestObjective> objectives = new List<QuestObjective>();
    
    [Header("Quest Progression")] // NEW: Add progression settings
    public string nextQuestId; // Quest that starts after this one completes
    
    public QuestData(string id, string title, string description)
    {
        questId = id;
        questTitle = title;
        questDescription = description;
        isActive = false;
        isCompleted = false;
        objectives = new List<QuestObjective>();
    }
    
    // NEW: Methods for managing objectives
    public void StartQuest()
    {
        isActive = true;
        if (objectives.Count > 0)
        {
            objectives[0].isActive = true; // Start first objective
        }
    }
    
    public void CompleteQuest()
    {
        isCompleted = true;
        isActive = false;
        foreach (var objective in objectives)
        {
            objective.isActive = false;
        }
    }
    
    public QuestObjective GetCurrentObjective()
    {
        return objectives.Find(o => o.isActive && !o.isCompleted);
    }
    
    public float GetOverallProgress()
    {
        if (objectives.Count == 0) return isCompleted ? 1f : 0f;
        
        int completedCount = 0;
        foreach (var objective in objectives)
        {
            if (objective.isCompleted) completedCount++;
        }
        
        return (float)completedCount / objectives.Count;
    }
    
    public void ProgressToNextObjective()
    {
        for (int i = 0; i < objectives.Count; i++)
        {
            if (objectives[i].isActive && objectives[i].isCompleted)
            {
                objectives[i].isActive = false;
                
                // Find next incomplete objective
                for (int j = i + 1; j < objectives.Count; j++)
                {
                    if (!objectives[j].isCompleted)
                    {
                        objectives[j].isActive = true;
                        return;
                    }
                }
                
                // If no more objectives, quest is complete
                CompleteQuest();
                return;
            }
        }
    }
}