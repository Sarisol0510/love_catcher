using UnityEngine;
using System.Collections.Generic;

namespace ClawMachine.Mechanics
{
    public class ClawGripper : MonoBehaviour
    {
        [Header("Claw Joints")]
        [Tooltip("인형을 잡는 집게 발들의 HingeJoint 배열")]
        public HingeJoint[] clawJoints;

        [Header("Motor Settings")]
        [Tooltip("집게를 닫을 때의 속도")]
        public float closeVelocity = 100f;
        [Tooltip("집게를 열 때의 속도")]
        public float openVelocity = -100f;
        [Tooltip("악력 (모터가 가하는 힘). 기획에 따라 재시도마다 이 값을 올려주면 천장 버프가 됩니다.")]
        public float gripForce = 15f;

        public float baseGripForce = 15f;

        private void Awake()
        {
            // 인스펙터 직렬화된 값(예: 50)이 코드 기본값(15)을 덮어씌우는 현상을 방지하기 위해 Awake에서 강제 주입
            gripForce = 15f;
            baseGripForce = 15f;
        }

        private void Start()
        {
            // 초기 상태는 열려있도록 설정
            OpenClaw();
        }

        /// <summary>
        /// 집게를 오므립니다 (악력 발생)
        /// </summary>
        public void CloseClaw()
        {
            foreach (var joint in clawJoints)
            {
                if (joint == null) continue;

                var motor = joint.motor;
                motor.targetVelocity = closeVelocity;
                motor.force = gripForce; // 설정된 악력 적용
                motor.freeSpin = false;
                
                joint.motor = motor;
                joint.useMotor = true;
            }
        }

        /// <summary>
        /// 집게를 폅니다
        /// </summary>
        public void OpenClaw()
        {
            foreach (var joint in clawJoints)
            {
                if (joint == null) continue;

                var motor = joint.motor;
                motor.targetVelocity = openVelocity;
                motor.force = gripForce * 0.5f; // 열 때는 부드럽게
                motor.freeSpin = false;
                
                joint.motor = motor;
                joint.useMotor = true;
            }
        }

        /// <summary>
        /// 천장(Pity) 시스템 발동 시 호출할 함수
        /// </summary>
        /// <param name="bonusForce">추가할 악력</param>
        public void AddPityBuff(float bonusForce)
        {
            gripForce = baseGripForce + bonusForce;
            Debug.Log($"[천장 버프 적용] 현재 악력: {gripForce}");
        }

        /// <summary>
        /// 악력을 기본값으로 되돌립니다.
        /// </summary>
        public void ResetGripForce()
        {
            gripForce = baseGripForce;
        }
    }
}
