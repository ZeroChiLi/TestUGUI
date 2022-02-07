// Obtain the vertices from the script and modify the position of one of them. Use OverrideGeometry() for this.
//Attach this script to a Sprite GameObject
//To see the vertices changing, make sure you have your Scene tab visible while in Play mode.
//Press the "Draw Debug" Button in the Game tab during Play Mode to draw the shape. Switch back to the Scene tab to see the shape.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum FillMethod
{
    Horizontal,
    Vertical,
    Radial90,
    Radial180,
    Radial360,
}

public class Test3 : MonoBehaviour
{
    public SpriteRenderer m_SpriteRenderer;
    public Sprite activeSprite;
    public float m_FillAmount = 0.6f;
    public Color color = Color.green;
    public FillMethod m_FillMethod;
    public bool m_FillClockwise;
    private int m_FillOrigin;
    private VertexHelper vh = new VertexHelper();

    void Awake()
    {
        m_SpriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        activeSprite = m_SpriteRenderer.sprite;
    }

    private void Update()
    {
        GenerateFilledSprite(ref vh, true);
        //List<UIVertex> uiVertices = new List<UIVertex>();
        //vh.GetUIVertexStream(uiVertices);
        //List<Vector2> vetList = new List<Vector2>();
        //for (int i = 0; i < uiVertices.Count; i++)
        //{
        //    vetList.Add(uiVertices[i].uv0);
        //}
        //activeSprite.OverrideGeometry(vetList.ToArray(), vh.t);
        //vh.FillMesh(activeSprite.)
    }

    void ChangeSprite()
    {
        //Fetch the Sprite and vertices from the SpriteRenderer
        Sprite sprite = m_SpriteRenderer.sprite;
        Vector2[] spriteVertices = sprite.vertices;

        for (int i = 0; i < spriteVertices.Length; i++)
        {
            spriteVertices[i].x = Mathf.Clamp(
                (sprite.vertices[i].x - sprite.bounds.center.x -
                    (sprite.textureRectOffset.x / sprite.texture.width) + sprite.bounds.extents.x) /
                (2.0f * sprite.bounds.extents.x) * sprite.rect.width,
                0.0f, sprite.rect.width);

            spriteVertices[i].y = Mathf.Clamp(
                (sprite.vertices[i].y - sprite.bounds.center.y -
                    (sprite.textureRectOffset.y / sprite.texture.height) + sprite.bounds.extents.y) /
                (2.0f * sprite.bounds.extents.y) * sprite.rect.height,
                0.0f, sprite.rect.height);

            // Make a small change to the second vertex
            if (i == 2)
            {
                //Make sure the vertices stay inside the Sprite rectangle
                if (spriteVertices[i].x < sprite.rect.size.x - 5)
                {
                    spriteVertices[i].x = spriteVertices[i].x + 5;
                }
                else spriteVertices[i].x = 0;
            }
        }
        //Override the geometry with the new vertices
        sprite.OverrideGeometry(spriteVertices, sprite.triangles);
    }


