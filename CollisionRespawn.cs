using UnityEngine;

/// <summary>
/// Attach this to your spawned asset prefab.
/// Detects collision with helicopter, destroys itself, and spawns a new one.
/// </summary>
public class CollisionRespawn : MonoBehaviour
{
    [Header("Collision Detection")]
    [SerializeField]
    [Tooltip("Tag of the helicopter (e.g., 'Helicopter' or 'Player')")]
    private string helicopterTag = "Helicopter";
    
    [Header("References")]
    [SerializeField]
    [Tooltip("Will auto-find if not assigned")]
    private RoomMeshAssetSpawner spawner;
    
    [Header("Feedback (Optional)")]
    [SerializeField]
    [Tooltip("Particle effect to play on collision")]
    private GameObject collisionEffect;
    
    [SerializeField]
    [Tooltip("Sound to play on collision")]
    private AudioClip collisionSound;
    
    [SerializeField]
    [Tooltip("Delay before destroying (allows effects to play)")]
    private float destroyDelay = 0.1f;
    
    private AudioSource audioSource;
    private bool hasCollided = false;
    
    void Start()
    {
        // Find spawner if not assigned
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<RoomMeshAssetSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning($"[{nameof(CollisionRespawn)}] RoomMeshAssetSpawner not found!");
            }
        }
        
        // Setup audio
        if (collisionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
        
        // Ensure we have a collider
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"[{nameof(CollisionRespawn)}] No collider found! Adding BoxCollider.");
            gameObject.AddComponent<BoxCollider>();
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }
    
    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }
    
    private void HandleCollision(GameObject collidedObject)
    {
        // Prevent multiple triggers
        if (hasCollided) return;
        
        // Check if it's the helicopter
        if (collidedObject.CompareTag(helicopterTag))
        {
            hasCollided = true;
            
            Debug.Log($"[{nameof(CollisionRespawn)}] Hit helicopter! Respawning...");
            
            // Play effects
            PlayCollisionEffects();
            
            // Spawn a new object
            if (spawner != null)
            {
                spawner.SpawnAssetInView();
            }
            
            // Destroy this object
            Destroy(gameObject, destroyDelay);
        }
    }
    
    private void PlayCollisionEffects()
    {
        // Spawn particle effect
        if (collisionEffect != null)
        {
            Instantiate(collisionEffect, transform.position, Quaternion.identity);
        }
        
        // Play sound
        if (collisionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collisionSound);
        }
    }
}