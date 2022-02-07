using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class LoopTextCom : MonoBehaviour
{
    public Text textCom;
    public Material maskMaterial;
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

    protected void Awake()
    {
        textCom = textCom ?? GetComponent<Text>();
        if (!textCom)
            return;

        if (maskMaterial)
            textCom.material = maskMaterial; 
        
        textCom.RegisterDirtyVerticesCallback(DirtyCallback);
        textCom.RegisterDirtyLayoutCallback(DirtyCallback);
    }

    private void DirtyCallback()
    {
        _srcDirty = true;
    }

    protected void OnEnable()
    {
        textCom.horizontalOverflow = HorizontalWrapMode.Overflow;
        _srcDirty = true;
        UpdateMinMaxOffset();
        UpdateMaskRect();
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
        var material = textCom.materialForRendering;
        if (material.HasProperty("_MaskRect"))
        {
            material.EnableKeyword("UNITY_UI_ALPHACLIP");

            var rect = material.GetVector("_MaskRect");
            var pos = textCom.rectTransform.position;
            var t0 = new Vector2(pos.x - Screen.width / 2, pos.y - Screen.height / 2);
            var t1 = t0 - textCom.rectTransform.rect.size / 2;
            var t2 = t0 + textCom.rectTransform.rect.size / 2;

            material.SetVector("_MaskRect", new Vector4(t1.x, t1.y, t2.x, t2.y));
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

        UpdateMinMaxOffset();
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
    
    protected void UpdateMinMaxOffset()
    {
        Vector2 extents = textCom.rectTransform.rect.size;

        // 计算水平Wrap的时候，最大最小x坐标
        var warpSettings = textCom.GetGenerationSettings(extents);
        warpSettings.horizontalOverflow = HorizontalWrapMode.Wrap;
        textCom.cachedTextGenerator.PopulateWithErrors(textCom.text, warpSettings, gameObject);
        IList<UIVertex> limitVerts = textCom.cachedTextGenerator.verts;
        Vector2 limitMin = Vector2.zero;
        Vector2 limitMax = Vector2.zero;
        GetVertexMinMax(limitVerts, out limitMin, out limitMax);

        // 计算水平最大范围的x坐标
        var maxSetting = textCom.GetGenerationSettings(extents);
        maxSetting.horizontalOverflow = HorizontalWrapMode.Overflow;
        textCom.cachedTextGenerator.PopulateWithErrors(textCom.text, maxSetting, gameObject);
        // Apply the offset to the vertices
        IList<UIVertex> verts = textCom.cachedTextGenerator.verts;
        Vector2 realMin = Vector2.zero;
        Vector2 realMax = Vector2.zero;
        GetVertexMinMax(verts, out realMin, out realMax);

        // 计算左右空间偏移
        offsetMin = limitMin.x - realMin.x;
        offsetMax = realMax.x - limitMax.x;
        
    }
}
