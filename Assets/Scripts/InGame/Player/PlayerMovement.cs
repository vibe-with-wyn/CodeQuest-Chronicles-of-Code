using UnityEngine;
using System.Collections;

[System.Serializable]
public class PlayerAttackData
{
    [Header("Attack Settings")]
    public int damage = 10;
    public float range = 0.0f;
    [SerializeField] private float colliderRadius = 0.2680953f;
    public float animationDuration = 0.5f;
    public float activeTime = 0.3f;
    
    public float ColliderRadius => colliderRadius;
}

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float walkSpeed = 3f; // Added back for IntroSequence
    
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CircleCollider2D attackCollider; // NEW: Direct reference to attack collider
    
    [Header("Attack Configuration")]
    [SerializeField] private PlayerAttackData[] attackData = new PlayerAttackData[5];
    
    [Header("Fireball Configuration")] // NEW: Fireball settings
    [SerializeField] private GameObject fireballPrefab; // Assign your Fireball prefab here
    [SerializeField] private Transform fireballSpawnPoint; // Optional: specific spawn point for fireball
    [SerializeField] private Vector3 fireballSpawnOffset = new Vector3(1.5f, 0.5f, 0); // Offset from player when no spawn point
    [SerializeField] private Vector3 fireballScale = Vector3.one; // Scale of the spawned fireball
    [SerializeField] private bool useSpawnPointOffset = true; // Whether to apply additional offset to spawn point
    [SerializeField] private Vector3 spawnPointOffset = new Vector3(0.5f, 0, 0); // Additional offset for spawn point
    [SerializeField] private float fireballSpawnDelay = 0.5f; // NEW: Delay before spawning fireball (adjust to match animation timing)
    
    // Core Components
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;
    private UIController uiController;
    private PlayerHealth playerHealth;
    private PlayerAttackCollider playerAttackCollider;
    
    // State Variables
    private bool isGrounded;
    private bool isJumping;
    private bool isRunning;
    private bool isAttacking;
    private bool isIntroWalking;
    private bool isDead;
    private bool isHurt;
    private bool isRespawning;
    private float facingDirection = 1f;
    
    // NEW: Fireball state tracking
    private bool isFireballAttackPending = false;
    
    // NEW: Store original physics values for proper reset
    private float originalGravityScale;
    private RigidbodyType2D originalBodyType;

    void Start()
    {
        InitializeComponents();
        ValidateAttackData();
        ValidateFireballSetup(); // NEW: Validate fireball setup
        
        // NEW: Store original physics values
        StoreOriginalPhysicsValues();
        
        Debug.Log($"PlayerMovement initialized on {gameObject.name}");
    }

    void Update()
    {
        CheckDeathState();
        
        // Block ALL actions when dead or respawning
        if (isDead || isRespawning) 
        {
            HandleDeadState();
            return;
        }

        if (isIntroWalking)
        {
            HandleIntroWalking();
            return;
        }

        HandleNormalMovement();
        UpdateFireballSpawnPoint(); // NEW: Update spawn point position
    }

    #region Initialization
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponentInChildren<Animator>();
        uiController = Object.FindFirstObjectByType<UIController>();
        playerHealth = GetComponent<PlayerHealth>();
        
        // Initialize attack collider if assigned
        if (attackCollider != null)
        {
            playerAttackCollider = attackCollider.GetComponent<PlayerAttackCollider>();
            if (playerAttackCollider == null)
                playerAttackCollider = attackCollider.gameObject.AddComponent<PlayerAttackCollider>();
            
            attackCollider.gameObject.SetActive(false);
            Debug.Log("Attack collider properly initialized");
        }
        else
        {
            Debug.LogError("Attack collider not assigned in inspector!");
        }

        // NEW: Initialize fireball spawn point
        InitializeFireballSpawnPoint();

        ValidateComponents();
    }

    // NEW: Store original physics values for proper reset
    private void StoreOriginalPhysicsValues()
    {
        if (rb != null)
        {
            originalGravityScale = rb.gravityScale;
            originalBodyType = rb.bodyType;
            Debug.Log($"Original physics stored: GravityScale={originalGravityScale}, BodyType={originalBodyType}");
        }
    }

    private void ValidateComponents()
    {
        if (animator == null) Debug.LogError("Animator not found on Player!");
        if (playerHealth == null) Debug.LogError("PlayerHealth component not found on Player!");
        if (attackCollider == null) Debug.LogError("Attack collider not assigned!");
    }

    private void ValidateAttackData()
    {
        if (attackData == null || attackData.Length != 5)
        {
            Debug.LogError("Attack data must contain exactly 5 attacks!");
            attackData = new PlayerAttackData[5]
            {
                new PlayerAttackData { damage = 10, range = 0.0f, animationDuration = 0.2680953f, activeTime = 0.3f },
                new PlayerAttackData { damage = 15, range = 0.0f, animationDuration = 0.2890902f, activeTime = 0.3f },
                new PlayerAttackData { damage = 12, range = 0.0f, animationDuration = 0.3194745f, activeTime = 0.3f },
                new PlayerAttackData { damage = 20, range = 2.0f, animationDuration = 1.0f, activeTime = 0.3f },
                new PlayerAttackData { damage = 25, range = 0.0f, animationDuration = 0.5604894f, activeTime = 0.3f }
            };
        }
    }

    // NEW: Initialize fireball spawn point
    private void InitializeFireballSpawnPoint()
    {
        if (fireballSpawnPoint == null)
        {
            // Try to find FireballSpawnPoint child
            Transform spawnPoint = transform.Find("FireballSpawnPoint");
            if (spawnPoint != null)
            {
                fireballSpawnPoint = spawnPoint;
                Debug.Log("Auto-found FireballSpawnPoint child");
            }
            else
            {
                Debug.LogWarning("FireballSpawnPoint child not found. Will use player position with offset.");
            }
        }

        // Ensure spawn point starts in correct position
        UpdateFireballSpawnPoint();
    }

    // NEW: Update fireball spawn point position dynamically
    private void UpdateFireballSpawnPoint()
    {
        if (fireballSpawnPoint != null)
        {
            // Position spawn point in front of player based on facing direction
            Vector3 baseOffset = useSpawnPointOffset ? spawnPointOffset : Vector3.zero;
            Vector3 finalOffset = new Vector3(baseOffset.x * facingDirection, baseOffset.y, baseOffset.z);
            fireballSpawnPoint.position = transform.position + finalOffset;
        }
    }

    // NEW: Validate fireball setup
    private void ValidateFireballSetup()
    {
        if (fireballPrefab == null)
        {
            Debug.LogError("Fireball prefab not assigned! Attack 4 will not work properly.");
            return;
        }

        // Check if prefab is active (it should be inactive)
        if (fireballPrefab.activeSelf)
        {
            Debug.LogWarning("Fireball prefab should be inactive in the project. Setting it inactive now.");
            fireballPrefab.SetActive(false);
        }

        // Validate fireball prefab has required components
        FireballProjectile fireballScript = fireballPrefab.GetComponent<FireballProjectile>();
        if (fireballScript == null)
        {
            Debug.LogError("Fireball prefab must have FireballProjectile script!");
        }
        
        Rigidbody2D fireballRb = fireballPrefab.GetComponent<Rigidbody2D>();
        if (fireballRb == null)
        {
            Debug.LogError("Fireball prefab must have Rigidbody2D component!");
        }
        
        CircleCollider2D fireballCollider = fireballPrefab.GetComponent<CircleCollider2D>();
        if (fireballCollider == null)
        {
            Debug.LogError("Fireball prefab must have CircleCollider2D component!");
        }
        
        Debug.Log("Fireball setup validated successfully");
    }
    #endregion

    #region State Management
    private void CheckDeathState()
    {
        if (playerHealth == null) return;

        bool wasAlive = !isDead && !isRespawning;
        bool isCurrentlyAlive = playerHealth.IsAlive();
        
        if (wasAlive && !isCurrentlyAlive)
            OnPlayerDeath();
        else if (!wasAlive && isCurrentlyAlive && !playerHealth.IsRespawning())
            OnPlayerRespawn();
        
        isDead = !isCurrentlyAlive;
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player movement COMPLETELY disabled due to death");
        
        isDead = true;
        isRespawning = true;
        
        // Complete physics lock
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        
        // Disable colliders
        SetCollidersEnabled(false);
        
        DisableAllAttackEffects();
        ResetAllStates();
        
        if (animator != null)
        {
            ResetAnimatorToDefaultState();
            StartCoroutine(TriggerDeathAnimation());
        }
    }

    private void OnPlayerRespawn()
    {
        Debug.Log("Player movement RE-ENABLED after respawn");
        
        isDead = false;
        isRespawning = false;
        
        // CRITICAL: Comprehensive state reset for consistent behavior
        PerformCompleteRespawnReset();
        
        if (animator != null)
            ResetAnimatorToDefaultState();
    }

    // NEW: Comprehensive respawn reset to ensure consistent behavior
    private void PerformCompleteRespawnReset()
    {
        Debug.Log("Performing complete respawn reset...");
        
        // Reset all state variables first
        ResetAllStates();
        
        // CRITICAL: Reset physics completely
        if (rb != null)
        {
            // Stop all movement immediately
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            
            // Restore original physics settings
            rb.bodyType = originalBodyType;
            rb.gravityScale = originalGravityScale;
            
            // Reset any accumulated forces
            rb.totalForce = Vector2.zero;
            rb.totalTorque = 0f;
            
            Debug.Log($"Physics reset: BodyType={rb.bodyType}, GravityScale={rb.gravityScale}, Velocity={rb.linearVelocity}");
        }
        
        // CRITICAL: Force grounding state reset
        ForceGroundingCheck();
        
        // Re-enable colliders
        SetCollidersEnabled(true);
        
        // Reset facing direction to default
        facingDirection = 1f;
        if (spriteRenderer != null)
            spriteRenderer.flipX = false;
        
        // Cancel any pending operations
        CancelAllInvokes();
        
        Debug.Log("Complete respawn reset finished - player should behave consistently now");
    }

    // NEW: Force a proper grounding check to prevent floating/jumping issues
    private void ForceGroundingCheck()
    {
        // Reset jumping states immediately
        isJumping = false;
        isGrounded = false;
        
        // Perform immediate ground check
        Collider2D[] groundColliders = Physics2D.OverlapBoxAll(
            transform.position + Vector3.down * 0.1f, 
            new Vector2(0.8f, 0.1f), 
            0f
        );
        
        foreach (Collider2D col in groundColliders)
        {
            if (col.CompareTag("Ground"))
            {
                isGrounded = true;
                Debug.Log("Force grounding check: Player is grounded");
                break;
            }
        }
        
        if (!isGrounded)
        {
            Debug.Log("Force grounding check: Player is not grounded");
        }
    }

    // NEW: Cancel all invokes to prevent weird behaviors
    private void CancelAllInvokes()
    {
        CancelInvoke(nameof(DisableAttackCollider));
        CancelInvoke(nameof(ResetAttackState));
        CancelInvoke(nameof(ExecuteFireballSpawn));
        CancelInvoke(nameof(ResetHurtState));
        Debug.Log("All invokes cancelled during respawn reset");
    }

    private void SetCollidersEnabled(bool enabled)
    {
        CapsuleCollider2D[] colliders = GetComponents<CapsuleCollider2D>();
        foreach (var col in colliders)
            col.enabled = enabled;
    }

    private void ResetAllStates()
    {
        isAttacking = false;
        isHurt = false;
        isRunning = false;
        isJumping = false; // CRITICAL: Always reset jumping state
        
        // NEW: Reset fireball state
        isFireballAttackPending = false;
        CancelInvoke(nameof(ExecuteFireballSpawn)); // Cancel any pending fireball spawns
        
        Debug.Log("All player states reset");
    }

    private void HandleDeadState()
    {
        // Complete movement lock
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        
        DisableAllAttackEffects();
        
        // Reset animation bools
        SetAnimatorBool("isRunning", false);
        SetAnimatorBool("isWalking", false);
        SetAnimatorBool("isJumping", false);
        SetAnimatorBool("isFalling", false);
    }

    private void DisableAllAttackEffects()
    {
        CancelInvoke(nameof(DisableAttackCollider));
        CancelInvoke(nameof(ResetAttackState));
        CancelInvoke(nameof(ExecuteFireballSpawn)); // NEW: Cancel fireball spawning
        
        if (attackCollider != null)
            attackCollider.gameObject.SetActive(false);
        
        isAttacking = false;
        isFireballAttackPending = false; // NEW: Reset fireball state
    }

    public void OnRespawnComplete()
    {
        Debug.Log("PlayerMovement: Respawn completion notification received");
        
        // ENHANCED: More thorough respawn completion handling
        isRespawning = false;
        
        // Perform complete reset again to be absolutely sure
        PerformCompleteRespawnReset();
        
        // Extra safety: Wait one frame then verify physics state
        StartCoroutine(VerifyRespawnState());
    }

    // NEW: Verify that respawn state is correct after one frame
    private IEnumerator VerifyRespawnState()
    {
        yield return new WaitForEndOfFrame();
        
        if (rb != null)
        {
            Debug.Log($"Post-respawn verification: Velocity={rb.linearVelocity}, BodyType={rb.bodyType}, GravityScale={rb.gravityScale}, Grounded={isGrounded}, Jumping={isJumping}");
            
            // Final safety check - if velocity is still weird, force reset it
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f || Mathf.Abs(rb.linearVelocity.y) > 0.1f)
            {
                Debug.LogWarning("Detected residual velocity after respawn - forcing zero");
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
    #endregion

    #region Movement Handling
    private void HandleNormalMovement()
    {
        if (isDead || isRespawning) return;

        if (isHurt)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        ProcessMovementInput();
        HandleJump();
        HandleAnimations();
    }

    private void ProcessMovementInput()
    {
        if (isDead || isRespawning) return;
        
        float moveInput = 0f;
        if (uiController != null)
        {
            if (uiController.IsMovingLeft) moveInput = -1f;
            else if (uiController.IsMovingRight) moveInput = 1f;
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        
        if (moveInput != 0 && !isAttacking && !isHurt)
        {
            facingDirection = moveInput > 0 ? 1f : -1f;
            spriteRenderer.flipX = facingDirection < 0;
        }

        isRunning = moveInput != 0 && !isAttacking && !isHurt;
    }

    private void HandleJump()
    {
        if (isDead || isRespawning) return;
        
        // ENHANCED: More strict jump conditions to prevent weird behavior
        if (uiController != null && uiController.IsJumping && isGrounded && !isAttacking && !isHurt && !isJumping)
        {
            // CRITICAL: Use consistent jump force every time
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = true;
            
            SetAnimatorBool("isJumping", true);
            uiController.ResetJump();
            
            Debug.Log($"Jump executed: JumpForce={jumpForce}, NewVelocity={rb.linearVelocity}");
        }
    }

    private void HandleAnimations()
    {
        if (isDead || isRespawning) return;
        
        SetAnimatorBool("isRunning", isRunning);
        SetAnimatorBool("isWalking", false);

        if (rb.linearVelocity.y < 0 && !isGrounded)
            SetAnimatorBool("isFalling", true);
        else
            SetAnimatorBool("isFalling", false);
    }

    private void HandleIntroWalking()
    {
        if (isDead || isRespawning) return;
        
        spriteRenderer.flipX = false;
        facingDirection = 1f;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        if (!stateInfo.IsName("Walk") && !stateInfo.IsName("RoranWalk"))
        {
            SetAnimatorBool("isWalking", true);
        }
        
        SetAnimatorBool("isRunning", false);
        SetAnimatorBool("isWalking", true);
    }
    #endregion

    #region Attack System
    public void TriggerAttack(int attackIndex)
    {
        if (isIntroWalking || isAttacking || isDead || isHurt || isRespawning) 
        {
            if (isDead || isRespawning) 
                Debug.Log("Attack BLOCKED - Player is dead or respawning");
            return;
        }

        if (!IsValidAttackIndex(attackIndex)) return;

        string attackTrigger = $"Attack{attackIndex}";
        
        if (HasAnimatorParameter(attackTrigger, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(attackTrigger);
            isAttacking = true;
            
            // NEW: Handle Attack 4 (Fireball) with delayed execution
            if (attackIndex == 4)
            {
                ScheduleFireballAttack(); // NEW: Schedule fireball instead of immediate spawn
            }
            else
            {
                SetupAttackCollider(attackIndex);
            }
            
            float duration = GetAttackAnimationDuration(attackIndex);
            Invoke(nameof(ResetAttackState), duration);
        }
        else
        {
            Debug.LogError($"{attackTrigger} trigger not found in animator");
        }
    }

    // NEW: Schedule fireball attack to execute after animation delay
    private void ScheduleFireballAttack()
    {
        if (isDead || isRespawning) 
        {
            Debug.Log("Fireball attack BLOCKED - Player is dead or respawning");
            return;
        }

        isFireballAttackPending = true;
        
        // Schedule the fireball to spawn after the specified delay
        Invoke(nameof(ExecuteFireballSpawn), fireballSpawnDelay);
        
        Debug.Log($"Fireball attack scheduled to execute in {fireballSpawnDelay} seconds");
    }

    // NEW: Execute the actual fireball spawn (called after delay)
    private void ExecuteFireballSpawn()
    {
        if (!isFireballAttackPending || isDead || isRespawning || fireballPrefab == null) 
        {
            if (isDead || isRespawning) 
                Debug.Log("Fireball spawn BLOCKED - Player is dead or respawning");
            else if (!isFireballAttackPending)
                Debug.Log("Fireball spawn BLOCKED - Attack was cancelled");
            else
                Debug.LogError("Fireball spawn BLOCKED - Fireball prefab is null!");
            
            isFireballAttackPending = false;
            return;
        }

        var attack = GetAttackData(4); // Attack 4 data
        if (attack == null) 
        {
            Debug.LogError("Attack 4 data not found!");
            isFireballAttackPending = false;
            return;
        }

        // Calculate spawn position
        Vector3 spawnPosition = CalculateFireballSpawnPosition();

        // Calculate direction (capture current facing direction at spawn time)
        Vector2 fireballDirection = new Vector2(facingDirection, 0);

        Debug.Log($"Executing fireball spawn at position: {spawnPosition} with direction: {fireballDirection}");

        // Instantiate fireball
        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
        
        if (fireball == null)
        {
            Debug.LogError("Failed to instantiate fireball!");
            isFireballAttackPending = false;
            return;
        }

        // Ensure fireball is active
        fireball.SetActive(true);

        // Set scale
        fireball.transform.localScale = fireballScale;

        // Initialize fireball
        FireballProjectile fireballScript = fireball.GetComponent<FireballProjectile>();
        if (fireballScript != null)
        {
            // NEW: Pass the facing direction to fireball for proper flipping
            fireballScript.Initialize(attack.damage, fireballDirection, facingDirection);
            Debug.Log($"Fireball successfully spawned with {attack.damage} damage in direction {fireballDirection} at scale {fireballScale} facing {facingDirection}");
        }
        else
        {
            Debug.LogError("FireballProjectile script not found on instantiated fireball!");
            Destroy(fireball);
        }
        
        // Reset fireball state
        isFireballAttackPending = false;
    }

    // NEW: Calculate proper fireball spawn position
    private Vector3 CalculateFireballSpawnPosition()
    {
        Vector3 spawnPosition;

        if (fireballSpawnPoint != null)
        {
            // Use the spawn point (which is already positioned correctly by UpdateFireballSpawnPoint)
            spawnPosition = fireballSpawnPoint.position;
            Debug.Log($"Using spawn point position: {spawnPosition}");
        }
        else
        {
            // Use player position with offset
            Vector3 adjustedOffset = new Vector3(
                fireballSpawnOffset.x * facingDirection, 
                fireballSpawnOffset.y, 
                fireballSpawnOffset.z
            );
            spawnPosition = transform.position + adjustedOffset;
            Debug.Log($"Using player position with offset: {spawnPosition} (offset: {adjustedOffset})");
        }

        return spawnPosition;
    }

    private void SetupAttackCollider(int attackIndex)
    {
        if (isDead || isRespawning || attackCollider == null || playerAttackCollider == null) 
        {
            if (isDead || isRespawning) 
                Debug.Log("Attack collider setup BLOCKED - Player is dead or respawning");
            return;
        }

        var attack = GetAttackData(attackIndex);
        if (attack == null) return;

        // Clear previous state
        DisableAttackCollider();
        
        // Setup new attack
        attackCollider.gameObject.SetActive(true);
        playerAttackCollider.SetDamage(attack.damage);
        
        // Position collider
        Vector3 attackPosition = transform.position + new Vector3(facingDirection * attack.range, 0, 0);
        attackCollider.transform.position = attackPosition;
        
        // Set radius directly
        attackCollider.radius = attack.ColliderRadius;
        
        // Disable after active time
        Invoke(nameof(DisableAttackCollider), attack.activeTime);
        
        Debug.Log($"Attack {attackIndex} setup: Position={attackPosition}, Radius={attack.ColliderRadius}, Damage={attack.damage}");
    }

    private void DisableAttackCollider()
    {
        if (attackCollider != null)
        {
            attackCollider.gameObject.SetActive(false);
        }
    }

    private void ResetAttackState()
    {
        isAttacking = false;
        DisableAttackCollider();
        
        // NOTE: We don't reset isFireballAttackPending here because the fireball might still need to spawn
        // The fireball state is managed separately in ExecuteFireballSpawn()
    }

    private bool IsValidAttackIndex(int attackIndex)
    {
        return attackIndex >= 1 && attackIndex <= attackData.Length;
    }

    private PlayerAttackData GetAttackData(int attackIndex)
    {
        if (!IsValidAttackIndex(attackIndex)) return null;
        return attackData[attackIndex - 1];
    }

    private float GetAttackAnimationDuration(int attackIndex)
    {
        var attack = GetAttackData(attackIndex);
        if (attack == null) return 0.5f;
        
        return attack.animationDuration / (uiController?.CharacterSpeedMultiplier ?? 1f);
    }
    #endregion

    #region Animation Helper Methods
    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType paramType)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == paramType)
                return true;
        }
        return false;
    }

    private void SetAnimatorBool(string paramName, bool value)
    {
        if (HasAnimatorParameter(paramName, AnimatorControllerParameterType.Bool))
            animator.SetBool(paramName, value);
    }

    private void ResetAnimatorToDefaultState()
    {
        if (animator == null) return;

        SetAnimatorBool("isRunning", false);
        SetAnimatorBool("isWalking", false);
        SetAnimatorBool("isJumping", false);
        SetAnimatorBool("isFalling", false);
        
        string[] triggers = {"Attack1", "Attack2", "Attack3", "Attack4", "Attack5", "Hurt", "Die"};
        foreach (string trigger in triggers)
        {
            if (HasAnimatorParameter(trigger, AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger(trigger);
        }
        
        animator.Update(0f);
        
        Debug.Log("Animator reset to default state");
    }

    private IEnumerator TriggerDeathAnimation()
    {
        yield return null;
        
        if (HasAnimatorParameter("Die", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Die");
            Debug.Log("Death animation triggered");
        }
    }
    #endregion

    #region Collision Handling
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || isRespawning) return;
        
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            isJumping = false;
            if (!isIntroWalking)
            {
                SetAnimatorBool("isJumping", false);
                SetAnimatorBool("isFalling", false);
            }
            
            // NEW: Debug log for grounding
            Debug.Log($"Player grounded: isGrounded={isGrounded}, isJumping={isJumping}");
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (isDead || isRespawning) return;
        
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            Debug.Log($"Player left ground: isGrounded={isGrounded}");
        }
    }
    #endregion

    #region Special States
    public void TriggerHurt()
    {
        if (isDead || isRespawning) return;
        
        isHurt = true;
        
        if (HasAnimatorParameter("Hurt", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Hurt");
        }
        
        Invoke(nameof(ResetHurtState), 0.5f);
    }

    private void ResetHurtState()
    {
        isHurt = false;
    }

    public void TriggerDeath()
    {
        if (isDead) return;
        
        isDead = true;
        isRespawning = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        
        DisableAllAttackEffects();
        ResetAllStates();
        
        if (animator != null)
        {
            ResetAnimatorToDefaultState();
            
            if (HasAnimatorParameter("Die", AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger("Die");
            }
        }
    }

    public void SetIntroWalking(bool walking)
    {
        if (isDead || isRespawning) return;
        
        isIntroWalking = walking;
        
        if (walking)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            ResetAnimatorToDefaultState();
            StartCoroutine(StartWalkSequence());
        }
        else
        {
            SetAnimatorBool("isWalking", false);
            ResetAnimatorToDefaultState();
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    private IEnumerator StartWalkSequence()
    {
        yield return new WaitForFixedUpdate();
        SetAnimatorBool("isWalking", true);
    }
    #endregion

    #region Public Properties and Methods
    public bool CanPerformAttack()
    {
        return !isAttacking && !isDead && !isHurt && isGrounded && !isJumping && !isIntroWalking && !isRespawning;
    }

    public bool IsAlive() => !isDead && !isRespawning;
    public int GetCurrentHealth() => playerHealth?.GetCurrentHealth() ?? 0;
    public int GetMaxHealth() => playerHealth?.GetMaxHealth() ?? 100;
    public float GetFacingDirection() => facingDirection;
    public float GetWalkSpeed() => walkSpeed; // ADDED: Missing method for IntroSequence
    #endregion
}