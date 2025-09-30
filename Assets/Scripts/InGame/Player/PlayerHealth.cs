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
        
        // Trigger hurt animation IMMEDIATELY if still alive
        if (currentHP > 0 && playerMovement != null)
        {
            playerMovement.TriggerHurt();
        }
        
        // Check for death IMMEDIATELY - but only if not already dead
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
        
        // CRITICAL: Disable UI buttons immediately when player dies
        if (uiController != null)
        {
            uiController.SetPlayerDeadState(true);
            Debug.Log("UI buttons disabled - player is dead");
        }
        
        // IMMEDIATELY trigger death animation through PlayerMovement
        if (playerMovement != null)
        {
            playerMovement.TriggerDeath();
        }
        
        // Start respawn sequence after a delay
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        Debug.Log("Starting respawn sequence...");
        isRespawning = true;
        
        // CRITICAL: Wait for FULL death animation to complete (3 seconds to be safe)
        yield return new WaitForSeconds(3.0f); // Extended time to ensure death animation completes
        
        Debug.Log("Respawning player...");
        
        // Respawn logic (using GameDataManager)
        if (GameDataManager.Instance != null)
        {
            transform.position = GameDataManager.Instance.GetPlayerData().RespawnPoint;
            Debug.Log($"Player respawned at position: {transform.position}");
        }
        else
        {
            Debug.Log("GameDataManager not found - respawning at current position");
        }
        
        // Restore health and reset states
        currentHP = maxHP;
        isDead = false;
        
        // Update UI
        if (uiController != null)
        {
            uiController.UpdateHealth(currentHP);
        }
        
        // CRITICAL: Wait additional frames to ensure everything is ready
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();
        
        // NOW mark respawn as complete
        isRespawning = false;
        
        // CRITICAL: Re-enable UI buttons only when fully respawned
        if (uiController != null)
        {
            uiController.SetPlayerDeadState(false);
            Debug.Log("UI buttons RE-ENABLED - player fully respawned");
        }
        
        // Notify PlayerMovement that respawn is complete
        if (playerMovement != null)
        {
            playerMovement.OnRespawnComplete();
        }
        
        Debug.Log($"Player FULLY respawned with full HP: {currentHP}/{maxHP} - IsAlive: {IsAlive()} - Respawning: {isRespawning}");
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
            uiController.SetPlayerDeadState(false); // Re-enable UI buttons
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