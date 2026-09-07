using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using ClawMachine.Utils;

namespace ClawMachine.Mechanics
{
    public class FirebaseRESTService : MonoBehaviour
    {
        private static FirebaseRESTService instance;
        public static FirebaseRESTService Instance
        {
            get
            {
                if (instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    instance = FindFirstObjectByType<FirebaseRESTService>();
#else
                    instance = FindObjectOfType<FirebaseRESTService>();
#endif
                    if (instance == null)
                    {
                        GameObject go = new GameObject("FirebaseRESTService");
                        instance = go.AddComponent<FirebaseRESTService>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [Header("Firebase Config")]
        [Tooltip("Firebase 콘솔에서 확인할 수 있는 프로젝트 고유 ID")]
        public string firebaseProjectId;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadConfig();
        }

        [Serializable]
        private class FirebaseConfig
        {
            public string firebaseProjectId;
        }

        private void LoadConfig()
        {
            // 1순위: .env 환경변수 파일 또는 시스템 환경변수에서 로드
            string envProjectId = EnvLoader.Get("FIREBASE_PROJECT_ID");
            if (!string.IsNullOrEmpty(envProjectId) && envProjectId != "your-firebase-project-id")
            {
                firebaseProjectId = envProjectId;
                Debug.Log($"[Firebase] ✅ .env 환경설정에서 Project ID 로드 완료: {firebaseProjectId}");
                return;
            }

            // 2순위: Resources/FirebaseConfig.json에서 로드
            if (string.IsNullOrEmpty(firebaseProjectId) || firebaseProjectId == "your-firebase-project-id")
            {
                TextAsset configAsset = Resources.Load<TextAsset>("FirebaseConfig");
                if (configAsset != null)
                {
                    try
                    {
                        FirebaseConfig config = JsonUtility.FromJson<FirebaseConfig>(configAsset.text);
                        if (config != null && !string.IsNullOrEmpty(config.firebaseProjectId) && config.firebaseProjectId != "your-firebase-project-id")
                        {
                            firebaseProjectId = config.firebaseProjectId;
                            Debug.Log($"[Firebase] Resources/FirebaseConfig에서 Project ID 로드 성공: {firebaseProjectId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Firebase] FirebaseConfig 로드 실패: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 참가자 정보를 Firestore에 신규 등록합니다.
        /// </summary>
        public IEnumerator RegisterPlayer(string name, string insta, string bio, string gender, int attempts, Action<bool> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                Debug.LogError("[Firebase] Project ID가 설정되지 않았습니다.");
                callback?.Invoke(false);
                yield break;
            }

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/Participants";

            // Firestore 문서 규격 빌드
            FirestoreDocument doc = new FirestoreDocument();
            doc.fields = new FirestoreFields();
            doc.fields.name = new FirestoreStringField(name);
            doc.fields.insta = new FirestoreStringField(insta);
            doc.fields.bio = new FirestoreStringField(bio);
            doc.fields.gender = new FirestoreStringField(gender);
            doc.fields.isPicked = new FirestoreBoolField(false); // 초기 상태는 뽑히지 않음
            doc.fields.attempts = new FirestoreIntField(attempts);

            string jsonPayload = JsonUtility.ToJson(doc);
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Firebase] 참가자 등록 성공: {name}");
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Firebase] 참가자 등록 실패: {request.error}\n응답: {request.downloadHandler.text}");
                    callback?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// 특정 인스타 아이디가 이미 등록되어 있는지 확인합니다.
        /// </summary>
        public IEnumerator CheckInstaIdExists(string instaId, Action<bool> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                callback?.Invoke(false);
                yield break;
            }

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents:runQuery";

            string queryPayload = "{" +
                "\"structuredQuery\": {" +
                    "\"from\": [{\"collectionId\": \"Participants\"}]," +
                    "\"where\": {" +
                        "\"fieldFilter\": {" +
                            "\"field\": {\"fieldPath\": \"insta\"}," +
                            "\"op\": \"EQUAL\"," +
                            "\"value\": {\"stringValue\": \"" + instaId + "\"}" +
                        "}" +
                    "}" +
                "}" +
            "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(queryPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string rawJson = request.downloadHandler.text;
                    string wrappedJson = "{\"items\":" + rawJson + "}";
                    try
                    {
                        RunQueryResponseList responseList = JsonUtility.FromJson<RunQueryResponseList>(wrappedJson);
                        if (responseList.items != null)
                        {
                            foreach (var item in responseList.items)
                            {
                                if (item.document != null && item.document.fields != null && !string.IsNullOrEmpty(item.document.name))
                                {
                                    callback?.Invoke(true); // Exists
                                    yield break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Firebase] 중복 확인 파싱 실패: {ex.Message}");
                    }
                }
                
                callback?.Invoke(false);
            }
        }

        /// <summary>
        /// 반대 성별 목록 중 'isPicked == false'인 참가자를 쿼리하여 무작위로 한 명을 선택합니다.
        /// </summary>
        public IEnumerator GetRandomMatch(string oppositeGender, Action<MatchedProfileResponse> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                Debug.LogError("[Firebase] Project ID가 설정되지 않았습니다.");
                callback?.Invoke(new MatchedProfileResponse { success = false });
                yield break;
            }

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents:runQuery";

            // structuredQuery JSONPayload 직접 빌드 (JsonUtility 중첩 한계를 극복하기 위해 문자열 빌더 사용)
            string queryPayload = "{" +
                "\"structuredQuery\": {" +
                    "\"from\": [{\"collectionId\": \"Participants\"}]," +
                    "\"where\": {" +
                        "\"compositeFilter\": {" +
                            "\"op\": \"AND\"," +
                            "\"filters\": [" +
                                "{" +
                                    "\"fieldFilter\": {" +
                                        "\"field\": {\"fieldPath\": \"gender\"}," +
                                        "\"op\": \"EQUAL\"," +
                                        "\"value\": {\"stringValue\": \"" + oppositeGender + "\"}" +
                                    "}" +
                                "}," +
                                "{" +
                                    "\"fieldFilter\": {" +
                                        "\"field\": {\"fieldPath\": \"isPicked\"}," +
                                        "\"op\": \"EQUAL\"," +
                                        "\"value\": {\"booleanValue\": false}" +
                                    "}" +
                                "}" +
                            "]" +
                        "}" +
                    "}" +
                "}" +
            "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(queryPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string rawJson = request.downloadHandler.text;
                    
                    // JsonUtility Array 래핑
                    string wrappedJson = "{\"items\":" + rawJson + "}";
                    
                    try
                    {
                        RunQueryResponseList responseList = JsonUtility.FromJson<RunQueryResponseList>(wrappedJson);
                        
                        // 유효한 매칭 대상(필드가 있는 문서) 필터링
                        List<RunQueryResponseItem> validItems = new List<RunQueryResponseItem>();
                        if (responseList.items != null)
                        {
                            foreach (var item in responseList.items)
                            {
                                if (item.document != null && item.document.fields != null && !string.IsNullOrEmpty(item.document.name))
                                {
                                    // 이름만 있고 인스타 아이디가 없는 데이터는 매칭(보상) 후보에서 완전히 제외
                                    string instaVal = "";
                                    if (item.document.fields.insta != null && !string.IsNullOrEmpty(item.document.fields.insta.stringValue))
                                    {
                                        instaVal = item.document.fields.insta.stringValue.Trim();
                                    }

                                    if (!string.IsNullOrEmpty(instaVal))
                                    {
                                        validItems.Add(item);
                                    }
                                }
                            }
                        }

                        if (validItems.Count > 0)
                        {
                            // 무작위 1명 추출
                            RunQueryResponseItem chosen = validItems[UnityEngine.Random.Range(0, validItems.Count)];
                            
                            // document.name(전체 경로)에서 documentId 추출
                            string docPath = chosen.document.name;
                            string docId = docPath.Substring(docPath.LastIndexOf('/') + 1);

                            MatchedProfileResponse match = new MatchedProfileResponse
                            {
                                success = true,
                                documentId = docId,
                                name = chosen.document.fields.name?.stringValue ?? "익명",
                                gender = chosen.document.fields.gender?.stringValue ?? oppositeGender,
                                insta = chosen.document.fields.insta?.stringValue ?? "@unknown",
                                bio = chosen.document.fields.bio?.stringValue ?? "인스타 친구해요!"
                            };

                            Debug.Log($"[Firebase] 매칭 대상 로드 성공: {match.name} ({match.insta})");
                            callback?.Invoke(match);
                        }
                        else
                        {
                            Debug.LogWarning("[Firebase] 조건에 부합하는 매칭 대상이 없습니다.");
                            callback?.Invoke(new MatchedProfileResponse { success = false });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Firebase] 응답 파싱 중 예외 발생: {ex.Message}\n원본: {rawJson}");
                        callback?.Invoke(new MatchedProfileResponse { success = false });
                    }
                }
                else
                {
                    Debug.LogError($"[Firebase] 매칭 쿼리 실패: {request.error}\n응답: {request.downloadHandler.text}");
                    callback?.Invoke(new MatchedProfileResponse { success = false });
                }
            }
        }

        /// <summary>
        /// 아직 뽑히지 않은 남성 및 여성 참가자 수를 조회합니다.
        /// </summary>
        public IEnumerator GetUnpickedCounts(Action<int, int> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                Debug.LogWarning("[Firebase] Project ID가 설정되지 않아 로컬 데이터를 사용합니다.");
                callback?.Invoke(-1, -1);
                yield break;
            }

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents:runQuery";

            string queryPayload = "{" +
                "\"structuredQuery\": {" +
                    "\"from\": [{\"collectionId\": \"Participants\"}]," +
                    "\"where\": {" +
                        "\"fieldFilter\": {" +
                            "\"field\": {\"fieldPath\": \"isPicked\"}," +
                            "\"op\": \"EQUAL\"," +
                            "\"value\": {\"booleanValue\": false}" +
                        "}" +
                    "}" +
                "}" +
            "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(queryPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string rawJson = request.downloadHandler.text;
                    string wrappedJson = "{\"items\":" + rawJson + "}";
                    
                    int maleCount = 0;
                    int femaleCount = 0;

                    try
                    {
                        RunQueryResponseList responseList = JsonUtility.FromJson<RunQueryResponseList>(wrappedJson);
                        if (responseList.items != null)
                        {
                            foreach (var item in responseList.items)
                            {
                                if (item.document != null && item.document.fields != null)
                                {
                                    string gender = item.document.fields.gender?.stringValue;
                                    if (gender == "남") maleCount++;
                                    else if (gender == "여") femaleCount++;
                                }
                            }
                        }
                        callback?.Invoke(maleCount, femaleCount);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Firebase] 카운트 파싱 예외 발생: {ex.Message}");
                        callback?.Invoke(-1, -1);
                    }
                }
                else
                {
                    Debug.LogError($"[Firebase] 카운트 쿼리 실패: {request.error}");
                    callback?.Invoke(-1, -1);
                }
            }
        }

