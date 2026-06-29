using UnityEngine;

public class ShadowZoneVisual : MonoBehaviour
{
    [SerializeField] private Color zoneColor = new Color(0.35f, 0.55f, 1f, 0.85f);
    [SerializeField] private float lineWidth = 0.15f;
    [SerializeField] private float floorOffset = 0.05f;
    [SerializeField] private bool pulse = true;
    [SerializeField] private float pulseSpeed = 2f;

    private LineRenderer lr;
    private BoxCollider box;
    private Color baseColor;

    void Start()
    {
        box = GetComponent<BoxCollider>();
        baseColor = zoneColor;
        BuildOutline();
    }

    void BuildOutline()
    {
        GameObject go = new GameObject("ShadowOutline");
        go.transform.SetParent(transform, false);

        lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 4;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.numCornerVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        lr.material = new Material(sh);
        lr.startColor = zoneColor;
        lr.endColor = zoneColor;

        UpdateCorners();
    }

    void UpdateCorners()
    {
        Vector3 c = box.center;
        Vector3 s = box.size * 0.5f;
        float y = c.y - s.y + floorOffset;

        Vector3[] local =
        {
            new Vector3(c.x - s.x, y, c.z - s.z),
            new Vector3(c.x + s.x, y, c.z - s.z),
            new Vector3(c.x + s.x, y, c.z + s.z),
            new Vector3(c.x - s.x, y, c.z + s.z),
        };

        for (int i = 0; i < 4; i++)
            lr.SetPosition(i, transform.TransformPoint(local[i]));
    }
    void Update()
    {
        if (lr == null || !pulse) return;
        float a = baseColor.a * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
        Color col = new Color(baseColor.r, baseColor.g, baseColor.b, a);
        lr.startColor = col;
        lr.endColor = col;
    }
}