using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System;

public class SceneBuilder : AssetPostprocessor
{
    [Serializable]
    public class SceneObject
    {
        public string name;
        public string action; // "create" | "modify" | "delete"
        public string mesh_name; // 매쉬를 할당할 경우 파일명 (예: House)
        public List<string> components;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public string parent;
    }

    [Serializable]
    public class SceneData
    {
        public List<SceneObject> objects;
    }

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string str in importedAssets)
        {
            if (str.EndsWith("scene_setup.json"))
            {
                Debug.Log("[SceneBuilder] scene_setup.json detected — building scene...");
                BuildScene();
                break;
            }
        }
    }

    void OnPreprocessModel()
    {
        string path = assetPath.Replace("\\", "/");
        if (path.StartsWith("Assets/Models/"))
        {
            ModelImporter importer = assetImporter as ModelImporter;
            if (importer != null)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                
                // [가장 중요] 메쉬가 깨지는 원인 완벽 차단
                importer.weldVertices = false; 
                importer.optimizeMeshVertices = false;
                importer.optimizeMeshPolygons = false;
                importer.importNormals = ModelImporterNormals.Import;
                importer.bakeAxisConversion = true; // [FIX] 좌표계 오류(누운 모델) 방지
            }
        }
    }

    void OnPreprocessTexture()
    {
        if (assetPath.Replace("\\", "/").StartsWith("Assets/Models/"))
        {
            TextureImporter importer = assetImporter as TextureImporter;
            if (importer != null)
            {
                // TripoSR의 xatlas 조각들이 Unity 압축/밉맵에 의해 서로 번지는(Bleeding) 것을 완벽 차단
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp; // 텍스처 늘어짐(Ghosting) 방지
            }
        }
    }

    void OnPostprocessModel(GameObject g)
    {
        if (g == null) return;
        string path = assetPath.Replace("\\", "/");
        if (path.StartsWith("Assets/Models/"))
        {
            string mName = System.IO.Path.GetFileNameWithoutExtension(path);
            string modelFolder = $"Assets/Models/{mName}";
            string texPath = $"{modelFolder}/texture.png";
            string matPath = $"{modelFolder}/{mName}_Mat.mat";

            EditorApplication.delayCall += () =>
            {
                // [Hardening] Parent directory must exist 방지
                if (!AssetDatabase.IsValidFolder(modelFolder)) {
                    string parent = "Assets/Models";
                    if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", "Models");
                    AssetDatabase.CreateFolder("Assets/Models", mName);
                }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    // TripoSR 텍스처는 라이팅 영향 없이 보이도록 커스텀 셰이더 우선
                    bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
                    Shader targetShader = isURP ? Shader.Find("Custom/URPVertexColor") : Shader.Find("Custom/UnlitVertexColor");
                    
                    if (targetShader == null || !targetShader.isSupported || targetShader.name.Contains("Error")) 
                    {
                        targetShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
                    }
                    if (targetShader == null) targetShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (targetShader == null) targetShader = Shader.Find("Standard");

                    if (targetShader != null)
                    {
                        mat = new Material(targetShader);
                        AssetDatabase.CreateAsset(mat, matPath);
                    }
                }

                if (mat != null)
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                    
                    bool hasUVs = false;
                    if (System.IO.File.Exists(path)) {
                        using (var reader = new System.IO.StreamReader(path)) {
                            string line;
                            int linesChecked = 0;
                            while ((line = reader.ReadLine()) != null && linesChecked < 10000) {
                                if (line.StartsWith("vt ")) {
                                    hasUVs = true;
                                    break;
                                }
                                linesChecked++;
                            }
                        }
                    }

                    if (tex != null && hasUVs)
                    {
                        // 텍스처 할당
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex); // URP
                        else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex); // Standard

                        // [핵심] 머티리얼의 Tiling/Offset을 이용해 안전하게 V축 뒤집기
                        Vector2 flipScale = new Vector2(1, -1);
                        Vector2 flipOffset = new Vector2(0, 1);

                        if (mat.HasProperty("_BaseMap")) {
                            mat.SetTextureScale("_BaseMap", flipScale);
                            mat.SetTextureOffset("_BaseMap", flipOffset);
                        }
                        else if (mat.HasProperty("_MainTex")) {
                            mat.SetTextureScale("_MainTex", flipScale);
                            mat.SetTextureOffset("_MainTex", flipOffset);
                        }
                    }
                    else
                    {
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
                        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
                    }
                    
                    EditorUtility.SetDirty(mat);
                        AssetDatabase.SaveAssets();
                }
            };
        }
    }

    [MenuItem("Tools/Build Scene from JSON (Manual)")]
    public static void BuildScene()
    {
        string path = "Assets/scene_setup.json";
        if (!File.Exists(path)) {
            Debug.LogError("[SceneBuilder] scene_setup.json not found at " + path);
            return;
        }

        string json = File.ReadAllText(path);
        SceneData data = JsonUtility.FromJson<SceneData>(json);

        if (data == null || data.objects == null) {
            Debug.LogError("[SceneBuilder] Failed to parse scene_setup.json or objects list is empty.");
            return;
        }

        Debug.Log($"[SceneBuilder] Processing {data.objects.Count} objects...");

        foreach (var objData in data.objects)
        {
            string action = (objData.action ?? "create").Trim().ToLower();
            switch (action)
            {
                case "delete":
                    DeleteObject(objData);
                    break;
                case "modify":
                    ModifyObject(objData);
                    break;
                case "create":
                default:
                    CreateObject(objData);
                    break;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[SceneBuilder] Scene Build Complete!");
    }

    private static void CreateObject(SceneObject objData)
    {
        GameObject go = GameObject.Find(objData.name);
        if (go == null) {
            go = new GameObject(objData.name);
            Debug.Log($"[SceneBuilder] Created: {objData.name}");
        } else {
            Debug.Log($"[SceneBuilder] Already exists (skipping duplicate create): {objData.name}");
            return;
        }
        ApplyTransformAndComponents(go, objData);
    }

    private static void ModifyObject(SceneObject objData)
    {
        GameObject go = GameObject.Find(objData.name);
        if (go == null) {
            Debug.LogWarning($"[SceneBuilder] MODIFY target not found, creating instead: {objData.name}");
            go = new GameObject(objData.name);
        } else {
            Debug.Log($"[SceneBuilder] Modifying: {objData.name}");
        }
        ApplyTransformAndComponents(go, objData);
    }

    private static void DeleteObject(SceneObject objData)
    {
        GameObject go = GameObject.Find(objData.name);
        if (go != null) {
            Undo.DestroyObjectImmediate(go);
            Debug.Log($"[SceneBuilder] Deleted: {objData.name}");
        } else {
            Debug.LogWarning($"[SceneBuilder] DELETE target not found (already gone?): {objData.name}");
        }
    }

    private static void ApplyTransformAndComponents(GameObject go, SceneObject objData)
    {
        Undo.RecordObject(go.transform, "SceneBuilder Apply");

        if (objData.position != null && objData.position.Length == 3)
            go.transform.position = new Vector3(objData.position[0], objData.position[1], objData.position[2]);

        if (objData.rotation != null && objData.rotation.Length == 3)
            go.transform.eulerAngles = new Vector3(objData.rotation[0], objData.rotation[1], objData.rotation[2]);

        if (objData.scale != null && objData.scale.Length == 3)
            go.transform.localScale = new Vector3(objData.scale[0], objData.scale[1], objData.scale[2]);

        if (!string.IsNullOrEmpty(objData.parent))
        {
            GameObject parentGo = GameObject.Find(objData.parent);
            if (parentGo != null) go.transform.SetParent(parentGo.transform, true);
        }

        if (objData.components != null)
        {
            foreach (string compName in objData.components)
            {
                Type type = GetTypeByName(compName);
                if (type != null)
                {
                    if (go.GetComponent(type) == null) {
                        Undo.AddComponent(go, type);
                        Debug.Log($"[SceneBuilder] Added component {compName} to {go.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[SceneBuilder] Component type not found: {compName}");
                }
            }
        }

        // --- 자동 Mesh & Material 할당 (MeshFilter, MeshRenderer 연동) ---
        string mName = string.IsNullOrEmpty(objData.mesh_name) ? objData.name : objData.mesh_name;
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh == null)
        {
            string objPath = $"Assets/Models/{mName}/{mName}.obj";
            string fbxPath = $"Assets/Models/{mName}/{mName}.fbx";
            
            Mesh mesh = null;
            // OBJ 우선 탐색
            if (File.Exists(objPath)) mesh = AssetDatabase.LoadAssetAtPath<Mesh>(objPath);
            if (mesh == null) mesh = AssetDatabase.LoadAssetAtPath<Mesh>(fbxPath);

            if (mesh != null) {
                mf.sharedMesh = mesh;
                Debug.Log($"[SceneBuilder] Auto-assigned Mesh ({mesh.name}) to {go.name}");
            }
        }

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mf != null && mf.sharedMesh != null)
        {
            string matPath = $"Assets/Models/{mName}/{mName}_Mat.mat";
            Material localMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Texture");

            if (localMat == null && unlitShader != null) {
                localMat = new Material(unlitShader);
                localMat.name = $"{mName}_Mat";
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Models/{mName}/texture.png");
                
                bool hasUVs = false;
                string objPath = $"Assets/Models/{mName}/{mName}.obj";
                if (System.IO.File.Exists(objPath)) {
                    using (var reader = new System.IO.StreamReader(objPath)) {
                        string line;
                        int linesChecked = 0;
                        while ((line = reader.ReadLine()) != null && linesChecked < 10000) {
                            if (line.StartsWith("vt ")) {
                                hasUVs = true;
                                break;
                            }
                            linesChecked++;
                        }
                    }
                }

                if (tex != null && hasUVs) {
                    if (localMat.HasProperty("_BaseMap")) localMat.SetTexture("_BaseMap", tex);
                    if (localMat.HasProperty("_MainTex")) localMat.SetTexture("_MainTex", tex);
                } else {
                    if (localMat.HasProperty("_BaseMap")) localMat.SetTexture("_BaseMap", null);
                    if (localMat.HasProperty("_MainTex")) localMat.SetTexture("_MainTex", null);
                }
                AssetDatabase.CreateAsset(localMat, matPath);
            }

            if (localMat != null && unlitShader != null && localMat.shader != unlitShader) {
                localMat.shader = unlitShader;
                EditorUtility.SetDirty(localMat);
                AssetDatabase.SaveAssets();
            }
            
            mr.sharedMaterial = localMat;
            if (localMat != null) {
                Debug.Log($"[SceneBuilder] Auto-assigned Material {localMat.name} to {go.name}");
            }
        }
    }

    private static Type GetTypeByName(string name)
    {
        Type t = Type.GetType(name);
        if (t != null) return t;
        string[] namespaces = { "", "UnityEngine.", "UnityEngine.UI.", "UnityEngine.Rendering." };
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var ns in namespaces)
            {
                t = assembly.GetType(ns + name);
                if (t != null) return t;
            }
        }
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.FullName.StartsWith("Unity") || assembly.FullName.StartsWith("Assembly-CSharp"))
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == name) return type;
                }
            }
        }
        return null;
    }
}
