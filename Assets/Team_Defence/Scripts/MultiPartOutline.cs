using System.Collections.Generic;
using UnityEngine;

public class MultiPartOutline : MonoBehaviour
{
    [Tooltip("分配上面创建的自定义 Outline 材质")]
    public Material outlineMaterial;

    private Renderer[] childRenderers;
    private bool isOutlined = false;

    private void Awake()
    {
        // 自动获取根节点及所有子物体的 MeshRenderer / SkinnedMeshRenderer
        childRenderers = GetComponentsInChildren<Renderer>();
    }

    public void EnableOutline()
    {
        if (isOutlined) return;
        isOutlined = true;

        foreach (Renderer ren in childRenderers)
        {
            List<Material> mats = new List<Material>(ren.sharedMaterials);
            if (!mats.Contains(outlineMaterial))
            {
                mats.Add(outlineMaterial);
                ren.materials = mats.ToArray(); // 更新材质数组
            }
        }
    }

    public void DisableOutline()
    {
        if (!isOutlined) return;
        isOutlined = false;

        foreach (Renderer ren in childRenderers)
        {
            List<Material> mats = new List<Material>(ren.sharedMaterials);
            if (mats.Contains(outlineMaterial))
            {
                mats.Remove(outlineMaterial);
                ren.materials = mats.ToArray();
            }
        }
    }
}