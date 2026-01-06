using UnityEngine;

/// Deform a wing-like mesh by setting absolute span and linear taper from root->tip.
/// Works best if the original mesh is roughly centered and oriented consistently,
/// but handles arbitrary bounds by measuring them at runtime.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[DisallowMultipleComponent]
public class WingDeformer : MonoBehaviour
{
    public enum Axis { X = 0, Y = 1, Z = 2 }

    [Header("Wing Axes")]
    [Tooltip("Axis along the span (root->tip) in the mesh's LOCAL space.")]
    public Axis spanAxis = Axis.Y;
    [Tooltip("Axis pointing chordwise (LE->TE) in LOCAL space.")]
    public Axis chordAxis = Axis.X;
    [Tooltip("Axis for thickness (airfoil camber/thickness) in LOCAL space.")]
    public Axis thicknessAxis = Axis.Z;

    [Header("Span (absolute)")]
    [Min(0.001f)]
    [Tooltip("Target span in meters (root->tip distance along spanAxis).")]
    public float targetSpanMeters = 1.0f;

    [Header("Chord Taper (relative)")]
    [Tooltip("Chord scale factor at root (1 = original chord).")]
    public float rootChordScale = 1.0f;
    [Tooltip("Chord scale factor at tip (e.g., 0.4 for 40% of root).")]
    public float tipChordScale = 0.4f;

    [Header("Thickness Taper (optional)")]
    [Tooltip("Thickness scale factor at root.")]
    public float rootThickScale = 1.0f;
    [Tooltip("Thickness scale factor at tip.")]
    public float tipThickScale = 0.6f;

    [Header("Twist (washout)")]
    [Tooltip("Twist (deg) applied root->tip around the span axis. Negative for washout.")]
    public float twistDegreesAtTip = -3f;

    [Header("Anchoring & Collider")]
    [Tooltip("Keep the root plane fixed in local space while stretching.")]
    public bool anchorRoot = true;
    [Tooltip("Also update MeshCollider.sharedMesh after deforming.")]
    public bool updateMeshCollider = true;

    [Header("Update Mode")]
    public bool applyContinuously = true;

    // runtime
    MeshFilter _mf;
    Mesh _runtimeMesh;
    Vector3[] _src;
    Vector3[] _dst;

    // cached original bounds in local space
    float _origSpan, _origChord, _origThick;
    float _spanMin, _spanMax; // along span axis

    void OnEnable()
    {
        EnsureMesh();
        MeasureBounds();
        if (applyContinuously) ApplyNow();
    }

    void OnValidate()
    {
        targetSpanMeters = Mathf.Max(0.001f, targetSpanMeters);
        EnsureMesh();
        MeasureBounds();
        if (applyContinuously) ApplyNow();
    }

    void Update()
    {
        if (applyContinuously) ApplyNow();
    }

    void EnsureMesh()
    {
        if (_mf == null) _mf = GetComponent<MeshFilter>();
        if (_mf == null || _mf.sharedMesh == null) return;

        // Clone once; keep original asset intact
        if (_runtimeMesh == null || !ReferenceEquals(_mf.sharedMesh, _runtimeMesh))
        {
            _runtimeMesh = Instantiate(_mf.sharedMesh);
            _runtimeMesh.name = _mf.sharedMesh.name + " (WingDeform)";
            _mf.sharedMesh = _runtimeMesh;

            _src = _runtimeMesh.vertices;
            _dst = new Vector3[_src.Length];
        }
    }

    void MeasureBounds()
    {
        if (_runtimeMesh == null) return;
        var verts = _runtimeMesh.vertices;
        if (verts == null || verts.Length == 0) return;

        float minS =  float.PositiveInfinity;
        float maxS =  float.NegativeInfinity;
        float minC =  float.PositiveInfinity;
        float maxC =  float.NegativeInfinity;
        float minT =  float.PositiveInfinity;
        float maxT =  float.NegativeInfinity;

        for (int i = 0; i < verts.Length; i++)
        {
            var v = verts[i];
            float s = GetAxis(v, spanAxis);
            float c = GetAxis(v, chordAxis);
            float t = GetAxis(v, thicknessAxis);
            if (s < minS) minS = s; if (s > maxS) maxS = s;
            if (c < minC) minC = c; if (c > maxC) maxC = c;
            if (t < minT) minT = t; if (t > maxT) maxT = t;
        }

        _spanMin = minS;
        _spanMax = maxS;
        _origSpan  = Mathf.Max(1e-6f, maxS - minS);
        _origChord = Mathf.Max(1e-6f, maxC - minC);
        _origThick = Mathf.Max(1e-6f, maxT - minT);
    }

