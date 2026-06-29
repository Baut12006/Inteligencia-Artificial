using UnityEngine;

public class PlayerStealthFeedback : MonoBehaviour
{
    [SerializeField] private PlayerModel playerModel;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color hiddenTint = new Color(0.35f, 0.45f, 0.7f, 1f);

    private MaterialPropertyBlock mpb;
    private bool wasHidden;

    void Awake()
    {
        if (playerModel == null) playerModel = GetComponent<PlayerModel>();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (playerModel == null) return;

        bool hidden = playerModel.IsInShadow && !playerModel.IsDead;
        if (hidden == wasHidden) return;
        wasHidden = hidden;

        Color tint = hidden ? hiddenTint : Color.white;
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", tint);
            mpb.SetColor("_Color", tint);
            r.SetPropertyBlock(mpb);
        }

    }
}