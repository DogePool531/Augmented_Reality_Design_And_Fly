using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class FunctionGrapherWithBackground : MonoBehaviour
{
    public enum Anchor { Center, LowerLeft }
    public AeroModel aeroModel;
    private float Cl;
    private float Cd_0;
    private float k;
    [Header("Data Domain (what you plot)")]
    public float xMinData = 0f;
    public float xMaxData = 10f;
    public float yMinData = 0f;
    public float yMaxData = 1f;
    public bool autoFitDataY = false;
    public float autoFitPadding = 0.05f;

    [Header("Graph Size (how big it renders)")]
    public Anchor anchor = Anchor.LowerLeft;
    public float graphWidth = 10f;
    public float graphHeight = 5f;
    public Vector3 graphOffset = Vector3.zero;

    [Header("Curve")]
    public int resolution = 300;
    public float lineWidth = 0.05f;
    public Color lineColor = Color.green;

    [Header("Live Update")]
    public bool updateEveryFrame = true;   // ← turn on to update in Play mode
    public bool animate = false;           // demo: animate Function with time
    public float timeScale = 1f;

    [Header("Background")]
    public bool showBackground = true;
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);

    [Header("Axes / Grid / Labels (in DATA units)")]
    public bool showAxes = true;
    public bool showGrid = true;
    public float xTickData = 1f;
    public float yTickData = 0.2f;
    public float axisWidth = 0.02f;
    public float gridWidth = 0.01f;
    public Color axisColor = Color.white;
    public Color gridColor = new Color(1f,1f,1f,0.2f);
    public Color labelColor = Color.white;
    public float labelSize = 0.2f;
    public float labelMargin = 0.15f;

    private LineRenderer lr;
    private GameObject bg;
    private Transform axesRoot, gridRoot, labelRoot;

    // --- change tracking to avoid rebuilding everything every frame ---
    private struct LayoutConfig
    {
        public float xMin, xMax, yMin, yMax;
        public float w, h;
        public Anchor anch;
        public bool showBg, showAx, showGr;
        public float xTick, yTick;
    }
    private LayoutConfig lastLayout;
    private bool layoutInitialized = false;

    void Awake() { Ensure(); }
    void Start() { RedrawAll(forceFull:true); }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // In edit mode, keep things live
            RedrawAll(forceFull:true);
            return;
        }