    static readonly Vector3[] s_Xy = new Vector3[4];
    static readonly Vector3[] s_Uv = new Vector3[4];
    void GenerateFilledSprite(ref VertexHelper toFill, bool preserveAspect)
    {
        toFill.Clear();

        if (m_FillAmount < 0.001f)
            return;

        Vector4 v = GetDrawingDimensions(preserveAspect);
        Vector4 outer = activeSprite != null ? UnityEngine.Sprites.DataUtility.GetOuterUV(activeSprite) : Vector4.zero;
        UIVertex uiv = UIVertex.simpleVert;
        uiv.color = color;

        float tx0 = outer.x;
        float ty0 = outer.y;
        float tx1 = outer.z;
        float ty1 = outer.w;

        // Horizontal and vertical filled sprites are simple -- just end the Image prematurely
        if (m_FillMethod == FillMethod.Horizontal || m_FillMethod == FillMethod.Vertical)
        {
            if (m_FillMethod == FillMethod.Horizontal)
            {
                float fill = (tx1 - tx0) * m_FillAmount;

                if (m_FillOrigin == 1)
                {
                    v.x = v.z - (v.z - v.x) * m_FillAmount;
                    tx0 = tx1 - fill;
                }
                else
                {
                    v.z = v.x + (v.z - v.x) * m_FillAmount;
                    tx1 = tx0 + fill;
                }
            }
            else if (m_FillMethod == FillMethod.Vertical)
            {
                float fill = (ty1 - ty0) * m_FillAmount;

                if (m_FillOrigin == 1)
                {
                    v.y = v.w - (v.w - v.y) * m_FillAmount;
                    ty0 = ty1 - fill;
                }
                else
                {
                    v.w = v.y + (v.w - v.y) * m_FillAmount;
                    ty1 = ty0 + fill;
                }
            }
        }

        s_Xy[0] = new Vector2(v.x, v.y);
        s_Xy[1] = new Vector2(v.x, v.w);
        s_Xy[2] = new Vector2(v.z, v.w);
        s_Xy[3] = new Vector2(v.z, v.y);

        s_Uv[0] = new Vector2(tx0, ty0);
        s_Uv[1] = new Vector2(tx0, ty1);
        s_Uv[2] = new Vector2(tx1, ty1);
        s_Uv[3] = new Vector2(tx1, ty0);

        {
            if (m_FillAmount < 1f && m_FillMethod != FillMethod.Horizontal && m_FillMethod != FillMethod.Vertical)
            {
                if (m_FillMethod == FillMethod.Radial90)
                {
                    if (RadialCut(s_Xy, s_Uv, m_FillAmount, m_FillClockwise, m_FillOrigin))
                        AddQuad(toFill, s_Xy, color, s_Uv);
                }
                else if (m_FillMethod == FillMethod.Radial180)
                {
                    for (int side = 0; side < 2; ++side)
                    {
                        float fx0, fx1, fy0, fy1;
                        int even = m_FillOrigin > 1 ? 1 : 0;

                        if (m_FillOrigin == 0 || m_FillOrigin == 2)
                        {
                            fy0 = 0f;
                            fy1 = 1f;
                            if (side == even)
                            {
                                fx0 = 0f;
                                fx1 = 0.5f;
                            }
                            else
                            {
                                fx0 = 0.5f;
                                fx1 = 1f;
                            }
                        }
                        else
                        {
                            fx0 = 0f;
                            fx1 = 1f;
                            if (side == even)
                            {
                                fy0 = 0.5f;
                                fy1 = 1f;
                            }
                            else
                            {
                                fy0 = 0f;
                                fy1 = 0.5f;
                            }
                        }

                        s_Xy[0].x = Mathf.Lerp(v.x, v.z, fx0);
                        s_Xy[1].x = s_Xy[0].x;
                        s_Xy[2].x = Mathf.Lerp(v.x, v.z, fx1);
                        s_Xy[3].x = s_Xy[2].x;

                        s_Xy[0].y = Mathf.Lerp(v.y, v.w, fy0);
                        s_Xy[1].y = Mathf.Lerp(v.y, v.w, fy1);
                        s_Xy[2].y = s_Xy[1].y;
                        s_Xy[3].y = s_Xy[0].y;

                        s_Uv[0].x = Mathf.Lerp(tx0, tx1, fx0);
                        s_Uv[1].x = s_Uv[0].x;
                        s_Uv[2].x = Mathf.Lerp(tx0, tx1, fx1);
                        s_Uv[3].x = s_Uv[2].x;

                        s_Uv[0].y = Mathf.Lerp(ty0, ty1, fy0);
                        s_Uv[1].y = Mathf.Lerp(ty0, ty1, fy1);
                        s_Uv[2].y = s_Uv[1].y;
                        s_Uv[3].y = s_Uv[0].y;

                        float val = m_FillClockwise ? m_FillAmount * 2f - side : m_FillAmount * 2f - (1 - side);

                        if (RadialCut(s_Xy, s_Uv, Mathf.Clamp01(val), m_FillClockwise, ((side + m_FillOrigin + 3) % 4)))
                        {
                            AddQuad(toFill, s_Xy, color, s_Uv);
                        }
                    }
                }
                else if (m_FillMethod == FillMethod.Radial360)
                {
                    for (int corner = 0; corner < 4; ++corner)
                    {
                        float fx0, fx1, fy0, fy1;

                        if (corner < 2)
                        {
                            fx0 = 0f;
                            fx1 = 0.5f;
                        }
                        else
                        {
                            fx0 = 0.5f;
                            fx1 = 1f;
                        }

                        if (corner == 0 || corner == 3)
                        {
                            fy0 = 0f;
                            fy1 = 0.5f;
                        }
                        else
                        {
                            fy0 = 0.5f;
                            fy1 = 1f;
                        }

                        s_Xy[0].x = Mathf.Lerp(v.x, v.z, fx0);
                        s_Xy[1].x = s_Xy[0].x;
                        s_Xy[2].x = Mathf.Lerp(v.x, v.z, fx1);
                        s_Xy[3].x = s_Xy[2].x;

                        s_Xy[0].y = Mathf.Lerp(v.y, v.w, fy0);
                        s_Xy[1].y = Mathf.Lerp(v.y, v.w, fy1);
                        s_Xy[2].y = s_Xy[1].y;
                        s_Xy[3].y = s_Xy[0].y;

                        s_Uv[0].x = Mathf.Lerp(tx0, tx1, fx0);
                        s_Uv[1].x = s_Uv[0].x;
                        s_Uv[2].x = Mathf.Lerp(tx0, tx1, fx1);
                        s_Uv[3].x = s_Uv[2].x;

                        s_Uv[0].y = Mathf.Lerp(ty0, ty1, fy0);
                        s_Uv[1].y = Mathf.Lerp(ty0, ty1, fy1);
                        s_Uv[2].y = s_Uv[1].y;
                        s_Uv[3].y = s_Uv[0].y;

                        float val = m_FillClockwise ?
                            m_FillAmount * 4f - ((corner + m_FillOrigin) % 4) :
                            m_FillAmount * 4f - (3 - ((corner + m_FillOrigin) % 4));

                        if (RadialCut(s_Xy, s_Uv, Mathf.Clamp01(val), m_FillClockwise, ((corner + 2) % 4)))
                            AddQuad(toFill, s_Xy, color, s_Uv);
                    }
                }
            }
            else
            {
                AddQuad(toFill, s_Xy, color, s_Uv);
            }
        }
    }