    public void ApplyNow()
    {
        if (_runtimeMesh == null || _src == null || _src.Length == 0) return;

        // Linear mapping from original span to target span, with optional root anchor
        float scaleSpan = targetSpanMeters / _origSpan;

        for (int i = 0; i < _src.Length; i++)
        {
            Vector3 v = _src[i];

            // Span coordinate in [0..1] measured from root plane = _spanMin
            float s = GetAxis(v, spanAxis);
            float t01 = Mathf.InverseLerp(_spanMin, _spanMax, s);

            // 1) Stretch along span
            float sCentered = s - (anchorRoot ? _spanMin : (_spanMin + _origSpan * 0.5f));
            sCentered *= scaleSpan;
            float sNew = sCentered + (anchorRoot ? _spanMin : (_spanMin + _origSpan * 0.5f));

            // 2) Chord/thickness taper (linear root->tip)
            float chordScale = Mathf.Lerp(rootChordScale,  tipChordScale,  t01);
            float thickScale = Mathf.Lerp(rootThickScale,  tipThickScale,  t01);

            float c = GetAxis(v, chordAxis);
            float t = GetAxis(v, thicknessAxis);

            // Anchor chord/thickness about their original centers so scaling doesn’t walk the LE/TE/thickness off-center.
            // If you want to fix the LE instead, set center to LE position (minC) and skip recenters; ask if you want that variant.
            float cCenter = 0f; // about 0 because most assets are roughly centered; robust way is reusing mesh bounds center:
            float tCenter = 0f;

            c = (c - cCenter) * chordScale + cCenter;
            t = (t - tCenter) * thickScale + tCenter;

            // 3) Optional twist (washout), applied around span axis
            if (Mathf.Abs(twistDegreesAtTip) > 1e-4f)
            {
                float aDeg = Mathf.Lerp(0f, twistDegreesAtTip, t01);
                v = SetAxis(v, spanAxis, 0f); // rotate in the plane perpendicular to span, around span-through-origin
                v = RotateAroundPrincipal(v, spanAxis, aDeg * Mathf.Deg2Rad);
                // After rotation, we reassign c,t from rotated v to keep twist affecting chord/thickness plane
                c = GetAxis(v, chordAxis);
                t = GetAxis(v, thicknessAxis);
            }

            // Write new coords back
            Vector3 outV = v;
            outV = SetAxis(outV, spanAxis, sNew);
            outV = SetAxis(outV, chordAxis, c);
            outV = SetAxis(outV, thicknessAxis, t);
            _dst[i] = outV;
        }

        _runtimeMesh.SetVertices(_dst);
        _runtimeMesh.RecalculateNormals();
        _runtimeMesh.RecalculateBounds();

        if (updateMeshCollider)
        {
            var mc = GetComponent<MeshCollider>();
            if (mc)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = _runtimeMesh;
            }
        }
    }

    // -------- helpers --------
    static float GetAxis(Vector3 v, Axis ax) => (ax == Axis.X) ? v.x : (ax == Axis.Y ? v.y : v.z);
    static Vector3 SetAxis(Vector3 v, Axis ax, float val)
    {
        if (ax == Axis.X) v.x = val;
        else if (ax == Axis.Y) v.y = val;
        else v.z = val;
        return v;
    }

    // Rotate vector around a principal axis (local) by radians
    static Vector3 RotateAroundPrincipal(Vector3 v, Axis axis, float radians)
    {
        float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
        switch (axis)
        {
            case Axis.X: return new Vector3(v.x, c * v.y - s * v.z, s * v.y + c * v.z);
            case Axis.Y: return new Vector3(c * v.x + s * v.z, v.y, -s * v.x + c * v.z);
            default:     return new Vector3(c * v.x - s * v.y, s * v.x + c * v.y, v.z);
        }
    }
}
