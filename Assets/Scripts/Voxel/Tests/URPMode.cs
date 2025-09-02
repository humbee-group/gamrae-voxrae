// Assets/Scripts/Voxel/Tests/URPMode.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class URPMode : MonoBehaviour
{
    void Start()
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
        {
            Debug.Log("Pas d'URP actif.");
            return;
        }

        var cam = Camera.main;
        if (cam == null)
        {
            Debug.Log("Pas de Camera.main");
            return;
        }

        var data = cam.GetUniversalAdditionalCameraData();
        var renderer = data?.scriptableRenderer;
        if (renderer == null)
        {
            Debug.Log("Pas de renderer URP détecté.");
            return;
        }

        Debug.Log($"Renderer actif : {renderer.GetType().Name}");
        // → si c'est "ForwardRenderer" → tu es en Forward
        // → si c'est "DeferredRenderer" → tu es en Deferred
    }
}