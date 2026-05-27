using UnityEngine;

namespace ClawMachine.Mechanics
{
    public class PlushieDoll : MonoBehaviour
    {
        [Header("Plushie Settings")]
        [Tooltip("인형의 엉덩이/골반 부위 (무게중심을 낮추기 위해 사용)")]
        public Rigidbody pelvisRigidbody;

        private void Start()
        {
            // 1. 모든 리지드바디 가져오기
            Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
            
            foreach (var rb in rbs)
            {
                // 너무 빨리 회전해서 튕겨나가는 버그 방지
                rb.maxAngularVelocity = 15f; 
                
                // 솜인형처럼 부드러운 움직임을 위해 물리 보간 켜기
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                
                // 연속 충돌 검사 (집게를 뚫고 나가는 현상 방지)
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            // 2. 하체(골반) 쪽을 무겁게 만들어 무게중심 낮추기 (오뚝이 느낌)
            // 이렇게 하면 집게로 머리를 잡았을 때 아래로 축 처지는 예쁜 실루엣이 나옵니다.
            if (pelvisRigidbody != null)
            {
                pelvisRigidbody.mass *= 3f; // 엉덩이 부분 무게를 3배로
            }
        }
    }
}