    /// <summary>
    /// Adjust the specified quad, making it be radially filled instead.
    /// </summary>

    static bool RadialCut(Vector3[] xy, Vector3[] uv, float fill, bool invert, int corner)
    {
        // Nothing to fill
        if (fill < 0.001f) return false;

        // Even corners invert the fill direction
        if ((corner & 1) == 1) invert = !invert;

        // Nothing to adjust
        if (!invert && fill > 0.999f) return true;

        // Convert 0-1 value into 0 to 90 degrees angle in radians
        float angle = Mathf.Clamp01(fill);
        if (invert) angle = 1f - angle;
        angle *= 90f * Mathf.Deg2Rad;

        // Calculate the effective X and Y factors
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        RadialCut(xy, cos, sin, invert, corner);
        RadialCut(uv, cos, sin, invert, corner);
        return true;
    }

    /// <summary>
    /// Adjust the specified quad, making it be radially filled instead.
    /// </summary>

    static void RadialCut(Vector3[] xy, float cos, float sin, bool invert, int corner)
    {
        int i0 = corner;
        int i1 = ((corner + 1) % 4);
        int i2 = ((corner + 2) % 4);
        int i3 = ((corner + 3) % 4);

        if ((corner & 1) == 1)
        {
            if (sin > cos)
            {
                cos /= sin;
                sin = 1f;

                if (invert)
                {
                    xy[i1].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
                    xy[i2].x = xy[i1].x;
                }
            }
            else if (cos > sin)
            {
                sin /= cos;
                cos = 1f;

                if (!invert)
                {
                    xy[i2].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                    xy[i3].y = xy[i2].y;
                }
            }
            else
            {
                cos = 1f;
                sin = 1f;
            }

            if (!invert) xy[i3].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
            else xy[i1].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
        }
        else
        {
            if (cos > sin)
            {
                sin /= cos;
                cos = 1f;

                if (!invert)
                {
                    xy[i1].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                    xy[i2].y = xy[i1].y;
                }
            }
            else if (sin > cos)
            {
                cos /= sin;
                sin = 1f;

                if (invert)
                {
                    xy[i2].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
                    xy[i3].x = xy[i2].x;
                }
            }
            else
            {
                cos = 1f;
                sin = 1f;
            }

            if (invert) xy[i3].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
            else xy[i1].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
        }
    }

    /// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
    private Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
    {
        //var padding = activeSprite == null ? Vector4.zero : Sprites.DataUtility.GetPadding(activeSprite);
        var padding = Vector4.zero;
        var size = activeSprite == null ? Vector2.zero : new Vector2(activeSprite.rect.width, activeSprite.rect.height);

        //Rect r = GetPixelAdjustedRect();
        // Debug.Log(string.Format("r:{2}, size:{0}, padding:{1}", size, padding, r));

        int spriteW = Mathf.RoundToInt(size.x);
        int spriteH = Mathf.RoundToInt(size.y);

        var v = new Vector4(
            padding.x / spriteW,
            padding.y / spriteH,
            (spriteW - padding.z) / spriteW,
            (spriteH - padding.w) / spriteH);

        if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
        {
            //PreserveSpriteAspectRatio(ref r, size);
        }

        //v = new Vector4(
        //    r.x + r.width * v.x,
        //    r.y + r.height * v.y,
        //    r.x + r.width * v.z,
        //    r.y + r.height * v.w
        //);
        var pos = transform.position;
        var scale = transform.localScale;
        v = new Vector4(
           pos.x + scale.x * v.x,
           pos.y + scale.y * v.y,
           pos.x + scale.x * v.z,
           pos.y + scale.y * v.w
        );
        return v;
    }

    static void AddQuad(VertexHelper vertexHelper, Vector3[] quadPositions, Color32 color, Vector3[] quadUVs)
    {
        int startIndex = vertexHelper.currentVertCount;

        for (int i = 0; i < 4; ++i)
            vertexHelper.AddVert(quadPositions[i], color, quadUVs[i]);

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }

    ///// <summary>
    ///// Returns a pixel perfect Rect closest to the Graphic RectTransform.
    ///// </summary>
    ///// <remarks>
    ///// Note: This is only accurate if the Graphic root Canvas is in Screen Space.
    ///// </remarks>
    ///// <returns>A Pixel perfect Rect.</returns>
    //public Rect GetPixelAdjustedRect()
    //{
    //    //if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0.0f || !canvas.pixelPerfect)
    //    //    return rectTransform.rect;
    //    //else
    //        //return RectTransformUtility.PixelAdjustRect(rectTransform, canvas);
    //}
}