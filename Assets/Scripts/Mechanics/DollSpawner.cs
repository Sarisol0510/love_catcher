using UnityEngine;
using System.Collections.Generic;

namespace ClawMachine.Mechanics
{
    public class DollSpawner : MonoBehaviour
    {
        public static DollSpawner Instance { get; private set; }

        [Header("Spawn Settings")]
        [Tooltip("리필될 인형이 생성될 위치 (빈 게임오브젝트를 배치해서 할당해주세요)")]
        public Transform spawnPoint;
        [Tooltip("비어있으면 시작 시 씬에 있는 하트들을 수집하여 프리팹 풀로 사용합니다.")]
        public List<GameObject> dollPrefabs = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 프리팹 리스트가 비어있으면 씬에 있는 Doll들을 기반으로 템플릿 생성
            if (dollPrefabs.Count == 0)
            {
                GameObject[] existingDolls = GameObject.FindGameObjectsWithTag("Doll");
                HashSet<string> uniqueNames = new HashSet<string>();

                foreach (var doll in existingDolls)
                {
                    // MeshRenderer의 재질로 종류(색상 등)를 구분해본다. 
                    MeshRenderer mr = doll.GetComponentInChildren<MeshRenderer>();
                    string key = mr != null ? mr.sharedMaterial.name : doll.name.Replace("(Clone)", "").Trim();
                    
                    if (!uniqueNames.Contains(key))
                    {
                        uniqueNames.Add(key);
                        // 복제해서 템플릿으로 만듦 (비활성화)
                        GameObject template = Instantiate(doll);
                        template.name = doll.name.Replace("(Clone)", "").Trim();
                        template.SetActive(false);
                        // 템플릿은 보이지 않는 곳에 보관
                        template.transform.SetParent(this.transform);
                        dollPrefabs.Add(template);
                    }
                }
                Debug.Log($"[DollSpawner] 씬에서 {dollPrefabs.Count}종류의 하트 템플릿을 수집했습니다.");
            }
        }

        public void RefillDoll(GameObject oldDoll)
        {
            // 기존에 성공한 인형은 파괴
            if (oldDoll != null)
            {
                Destroy(oldDoll);
            }

            if (spawnPoint == null)
            {
                Debug.LogWarning("[DollSpawner] SpawnPoint가 지정되지 않아 리필할 수 없습니다.");
                return;
            }

            // 리스트에서 이미 파괴되었거나 null인 항목들을 동적으로 제거하여 에러 방지
            dollPrefabs.RemoveAll(item => item == null);

            if (dollPrefabs.Count > 0)
            {
                // 랜덤으로 하나 선택하여 스폰
                GameObject prefab = dollPrefabs[Random.Range(0, dollPrefabs.Count)];
                if (prefab != null)
                {
                    GameObject newDoll = Instantiate(prefab, spawnPoint.position, Random.rotation);
                    newDoll.SetActive(true);
                    // 부모에서 분리하여 물리엔진 적용을 온전히 받도록 함
                    newDoll.transform.SetParent(null);
                    Debug.Log($"[DollSpawner] 새 인형({newDoll.name}) 리필 완료!");
                }
                else
                {
                    Debug.LogWarning("[DollSpawner] 선택된 인형 프리팹이 null입니다.");
                }
            }
            else
            {
                Debug.LogWarning("[DollSpawner] 사용할 수 있는 인형 프리팹/템플릿이 없습니다.");
            }
        }
    }
}
