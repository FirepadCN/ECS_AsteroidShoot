using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rain : PostEffectsBase {

    public Shader shader;
    public float Speed=0.1f;
    public int TileNum=5;
    public int AspectRatio=4;
    public int TailTileNum=3;
    public int Period=5;
    public float Angle=15;
    private Material material;

    public Material Material
    {
        get
        {
            material = CheckShaderAndCreateMaterial(shader, material);
            return material;
        }
    }


    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (Material != null)
        {
            Material.SetFloat("_Speed",Speed);
            Material.SetInt("_TileNum", TileNum);
            Material.SetInt("_AspectRatio", AspectRatio);
            Material.SetInt("_TailTileNum",TailTileNum);
            Material.SetInt("_Period", Period);
            Material.SetFloat("_Angle", Angle);
            Graphics.Blit(src, dest, Material);
        }
        else
            Graphics.Blit(src, dest);
    }
}