        /// <summary>
        /// 특정 참가자의 'isPicked' 상태를 변경(예: true로 업데이트하여 뽑힘 처리)합니다.
        /// </summary>
        public IEnumerator UpdatePickedStatus(string documentId, bool isPicked, Action<bool> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId) || string.IsNullOrEmpty(documentId))
            {
                Debug.LogError("[Firebase] Project ID 또는 Document ID가 유효하지 않습니다.");
                callback?.Invoke(false);
                yield break;
            }

            // updateMask 쿼리 파라미터를 사용해 오직 'isPicked' 필드만 교체(PATCH)
            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/Participants/{documentId}?updateMask.fieldPaths=isPicked";

            // PATCH 바디용 데이터 빌드
            FirestoreUpdateDocument patchDoc = new FirestoreUpdateDocument();
            patchDoc.fields = new FirestoreUpdateFields();
            patchDoc.fields.isPicked = new FirestoreBoolField(isPicked);

            string jsonPayload = JsonUtility.ToJson(patchDoc);

            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Firebase] 문서({documentId}) isPicked={isPicked} 업데이트 완료");
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Firebase] 문서 업데이트 실패: {request.error}\n응답: {request.downloadHandler.text}");
                    callback?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// (개발자용) DB에 등록된 모든 참가자의 뽑힘 상태를 초기화(false) 합니다.
        /// </summary>
        public IEnumerator ResetAllPickedStatus(Action<bool> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                callback?.Invoke(false);
                yield break;
            }

            // 1. 모든 문서 가져오기
            string getUrl = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/Participants";
            
            using (UnityWebRequest request = UnityWebRequest.Get(getUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Firebase] 전체 문서 가져오기 실패: {request.error}");
                    callback?.Invoke(false);
                    yield break;
                }

                string rawJson = request.downloadHandler.text;
                
                ListDocumentsResponse responseList = null;
                try
                {
                    responseList = JsonUtility.FromJson<ListDocumentsResponse>(rawJson);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Firebase] List 파싱 예외 발생: {ex.Message}");
                    callback?.Invoke(false);
                    yield break;
                }

                if (responseList == null || responseList.documents == null)
                {
                    // 문서가 0개
                    callback?.Invoke(true);
                    yield break;
                }

                int totalCount = responseList.documents.Length;
                int successCount = 0;

                foreach (var doc in responseList.documents)
                {
                    if (doc == null || string.IsNullOrEmpty(doc.name)) continue;

                    string docPath = doc.name;
                    string docId = docPath.Substring(docPath.LastIndexOf('/') + 1);

                    yield return UpdatePickedStatus(docId, false, (success) => {
                        if (success) successCount++;
                    });
                }

                Debug.Log($"[Firebase] 초기화 완료: {successCount}/{totalCount} 개 업데이트 됨.");
                callback?.Invoke(true);
            }
        }

        /// <summary>
        /// 모든 참가자 데이터를 조회하여 리스트로 반환합니다.
        /// </summary>
        public IEnumerator GetAllParticipants(Action<List<ParticipantData>> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                callback?.Invoke(new List<ParticipantData>());
                yield break;
            }

            string baseUrl = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/Participants";
            string currentUrl = baseUrl + "?pageSize=300";
            List<ParticipantData> result = new List<ParticipantData>();

            while (!string.IsNullOrEmpty(currentUrl))
            {
                using (UnityWebRequest request = UnityWebRequest.Get(currentUrl))
                {
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[Firebase] 전체 문서 가져오기 실패: {request.error}");
                        callback?.Invoke(result);
                        yield break;
                    }

                    string rawJson = request.downloadHandler.text;
                    ListDocumentsResponse responseList = null;
                    try
                    {
                        responseList = JsonUtility.FromJson<ListDocumentsResponse>(rawJson);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Firebase] 파싱 에러: {ex.Message}");
                        callback?.Invoke(result);
                        yield break;
                    }

                    if (responseList != null && responseList.documents != null)
                    {
                        foreach (var doc in responseList.documents)
                        {
                            if (doc == null || string.IsNullOrEmpty(doc.name)) continue;

                            string docPath = doc.name;
                            string docId = docPath.Substring(docPath.LastIndexOf('/') + 1);
                            int attemptsVal = 0;
                            if (doc.fields != null && doc.fields.attempts != null && !string.IsNullOrEmpty(doc.fields.attempts.integerValue))
                            {
                                int.TryParse(doc.fields.attempts.integerValue, out attemptsVal);
                            }

                            if (doc.fields != null)
                            {
                                result.Add(new ParticipantData
                                {
                                    documentId = docId,
                                    name = doc.fields.name?.stringValue ?? "",
                                    insta = doc.fields.insta?.stringValue ?? "",
                                    bio = doc.fields.bio?.stringValue ?? "",
                                    gender = doc.fields.gender?.stringValue ?? "",
                                    isPicked = doc.fields.isPicked?.booleanValue ?? false,
                                    attempts = attemptsVal
                                });
                            }
                        }
                    }

                    if (responseList != null && !string.IsNullOrEmpty(responseList.nextPageToken))
                    {
                        currentUrl = baseUrl + "?pageSize=300&pageToken=" + responseList.nextPageToken;
                    }
                    else
                    {
                        currentUrl = null;
                    }
                }
            }
            callback?.Invoke(result);
        }

        /// <summary>
        /// 참가자 데이터를 덮어씌웁니다. (전체 필드 업데이트)
        /// </summary>
        public IEnumerator UpdateParticipantFullData(ParticipantData data, Action<bool> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId) || string.IsNullOrEmpty(data.documentId))
            {
                callback?.Invoke(false);
                yield break;
            }

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/Participants/{data.documentId}";

            FirestoreDocument doc = new FirestoreDocument();
            doc.fields = new FirestoreFields();
            doc.fields.name = new FirestoreStringField(data.name);
            doc.fields.insta = new FirestoreStringField(data.insta);
            doc.fields.bio = new FirestoreStringField(data.bio);
            doc.fields.gender = new FirestoreStringField(data.gender);
            doc.fields.isPicked = new FirestoreBoolField(data.isPicked);
            doc.fields.attempts = new FirestoreIntField(data.attempts);

            string jsonPayload = JsonUtility.ToJson(doc);

            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Firebase] 데이터 업데이트 성공: {data.documentId}");
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Firebase] 데이터 업데이트 실패: {request.error}");
                    callback?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// 특정 참가자 문서를 삭제합니다.
        /// </summary>
        public IEnumerator DeleteParticipant(string documentId, Action<bool> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId) || string.IsNullOrEmpty(documentId))
            {
                callback?.Invoke(false);
                yield break;
            }

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/Participants/{documentId}";

            using (UnityWebRequest request = new UnityWebRequest(url, "DELETE"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Firebase] 데이터 삭제 성공: {documentId}");
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Firebase] 데이터 삭제 실패: {request.error}");
                    callback?.Invoke(false);
                }
            }
        }

        // =========================================================================
        // GameState Stats API
        // =========================================================================

        /// <summary>
        /// GameState/stats 문서에서 실물 인형 개수(totalDolls)를 가져옵니다.
        /// </summary>
        public IEnumerator GetTotalDolls(Action<int> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                callback?.Invoke(100); // 로컬 기본값
                yield break;
            }

            string getUrl = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/GameState/stats";
            using (UnityWebRequest request = UnityWebRequest.Get(getUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Firebase] GameState/stats 조회 실패 (초기값이 없을 수 있음): {request.error}");
                    callback?.Invoke(100);
                    yield break;
                }

                string rawJson = request.downloadHandler.text;
                try
                {
                    var doc = JsonUtility.FromJson<GameStateDocument>(rawJson);
                    if (doc != null && doc.fields != null && doc.fields.totalDolls != null && !string.IsNullOrEmpty(doc.fields.totalDolls.integerValue))
                    {
                        if (int.TryParse(doc.fields.totalDolls.integerValue, out int count))
                        {
                            callback?.Invoke(count);
                            yield break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Firebase] totalDolls 파싱 에러: {ex.Message}");
                }
                
                callback?.Invoke(100);
            }
        }

        /// <summary>
        /// GameState/stats 문서의 totalDolls 필드를 갱신합니다.
        /// </summary>
        public IEnumerator UpdateTotalDolls(int count, Action<bool> callback)
        {
            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                callback?.Invoke(false);
                yield break;
            }

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/GameState/stats?updateMask.fieldPaths=totalDolls";

            string jsonPayload = $"{{\"fields\":{{\"totalDolls\":{{\"integerValue\":\"{count}\"}}}}}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Firebase] 남은 인형 개수 업데이트 성공: {count}개");
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Firebase] 남은 인형 개수 업데이트 실패: {request.error}");
                    callback?.Invoke(false);
                }
            }
        }

        // =========================================================================
        // GameState Stats API - Extended Metrics
        // =========================================================================

        /// <summary>
        /// GameState/stats 문서에서 모든 통계 데이터를 가져옵니다.
        /// </summary>
        public IEnumerator GetGameStats(Action<GameStatsData> callback)
        {
            GameStatsData defaultStats = new GameStatsData
            {
                totalDolls = 100,
                totalRevenue = 0,
                totalRegistrations = 0,
                totalPlays = 0,
                totalSuccesses = 0
            };

            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                defaultStats.totalDolls = PlayerPrefs.GetInt("Stats_TotalDolls", 100);
                defaultStats.totalRevenue = PlayerPrefs.GetInt("Stats_TotalRevenue", 0);
                defaultStats.totalRegistrations = PlayerPrefs.GetInt("Stats_TotalRegistrations", 0);
                defaultStats.totalPlays = PlayerPrefs.GetInt("Stats_TotalPlays", 0);
                defaultStats.totalSuccesses = PlayerPrefs.GetInt("Stats_TotalSuccesses", 0);
                callback?.Invoke(defaultStats);
                yield break;
            }

            string getUrl = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/GameState/stats";
            using (UnityWebRequest request = UnityWebRequest.Get(getUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Firebase] GameState/stats 조회 실패 (초기값이 없을 수 있음): {request.error}");
                    defaultStats.totalDolls = PlayerPrefs.GetInt("Stats_TotalDolls", 100);
                    defaultStats.totalRevenue = PlayerPrefs.GetInt("Stats_TotalRevenue", 0);
                    defaultStats.totalRegistrations = PlayerPrefs.GetInt("Stats_TotalRegistrations", 0);
                    defaultStats.totalPlays = PlayerPrefs.GetInt("Stats_TotalPlays", 0);
                    defaultStats.totalSuccesses = PlayerPrefs.GetInt("Stats_TotalSuccesses", 0);
                    callback?.Invoke(defaultStats);
                    yield break;
                }

                string rawJson = request.downloadHandler.text;
                try
                {
                    var doc = JsonUtility.FromJson<GameStateDocument>(rawJson);
                    if (doc != null && doc.fields != null)
                    {
                        GameStatsData stats = new GameStatsData();
                        
                        stats.totalDolls = (doc.fields.totalDolls != null && int.TryParse(doc.fields.totalDolls.integerValue, out int td)) ? td : PlayerPrefs.GetInt("Stats_TotalDolls", 100);
                        stats.totalRevenue = (doc.fields.totalRevenue != null && int.TryParse(doc.fields.totalRevenue.integerValue, out int tr)) ? tr : PlayerPrefs.GetInt("Stats_TotalRevenue", 0);
                        stats.totalRegistrations = (doc.fields.totalRegistrations != null && int.TryParse(doc.fields.totalRegistrations.integerValue, out int treg)) ? treg : PlayerPrefs.GetInt("Stats_TotalRegistrations", 0);
                        stats.totalPlays = (doc.fields.totalPlays != null && int.TryParse(doc.fields.totalPlays.integerValue, out int tp)) ? tp : PlayerPrefs.GetInt("Stats_TotalPlays", 0);
                        stats.totalSuccesses = (doc.fields.totalSuccesses != null && int.TryParse(doc.fields.totalSuccesses.integerValue, out int ts)) ? ts : PlayerPrefs.GetInt("Stats_TotalSuccesses", 0);

                        PlayerPrefs.SetInt("Stats_TotalDolls", stats.totalDolls);
                        PlayerPrefs.SetInt("Stats_TotalRevenue", stats.totalRevenue);
                        PlayerPrefs.SetInt("Stats_TotalRegistrations", stats.totalRegistrations);
                        PlayerPrefs.SetInt("Stats_TotalPlays", stats.totalPlays);
                        PlayerPrefs.SetInt("Stats_TotalSuccesses", stats.totalSuccesses);
                        PlayerPrefs.Save();

                        callback?.Invoke(stats);
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Firebase] GameStats 파싱 에러: {ex.Message}");
                }
                
                callback?.Invoke(defaultStats);
            }
        }

        /// <summary>
        /// GameState/stats 문서의 특정 필드들을 업데이트합니다.
        /// </summary>
        public IEnumerator UpdateGameStats(GameStatsData stats, List<string> fieldsToUpdate, Action<bool> callback)
        {
            if (fieldsToUpdate == null || fieldsToUpdate.Count == 0)
            {
                callback?.Invoke(false);
                yield break;
            }

            foreach (var f in fieldsToUpdate)
            {
                if (f == "totalDolls") PlayerPrefs.SetInt("Stats_TotalDolls", stats.totalDolls);
                else if (f == "totalRevenue") PlayerPrefs.SetInt("Stats_TotalRevenue", stats.totalRevenue);
                else if (f == "totalRegistrations") PlayerPrefs.SetInt("Stats_TotalRegistrations", stats.totalRegistrations);
                else if (f == "totalPlays") PlayerPrefs.SetInt("Stats_TotalPlays", stats.totalPlays);
                else if (f == "totalSuccesses") PlayerPrefs.SetInt("Stats_TotalSuccesses", stats.totalSuccesses);
            }
            PlayerPrefs.Save();

            if (string.IsNullOrEmpty(firebaseProjectId))
            {
                callback?.Invoke(true);
                yield break;
            }

            StringBuilder maskBuilder = new StringBuilder();
            StringBuilder fieldsBuilder = new StringBuilder();

            fieldsBuilder.Append("{");
            for (int i = 0; i < fieldsToUpdate.Count; i++)
            {
                string fieldName = fieldsToUpdate[i];
                maskBuilder.Append($"updateMask.fieldPaths={fieldName}");
                if (i < fieldsToUpdate.Count - 1)
                {
                    maskBuilder.Append("&");
                }

                int val = 0;
                if (fieldName == "totalDolls") val = stats.totalDolls;
                else if (fieldName == "totalRevenue") val = stats.totalRevenue;
                else if (fieldName == "totalRegistrations") val = stats.totalRegistrations;
                else if (fieldName == "totalPlays") val = stats.totalPlays;
                else if (fieldName == "totalSuccesses") val = stats.totalSuccesses;

                fieldsBuilder.Append($"\"{fieldName}\":{{\"integerValue\":\"{val}\"}}");
                if (i < fieldsToUpdate.Count - 1)
                {
                    fieldsBuilder.Append(",");
                }
            }
            fieldsBuilder.Append("}");

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/GameState/stats?{maskBuilder.ToString()}";
            string jsonPayload = $"{{\"fields\":{fieldsBuilder.ToString()}}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Firebase] 통계 업데이트 성공: {fieldsBuilder.ToString()}");
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Firebase] 통계 업데이트 실패: {request.error}\n응답: {request.downloadHandler.text}");
                    callback?.Invoke(false);
                }
            }
        }

        public void IncrementRegistrationCount()
        {
            StartCoroutine(IncrementStatCoroutine("totalRegistrations", 1));
        }

        public void IncrementPlayCountAndRevenue(int revenue)
        {
            StartCoroutine(IncrementPlayAndRevenueCoroutine(revenue));
        }

        public void IncrementSuccessCount()
        {
            StartCoroutine(IncrementStatCoroutine("totalSuccesses", 1));
        }

        private IEnumerator IncrementStatCoroutine(string fieldName, int amount)
        {
            GameStatsData currentStats = new GameStatsData();
            bool fetchDone = false;
            yield return GetGameStats((stats) => {
                currentStats = stats;
                fetchDone = true;
            });
            yield return new WaitUntil(() => fetchDone);

            if (fieldName == "totalRegistrations") currentStats.totalRegistrations += amount;
            else if (fieldName == "totalPlays") currentStats.totalPlays += amount;
            else if (fieldName == "totalSuccesses") currentStats.totalSuccesses += amount;
            else if (fieldName == "totalRevenue") currentStats.totalRevenue += amount;
            else if (fieldName == "totalDolls") currentStats.totalDolls += amount;

            yield return UpdateGameStats(currentStats, new List<string> { fieldName }, null);
        }

        private IEnumerator IncrementPlayAndRevenueCoroutine(int revenue)
        {
            GameStatsData currentStats = new GameStatsData();
            bool fetchDone = false;
            yield return GetGameStats((stats) => {
                currentStats = stats;
                fetchDone = true;
            });
            yield return new WaitUntil(() => fetchDone);

            currentStats.totalPlays += 1;
            currentStats.totalRevenue += revenue;

            yield return UpdateGameStats(currentStats, new List<string> { "totalPlays", "totalRevenue" }, null);
        }
    }

    // =========================================================================
    // REST API JSON 직렬화용 데이터 세트 (JsonUtility 규격 준수 - null 비교 및 안전성을 위해 class로 선언)
    // =========================================================================

    [Serializable]
    public class FirestoreDocument
    {
        public FirestoreFields fields;
    }

    [Serializable]
    public class FirestoreFields
    {
        public FirestoreStringField name;
        public FirestoreStringField insta;
        public FirestoreStringField bio;
        public FirestoreStringField gender;
        public FirestoreBoolField isPicked;
        public FirestoreIntField attempts;
    }

    [Serializable]
    public class GameStateDocument
    {
        public GameStateFields fields;
    }

    [Serializable]
    public class GameStateFields
    {
        public FirestoreIntField totalDolls;
        public FirestoreIntField totalRevenue;
        public FirestoreIntField totalRegistrations;
        public FirestoreIntField totalPlays;
        public FirestoreIntField totalSuccesses;
    }

    [Serializable]
    public class FirestoreUpdateDocument
    {
        public FirestoreUpdateFields fields;
    }

    [Serializable]
    public class FirestoreUpdateFields
    {
        public FirestoreBoolField isPicked;
    }

    [Serializable]
    public class FirestoreStringField
    {
        public string stringValue;
        public FirestoreStringField(string val) => stringValue = val;
    }

    [Serializable]
    public class FirestoreBoolField
    {
        public bool booleanValue;
        public FirestoreBoolField(bool val) => booleanValue = val;
    }

    [Serializable]
    public class FirestoreIntField
    {
        public string integerValue; // Firestore API 상에서 int형도 string 형태 전송
        public FirestoreIntField(int val) => integerValue = val.ToString();
    }

    // =========================================================================
    // :runQuery Array 응답 파싱용 데이터 세트 (JsonUtility 규격 준수 - null 비교 및 안전성을 위해 class로 선언)
    // =========================================================================

    [Serializable]
    public class RunQueryResponseList
    {
        public RunQueryResponseItem[] items;
    }

    [Serializable]
    public class RunQueryResponseItem
    {
        public FirestoreDocumentResponse document;
    }

    [Serializable]
    public class FirestoreDocumentResponse
    {
        public string name; // projects/{projectId}/databases/(default)/documents/Participants/{documentId}
        public FirestoreFields fields;
    }

    // =========================================================================
    // :List Documents 응답 파싱용 데이터 세트
    // =========================================================================

    [Serializable]
    public class ListDocumentsResponse
    {
        public FirestoreDocumentResponse[] documents;
        public string nextPageToken;
    }

    // =========================================================================
    // 행사 통계용 구조체
    // =========================================================================
    public struct GameStatsData
    {
        public int totalDolls;
        public int totalRevenue;
        public int totalRegistrations;
        public int totalPlays;
        public int totalSuccesses;
    }

    // =========================================================================
    // 콜백 연동용 최종 클린 매칭 구조체
    // =========================================================================

    public struct MatchedProfileResponse
    {
        public bool success;
        public string documentId;
        public string name;
        public string gender;
        public string insta;
        public string bio;
    }

    // =========================================================================
    // DB 뷰어/에디터용 데이터 구조체
    // =========================================================================

    public struct ParticipantData
    {
        public string documentId;
        public string name;
        public string insta;
        public string bio;
        public string gender;
        public bool isPicked;
        public int attempts;
    }
}
