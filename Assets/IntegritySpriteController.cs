using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Controls per-instance properties on the IntegritySprite shader
/// without creating separate material instances.
///
/// Attach this to any GameObject that has a SpriteRenderer using
/// the IntegritySprite material.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways] // Updates in the editor too, so Inspector edits show live
public class IntegritySpriteController : MonoBehaviour
{
    [Header("Integrity")]
    [Range(0f, 1f)]
    public float integrity = 1f;

    [Header("Overlay")]
    public Texture2D overlaySprite;

    [Header("Colour Remapping")]
    public ColourRemap[] colourRemaps = new ColourRemap[4];

    [Range(0f, 1f)]
    public float colourThreshold = 0.1f;

    public Color spriteTint = Color.white;

    // -------------------------------------------------------

    [System.Serializable]
    public struct ColourRemap
    {
        public Color source;
        public Color target;
    }

    // -------------------------------------------------------

    private SpriteRenderer    _renderer;
    private MaterialPropertyBlock _block;

    // Cached shader property IDs (faster than string lookups every frame)
    private static readonly int ID_Integrity      = Shader.PropertyToID("_Integrity");
    private static readonly int ID_OverlayTex     = Shader.PropertyToID("_OverlayTex");
    private static readonly int ID_Threshold      = Shader.PropertyToID("_ColourThreshold");
    private static readonly int ID_SpriteTint     = Shader.PropertyToID("_SpriteTint");

    private static readonly int[] ID_Source = new int[]
    {
        Shader.PropertyToID("_SourceColour0"),
        Shader.PropertyToID("_SourceColour1"),
        Shader.PropertyToID("_SourceColour2"),
        Shader.PropertyToID("_SourceColour3"),
    };
    private static readonly int[] ID_Target = new int[]
    {
        Shader.PropertyToID("_TargetColour0"),
        Shader.PropertyToID("_TargetColour1"),
        Shader.PropertyToID("_TargetColour2"),
        Shader.PropertyToID("_TargetColour3"),
    };

    // -------------------------------------------------------

    private void Awake()  => Initialise();
    private void OnEnable() => Apply();

    private void Initialise()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _block    = new MaterialPropertyBlock();

        // Seed default colour remaps if the array is uninitialised
        if (colourRemaps == null || colourRemaps.Length != 4)
        {
            colourRemaps = new ColourRemap[4];
            for (int i = 0; i < 4; i++)
            {
                colourRemaps[i].source = Color.white;
                colourRemaps[i].target = Color.white;
            }
        }
    }

    /// <summary>
    /// Push all values into the MaterialPropertyBlock and apply it.
    /// Call this whenever you change a value at runtime.
    /// </summary>
    public void Apply()
    {
        if (_renderer == null) Initialise();

        _renderer.GetPropertyBlock(_block);

        _block.SetFloat(ID_Integrity,  integrity);
        _block.SetFloat(ID_Threshold,  colourThreshold);
        _block.SetColor(ID_SpriteTint, spriteTint);

        if (overlaySprite != null)
            _block.SetTexture(ID_OverlayTex, overlaySprite);

        int count = Mathf.Min(colourRemaps.Length, 4);
        for (int i = 0; i < count; i++)
        {
            _block.SetColor(ID_Source[i], colourRemaps[i].source);
            _block.SetColor(ID_Target[i], colourRemaps[i].target);
        }

        _renderer.SetPropertyBlock(_block);
    }

    // -------------------------------------------------------
    // Convenience setters — call these from other scripts
    // instead of setting the public fields directly, so Apply()
    // is always triggered.
    // -------------------------------------------------------

    public void SetIntegrity(float value)
    {
        integrity = Mathf.Clamp01(value);
        Apply();
    }

    public void SetOverlay(Texture2D tex)
    {
        overlaySprite = tex;
        Apply();
    }

    public void SetColourRemap(int index, Color source, Color target)
    {
        if (index < 0 || index >= colourRemaps.Length) return;
        colourRemaps[index].source = source;
        colourRemaps[index].target = target;
        Apply();
    }

    // -------------------------------------------------------
    // Editor live-preview: re-apply whenever the Inspector changes
    // -------------------------------------------------------
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_block == null) Initialise();
        Apply();
    }
#endif
}
