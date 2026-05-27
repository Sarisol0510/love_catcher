using UnityEngine;

namespace ClawMachine.Mechanics
{
    public class ClawPityMagnet : MonoBehaviour
    {
        [Header("Pity Settings")]
        [Tooltip("자석 효과가 적용될 반경 (Trigger)")]
        public float magnetRadius = 2.0f;
        [Tooltip("인형을 위쪽/집게 중심으로 당기는 자석 힘")]
        public float magnetForce = 8.0f;

        private SphereCollider triggerCollider;
        private bool isPityActive = false;
        private ClawMachineController controller;

        private void Awake()
        {
            controller = GetComponentInParent<ClawMachineController>();
            
            // 트리거 콜라이더 자동 추가 또는 셋업
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }
            triggerCollider.isTrigger = true;
            triggerCollider.radius = magnetRadius;
        }

        public void SetPityActive(bool active)
        {
            isPityActive = active;
            Debug.Log($"[Pity Magnet] 자석 보조 상태 변경: {active}");
        }

        private void OnTriggerStay(Collider other)
        {
            // Pity 모드가 활성화되어 있고, 집게가 집어올리는 상태일 때만 보조 물리 힘을 가함
            if (!isPityActive || controller == null) return;
            
            // 집게가 인형을 오므려 잡는 상태(Grabbing)이거나 상승하는 상태(Returning)일 때 작동
            if (controller.currentState != ClawState.Grabbing && controller.currentState != ClawState.Returning) 
                return;

            if (other.CompareTag("Doll"))
            {
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 집게 중앙을 향해 끌어당기는 힘 계산
                    Vector3 directionToClaw = (transform.position - other.transform.position).normalized;
                    
                    // 중력을 상쇄하고 집게 중심에 안착하도록 미세한 AddForce 적용
                    Vector3 assistForce = (directionToClaw + Vector3.up * 0.5f).normalized * magnetForce;
                    rb.AddForce(assistForce, ForceMode.Force);
                }
            }
        }
    }
}
