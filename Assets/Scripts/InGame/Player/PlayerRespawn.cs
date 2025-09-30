using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 spawnPoint;

    void Start()
    {
        spawnPoint = transform.position;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Death"))
        {
            transform.position = spawnPoint;
            GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }
    }
}