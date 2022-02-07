using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoopText3 : Text
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

    private void UpdateMaskRect()
    {
        var mat = material;
        if (mat.HasProperty("_MaskRect"))
        {
            mat.EnableKeyword("UNITY_UI_ALPHACLIP");
            var rect = mat.GetVector("_MaskRect");
            var pos = rectTransform.position;
            var minPos = rectTransform.rect.min;
            var maxPos = rectTransform.rect.max;
            minPos += new Vector2(pos.x, pos.y);
            maxPos += new Vector2(pos.x, pos.y);
            //var t1 = rectTransform.TransformPoint(rectTransform.offsetMin);
            //var t2 = rectTransform.TransformPoint(rectTransform.offsetMax);
            //var t1 = rectTransform.offsetMin;
            //var t2 = rectTransform.offsetMax;
            //var t0 = new Vector2(rectTransform.position.x + rectTransform.localPosition.x, rectTransform.position.y + rectTransform.localPosition.y);
            var t0 = new Vector2(pos.x - Screen.width / 2, pos.y - Screen.height / 2);
            var t1 = t0 ;
            var t2 = t0 + rectTransform.rect.size;

            mat.SetVector("_MaskRect", new Vector4(t1.x, t1.y, t2.x, t2.y));
        }
    }

    protected void Update()
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
        UpdateMaskRect();
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
        return true;
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
