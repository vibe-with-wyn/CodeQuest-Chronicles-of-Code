using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    private int currentHP;
    private UIController uiController;
    private PlayerMovement playerMovement;
    private bool isDead = false;
    private bool isRespawning = false;

    void Start()
    {
        currentHP = maxHP;
        isDead = false;
        isRespawning = false;

        uiController = Object.FindFirstObjectByType<UIController>();
        playerMovement = GetComponent<PlayerMovement>();

        if (uiController != null)
        {
            uiController.UpdateHealth(currentHP);
            Debug.Log($"PlayerHealth initialized: {currentHP}/{maxHP} HP, UI updated");
        }
        else
        {
            Debug.LogError("UIController not found!");
        }

        Debug.Log($"PlayerHealth component initialized on {gameObject.name} - IsAlive: {IsAlive()}");
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            Debug.Log($"Damage ignored - Player is already dead");
            return;
        }

        Debug.Log($"TakeDamage called - Current state: HP={currentHP}, Dead={isDead}, Respawning={isRespawning}");

        int previousHP = currentHP;
        currentHP = Mathf.Max(0, currentHP - damage);

        if (uiController != null)
        {
            uiController.UpdateHealth(currentHP);
        }

        Debug.Log($"Player took {damage} damage. HP: {previousHP} -> {currentHP}/{maxHP}");

        if (currentHP > 0 && playerMovement != null)
        {
            playerMovement.TriggerHurt();
        }

        if (currentHP <= 0 && !isDead)
        {
            Debug.Log("Player HP reached 0, triggering death");
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (isDead || isRespawning) return;

        int previousHP = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + healAmount);

        if (uiController != null)
        {
            uiController.UpdateHealth(currentHP);
        }

        Debug.Log($"Player healed {healAmount} HP. HP: {previousHP} -> {currentHP}/{maxHP}");
    }

    public int GetCurrentHealth() => currentHP;
    public int GetMaxHealth() => maxHP;
    public float GetHealthPercentage() => (float)currentHP / maxHP;
    public bool IsAlive() => currentHP > 0 && !isDead;
    public bool IsRespawning() => isRespawning;

    private void Die()
    {
        if (isDead)
        {
            Debug.Log("Die() called but player is already dead - ignoring");
            return;
        }

        isDead = true;
        Debug.Log("Player died! Triggering death animation immediately...");

        if (uiController != null)
        {
            uiController.SetPlayerDeadState(true);
            Debug.Log("UI buttons disabled - player is dead");
        }

        if (playerMovement != null)
        {
            playerMovement.TriggerDeath();
        }

        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        Debug.Log("Starting respawn sequence...");
        isRespawning = true;

        yield return new WaitForSeconds(3.0f); // wait death anim

        Debug.Log("Respawning player...");

        // Resolve respawn point with robust fallback
        Vector3 respawnPos;
        string source = TryResolveRespawnPoint(out respawnPos) ? "GameDataManager/PlayerPrefs" : "CurrentPosition";
        if (source == "CurrentPosition")
        {
            respawnPos = transform.position; // last resort
        }

        transform.position = respawnPos;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Debug.Log($"Player respawned at position: {transform.position} (source: {source})");

        currentHP = maxHP;
        isDead = false;

        if (uiController != null)
        {
            uiController.UpdateHealth(currentHP);
        }

        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();

        isRespawning = false;

        if (uiController != null)
        {
            uiController.SetPlayerDeadState(false);
            Debug.Log("UI buttons RE-ENABLED - player fully respawned");
        }

        if (playerMovement != null)
        {
            playerMovement.OnRespawnComplete();
        }

        Debug.Log($"Player FULLY respawned with full HP: {currentHP}/{maxHP} - IsAlive: {IsAlive()} - Respawning: {isRespawning}");
    }

    // Reads GameDataManager first, then PlayerPrefs as fallback
    private bool TryResolveRespawnPoint(out Vector3 pos)
    {
        // Default
        pos = transform.position;

        // 1) GameDataManager
        if (GameDataManager.Instance != null)
        {
            var data = GameDataManager.Instance.GetPlayerData();
            if (data != null)
            {
                // Consider Vector3.zero as "unset" for safety
                if (data.RespawnPoint != default(Vector3))
                {
                    pos = data.RespawnPoint;
                    return true;
                }
            }
        }

        // 2) PlayerPrefs fallback
        if (PlayerPrefs.HasKey("RespawnX"))
        {
            pos = new Vector3(
                PlayerPrefs.GetFloat("RespawnX"),
                PlayerPrefs.GetFloat("RespawnY"),
                PlayerPrefs.GetFloat("RespawnZ")
            );
            return true;
        }

        return false;
    }

    public void Revive()
    {
        Debug.Log("Reviving player instantly...");
        isDead = false;
        isRespawning = false;
        currentHP = maxHP;

        if (uiController != null)
        {
            uiController.UpdateHealth(currentHP);
            uiController.SetPlayerDeadState(false);
        }

        if (playerMovement != null)
        {
            playerMovement.OnRespawnComplete();
        }

        Debug.Log($"Player revived instantly - IsAlive: {IsAlive()}");
    }

    void OnValidate()
    {
        if (maxHP <= 0)
            maxHP = 100;
    }
}