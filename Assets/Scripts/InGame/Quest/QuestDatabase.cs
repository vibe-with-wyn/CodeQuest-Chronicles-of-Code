using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quest System/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [Header("All Quests")]
    [SerializeField] private List<QuestData> allQuests = new List<QuestData>();
    
    public List<QuestData> GetAllQuests()
    {
        return new List<QuestData>(allQuests);
    }
    
    public QuestData GetQuestById(string questId)
    {
        return allQuests.Find(q => q.questId == questId);
    }
}