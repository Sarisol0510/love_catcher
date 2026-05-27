using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// OBJ 파일 임포트 시, 동일 폴더에 있는 texture.png를 찾아 
/// URP Lit 머티리얼을 자동 생성하고 텍스처를 할당해주는 스크립트입니다.
/// </summary>
public class URPTextureAutoAssigner : AssetPostprocessor
{
    // 1. 모델 임포트 전 설정
    void OnPreprocessModel()
    {
        if (!assetPath.ToLower().EndsWith(".obj")) return;

        ModelImporter importer = assetImporter as ModelImporter;
        if (importer != null)
        {
            // 머티리얼을 모델 내부에 숨기지 않고 외부(.mat)로 추출하도록 설정
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
        }

        // 임포트 순서 문제 해결: OBJ보다 텍스처가 나중에 임포트되면 텍스처를 찾지 못함.
        // 따라서 OBJ 임포트 직전에 텍스처를 강제로 먼저 임포트합니다.
        string dir = Path.GetDirectoryName(assetPath).Replace("\\", "/");
        string texPath = dir + "/texture.png";
        
        if (File.Exists(texPath))
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
            {
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }

    // 2. 머티리얼 생성 시점에 텍스처 할당
    void OnPostprocessMaterial(Material material)
    {
        if (!assetPath.ToLower().EndsWith(".obj")) return;

        // URP Lit 셰이더 찾기 (없으면 Standard)
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard");

        if (urpShader != null)
        {
            material.shader = urpShader;
        }

        // 텍스처 적용
        string dir = Path.GetDirectoryName(assetPath).Replace("\\", "/");
        string texPath = dir + "/texture.png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        if (tex != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", tex);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", tex);

            material.SetColor("_BaseColor", Color.white);
            
            Debug.Log($"[AutoTextureAssigner] {assetPath} 에 texture.png 자동 할당 완료 (URP Lit)");
        }
        else
        {
            Debug.LogWarning($"[AutoTextureAssigner] {texPath} 를 로드하지 못했습니다.");
        }
    }
}
