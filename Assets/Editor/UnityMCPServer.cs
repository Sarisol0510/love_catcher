using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Unity MCP Server – AI 오케스트레이터(api.py)와 TCP 소켓으로 통신하는 Unity Editor 서버.
/// [InitializeOnLoad]로 Unity Editor 시작 시 자동으로 port 6400에서 대기합니다.
/// 지원 커맨드: ping, refresh_assets, build_scene, get_compile_status, execute_menu, list_assets
/// </summary>
[InitializeOnLoad]
public static class UnityMCPServer
{
    private static TcpListener _listener;
    private static Thread _listenerThread;
    private static volatile bool _isRunning = false;
    private const int Port = 6400;

    // 백그라운드 스레드에서 읽은 커맨드를 메인 스레드로 전달하는 큐
    private static readonly ConcurrentQueue<(TcpClient client, string commandJson)> _pendingCommands
        = new ConcurrentQueue<(TcpClient, string)>();

    // ─── 초기화 (Unity Editor 시작 시 자동 실행) ────────────────────────────

    static UnityMCPServer()
    {
        Start();
        EditorApplication.update += ProcessPendingCommands;
        EditorApplication.quitting += Stop;
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
    }

    // ─── 서버 시작/종료 ──────────────────────────────────────────────────────

    [MenuItem("Tools/UnityMCP/서버 재시작")]
    public static void RestartServer()
    {
        Stop();
        System.Threading.Thread.Sleep(200);
        Start();
    }

    [MenuItem("Tools/UnityMCP/연결 상태 확인")]
    public static void CheckStatus()
    {
        Debug.Log($"[UnityMCP] 서버 실행 중: {_isRunning}, Port: {Port}, 대기 중인 커맨드: {_pendingCommands.Count}");
    }

