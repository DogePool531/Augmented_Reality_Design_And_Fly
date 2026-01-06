using UnityEngine;
using Meta.XR.BuildingBlocks;
using Oculus.Interaction;

public class RoomMeshAssetSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject assetPrefab;
    [SerializeField] private int maxAttempts = 50;
    [SerializeField] private float minDistanceFromSurface = 0.3f;
    [SerializeField] private float minDistanceFromUser = 1.0f;
    [SerializeField] private float maxDistanceFromUser = 5.0f;
    
    [Header("View Settings")]
    [SerializeField] private float viewAngle = 60f; // Field of view cone
    
    [Header("Poke Button (Optional)")]
    [SerializeField]
    [Tooltip("Optional: Assign a PokeInteractable to trigger spawning")]
    private PokeInteractable pokeButton;
    
    private RoomMeshEvent roomMeshEvent;
    private MeshFilter roomMeshFilter;
    private Mesh roomMesh;
    private Transform roomMeshTransform;
    private Camera mainCamera;
    private bool meshReady = false;
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // Find and subscribe to room mesh event
        roomMeshEvent = FindFirstObjectByType<RoomMeshEvent>();
        if (roomMeshEvent != null)
        {
            roomMeshEvent.OnRoomMeshLoadCompleted.AddListener(OnRoomMeshLoaded);
            Debug.Log("Subscribed to room mesh load event");
        }
        else
        {
            Debug.LogError("RoomMeshEvent not found in scene!");
        }
        
        // Connect to poke button if assigned
        if (pokeButton != null)
        {
            pokeButton.WhenPointerEventRaised += HandlePokeEvent;
            Debug.Log("Connected to poke button - ready to spawn on poke!");
        }
    }
    
    private void OnRoomMeshLoaded(MeshFilter meshFilter)
    {
        roomMeshFilter = meshFilter;
        roomMesh = meshFilter.sharedMesh;
        roomMeshTransform = meshFilter.transform;
        meshReady = true;
        
        Debug.Log($"Room mesh loaded with {roomMesh.vertexCount} vertices");
    }
    
    private void HandlePokeEvent(PointerEvent pointerEvent)
    {
        // Only spawn when button is actually pressed (not just hovered)
        if (pointerEvent.Type == PointerEventType.Select)
        {
            SpawnAssetInView();
        }
    }
    
    public GameObject SpawnAssetInView()
    {
        if (!meshReady)
        {
            Debug.LogWarning("Room mesh not ready yet!");
            return null;
        }
        
        if (assetPrefab == null)
        {
            Debug.LogError("Asset prefab not assigned!");
            return null;
        }
        
        // Try multiple times to find a valid position
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 spawnPos = GenerateRandomPositionInView();
            
            if (IsValidSpawnPosition(spawnPos))
            {
                GameObject spawnedAsset = Instantiate(assetPrefab, spawnPos, Quaternion.identity);
                
                // Optional: make it face the user
                Vector3 directionToUser = mainCamera.transform.position - spawnPos;
                directionToUser.y = 0;
                if (directionToUser != Vector3.zero)
                {
                    spawnedAsset.transform.rotation = Quaternion.LookRotation(directionToUser);
                }
                
                Debug.Log($"Asset spawned at {spawnPos} on attempt {i + 1}");
                return spawnedAsset;
            }
        }
        
        Debug.LogWarning($"Could not find valid spawn position after {maxAttempts} attempts");
        return null;
    }
    
    private Vector3 GenerateRandomPositionInView()
    {
        // Get camera position and forward direction
        Vector3 camPos = mainCamera.transform.position;
        
        // Random distance from user
        float distance = Random.Range(minDistanceFromUser, maxDistanceFromUser);
        
        // Random angle within view cone
        float horizontalAngle = Random.Range(-viewAngle / 2f, viewAngle / 2f);
        float verticalAngle = Random.Range(-viewAngle / 2f, viewAngle / 2f);
        
        // Calculate direction
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
        Vector3 direction = mainCamera.transform.rotation * rotation * Vector3.forward;
        
        // Calculate position
        return camPos + direction * distance;
    }
    
    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Check if position is in camera view
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(position);
        if (viewportPoint.z <= 0 || viewportPoint.x < 0 || viewportPoint.x > 1 || 
            viewportPoint.y < 0 || viewportPoint.y > 1)
        {
            return false;
        }
        
        // Check distance from user
        float distToUser = Vector3.Distance(position, mainCamera.transform.position);
        if (distToUser < minDistanceFromUser || distToUser > maxDistanceFromUser)
        {
            return false;
        }
        
        // Check if position is inside the room mesh volume
        if (!IsPointInsideMesh(position))
        {
            return false;
        }
        
        // Check if there's clear line of sight from camera
        RaycastHit hit;
        Vector3 directionToPos = position - mainCamera.transform.position;
        if (Physics.Raycast(mainCamera.transform.position, directionToPos.normalized, 
            out hit, directionToPos.magnitude))
        {
            // Only block if we hit something that's not the room mesh itself
            if (hit.collider.GetComponent<MeshFilter>() != roomMeshFilter)
            {
                return false;
            }
        }
        
        // Check minimum distance from surfaces
        if (!HasMinDistanceFromSurfaces(position))
        {
            return false;
        }
        
        return true;
    }
    
    private bool IsPointInsideMesh(Vector3 worldPoint)
    {
        // Convert world point to local space of the mesh
        Vector3 localPoint = roomMeshTransform.InverseTransformPoint(worldPoint);
        
        // Simple bounds check first
        if (!roomMesh.bounds.Contains(localPoint))
        {
            return false;
        }
        
        // Ray casting method: cast rays in multiple directions
        // If odd number of intersections, point is inside
        int intersectionCount = 0;
        Vector3[] directions = new Vector3[]
        {
            Vector3.right,
            Vector3.left,
            Vector3.up,
            Vector3.down,
            Vector3.forward,
            Vector3.back
        };
        
        foreach (Vector3 dir in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(worldPoint, dir, out hit, 100f))
            {
                // Check if we hit the room mesh
                if (hit.collider.GetComponent<MeshFilter>() == roomMeshFilter)
                {
                    intersectionCount++;
                }
            }
        }
        
        // If majority of rays hit something, we're likely inside
        return intersectionCount >= 3;
    }
    
    private bool HasMinDistanceFromSurfaces(Vector3 position)
    {
        // Cast rays in all directions to find nearest surface
        Vector3[] directions = new Vector3[]
        {
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back,
            new Vector3(1, 1, 0).normalized,
            new Vector3(-1, 1, 0).normalized,
            new Vector3(1, -1, 0).normalized,
            new Vector3(-1, -1, 0).normalized
        };
        
        foreach (Vector3 dir in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(position, dir, out hit, minDistanceFromSurface))
            {
                if (hit.collider.GetComponent<MeshFilter>() == roomMeshFilter)
                {
                    return false; // Too close to a surface
                }
            }
        }
        
        return true;
    }
    
    // Public methods for easy triggering
    public void SpawnOnClick()
    {
        SpawnAssetInView();
    }
    
    public void SpawnMultiple(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnAssetInView();
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from event
        if (roomMeshEvent != null)
        {
            roomMeshEvent.OnRoomMeshLoadCompleted.RemoveListener(OnRoomMeshLoaded);
        }
        
        // Unsubscribe from poke button
        if (pokeButton != null)
        {
            pokeButton.WhenPointerEventRaised -= HandlePokeEvent;
        }
    }
}