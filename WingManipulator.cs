using UnityEngine;

/// <summary>
/// WingManipulator reads positional data from another script (like WingReadout)
/// and provides functions to scale, stretch, or taper a mesh based on that data.
/// </summary>
public class WingManipulator : MonoBehaviour
{
    // Reference to the script that stores the wing positions
    public MonoBehaviour wingReadoutScript;
    public Vector3 Stretch;
    public float xScale;
    // (At runtime, this would be cast to your actual readout type, e.g., WingReadout)

    // Cached wing positions (these are read from the other script)
    private Vector3 wingBasePos;
    private Vector3 wingtipPos;

    // Reference to the mesh you want to manipulate
    public MeshFilter meshFilter;

    void Update()
    {
        // Here you'd typically:
        // 1. Access the WingReadout component
        // 2. Read its public Vector3 fields (wingBasePos, wingtipPos)
        var readout = wingReadoutScript as WingPositionReader;
        if (readout == null) return;

        wingBasePos = readout.wingBasePos;
        wingtipPos = readout.wingtipPos;


        xScale = -10f * wingtipPos.x;
        float zScale = -15f * wingtipPos.z;
        Stretch = new Vector3(xScale, zScale, zScale);
        transform.localScale = Stretch;
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
        // Comment: Useful when you want to grow or shrink the entire wing equally.
    }

    // 2. Directional stretching
    // Extends the mesh along a direction vector (like from base to tip).
    void StretchAlong(Vector3 direction, float amount)
    {
        // Example: transform.localScale += direction.normalized * amount;
        // Comment: Can simulate spanwise stretch (longer wings).
    }

    // 3. Tapering
    // Gradually reduces mesh scale toward one end (e.g., the wingtip).
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
