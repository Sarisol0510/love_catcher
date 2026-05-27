using UnityEngine;
using System;

namespace ClawMachine.Mechanics
{
    public class GoalBoxTrigger : MonoBehaviour
    {
        public static event Action<GameObject> OnDollScored;

        [Header("Tag Settings")]
        [Tooltip("인형 콜라이더에 달릴 태그")]
        public string dollTag = "Doll";

        private void OnTriggerEnter(Collider other)
        {
            // 인형이 골 박스(배출구 내부 바닥 트리거)에 들어오면 성공 판정
            if (other.CompareTag(dollTag))
            {
                Debug.Log($"[골인] 인형 감지: {other.gameObject.name}");
                OnDollScored?.Invoke(other.gameObject);
            }
        }
    }
}
