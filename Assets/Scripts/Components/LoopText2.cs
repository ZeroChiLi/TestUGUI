using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class LoopText2 : Text
{
    public bool enableLoop = true;
    public float offsetValue = 1;
    public float offsetMin = 0;
    public float offsetMax = 0;
    public float startDelayTime = 1;
    public float movePerSecond = 20;
    public float stopDelayTime = 1;

    private float _lastStartDelayTime;
    private float _lastMovePerSecond;
    private float _lastStopDelayTime;
    private float _startLoopTime;
    private float _loopTime;

    private bool _srcDirty;
    private UnityAction _dirtyCallback;

    [SerializeField]
    private bool m_ShowMaskGraphic = true;

    /// <summary>
    /// Show the graphic that is associated with the Mask render area.
    /// </summary>
    public bool showMaskGraphic
    {
        get { return m_ShowMaskGraphic; }
        set
        {
            if (m_ShowMaskGraphic == value)
                return;

            m_ShowMaskGraphic = value;
            if (graphic != null)
                graphic.SetMaterialDirty();
        }
    }

    [NonSerialized]
    private Graphic m_Graphic;

    /// <summary>
    /// The graphic associated with the Mask.
    /// </summary>
    public Graphic graphic
    {
        get { return m_Graphic ?? (m_Graphic = GetComponent<Graphic>()); }
    }

    [NonSerialized]
    private Material m_MaskMaterial;

    [NonSerialized]
    private Material m_UnmaskMaterial;
    
    public virtual bool MaskEnabled() { return IsActive() && graphic != null; }

    [Obsolete("Not used anymore.")]
    public virtual void OnSiblingGraphicEnabledDisabled() { }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (graphic != null)
        {
            graphic.canvasRenderer.hasPopInstruction = true;
            graphic.SetMaterialDirty();
        }

        MaskUtilities.NotifyStencilStateChanged(this);
    }

    protected override void OnDisable()
    {
        // we call base OnDisable first here
        // as we need to have the IsActive return the
        // correct value when we notify the children
        // that the mask state has changed.
        base.OnDisable();
        if (graphic != null)
        {
            graphic.SetMaterialDirty();
            graphic.canvasRenderer.hasPopInstruction = false;
            graphic.canvasRenderer.popMaterialCount = 0;
        }

        StencilMaterial.Remove(m_MaskMaterial);
        m_MaskMaterial = null;
        StencilMaterial.Remove(m_UnmaskMaterial);
        m_UnmaskMaterial = null;

        MaskUtilities.NotifyStencilStateChanged(this);
    }

    public virtual bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (!isActiveAndEnabled)
            return true;

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera);
    }

    /// Stencil calculation time!
    override public Material GetModifiedMaterial(Material baseMaterial)
    {
        return baseMaterial;
        if (!MaskEnabled())
            return baseMaterial;

        var rootSortCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
        var stencilDepth = MaskUtilities.GetStencilDepth(transform, rootSortCanvas);
        if (stencilDepth >= 8)
        {
            Debug.LogWarning("Attempting to use a stencil mask with depth > 8", gameObject);
            return baseMaterial;
        }

        int desiredStencilBit = 1 << stencilDepth;

        // if we are at the first level...
        // we want to destroy what is there
        if (desiredStencilBit == 1)
        {
            var maskMaterial = StencilMaterial.Add(baseMaterial, 1, StencilOp.Replace, CompareFunction.Always, m_ShowMaskGraphic ? ColorWriteMask.All : 0);
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = maskMaterial;

            var unmaskMaterial = StencilMaterial.Add(baseMaterial, 1, StencilOp.Zero, CompareFunction.Always, 0);
            StencilMaterial.Remove(m_UnmaskMaterial);
            m_UnmaskMaterial = unmaskMaterial;
            graphic.canvasRenderer.popMaterialCount = 1;
            graphic.canvasRenderer.SetPopMaterial(m_UnmaskMaterial, 0);

            return m_MaskMaterial;
        }

        //otherwise we need to be a bit smarter and set some read / write masks
        var maskMaterial2 = StencilMaterial.Add(baseMaterial, desiredStencilBit | (desiredStencilBit - 1), StencilOp.Replace, CompareFunction.Equal, m_ShowMaskGraphic ? ColorWriteMask.All : 0, desiredStencilBit - 1, desiredStencilBit | (desiredStencilBit - 1));
        StencilMaterial.Remove(m_MaskMaterial);
        m_MaskMaterial = maskMaterial2;

        graphic.canvasRenderer.hasPopInstruction = true;
        var unmaskMaterial2 = StencilMaterial.Add(baseMaterial, desiredStencilBit - 1, StencilOp.Replace, CompareFunction.Equal, 0, desiredStencilBit - 1, desiredStencilBit | (desiredStencilBit - 1));
        StencilMaterial.Remove(m_UnmaskMaterial);
        m_UnmaskMaterial = unmaskMaterial2;
        graphic.canvasRenderer.popMaterialCount = 1;
        graphic.canvasRenderer.SetPopMaterial(m_UnmaskMaterial, 0);

        return m_MaskMaterial;
    }
    protected override void Awake()
    {
        base.Awake();
        _dirtyCallback = DirtyCallback;
        RegisterDirtyVerticesCallback(_dirtyCallback);
        RegisterDirtyLayoutCallback(_dirtyCallback);
    }

    private void DirtyCallback()
    {
        _srcDirty = true;
    }

    virtual protected bool CheckDirty()
    {
        if (_srcDirty || _lastStartDelayTime != startDelayTime || _lastMovePerSecond != movePerSecond || _lastStopDelayTime != stopDelayTime)
        {
            _srcDirty = false;
            _lastStartDelayTime = startDelayTime;
            _lastMovePerSecond = movePerSecond;
            _lastStopDelayTime = stopDelayTime;
            return true;
        }
        return false;
    }

    protected void LateUpdate()
    {
        if (!enableLoop && offsetMin != 0 && offsetMax != 0)
            return;

        if (CheckDirty())
        {
            _startLoopTime = Time.time;
            _loopTime = (offsetMax + offsetMin) / movePerSecond;
        }
        var curTime = Time.time - _startLoopTime;
        if (curTime < startDelayTime)
        {
            offsetValue = offsetMin;
        }
        else if (curTime < startDelayTime + _loopTime)
        {
            offsetValue = offsetMin - ((curTime - startDelayTime) / _loopTime) * (offsetMax + offsetMin);
        }
        else if (curTime < startDelayTime + _loopTime + stopDelayTime)
        {
            offsetValue = -offsetMax;
        }
        else
        {
            // 重新开始
            _startLoopTime = Time.time;
        }

        UpdateGeometry();


        //var mat = material;
        //var vec = mat.GetVector("_ClipRect");
        //Debug.LogError($"_ClipRect: {vec}");
    }

    private bool GetVertexMinMax(IList<UIVertex> verts, out Vector2 min, out Vector2 max)
    {
        min = Vector2.zero;
        max = Vector2.zero;
        var count = verts.Count;
        if (count > 0)
        {
            min = verts[1].position;
            max = verts[1].position;
            for (int i = 1; i < count; ++i)
            {
                min.x = Mathf.Min(min.x, verts[i].position.x);
                min.y = Mathf.Min(min.y, verts[i].position.y);
                max.x = Mathf.Max(max.x, verts[i].position.x);
                max.y = Mathf.Max(max.y, verts[i].position.y);
            }
            return true;
        }
        return false;
    }

    private bool PointInRange(Vector2 value, Vector2 min, Vector2 max)
    {
        //return value.x >= min.x && value.y >= min.y && value.x <= max.x && value.y <= max.y;
        return value.x >= min.x && value.x <= max.x;
    }

    readonly UIVertex[] m_TempVerts = new UIVertex[4];
    override protected void OnPopulateMesh(VertexHelper toFill)
    {
        if (!enableLoop)
        {
            base.OnPopulateMesh(toFill);
            return;
        }

        if (font == null)
            return;

        // We don't care if we the font Texture changes while we are doing our Update.
        // The end result of cachedTextGenerator will be valid for this instance.
        // Otherwise we can get issues like Case 619238.
        m_DisableFontTextureRebuiltCallback = true;

        Vector2 extents = rectTransform.rect.size;

        // 计算水平Wrap的时候，最大最小x坐标
        var warpSettings = GetGenerationSettings(extents);
        warpSettings.horizontalOverflow = HorizontalWrapMode.Wrap;
        cachedTextGenerator.PopulateWithErrors(text, warpSettings, gameObject);
        IList<UIVertex> limitVerts = cachedTextGenerator.verts;
        Vector2 limitMin = Vector2.zero;
        Vector2 limitMax = Vector2.zero;
        GetVertexMinMax(limitVerts, out limitMin, out limitMax);

        // 计算水平最大范围的x坐标
        var maxSetting = GetGenerationSettings(extents);
        maxSetting.horizontalOverflow = HorizontalWrapMode.Overflow;
        cachedTextGenerator.PopulateWithErrors(text, maxSetting, gameObject);
        // Apply the offset to the vertices
        IList<UIVertex> verts = cachedTextGenerator.verts;
        Vector2 realMin = Vector2.zero;
        Vector2 realMax = Vector2.zero;
        GetVertexMinMax(verts, out realMin, out realMax);

        // 计算左右空间偏移
        offsetMin = limitMin.x - realMin.x;
        offsetMax = realMax.x - limitMax.x;

        float unitsPerPixel = 1 / pixelsPerUnit;
        int vertCount = verts.Count;

        // We have no verts to process just return (case 1037923)
        if (vertCount <= 0)
        {
            toFill.Clear();
            return;
        }

        Vector2 roundingOffset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
        roundingOffset = PixelAdjustPoint(roundingOffset) - roundingOffset;
        toFill.Clear();
        if (roundingOffset != Vector2.zero)
        {
            for (int i = 0; i < vertCount; ++i)
            {
                int tempVertsIndex = i & 3;
                m_TempVerts[tempVertsIndex] = verts[i];
                m_TempVerts[tempVertsIndex].position *= unitsPerPixel;
                m_TempVerts[tempVertsIndex].position.x += roundingOffset.x;
                m_TempVerts[tempVertsIndex].position.y += roundingOffset.y;
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(m_TempVerts);
            }
        }
        else
        {
            //for (int i = 0; i < vertCount; ++i)
            //{
            //    int tempVertsIndex = i & 3;
            //    m_TempVerts[tempVertsIndex] = verts[i];
            //    m_TempVerts[tempVertsIndex].position *= unitsPerPixel;
            //    m_TempVerts[tempVertsIndex].position.x += testValue1;
            //    if (tempVertsIndex == 3)
            //        toFill.AddUIVertexQuad(m_TempVerts);
            //}
            for (int i = 0; i < vertCount; i += 4)
            {
                m_TempVerts[0] = verts[i];
                m_TempVerts[0].position *= unitsPerPixel;
                m_TempVerts[0].position.x += offsetValue;

                m_TempVerts[1] = verts[i + 1];
                m_TempVerts[1].position *= unitsPerPixel;
                m_TempVerts[1].position.x += offsetValue;

                m_TempVerts[2] = verts[i + 2];
                m_TempVerts[2].position *= unitsPerPixel;
                m_TempVerts[2].position.x += offsetValue;

                m_TempVerts[3] = verts[i + 3];
                m_TempVerts[3].position *= unitsPerPixel;
                m_TempVerts[3].position.x += offsetValue;

                // 不在范围内的面不加进去
                if (PointInRange(m_TempVerts[0].position, limitMin, limitMax) &&
                    PointInRange(m_TempVerts[1].position, limitMin, limitMax) &&
                    PointInRange(m_TempVerts[2].position, limitMin, limitMax) &&
                    PointInRange(m_TempVerts[3].position, limitMin, limitMax))
                {
                    toFill.AddUIVertexQuad(m_TempVerts);
                }
            }
        }

        m_DisableFontTextureRebuiltCallback = false;
    }
}
