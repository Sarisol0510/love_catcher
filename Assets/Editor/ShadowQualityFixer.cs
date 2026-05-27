using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public static class ShadowQualityFixer
{
    [InitializeOnLoadMethod]
    public static void FixShadows()
    {
        // One-time execution flag
        if (SessionState.GetBool("ShadowFixed_V1", false)) return;
        SessionState.SetBool("ShadowFixed_V1", true);

        var pipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipelineAsset == null)
        {
            Debug.LogError("[ShadowQualityFixer] 현재 적용된 URP 에셋을 찾을 수 없습니다.");
            return;
        }

        SerializedObject so = new SerializedObject(pipelineAsset);
        
        // 1. Anti Aliasing (MSAA 8x) - 전반적인 외곽선 계단 현상 제거
        SerializedProperty msaa = so.FindProperty("m_MSAA");
        if (msaa != null) msaa.intValue = 8; // 8x Multi Sampling

        // 2. Main Light Shadow Resolution - 그림자 해상도를 최고 수준(4096)으로 상향
        SerializedProperty shadowRes = so.FindProperty("m_MainLightShadowmapResolution");
        if (shadowRes != null) shadowRes.intValue = 4096;
        
        // 3. Soft Shadows - 부드러운 그림자 경계선 활성화
        SerializedProperty softShadows = so.FindProperty("m_SoftShadowsSupported");
        if (softShadows != null) softShadows.boolValue = true;
        
        // 4. Shadow Cascades - 카메라 거리에 따른 그림자 품질 유지 (4 Cascades)
        SerializedProperty cascades = so.FindProperty("m_ShadowCascadeCount");
        if (cascades != null) cascades.intValue = 4;

        // 5. Cascade Split 설정 (가까운 곳의 그림자를 더욱 선명하게)
        SerializedProperty cascade2Split = so.FindProperty("m_Cascade2Split");
        if (cascade2Split != null) cascade2Split.floatValue = 0.25f;
        SerializedProperty cascade4Split = so.FindProperty("m_Cascade4Split");
        if (cascade4Split != null) cascade4Split.vector3Value = new Vector3(0.067f, 0.2f, 0.467f);

        // Apply changes
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(pipelineAsset);
        AssetDatabase.SaveAssets();

        Debug.Log("[ShadowQualityFixer] 그림자 해상도(4096), Soft Shadows, 4 Cascades 및 안티앨리어싱(MSAA 8x) 세팅이 적용되어 계단 현상을 제거했습니다!");
    }
}
