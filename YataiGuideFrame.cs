using UnityEngine;

public class YataiGuideFrame : MonoBehaviour
{
    [Header("マテリアル設定")]
    [SerializeField] private Material frameMaterial;
    [SerializeField] private Material frontMarkMaterial;

    void Start()
    {
        CreateLineObject("Guide_Frame", GetFramePoints(), frameMaterial, 0.05f, true);

        CreateLineObject("Guide_FrontMark", GetFrontMarkPoints(), frontMarkMaterial, 0.12f, false);
    }

    void CreateLineObject(string name, Vector3[] points, Material mat, float width, bool loop)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(this.transform, false);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = lr.endWidth = width;
        lr.loop = loop;
        lr.positionCount = points.Length;
        lr.SetPositions(points);

        if (mat != null)
        {
            lr.material = mat;
        }
        else
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    Vector3[] GetFramePoints()
    {
        float w = 1.5f;
        float d = 1.25f;
        float y = 0.01f;
        return new Vector3[] {
            new Vector3(-w, y, -d),
            new Vector3( w, y, -d),
            new Vector3( w, y,  d),
            new Vector3(-w, y,  d)
        };
    }

    //くの字マーク
    Vector3[] GetFrontMarkPoints()
    {
        float d = 1.25f;
        float y = 0.02f;
        float tipZ = d + 0.6f;
        float wingZ = d + 0.2f;
        float wingX = 0.4f;

        return new Vector3[] {
            new Vector3(-wingX, y, wingZ), //左
            new Vector3(0,      y, tipZ), //先端
            new Vector3( wingX, y, wingZ) //右
        };
    }
}