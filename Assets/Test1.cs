using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Test1 : MonoBehaviour
{
    //public SpriteRenderer spriteRenderer;
    public Sprite sprite;
    public Mesh mesh;
    public MeshRenderer meshRenderer;
    public Material material;

    public void Update()
    {
        VertexHelper vh = new VertexHelper();
        vh.Clear();

        // 添加顶点
        vh.AddVert(new Vector3(0, 0, 0), Color.red, new Vector2(0, 0));
        vh.AddVert(new Vector3(1, 0, 0), Color.green, new Vector2(1, 0));
        vh.AddVert(new Vector3(1, 1, 0), Color.yellow, new Vector2(1, 1));
        vh.AddVert(new Vector3(0, 1, 0), Color.cyan, new Vector2(0, 1));

        // 设置三角形顺序
        vh.AddTriangle(0, 2, 1);
        vh.AddTriangle(0, 3, 2);

        // 将结果展示出来
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.name = "Quad";
        vh.FillMesh(mesh);
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.material = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

}
