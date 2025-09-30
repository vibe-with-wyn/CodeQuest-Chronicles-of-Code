using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public string SelectedLanguage;
    public string SelectedCharacter;
    public int ProgressLevel;
    public string LastScene;
    public Vector3 RespawnPoint;

    public PlayerData(string language, string character, int progress, string scene, Vector3 respawn)
    {
        SelectedLanguage = language;
        SelectedCharacter = character;
        ProgressLevel = progress;
        LastScene = scene;
        RespawnPoint = respawn;
    }
}