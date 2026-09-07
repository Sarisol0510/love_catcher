using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ClawMachine.Utils
{
    /// <summary>
    /// .env 환경변수 파일을 자동으로 탐색하고 파싱하여 제공하는 유틸리티 클래스입니다.
    /// 에디터 및 런타임 시작 시 씬 로드 전에 자동으로 실행됩니다.
    /// </summary>
    public static class EnvLoader
    {
        private static readonly Dictionary<string, string> _envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static bool _isLoaded = false;

        public static bool IsLoaded => _isLoaded;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_isLoaded) return;
            LoadEnv();
        }

        /// <summary>
        /// .env 파일을 탐색하고 로드합니다.
        /// </summary>
        public static void LoadEnv()
        {
            _envVars.Clear();

            // 탐색할 .env 파일 경로 후보 목록
            List<string> candidatePaths = new List<string>
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(Application.dataPath, "..", ".env"),
                Path.Combine(Application.dataPath, ".env"),
                Path.Combine(Application.persistentDataPath, ".env")
            };

            string foundPath = null;
            foreach (var path in candidatePaths)
            {
                try
                {
                    string fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        foundPath = fullPath;
                        break;
                    }
                }
                catch
                {
                    // 경로 파싱 예외 무시
                }
            }

            if (foundPath != null)
            {
                try
                {
                    ParseFile(foundPath);
                    _isLoaded = true;
                    Debug.Log($"[EnvLoader] ✅ .env 파일 로드 성공: {foundPath} (총 {_envVars.Count}개 변수)");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[EnvLoader] .env 파일 읽기 중 오류 발생: {ex.Message}");
                }
            }
            else
            {
                Debug.Log("[EnvLoader] ℹ️ .env 파일을 찾을 수 없습니다. 기본 설정 또는 시스템 환경변수를 사용합니다.");
            }
        }

        private static void ParseFile(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                // 빈 줄 또는 주석(#, //) 건너뛰기
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                {
                    continue;
                }

                int eqIdx = line.IndexOf('=');
                if (eqIdx <= 0) continue;

                string key = line.Substring(0, eqIdx).Trim();
                string val = line.Substring(eqIdx + 1).Trim();

                // 따옴표로 감싸진 경우 제거
                if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                {
                    if (val.Length >= 2)
                    {
                        val = val.Substring(1, val.Length - 2);
                    }
                }

                if (!string.IsNullOrEmpty(key))
                {
                    _envVars[key] = val;

                    // 시스템 환경 변수에도 동기화
                    try
                    {
                        Environment.SetEnvironmentVariable(key, val);
                    }
                    catch
                    {
                        // OS 보안 정책에 따른 예외 무시
                    }
                }
            }
        }

        /// <summary>
        /// 환경 변수 값을 가져옵니다. 값이 없으면 defaultValue를 반환합니다.
        /// </summary>
        public static string Get(string key, string defaultValue = "")
        {
            if (!_isLoaded)
            {
                LoadEnv();
            }

            if (_envVars.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            // 시스템 환경변수 확인 (Fallback)
            string sysVal = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(sysVal))
            {
                return sysVal;
            }

            return defaultValue;
        }

        /// <summary>
        /// 환경 변수 존재 여부를 확인하고 값을 가져옵니다.
        /// </summary>
        public static bool TryGet(string key, out string value)
        {
            value = Get(key, null);
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// 특정 키가 설정되어 있는지 확인합니다.
        /// </summary>
        public static bool HasKey(string key)
        {
            return TryGet(key, out _);
        }
    }
}