#endif
        // In play mode:
        if (updateEveryFrame || animate)
        {
            // Only rebuild layout if inputs affecting layout changed
            if (LayoutChanged())
                RedrawAll(forceFull:true);
            else
                BuildCurveAndMaybeAutoFit(); // fast path: just curve (and auto-fit if enabled)
        }
    }

    // ---------- Core ----------
    void Ensure()
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.widthMultiplier = lineWidth;
        lr.startColor = lineColor; lr.endColor = lineColor;

        axesRoot = EnsureChild("AxesRoot");
        gridRoot = EnsureChild("GridRoot");
        labelRoot = EnsureChild("LabelsRoot");
    }

    Transform EnsureChild(string name)
    {
        var t = transform.Find(name);
        if (!t)
        {
            var go = new GameObject(name);
            t = go.transform;
            t.SetParent(transform, false);
        }
        return t;
    }

    Vector2 ClampDataRange()
    {
        float dx = Mathf.Max(1e-6f, xMaxData - xMinData);
        float dy = Mathf.Max(1e-6f, yMaxData - yMinData);
        return new Vector2(dx, dy);
    }

    Vector3 GraphOriginLocal() => graphOffset;

    Vector3 DataToLocal(float x, float y)
    {
        var d = ClampDataRange();
        float u = Mathf.InverseLerp(xMinData, xMaxData, x);
        float v = Mathf.InverseLerp(yMinData, yMaxData, y);

        if (anchor == Anchor.Center)
        {
            float lx = (u - 0.5f) * graphWidth;
            float ly = (v - 0.5f) * graphHeight;
            return GraphOriginLocal() + new Vector3(lx, ly, 0f);
        }
        else // LowerLeft
        {
            float lx = u * graphWidth;
            float ly = v * graphHeight;
            return GraphOriginLocal() + new Vector3(lx, ly, 0f);
        }
    }

    // Call this when you change anything significant or on start
    void RedrawAll(bool forceFull = false)
    {
        Ensure();
        if (forceFull || !layoutInitialized || LayoutChanged())
        {
            // Full rebuild (grid/axes/labels/bg + curve)
            ClearChildren(gridRoot);
            ClearChildren(axesRoot);
            ClearChildren(labelRoot);

            if (showBackground) BuildBackground();
            else DestroyImmediateSafe(ref bg);

            if (showGrid) BuildGrid();
            if (showAxes) BuildAxesAndLabels();

            SnapshotLayout();
        }

        // Always (re)build the curve last
        BuildCurveAndMaybeAutoFit();
    }

    // ---------- Curve ----------
    void BuildCurveAndMaybeAutoFit()
    {
        int n = Mathf.Max(2, resolution);
        lr.positionCount = n;

        float localMin = float.MaxValue, localMax = float.MinValue;

        // First pass: evaluate function for min/max (for auto-fit)
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            float x = Mathf.Lerp(xMinData, xMaxData, t);
            float y = Function(x);
            localMin = Mathf.Min(localMin, y);
            localMax = Mathf.Max(localMax, y);
        }

        // Auto-fit Y range if enabled
        if (autoFitDataY)
        {
            float dataSpan = Mathf.Max(1e-6f, localMax - localMin);
            float pad = dataSpan * Mathf.Clamp01(autoFitPadding);
            yMinData = localMin - pad;
            yMaxData = localMax + pad;
        }

        // Second pass: map to local positions
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            float x = Mathf.Lerp(xMinData, xMaxData, t);
            float y = Function(x);
            lr.SetPosition(i, DataToLocal(x, y));
        }

        lr.widthMultiplier = lineWidth;
        lr.startColor = lineColor; lr.endColor = lineColor;

        // If auto-fit changed y-range, background/grid positions may need an update
        if (autoFitDataY && showBackground) UpdateBackgroundTransformOnly();
    }

    // Your function (animated if desired)
    float Function(float x)
{
    // Pull latest aero params (null-safe)
   
     Cl = 2f; Cd_0 = 0.02f; k = 0.03f;
     if(aeroModel){
        Cl = aeroModel.Cl_a;
        Cd_0 = aeroModel.Cd_0;
        k = aeroModel.k;
     }
     

    // degrees -> radians (keep your original intent)
    float xr = 3.1415926f * x / 180f;

    // your exact formula, but protect the denominator
    // return Cl * x / ((x * x * Cl * Cl*k) + Cd_0);  // original (x was degrees)
    float denom = (xr * xr * Cl * Cl * k) +  Cd_0;
    float val = (Cl * xr) / denom;

    // final guard so we never emit NaN/∞ (prevents Invalid AABB & IsFinite asserts)
    if (float.IsNaN(val) || float.IsInfinity(val)) val = 0f;
    return val;
}


    // ---------- Background ----------
    void BuildBackground()
    {
        if (!bg)
        {
            bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BackgroundPlane";
            bg.transform.SetParent(transform, false);
            var r = bg.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Unlit/Color"));
        }
        UpdateBackgroundTransformOnly();
        bg.GetComponent<Renderer>().material.color = backgroundColor;
    }

    void UpdateBackgroundTransformOnly()
    {
        if (!bg) return;

        var center = (anchor == Anchor.Center)
            ? GraphOriginLocal()
            : GraphOriginLocal() + new Vector3(graphWidth * 0.5f, graphHeight * 0.5f, 0f);

        bg.transform.localPosition = center + Vector3.forward * 0.02f;
        bg.transform.localScale = new Vector3(graphWidth, graphHeight, 1f);
    }

    // ---------- Grid / Axes / Labels ----------
    void BuildGrid()
    {
        if (xTickData > 0f)
        {
            int a = Mathf.CeilToInt(xMinData / xTickData);
            int b = Mathf.FloorToInt(xMaxData / xTickData);
            for (int k = a; k <= b; k++)
            {
                float x = k * xTickData;
                var p0 = DataToLocal(x, yMinData);
                var p1 = DataToLocal(x, yMaxData);
                CreateLine(gridRoot, $"Grid_X_{k}", p0, p1, gridWidth, gridColor);
            }
        }

        if (yTickData > 0f)
        {
            int a = Mathf.CeilToInt(yMinData / yTickData);
            int b = Mathf.FloorToInt(yMaxData / yTickData);
            for (int k = a; k <= b; k++)
            {
                float y = k * yTickData;
                var p0 = DataToLocal(xMinData, y);
                var p1 = DataToLocal(xMaxData, y);
                CreateLine(gridRoot, $"Grid_Y_{k}", p0, p1, gridWidth, gridColor);
            }
        }
    }

    void BuildAxesAndLabels()
    {
        bool showYaxis = xMinData <= 0f && 0f <= xMaxData;
        bool showXaxis = yMinData <= 0f && 0f <= yMaxData;

        if (showXaxis)
        {
            var a = DataToLocal(xMinData, 0f);
            var b = DataToLocal(xMaxData, 0f);
            CreateLine(axesRoot, "X_Axis", a, b, axisWidth, axisColor);
        }
        if (showYaxis)
        {
            var a = DataToLocal(0f, yMinData);
            var b = DataToLocal(0f, yMaxData);
            CreateLine(axesRoot, "Y_Axis", a, b, axisWidth, axisColor);
        }

        // X labels
        if (xTickData > 0f)
        {
            int aT = Mathf.CeilToInt(xMinData / xTickData);
            int bT = Mathf.FloorToInt(xMaxData / xTickData);
            for (int k = aT; k <= bT; k++)
            {
                float x = k * xTickData;
                Vector3 basePos = DataToLocal(x, yMinData);
                var pos = basePos + Vector3.down * labelMargin;
                CreateLabel($"X_Label_{k}", pos, x.ToString("0.###"));
            }
        }

        // Y labels
        if (yTickData > 0f)
        {
            int aT = Mathf.CeilToInt(yMinData / yTickData);
            int bT = Mathf.FloorToInt(yMaxData / yTickData);
            for (int k = aT; k <= bT; k++)
            {
                float y = k * yTickData;
                Vector3 basePos = DataToLocal(xMinData, y);
                var pos = basePos + Vector3.left * labelMargin;
                CreateLabel($"Y_Label_{k}", pos, y.ToString("0.###"));
            }
        }
    }

    void CreateLine(Transform parent, string name, Vector3 a, Vector3 b, float width, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var lr2 = go.AddComponent<LineRenderer>();
        lr2.useWorldSpace = false;
        lr2.material = new Material(Shader.Find("Sprites/Default"));
        lr2.widthMultiplier = width;
        lr2.positionCount = 2;
        lr2.SetPosition(0, a);
        lr2.SetPosition(1, b);
        lr2.startColor = color;
        lr2.endColor = color;
    }

    void CreateLabel(string name, Vector3 pos, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(labelRoot, false);
        go.transform.localPosition = pos;

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = labelSize;
        tm.color = labelColor;
        tm.anchor = TextAnchor.MiddleCenter;
    }

    void ClearChildren(Transform t)
    {
        var list = new List<GameObject>();
        foreach (Transform c in t) list.Add(c.gameObject);
        foreach (var g in list)
        {
            if (Application.isPlaying) Destroy(g);
            else DestroyImmediate(g);
        }
    }

    void DestroyImmediateSafe(ref GameObject g)
    {
        if (!g) return;
        if (Application.isPlaying) Destroy(g);
        else DestroyImmediate(g);
        g = null;
    }

    // ----- layout change detection -----
    bool LayoutChanged()
    {
        if (!layoutInitialized) return true;
        return
            !Mathf.Approximately(lastLayout.xMin, xMinData) ||
            !Mathf.Approximately(lastLayout.xMax, xMaxData) ||
            !Mathf.Approximately(lastLayout.yMin, yMinData) ||
            !Mathf.Approximately(lastLayout.yMax, yMaxData) ||
            !Mathf.Approximately(lastLayout.w, graphWidth)  ||
            !Mathf.Approximately(lastLayout.h, graphHeight) ||
            lastLayout.anch != anchor ||
            lastLayout.showBg != showBackground ||
            lastLayout.showAx != showAxes ||
            lastLayout.showGr != showGrid ||
            !Mathf.Approximately(lastLayout.xTick, xTickData) ||
            !Mathf.Approximately(lastLayout.yTick, yTickData);
    }

    void SnapshotLayout()
    {
        lastLayout = new LayoutConfig
        {
            xMin = xMinData, xMax = xMaxData,
            yMin = yMinData, yMax = yMaxData,
            w = graphWidth, h = graphHeight,
            anch = anchor,
            showBg = showBackground, showAx = showAxes, showGr = showGrid,
            xTick = xTickData, yTick = yTickData
        };
        layoutInitialized = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (resolution < 2) resolution = 2;
        if (graphWidth <= 0f) graphWidth = 0.001f;
        if (graphHeight <= 0f) graphHeight = 0.001f;

        if (Mathf.Approximately(xMaxData, xMinData)) xMaxData = xMinData + 1f;
        if (Mathf.Approximately(yMaxData, yMinData)) yMaxData = yMinData + 1f;

        // Defer the redraw to avoid DestroyImmediate in OnValidate
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) RedrawAll(forceFull: true);
        };
    }
#endif
}
