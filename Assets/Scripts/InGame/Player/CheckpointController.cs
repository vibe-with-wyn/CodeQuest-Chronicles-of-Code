using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class CheckpointController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform spawnPoint;              // Where player should reappear (defaults to this object)
    [SerializeField] private bool oneTimeActivation = false;

    [Header("Visuals")]
    [SerializeField] private GameObject activatedVfx;
    [SerializeField] private GameObject idleVfx;

    private bool isActivated = false;

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnValidate()
    {
        if (spawnPoint == null) spawnPoint = transform;
    }

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        if (spawnPoint == null) spawnPoint = transform;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated && oneTimeActivation) return;

        // Robust player detection
        var player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        Vector3 newRespawn = (spawnPoint != null ? spawnPoint.position : transform.position);

        // Persist to GameDataManager (if available) AND PlayerPrefs (always)
        if (GameDataManager.Instance != null)
        {
            PlayerData data = GameDataManager.Instance.GetPlayerData();
            if (data != null)
            {
                data.RespawnPoint = newRespawn;
                data.LastScene = SceneManager.GetActiveScene().name;
            }
        }

        PlayerPrefs.SetFloat("RespawnX", newRespawn.x);
        PlayerPrefs.SetFloat("RespawnY", newRespawn.y);
        PlayerPrefs.SetFloat("RespawnZ", newRespawn.z);
        PlayerPrefs.Save();

        ToggleVisuals(true);
        isActivated = true;
        Debug.Log($"[Checkpoint] Set respawn to {newRespawn} in scene {SceneManager.GetActiveScene().name}");
    }

    private void ToggleVisuals(bool active)
    {
        if (activatedVfx != null) activatedVfx.SetActive(active);
        if (idleVfx != null) idleVfx.SetActive(!active);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((spawnPoint ? spawnPoint.position : transform.position), 0.2f);
    }
}