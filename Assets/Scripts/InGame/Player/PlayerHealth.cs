using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHP = 100;

    [Header("Death/Respawn Timing")]
    [Tooltip("How long to wait for the death animation to finish (seconds). If Use Animation Event is on, this is a safety timeout.")]
    [SerializeField] private float deathAnimationDuration = 3.0f;

    [Tooltip("Extra delay after the death animation BEFORE teleporting to the checkpoint. Set 0 to avoid 'stand up then teleport'.")]
    [SerializeField] private float preTeleportDelay = 0.0f;

    public enum UIUnlockMoment { AfterDeathAnimation, AfterTeleport }

    [Tooltip("When to re-enable UI inputs (buttons).")]
    [SerializeField] private UIUnlockMoment uiUnlockMoment = UIUnlockMoment.AfterTeleport;

    [Tooltip("Additional delay applied after the chosen unlock moment before enabling inputs.")]
    [SerializeField] private float uiUnlockDelay = 0.0f;

    [Header("Deterministic End Of Death Animation")]
    [Tooltip("If true, wait for an Animator event to end the death wait. Call OnDeathAnimationComplete() from the last frame of the death clip.")]
    [SerializeField] private bool useDeathAnimEvent = false;

    [Tooltip("Max time to wait for the animation event (fallback). Uses Death Animation Duration if <= 0.")]
    [SerializeField] private float deathAnimEventTimeout = 0f;

    [Header("Respawn Grounding")]
    [Tooltip("Layer(s) considered ground for snapping after teleport.")]
    [SerializeField] private LayerMask groundLayerMask = 1;
    [Tooltip("How far down we search for ground below the respawn point.")]
    [SerializeField] private float groundSnapMaxDistance = 5f;
    [Tooltip("Small offset above ground to avoid clipping into colliders.")]
    [SerializeField] private float groundSkin = 0.02f;

    private int currentHP;
    private UIController uiController;
    private PlayerMovement playerMovement;

    private bool isDead = false;
    private bool isRespawning = false;

    // Run id to cancel stale coroutines/events
    private int respawnRunId = 0;

    // Animation event flag
    private bool deathAnimCompletedFlag = false;

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
            Debug.Log("Damage ignored - Player is already dead");
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

        // Trigger hurt animation if still alive
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
        isRespawning = true;

        // Start a new run; cancel any pending unlocks from older runs
        int runId = ++respawnRunId;
        deathAnimCompletedFlag = false;

        Debug.Log("Player died! Triggering death animation...");

        // Disable UI buttons immediately
        uiController?.SetPlayerDeadState(true);

        // Trigger death animation
        playerMovement?.TriggerDeath();

        // Start respawn sequence
        StartCoroutine(RespawnSequence(runId));
    }

    private IEnumerator RespawnSequence(int runId)
    {
        Debug.Log($"[Respawn] Sequence started (run {runId})");

        // 1) Wait for death animation end
        if (useDeathAnimEvent)
        {
            float maxWait = deathAnimEventTimeout > 0f ? deathAnimEventTimeout : Mathf.Max(0.01f, deathAnimationDuration);
            float t = 0f;
            while (!deathAnimCompletedFlag && t < maxWait)
            {
                if (runId != respawnRunId) yield break; // cancelled by a new death
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!deathAnimCompletedFlag)
            {
                Debug.LogWarning($"[Respawn] Death animation event not received within {maxWait}s. Continuing.");
            }
        }
        else
        {
            if (deathAnimationDuration > 0f)
                yield return new WaitForSeconds(deathAnimationDuration);
        }

        if (runId != respawnRunId) yield break;

        // Optionally unlock UI right after death animation
        if (uiUnlockMoment == UIUnlockMoment.AfterDeathAnimation)
        {
            if (uiUnlockDelay > 0f) yield return new WaitForSeconds(uiUnlockDelay);
            if (runId != respawnRunId) yield break;
            uiController?.SetPlayerDeadState(false);
            Debug.Log("[Respawn] UI unlocked after death animation (by setting)");
        }

        // 2) Optional hold BEFORE teleport
        if (preTeleportDelay > 0f)
        {
            float elapsed = 0f;
            while (elapsed < preTeleportDelay)
            {
                if (runId != respawnRunId) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (runId != respawnRunId) yield break;

        // 3) Teleport to checkpoint
        Vector3 respawnPos;
        string source = TryResolveRespawnPoint(out respawnPos) ? "GameDataManager/PlayerPrefs" : "CurrentPosition";
        if (source == "CurrentPosition")
        {
            respawnPos = transform.position; // last resort
        }

        transform.position = respawnPos;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Debug.Log($"[Respawn] Teleported to {transform.position} (source: {source})");

        // 3b) Snap to ground so jump/attacks are available instantly
        SnapToGround();

        // 4) Restore HP and mark not dead
        currentHP = maxHP;
        isDead = false;

        uiController?.UpdateHealth(currentHP);

        // 5) Let things settle
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();

        if (runId != respawnRunId) yield break;

        // 6) Now mark respawning false and notify movement to fully reset
        isRespawning = false;

        playerMovement?.OnRespawnComplete();

        // 7) Or unlock UI after teleport (recommended default)
        if (uiUnlockMoment == UIUnlockMoment.AfterTeleport)
        {
            if (uiUnlockDelay > 0f) yield return new WaitForSeconds(uiUnlockDelay);
            if (runId != respawnRunId) yield break;
            uiController?.SetPlayerDeadState(false);
            Debug.Log("[Respawn] UI unlocked after teleport (by setting)");
        }

        Debug.Log($"[Respawn] Sequence complete (run {runId})");
    }

    private void SnapToGround()
    {
        // Raycast down from a bit above current position
        float castStartY = transform.position.y + 0.5f;
        Vector2 origin = new Vector2(transform.position.x, castStartY);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundSnapMaxDistance, groundLayerMask);

        if (hit.collider != null)
        {
            float halfHeight = 0.9f;
            var capsule = GetComponent<CapsuleCollider2D>();
            if (capsule != null)
                halfHeight = (capsule.size.y * Mathf.Abs(transform.localScale.y)) * 0.5f;

            float newY = hit.point.y + halfHeight + groundSkin;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            Debug.Log($"[Respawn] Snapped to ground at {hit.point}, newY={newY}");
        }
    }

    private IEnumerator UnlockUIAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        uiController?.SetPlayerDeadState(false);
        Debug.Log("[Respawn] UI unlocked (delayed)");
    }

    // Animator event hook (call from the end of your Death animation clip)
    // Add an Animation Event on the last frame: function name = OnDeathAnimationComplete
    public void OnDeathAnimationComplete()
    {
        deathAnimCompletedFlag = true;
        Debug.Log("[Respawn] Death animation event received");
    }

    // Reads GameDataManager first, then PlayerPrefs as fallback
    private bool TryResolveRespawnPoint(out Vector3 pos)
    {
        pos = transform.position;

        if (GameDataManager.Instance != null)
        {
            var data = GameDataManager.Instance.GetPlayerData();
            if (data != null && data.RespawnPoint != default(Vector3))
            {
                pos = data.RespawnPoint;
                return true;
            }
        }

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
        // Cancel any pending respawn sequence
        ++respawnRunId;

        Debug.Log("Reviving player instantly...");
        isDead = false;
        isRespawning = false;
        currentHP = maxHP;

        uiController?.UpdateHealth(currentHP);
        uiController?.SetPlayerDeadState(false);

        playerMovement?.OnRespawnComplete();

        Debug.Log($"Player revived instantly - IsAlive: {IsAlive()}");
    }

    void OnValidate()
    {
        if (maxHP <= 0) maxHP = 100;
        if (deathAnimationDuration < 0f) deathAnimationDuration = 0f;
        if (preTeleportDelay < 0f) preTeleportDelay = 0f;
        if (uiUnlockDelay < 0f) uiUnlockDelay = 0f;
        if (deathAnimEventTimeout < 0f) deathAnimEventTimeout = 0f;
        if (groundSnapMaxDistance < 0.1f) groundSnapMaxDistance = 0.1f;
        if (groundSkin < 0f) groundSkin = 0f;
    }
}