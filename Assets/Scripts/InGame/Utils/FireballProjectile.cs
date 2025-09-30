using UnityEngine;
using System.Collections;

public class FireballProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float explosionAnimationDuration = 0.5f;
    
    [Header("Visual Settings")] // NEW: Visual settings
    [SerializeField] private bool useScaleFlipping = true; // Use scale to flip instead of SpriteRenderer
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private int damage;
    private Vector2 direction;
    private Vector2 startPosition;
    private Rigidbody2D rb;
    private CircleCollider2D projectileCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer; // NEW: For sprite flipping
    private bool hasHit = false;
    private bool hasExploded = false;
    private bool isInitialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<CircleCollider2D>();
        
        // Find animator in child object
        animator = GetComponentInChildren<Animator>();
        
        // NEW: Find sprite renderer (could be on this object or child)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (animator == null)
        {
            Debug.LogError("Animator not found in Fireball or its children!");
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning("SpriteRenderer not found in Fireball or its children! Visual flipping may not work.");
        }

        // Ensure collider is set as trigger
        if (projectileCollider != null)
        {
            projectileCollider.isTrigger = true;
        }

        if (enableDebugLogs)
            Debug.Log("Fireball Awake completed");
    }

    void Start()
    {
        if (!isInitialized)
        {
            Debug.LogError("Fireball Start() called but Initialize() was never called!");
            Destroy(gameObject);
            return;
        }

        startPosition = transform.position;
        
        // Set velocity for movement
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
            if (enableDebugLogs)
                Debug.Log($"Fireball velocity set to: {direction * speed}");
        }
        
        // Auto-destroy after lifetime as safety net
        Destroy(gameObject, lifeTime);
        
        if (enableDebugLogs)
            Debug.Log($"Fireball projectile launched with damage: {damage}, direction: {direction}, position: {transform.position}");
    }

    void Update()
    {
        if (!isInitialized || hasExploded) return;

        // Check if fireball has traveled max distance
        float distanceTraveled = Vector2.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            if (enableDebugLogs)
                Debug.Log($"Fireball reached max distance ({distanceTraveled:F2}/{maxDistance}) - exploding");
            ExplodeFireball();
        }
    }

    public void Initialize(int projectileDamage, Vector2 projectileDirection, float playerFacingDirection = 1f)
    {
        damage = projectileDamage;
        direction = projectileDirection.normalized;
        hasHit = false;
        hasExploded = false;
        isInitialized = true;
        
        // NEW: Set visual orientation based on player facing direction
        SetVisualOrientation(playerFacingDirection);
        
        // Ensure the fireball is active
        gameObject.SetActive(true);
        
        if (enableDebugLogs)
            Debug.Log($"Fireball initialized: Damage={damage}, Direction={direction}, FacingDirection={playerFacingDirection}, Position={transform.position}");
    }

    // NEW: Set the visual orientation of the fireball
    private void SetVisualOrientation(float facingDirection)
    {
        if (useScaleFlipping)
        {
            // Method 1: Use scale flipping (recommended for most cases)
            Vector3 currentScale = transform.localScale;
            if (facingDirection < 0)
            {
                // Player facing left - flip fireball horizontally
                currentScale.x = Mathf.Abs(currentScale.x) * -1f;
            }
            else
            {
                // Player facing right - normal orientation
                currentScale.x = Mathf.Abs(currentScale.x);
            }
            transform.localScale = currentScale;
            
            if (enableDebugLogs)
                Debug.Log($"Fireball scale set to: {currentScale} (facing: {facingDirection})");
        }
        else if (spriteRenderer != null)
        {
            // Method 2: Use SpriteRenderer flipX (alternative method)
            spriteRenderer.flipX = facingDirection < 0;
            
            if (enableDebugLogs)
                Debug.Log($"Fireball sprite flipX set to: {spriteRenderer.flipX} (facing: {facingDirection})");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("Cannot set fireball orientation - no SpriteRenderer found and scale flipping disabled");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || hasExploded || !isInitialized) return;

        if (enableDebugLogs)
            Debug.Log($"Fireball collision detected with: {other.name} (tag: {other.tag}) on GameObject: {other.gameObject.name}");

        // Hit enemy
        if (other.CompareTag("Enemy"))
        {
            if (IsValidEnemyBodyHit(other))
            {
                EnemyAI enemyAI = other.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.TakeDamage(damage);
                    hasHit = true;
                    if (enableDebugLogs)
                        Debug.Log($"Fireball dealt {damage} damage to enemy {other.name}");
                    ExplodeFireball();
                    return;
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"Enemy {other.name} has no EnemyAI component!");
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"Fireball hit enemy detection zone, not body - continuing flight");
                return; // Don't explode on detection zones
            }
        }
        // Hit ground or obstacles
        else if (other.CompareTag("Ground") || other.CompareTag("Wall") || other.CompareTag("Platform"))
        {
            if (enableDebugLogs)
                Debug.Log($"Fireball hit {other.tag} ({other.name}) - exploding");
            ExplodeFireball();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"Fireball hit {other.tag} ({other.name}) - ignoring collision");
        }
    }

    private bool IsValidEnemyBodyHit(Collider2D hitCollider)
    {
        // Same logic as PlayerAttackCollider for consistency
        EnemyAI enemyAI = hitCollider.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            bool isBodyHit = enemyAI.IsBodyCollider(hitCollider);
            bool isDetectionHit = enemyAI.IsDetectionCollider(hitCollider);
            
            if (isBodyHit)
            {
                if (enableDebugLogs)
                    Debug.Log("✓ Fireball hit confirmed: Enemy body collider");
                return true;
            }
            else if (isDetectionHit)
            {
                if (enableDebugLogs)
                    Debug.Log("✗ Fireball hit rejected: Enemy detection collider");
                return false;
            }
        }

        // Fallback logic
        if (hitCollider is CapsuleCollider2D)
        {
            if (enableDebugLogs)
                Debug.Log("✓ Fireball hit confirmed: CapsuleCollider2D (assumed enemy body)");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            if (enableDebugLogs)
                Debug.Log("✗ Fireball hit rejected: CircleCollider2D trigger (assumed detection zone)");
            return false;
        }

        // Default allow hit
        if (enableDebugLogs)
            Debug.Log("✓ Fireball hit allowed: Unknown collider type, defaulting to allow");
        return true;
    }

    private void ExplodeFireball()
    {
        if (hasExploded) return;
        
        hasExploded = true;
        
        if (enableDebugLogs)
            Debug.Log("Fireball exploding...");
        
        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        // Disable collider to prevent multiple hits
        if (projectileCollider != null)
        {
            projectileCollider.enabled = false;
        }
        
        // Trigger explosion animation
        if (animator != null && HasAnimatorParameter("Hit", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Hit");
            if (enableDebugLogs)
                Debug.Log("Fireball explosion animation triggered");
            
            // Destroy after explosion animation
            StartCoroutine(DestroyAfterExplosion());
        }
        else
        {
            // No explosion animation, destroy immediately
            if (enableDebugLogs)
                Debug.LogWarning("Hit trigger not found in fireball animator - destroying immediately");
            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterExplosion()
    {
        // Wait for explosion animation to complete
        yield return new WaitForSeconds(explosionAnimationDuration);
        
        if (enableDebugLogs)
            Debug.Log("Fireball destroyed after explosion animation");
        
        Destroy(gameObject);
    }

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

    void OnDestroy()
    {
        if (enableDebugLogs)
            Debug.Log($"Fireball projectile destroyed at position: {transform.position}");
    }

    void OnDrawGizmosSelected()
    {
        // Visualize max distance in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
        
        // Visualize direction
        if (isInitialized)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
    }
}