    private static void Start()
    {
        if (_isRunning) return;

        try
        {
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();
            _isRunning = true;

            _listenerThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "UnityMCPServer-Accept"
            };
            _listenerThread.Start();

            Debug.Log($"[UnityMCP] ✅ 서버 시작됨 (localhost:{Port}) — API 오케스트레이터와 연결 준비 완료");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UnityMCP] 서버 시작 실패: {e.Message}\n포트 {Port}가 이미 사용 중일 수 있습니다. Tools > UnityMCP > 서버 재시작을 시도하세요.");
        }
    }

    private static void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;

        EditorApplication.update -= ProcessPendingCommands;

        try { _listener?.Stop(); } catch { }
        try { _listenerThread?.Join(1000); } catch { }

        // 대기 중인 클라이언트 연결 정리
        while (_pendingCommands.TryDequeue(out var item))
        {
            try { item.client?.Close(); } catch { }
        }

        Debug.Log("[UnityMCP] 서버 종료됨");
    }

    // ─── 백그라운드: 연결 수락 루프 ─────────────────────────────────────────

    private static void AcceptLoop()
    {
        while (_isRunning)
        {
            try
            {
                if (!_listener.Pending())
                {
                    Thread.Sleep(20);
                    continue;
                }

                TcpClient client = _listener.AcceptTcpClient();
                client.ReceiveTimeout = 5000;

                // 각 클라이언트를 별도 스레드에서 읽기
                Thread t = new Thread(() => ReadCommand(client)) { IsBackground = true };
                t.Start();
            }
            catch (Exception e)
            {
                if (_isRunning)
                    Debug.LogWarning($"[UnityMCP] Accept 오류: {e.Message}");
            }
        }
    }

    private static void ReadCommand(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            stream.ReadTimeout = 5000;
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                if (sb.ToString().Contains("\n")) break;
            }

            string line = sb.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(line))
            {
                _pendingCommands.Enqueue((client, line));
            }
            else
            {
                try { client.Close(); } catch { }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UnityMCP] 커맨드 읽기 오류: {e.Message}");
            try { client.Close(); } catch { }
        }
    }

    // ─── 메인 스레드: 커맨드 처리 (EditorApplication.update에서 호출) ────────

    private static void ProcessPendingCommands()
    {
        while (_pendingCommands.TryDequeue(out var item))
        {
            string responseJson;
            try
            {
                responseJson = ExecuteCommand(item.commandJson);
            }
            catch (Exception e)
            {
                responseJson = "{\"status\":\"error\",\"message\":\"" + EscapeJson(e.Message) + "\"}";
            }

            // 응답 전송 후 연결 종료
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(responseJson + "\n");
                item.client.GetStream().Write(bytes, 0, bytes.Length);
                item.client.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnityMCP] 응답 전송 오류: {e.Message}");
            }
        }
    }

    // ─── 커맨드 실행 (메인 스레드에서만 호출됨) ──────────────────────────────

    private static string ExecuteCommand(string commandJson)
    {
        string command = ExtractJsonStringField(commandJson, "command");

        switch (command)
        {
            // ── ping ──────────────────────────────────────────────────────────
            case "ping":
                return "{\"status\":\"ok\",\"message\":\"pong\",\"unity_version\":\"" + Application.unityVersion + "\"}";

            // ── refresh_assets ────────────────────────────────────────────────
            case "refresh_assets":
                AssetDatabase.Refresh();
                Debug.Log("[UnityMCP] AssetDatabase.Refresh() 실행됨");
                return "{\"status\":\"ok\",\"message\":\"Assets refreshed\"}";

            // ── build_scene ───────────────────────────────────────────────────
            case "build_scene":
                return BuildSceneCommand();

            // ── get_compile_status ────────────────────────────────────────────
            case "get_compile_status":
                bool isCompiling = EditorApplication.isCompiling;
                bool scriptFailed = EditorUtility.scriptCompilationFailed;
                return "{\"status\":\"ok\",\"is_compiling\":" + (isCompiling ? "true" : "false") + ",\"has_error\":" + (scriptFailed ? "true" : "false") + "}";

            case "get_compilation_errors":
                return GetCompilationErrors();

            // ── execute_menu ──────────────────────────────────────────────────
            case "execute_menu":
                string menuPath = ExtractJsonStringField(commandJson, "menu_path");
                if (string.IsNullOrEmpty(menuPath))
                    return "{\"status\":\"error\",\"message\":\"menu_path 필드가 없습니다.\"}";

                bool menuSuccess = EditorApplication.ExecuteMenuItem(menuPath);
                string menuStatus = menuSuccess ? "ok" : "error";
                string menuMsg = menuSuccess ? "실행됨" : "찾을 수 없음";
                return "{\"status\":\"" + menuStatus + "\",\"message\":\"MenuItem '" + EscapeJson(menuPath) + "' " + menuMsg + "\"}";

            // ── list_assets ───────────────────────────────────────────────
            case "list_assets":
                string assetPath = ExtractJsonStringField(commandJson, "directory_path");
                return ListAssetsCommand(assetPath);

            // ── get_scene_hierarchy ───────────────────────────────────────
            case "get_scene_hierarchy":
                return GetSceneHierarchyCommand();

            // ── get_components ────────────────────────────────────────────
            case "get_components":
                string goName = ExtractJsonStringField(commandJson, "game_object_name");
                return GetComponentsCommand(goName);

            // ── find_assets_by_type ───────────────────────────────────────
            case "find_assets_by_type":
                string assetType = ExtractJsonStringField(commandJson, "asset_type");
                return FindAssetsByTypeCommand(assetType);

            // ── read_script ───────────────────────────────────────────────
            case "read_script":
                string scriptPath = ExtractJsonStringField(commandJson, "script_path");
                return ReadScriptCommand(scriptPath);

            // ── import_3d_asset ───────────────────────────────────────────
            case "import_3d_asset":
                string objPath = ExtractJsonStringField(commandJson, "obj_path");
                string texPath = ExtractJsonStringField(commandJson, "texture_path");
                return Import3DAssetCommand(objPath, texPath);

            // ── unknown ───────────────────────────────────────────────────────
            default:
                return "{\"status\":\"error\",\"message\":\"알 수 없는 커맨드: " + EscapeJson(command) + "\"}";
        }
    }

    private static string BuildSceneCommand()
    {
        // SceneBuilder 클래스를 리플렉션으로 찾아서 BuildScene() 호출
        Type sceneBuilderType = FindTypeByName("SceneBuilder");
        if (sceneBuilderType != null)
        {
            MethodInfo buildMethod = sceneBuilderType.GetMethod(
                "BuildScene",
                BindingFlags.Public | BindingFlags.Static
            );
            if (buildMethod != null)
            {
                buildMethod.Invoke(null, null);
                Debug.Log("[UnityMCP] SceneBuilder.BuildScene() 실행됨");
                return "{\"status\":\"ok\",\"message\":\"Scene built via SceneBuilder\"}";
            }
        }

        // Fallback: 메뉴 아이템으로 실행
        bool success = EditorApplication.ExecuteMenuItem("Tools/Build Scene from JSON (Manual)");
        if (success)
            return "{\"status\":\"ok\",\"message\":\"Scene built via menu item (fallback)\"}";

        return "{\"status\":\"error\",\"message\":\"SceneBuilder.cs를 찾을 수 없습니다. /generate-code를 먼저 실행하세요.\"}";
    }

    private static string ListAssetsCommand(string relPath)
    {
        try
        {
            // Assets/ 접두어 제거 (Application.dataPath 자체가 Assets 폴더)
            string cleanPath = relPath.TrimStart('/').TrimStart('\\');
            if (cleanPath.StartsWith("Assets/") || cleanPath.StartsWith("Assets\\"))
                cleanPath = cleanPath.Substring(7);

            string fullPath = Path.Combine(Application.dataPath, cleanPath);

            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"[UnityMCP] list_assets: 디렉토리 없음 → {fullPath}");
                return "{\"status\":\"error\",\"message\":\"디렉토리를 찾을 수 없습니다: " + EscapeJson(relPath) + "\"}";
            }

            string[] assets = Directory.GetFiles(fullPath, "*.asset", SearchOption.TopDirectoryOnly);

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"status\":\"ok\",\"assets\":[");
            for (int i = 0; i < assets.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(assets[i]);
                sb.Append("\"" + EscapeJson(name) + "\"");
                if (i < assets.Length - 1) sb.Append(",");
            }
            sb.Append("]}");

            Debug.Log($"[UnityMCP] list_assets: {relPath}에서 {assets.Length}개 에셋 발견");
            return sb.ToString();
        }
        catch (Exception e)
        {
            return "{\"status\":\"error\",\"message\":\"list_assets 오류: " + EscapeJson(e.Message) + "\"}";
        }
    }

    // ─── Audit: 씬 계층 구조 반환 ───────────────────────────────────────────

    private static string GetSceneHierarchyCommand()
    {
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            var sb = new StringBuilder();
            sb.Append("{\"status\":\"ok\",\"scene_name\":\"" + EscapeJson(scene.name) + "\",\"objects\":[");

            for (int i = 0; i < rootObjects.Length; i++)
            {
                AppendGameObjectJson(sb, rootObjects[i], 0);
                if (i < rootObjects.Length - 1) sb.Append(",");
            }

            sb.Append("]}");
            return sb.ToString();
        }
        catch (Exception e)
        {
            return "{\"status\":\"error\",\"message\":\"get_scene_hierarchy 오류: " + EscapeJson(e.Message) + "\"}";
        }
    }

    private static void AppendGameObjectJson(StringBuilder sb, GameObject go, int depth)
    {
        var comps = go.GetComponents<Component>()
            .Where(c => c != null)
            .Select(c => c.GetType().Name)
            .ToArray();

        var pos = go.transform.position;
        var rot = go.transform.eulerAngles;
        var scl = go.transform.localScale;

        sb.Append("{");
        sb.Append("\"name\":\"" + EscapeJson(go.name) + "\",");
        sb.Append("\"active\":" + (go.activeSelf ? "true" : "false") + ",");
        sb.Append("\"position\":[" + pos.x + "," + pos.y + "," + pos.z + "],");
        sb.Append("\"rotation\":[" + rot.x + "," + rot.y + "," + rot.z + "],");
        sb.Append("\"scale\":[" + scl.x + "," + scl.y + "," + scl.z + "],");
        sb.Append("\"components\":[");
        for (int i = 0; i < comps.Length; i++)
        {
            sb.Append("\"" + EscapeJson(comps[i]) + "\"");
            if (i < comps.Length - 1) sb.Append(",");
        }
        sb.Append("],");

        // 자식 오브젝트
        int childCount = go.transform.childCount;
        sb.Append("\"children\":[");
        for (int i = 0; i < childCount; i++)
        {
            AppendGameObjectJson(sb, go.transform.GetChild(i).gameObject, depth + 1);
            if (i < childCount - 1) sb.Append(",");
        }
        sb.Append("]}");
    }

    // ─── Audit: 특정 오브젝트 컴포넌트 목록 ────────────────────────────────

    private static string GetComponentsCommand(string goName)
    {
        if (string.IsNullOrEmpty(goName))
            return "{\"status\":\"error\",\"message\":\"game_object_name 필드가 없습니다.\"}";

        try
        {
            GameObject go = GameObject.Find(goName);
            if (go == null)
                return "{\"status\":\"error\",\"message\":\"GameObject를 찾을 수 없습니다: " + EscapeJson(goName) + "\"}";

            var comps = go.GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => c.GetType().Name)
                .ToArray();

            var sb = new StringBuilder();
            sb.Append("{\"status\":\"ok\",\"game_object\":\"" + EscapeJson(goName) + "\",\"components\":[");
            for (int i = 0; i < comps.Length; i++)
            {
                sb.Append("\"" + EscapeJson(comps[i]) + "\"");
                if (i < comps.Length - 1) sb.Append(",");
            }
            sb.Append("]}");
            return sb.ToString();
        }
        catch (Exception e)
        {
            return "{\"status\":\"error\",\"message\":\"get_components 오류: " + EscapeJson(e.Message) + "\"}";
        }
    }

    // ─── Audit: 타입별 에셋 검색 (최대 50건) ──────────────────────────────

    private static string FindAssetsByTypeCommand(string assetType)
    {
        try
        {
            string[] extensions;
            switch (assetType.ToLower())
            {
                case "mesh":    extensions = new[] { ".fbx", ".obj", ".glb", ".gltf", ".blend" }; break;
                case "prefab":  extensions = new[] { ".prefab" }; break;
                case "script":  extensions = new[] { ".cs" }; break;
                case "texture": extensions = new[] { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tiff" }; break;
                case "material":extensions = new[] { ".mat" }; break;
                case "scene":   extensions = new[] { ".unity" }; break;
                default:        extensions = new[] { "." + assetType.ToLower() }; break;
            }

            string assetsRoot = Application.dataPath;
            var found = new List<string>();

            foreach (string ext in extensions)
            {
                var files = Directory.GetFiles(assetsRoot, "*" + ext, SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    if (found.Count >= 2000) break;
                    // Assets/ 상대 경로로 변환
                    string rel = "Assets" + f.Substring(assetsRoot.Length).Replace('\\', '/');
                    found.Add(rel);
                }
                if (found.Count >= 2000) break;
            }

            var sb = new StringBuilder();
            sb.Append("{\"status\":\"ok\",\"asset_type\":\"" + EscapeJson(assetType) + "\",\"assets\":[");
            for (int i = 0; i < found.Count; i++)
            {
                sb.Append("\"" + EscapeJson(found[i]) + "\"");
                if (i < found.Count - 1) sb.Append(",");
            }
            sb.Append("]}");
            return sb.ToString();
        }
        catch (Exception e)
        {
            return "{\"status\":\"error\",\"message\":\"find_assets_by_type 오류: " + EscapeJson(e.Message) + "\"}";
        }
    }

    // ─── Audit: 스크립트 파일 내용 읽기 ─────────────────────────────────────

    private static string ReadScriptCommand(string scriptPath)
    {
        if (string.IsNullOrEmpty(scriptPath))
            return "{\"status\":\"error\",\"message\":\"script_path 필드가 없습니다.\"}";

        try
        {
            // Assets/ 상대경로 → 절대경로 변환
            string cleanPath = scriptPath.TrimStart('/');
            if (cleanPath.StartsWith("Assets/") || cleanPath.StartsWith("Assets\\"))
                cleanPath = cleanPath.Substring(7);

            string fullPath = Path.Combine(Application.dataPath, cleanPath);

            if (!File.Exists(fullPath))
                return "{\"status\":\"error\",\"message\":\"파일을 찾을 수 없습니다: " + EscapeJson(scriptPath) + "\"}";

            string content = File.ReadAllText(fullPath, Encoding.UTF8);

            // 내용이 4000자를 초과하면 앞부분만 반환 (토큰 절약)
            if (content.Length > 4000)
                content = content.Substring(0, 4000) + "\n// ... (truncated)";

            return "{\"status\":\"ok\",\"path\":\"" + EscapeJson(scriptPath) + "\",\"content\":\"" + EscapeJson(content) + "\"}";
        }
        catch (Exception e)
        {
            return "{\"status\":\"error\",\"message\":\"read_script 오류: " + EscapeJson(e.Message) + "\"}";
        }
    }

    private static string NormalizeToAssetsPath(string originalPath)
    {
        if (string.IsNullOrEmpty(originalPath)) return "";

        string normalized = originalPath.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');

        if (normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            return "Assets" + normalized.Substring(dataPath.Length);

        int assetsIndex = normalized.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
        if (assetsIndex >= 0)
            return normalized.Substring(assetsIndex + 1);

        if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return normalized;

        return normalized;
    }

    private static string GetCompilationErrors()
    {
        try
        {
            if (EditorApplication.isCompiling)
                return "{\"status\":\"ok\",\"is_compiling\":true,\"has_error\":false,\"errors\":[]}";

            if (!EditorUtility.scriptCompilationFailed)
                return "{\"status\":\"ok\",\"has_error\":false,\"errors\":[]}";

            var errors = new List<string>();

            // ── Strategy 1: CompilationPipeline API (Unity 2019.4+, 가장 안정적) ──
            try
            {
                var pipelineType = typeof(UnityEditor.Compilation.CompilationPipeline);
                var getMessagesMethod = pipelineType.GetMethod(
                    "GetCompilerMessages",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    Type.EmptyTypes,
                    null
                );

                // Unity 2021+ 에는 파라미터 없는 오버로드가 있을 수 있음
                if (getMessagesMethod == null)
                {
                    // GetCompilerMessages() 없으면 어셈블리별로 가져오기
                    var assembliesMethod = pipelineType.GetMethod("GetAssemblies", BindingFlags.Public | BindingFlags.Static);
                    if (assembliesMethod != null)
                    {
                        var assemblies = assembliesMethod.Invoke(null, null) as Array;
                        // 어셈블리별 에러는 복잡하므로 Strategy 2로 넘김
                    }
                }
            }
            catch { /* 무시하고 다음 전략으로 */ }

            // ── Strategy 2: LogEntries 리플렉션 (다양한 Unity 버전 대응) ──
            if (errors.Count == 0)
            {
                try
                {
                    var editorAssembly = typeof(EditorWindow).Assembly;
                    var logEntriesType = editorAssembly.GetType("UnityEditor.LogEntries");

                    if (logEntriesType != null)
                    {
                        // Unity 6에서는 GetEntryInternal 또는 GetLinesAndModeFromEntryInternal 사용
                        var startMethod = logEntriesType.GetMethod("StartGettingEntries",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        var endMethod = logEntriesType.GetMethod("EndGettingEntries",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        var getCountMethod = logEntriesType.GetMethod("GetCount",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                        // GetEntryInternal(int row, LogEntry entry) 시도
                        var logEntryType = editorAssembly.GetType("UnityEditor.LogEntry");
                        var getEntryInternal = logEntriesType.GetMethod("GetEntryInternal",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                        if (getCountMethod != null && getEntryInternal != null && logEntryType != null)
                        {
                            if (startMethod != null) startMethod.Invoke(null, null);

                            try
                            {
                                int count = (int)getCountMethod.Invoke(null, null);
                                object logEntry = Activator.CreateInstance(logEntryType);

                                // LogEntry의 필드 조회 (Unity 버전별로 이름이 다를 수 있음)
                                var conditionField = logEntryType.GetField("message", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?? logEntryType.GetField("condition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                var modeField = logEntryType.GetField("mode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?? logEntryType.GetField("flags", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                var fileField = logEntryType.GetField("file", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                var lineField = logEntryType.GetField("line", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?? logEntryType.GetField("lineNumber", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                                for (int i = 0; i < count && i < 100; i++)
                                {
                                    bool ok = (bool)getEntryInternal.Invoke(null, new object[] { i, logEntry });
                                    if (!ok) continue;

                                    string message = conditionField != null ? Convert.ToString(conditionField.GetValue(logEntry)) : "";
                                    int mode = modeField != null ? Convert.ToInt32(modeField.GetValue(logEntry)) : 0;

                                    bool isError = (mode & 1) != 0 || (mode & 32) != 0 || (mode & 128) != 0;
                                    bool isCompileError = !string.IsNullOrEmpty(message) &&
                                        (message.Contains("error CS") || message.Contains("): error ") || message.Contains("Compilation failed"));

                                    if (!isError && !isCompileError) continue;

                                    string file = fileField != null ? Convert.ToString(fileField.GetValue(logEntry)) : "";
                                    int line = 0;
                                    if (lineField != null) try { line = Convert.ToInt32(lineField.GetValue(logEntry)); } catch { }

                                    string normalizedFile = NormalizeToAssetsPath(file);
                                    if (!string.IsNullOrEmpty(normalizedFile))
                                        errors.Add($"{normalizedFile}:{line} - {message}");
                                    else
                                        errors.Add(message);
                                }
                            }
                            finally
                            {
                                if (endMethod != null) endMethod.Invoke(null, null);
                            }
                        }
                    }
                }
                catch { /* Strategy 2 실패 — Strategy 3으로 */ }
            }

            // ── Strategy 3: Editor.log 파일 직접 파싱 (최후의 수단) ──
            if (errors.Count == 0)
            {
                try
                {
                    string logPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Unity", "Editor", "Editor.log"
                    );

                    if (File.Exists(logPath))
                    {
                        // 파일 끝에서부터 읽어서 최근 에러를 가져옴
                        using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs, Encoding.UTF8))
                        {
                            // 마지막 5000자만 읽기
                            if (fs.Length > 5000)
                                fs.Seek(-5000, SeekOrigin.End);

                            string tail = reader.ReadToEnd();
                            string[] lines = tail.Split('\n');

                            foreach (string rawLine in lines)
                            {
                                string ln = rawLine.Trim();
                                if (ln.Contains("error CS") || (ln.Contains("): error ") && ln.Contains(".cs(")))
                                {
                                    errors.Add(ln);
                                }
                            }
                        }
                    }
                }
                catch { /* Editor.log 접근 실패 */ }
            }

            // ── 결과 반환 ──
            if (errors.Count == 0)
                return "{\"status\":\"ok\",\"has_error\":true,\"errors\":[],\"message\":\"컴파일 실패 감지됨. Unity Console에서 상세 에러를 확인하세요.\"}";

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"status\":\"ok\",\"has_error\":true,\"errors\":[");
            for (int i = 0; i < errors.Count; i++)
            {
                sb.Append("\"" + EscapeJson(errors[i]) + "\"");
                if (i < errors.Count - 1) sb.Append(",");
            }
            sb.Append("]}");
            return sb.ToString();
        }
        catch (Exception e)
        {
            return "{\"status\":\"error\",\"message\":\"get_compilation_errors 오류: " + EscapeJson(e.Message) + "\"}";
        }
    }


    // ─── 유니티 3D 에셋 자동 임포트 & URP 머티리얼 구성 ────────────────────────

    private static string Import3DAssetCommand(string objRelPath, string texRelPath)
    {
        try
        {
            // 1. 에셋 리프레시 (유니티가 파일을 인식하도록)
            AssetDatabase.Refresh();

            // 2. 텍스처와 메쉬 임포트 상태 확인
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texRelPath);
            GameObject objPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(objRelPath);

            if (objPrefab == null)
                return "{\"status\":\"error\",\"message\":\"OBJ 프리팹을 로드할 수 없습니다: " + EscapeJson(objRelPath) + "\"}";

            // 3. 머티리얼 생성 (URP 지원 시 URP Lit, 아니면 Standard)
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader == null)
                return "{\"status\":\"error\",\"message\":\"적절한 셰이더를 찾을 수 없습니다.\"}";

            Material mat = new Material(shader);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex); // URP
                else if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex); // Standard
            }

            // 머티리얼 저장
            string matPath = Path.Combine(Path.GetDirectoryName(objRelPath), Path.GetFileNameWithoutExtension(objRelPath) + "_Material.mat").Replace('\\', '/');
            AssetDatabase.CreateAsset(mat, matPath);

            // 4. 새 프리팹 조립 (기본 OBJ에서 메쉬를 가져와 머티리얼 적용)
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(objPrefab);
            MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                r.sharedMaterial = mat;
            }

            // 5. 조립된 게임오브젝트를 프리팹으로 저장
            string finalPrefabPath = Path.Combine(Path.GetDirectoryName(objRelPath), Path.GetFileNameWithoutExtension(objRelPath) + "_Final.prefab").Replace('\\', '/');
            PrefabUtility.SaveAsPrefabAsset(instance, finalPrefabPath);
            GameObject.DestroyImmediate(instance);

            AssetDatabase.Refresh();

            return "{\"status\":\"ok\",\"message\":\"3D Asset imported and configured for URP: " + EscapeJson(finalPrefabPath) + "\"}";
        }
        catch (Exception e)
        {
            return "{\"status\":\"error\",\"message\":\"import_3d_asset 오류: " + EscapeJson(e.Message) + "\"}";
        }
    }

    // ─── 유틸리티 ─────────────────────────────────────────────────────────────

    /// <summary>외부 라이브러리 없이 JSON 문자열 필드를 추출합니다.</summary>
    private static string ExtractJsonStringField(string json, string fieldName)
    {
        string key = "\"" + fieldName + "\"";
        int keyIdx = json.IndexOf(key, StringComparison.Ordinal);
        if (keyIdx < 0) return "";

        int colonIdx = json.IndexOf(':', keyIdx + key.Length);
        if (colonIdx < 0) return "";

        int valueStart = colonIdx + 1;
        while (valueStart < json.Length && (json[valueStart] == ' ' || json[valueStart] == '\t'))
            valueStart++;

        if (valueStart >= json.Length) return "";

        if (json[valueStart] == '"')
        {
            int end = json.IndexOf('"', valueStart + 1);
            if (end < 0) return json.Substring(valueStart + 1);
            return json.Substring(valueStart + 1, end - valueStart - 1);
        }

        // 비문자열 값 (숫자, bool, null)
        int nonStringEnd = json.IndexOfAny(new[] { ',', '}', ']', ' ', '\n', '\r' }, valueStart);
        return nonStringEnd < 0 ? json.Substring(valueStart) : json.Substring(valueStart, nonStringEnd - valueStart);
    }

    private static string EscapeJson(string str)
    {
        if (str == null) return "";
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    private static Type FindTypeByName(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string name = assembly.FullName;
            if (!name.StartsWith("Unity") && !name.StartsWith("Assembly-CSharp"))
                continue;

            foreach (var type in assembly.GetTypes())
            {
                if (type.Name == typeName)
                    return type;
            }
        }
        return null;
    }
}
