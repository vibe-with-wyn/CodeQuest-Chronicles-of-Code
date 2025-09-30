﻿using UnityEngine;

public class PlayerAttackCollider : MonoBehaviour
{
    private int damage;
    private bool hasHit = false;

    void OnEnable()
    {
        hasHit = false;
        Debug.Log($"Player attack collider enabled with damage: {damage}");
    }

    public void SetDamage(int damageValue)
    {
        damage = damageValue;
        hasHit = false;
        Debug.Log($"Player attack collider damage set to: {damage}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return; // Prevent multiple hits from same attack

        Debug.Log($"Player attack collider detected: {other.name} with tag: {other.tag}");

        if (other.CompareTag("Enemy"))
        {
            // CRITICAL: Check if we hit the actual enemy body, not just the detection zone
            if (IsValidEnemyBodyHit(other))
            {
                EnemyAI enemyAI = other.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.TakeDamage(damage);
                    hasHit = true;
                    Debug.Log($"Player dealt {damage} damage to enemy {other.name} - HIT CONFIRMED ON BODY");
                    ShowDamageEffect(other.transform.position);
                }
                else
                {
                    Debug.LogWarning($"Enemy {other.name} does not have EnemyAI component!");
                }
            }
            else
            {
                Debug.Log($"Player attack hit {other.name} but it's not the enemy body (likely detection zone) - DAMAGE IGNORED");
            }
        }
    }

    /// <summary>
    /// Check if the collider we hit is actually the enemy's body (not detection zone)
    /// </summary>
    private bool IsValidEnemyBodyHit(Collider2D hitCollider)
    {
        // Method 1: Check through EnemyAI component
        EnemyAI enemyAI = hitCollider.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            bool isBodyHit = enemyAI.IsBodyCollider(hitCollider);
            bool isDetectionHit = enemyAI.IsDetectionCollider(hitCollider);
            
            Debug.Log($"Collision analysis - Body: {isBodyHit}, Detection: {isDetectionHit}");
            
            if (isBodyHit)
            {
                Debug.Log("✓ Hit confirmed: Enemy body collider");
                return true;
            }
            else if (isDetectionHit)
            {
                Debug.Log("✗ Hit rejected: Enemy detection collider");
                return false;
            }
        }

        // Method 2: Check by collider type (fallback)
        if (hitCollider is CapsuleCollider2D)
        {
            Debug.Log("✓ Hit confirmed: CapsuleCollider2D (likely enemy body)");
            return true;
        }
        else if (hitCollider is CircleCollider2D circleCol && circleCol.isTrigger)
        {
            Debug.Log("✗ Hit rejected: CircleCollider2D with isTrigger=true (likely detection zone)");
            return false;
        }

        // Method 3: Check by GameObject name (additional fallback)
        if (hitCollider.gameObject.name.ToLower().Contains("detection") || 
            hitCollider.gameObject.name.ToLower().Contains("trigger"))
        {
            Debug.Log("✗ Hit rejected: GameObject name suggests detection/trigger collider");
            return false;
        }

        // Default: allow hit but warn
        Debug.LogWarning($"Unable to determine collider type for {hitCollider.name}, allowing hit as fallback");
        return true;
    }

    void OnDisable()
    {
        Debug.Log("Player attack collider disabled");
    }

    private void ShowDamageEffect(Vector3 position)
    {
        Debug.Log($"Player damage effect at position: {position}");
        // You can add particle effects or damage numbers here
    }
}