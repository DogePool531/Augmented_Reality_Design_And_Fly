using UnityEngine;

/// <summary>
/// TailManipulator reads positional data from another script (like TailReadout)
/// and provides functions to scale, stretch, or taper a mesh based on that data.
/// </summary>
public class TailManipulator : MonoBehaviour
{
    // Reference to the script that stores the tail positions
    public Transform tailGrabPos;
    public Vector3 Scale3D;
    public float xScale;
    public float yScale;
    public float zScale;

    // Cached tail positions (these are read from the other script)
    private Vector3 tailBasePos;
    private Vector3 tailtipPos;
    private Vector3 StartPosition;
    // Reference to the mesh you want to manipulate
    public MeshFilter meshFilter;

    void Start()
    {   
        StartPosition = tailGrabPos.localPosition;
    }
    void Update()
    {
        // Access the TailReadout component
        // Read its public Vector3 fields (tailBasePos, tailtipPos)
        if (tailGrabPos == null) return;

        tailtipPos = tailGrabPos.localPosition;

        // Calculate 3D scaling based on tail tip position
        
        xScale = 1.5f * (tailtipPos.x/StartPosition.x - 1f) + 1f;
        zScale = 1.5f * (tailtipPos.z/StartPosition.z - 1f) + 1f;
        yScale = 0.5f * (tailtipPos.y/StartPosition.y - 1f) + 1f ;
        
        Scale3D = new Vector3(xScale, yScale, zScale);
        transform.localScale = Scale3D;
    }

    // ----------------------------
    // Example Function Concepts
    // ----------------------------

    // 1. Uniform scaling
    // Scales the entire mesh uniformly around its origin or pivot.
    // Typically done by modifying transform.localScale.
    void UniformScale(float scaleFactor)
    {
        // Example: transform.localScale = Vector3.one * scaleFactor;
        // Comment: Useful when you want to grow or shrink the entire tail equally.
    }

    // 2. Directional stretching
    // Extends the mesh along a direction vector (like from base to tip).
    void StretchAlong(Vector3 direction, float amount)
    {
        // Example: transform.localScale += direction.normalized * amount;
        // Comment: Can simulate length stretch (longer tail).
    }

    // 3. Tapering
    // Gradually reduces mesh scale toward one end (e.g., the tail tip).
    void ApplyTaper(float taperFactor)
    {
        // Example: modify mesh vertices by scaling their local x/y relative to position.z
        // Comment: Used for aerodynamic shaping — thinner at the tip, thicker at the root.
    }

    // 4. Resetting transformations
    void ResetMesh()
    {
        // Example: transform.localScale = Vector3.one;
        // Comment: Handy for returning the mesh to its default proportions.
    }
}