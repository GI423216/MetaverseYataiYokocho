using System;
using UnityEngine;

public class WallCtrl : MonoBehaviour
{
    [Header("フェード設定")]
    public float minFadeDistance;
    public float maxFadeDistance;

    private const float MIN_ALPHA = 0f;
    private const float MAX_ALPHA = 0.5f;

    private Transform cameraTransform;
    private Renderer wallRenderer;
    private Material wallMaterial;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        wallRenderer = GetComponent<Renderer>();
        wallMaterial = wallRenderer.material;
        wallMaterial.SetFloat("_Mode", 2); // 2: Fade, 3: Transparent
        wallMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        wallMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        wallMaterial.SetInt("_ZWrite", 0);
        wallMaterial.DisableKeyword("_ALPHATEST_ON");
        wallMaterial.EnableKeyword("_ALPHABLEND_ON");
        wallMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        wallMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    void Update()
    {
        if (cameraTransform == null || wallMaterial == null) return;

        float distance = Vector3.Distance(transform.position, cameraTransform.position);//カメラとの差
        float t = Mathf.InverseLerp(maxFadeDistance, minFadeDistance, distance);

        float Alpha = Mathf.Lerp(MAX_ALPHA, MIN_ALPHA, t);

        Color color = wallMaterial.color;
        color.a = Alpha;
        wallMaterial.color = color;
    }
}