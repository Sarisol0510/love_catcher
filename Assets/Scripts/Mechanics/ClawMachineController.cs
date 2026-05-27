using UnityEngine;
using System.Collections;
using ClawMachine.Input;
using ClawMachine.UI;

namespace ClawMachine.Mechanics
{
    public enum ClawState
    {
        Idle,       // 대기 (이동 가능)
        Moving,     // 조이스틱으로 이동 중
        Dropping,   // 버튼을 눌러 아래로 하강 중
        Grabbing,   // 바닥에 닿아 집게를 닫는 중
        Returning,  // 위로 올라와서 원위치로 돌아가는 중
        Releasing,  // 인형을 배출구에 놓는 중
        Done        // 1회차 끝 (결과 판정 대기)
    }

    public class ClawMachineController : MonoBehaviour
    {
        [Header("References")]
        public ClawGripper clawGripper;
        [Tooltip("물리적으로 이동할 집게의 최상위 부모 Rigidbody")]
        public Rigidbody clawRigidbody;

        [Header("Movement Settings")]
        public float moveSpeed = 2f;
        public float dropSpeed = 3f;
        
        [Header("Boundaries")]
        public Vector2 minX_Z = new Vector2(-2f, -2f);
        public Vector2 maxX_Z = new Vector2(2f, 2f);
        public float maxY = 3f;   // 기본 높이
        public float minY = 0.5f; // 최대 하강 높이

        [Header("Drop Position (배출구)")]
        public Vector3 dropZonePosition = new Vector3(-1.5f, 3f, -1.5f);

        [Header("State")]
        public ClawState currentState = ClawState.Idle;

        [Header("SFX")]
        public AudioClip moveSound;
        public AudioClip dropSound;
        private AudioSource moveAudioSource;

        private Vector3 startPosition;
        private Coroutine activeRoutine;

        private void Start()
        {
            if (ArcadeInputManager.Instance != null)
            {
                ArcadeInputManager.Instance.OnActionDown += HandleActionDown;
                ArcadeInputManager.Instance.OnResetPressed += HandleResetPressed;
            }
            
            // 시작 위치 기억 및 시작 높이 고정
            startPosition = clawRigidbody.position;
            startPosition.y = maxY;
            clawRigidbody.position = startPosition;

            // 이동음 AudioSource 세팅
            moveAudioSource = gameObject.AddComponent<AudioSource>();
            moveAudioSource.loop = true;
            moveAudioSource.playOnAwake = false;
            moveAudioSource.clip = moveSound;
            moveAudioSource.volume = 0.5f;
        }

        private void OnDestroy()
        {
            if (ArcadeInputManager.Instance != null)
            {
                ArcadeInputManager.Instance.OnActionDown -= HandleActionDown;
                ArcadeInputManager.Instance.OnResetPressed -= HandleResetPressed;
            }
        }

        private void FixedUpdate()
        {
            if (currentState == ClawState.Idle || currentState == ClawState.Moving)
            {
                HandleMovement();
            }
        }

        private void HandleMovement()
        {
            if (ArcadeInputManager.Instance == null) return;

            Vector2 input = ArcadeInputManager.Instance.JoystickInput;
            
            if (input.sqrMagnitude > 0.01f)
            {
                if (currentState != ClawState.Moving)
                {
                    if (moveSound != null && !moveAudioSource.isPlaying) moveAudioSource.Play();
                }
                currentState = ClawState.Moving;
                
                Vector3 moveDir = new Vector3(input.x, 0, input.y);
                Vector3 nextPos = clawRigidbody.position + (moveDir * moveSpeed * Time.fixedDeltaTime);
                
                // 이동 제한(Boundary) 적용
                nextPos.x = Mathf.Clamp(nextPos.x, minX_Z.x, maxX_Z.x);
                nextPos.z = Mathf.Clamp(nextPos.z, minX_Z.y, maxX_Z.y);
                nextPos.y = maxY; // 높이는 고정

                clawRigidbody.MovePosition(nextPos);
            }
            else
            {
                if (currentState == ClawState.Moving)
                {
                    moveAudioSource.Stop();
                }
                currentState = ClawState.Idle;
            }
        }

        private void HandleActionDown()
        {
            if (!enabled) return;
            if (ClawMachineUIManager.Instance != null && ClawMachineUIManager.Instance.IsUserTyping()) return;

            if (currentState == ClawState.Idle || currentState == ClawState.Moving)
            {
                // 드롭 시작
                if (activeRoutine != null) StopCoroutine(activeRoutine);
                activeRoutine = StartCoroutine(DropRoutine());
            }
        }

        public void TriggerReset()
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(ResetRoutine());
        }

        private void HandleResetPressed()
        {
            if (ClawMachineUIManager.Instance != null && ClawMachineUIManager.Instance.IsUserTyping()) return;

            // R 키나 리셋 입력이 오면 GameFlowManager를 거쳐서 정식 재시도 처리
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.RequestRetry();
            }
            else
            {
                if (!enabled) return;
                // 테스트용 로컬 리셋
                TriggerReset();
            }
        }

        private IEnumerator DropRoutine()
        {
            currentState = ClawState.Dropping;
            if (moveAudioSource != null && moveAudioSource.isPlaying) moveAudioSource.Stop();
            
            // 하강 사운드 재생
            if (dropSound != null && ClawMachine.Audio.SoundManager.Instance != null)
            {
                ClawMachine.Audio.SoundManager.Instance.PlaySFX(dropSound);
            }
            
            // 1. 하강
            while (clawRigidbody.position.y > minY)
            {
                Vector3 nextPos = clawRigidbody.position + Vector3.down * dropSpeed * Time.fixedDeltaTime;
                clawRigidbody.MovePosition(nextPos);
                yield return new WaitForFixedUpdate();
            }

            // 2. 잡기
            currentState = ClawState.Grabbing;
            if (clawGripper != null)
            {
                clawGripper.CloseClaw();
            }
            
            // 꽉 쥘 때까지 잠시 대기
            yield return new WaitForSeconds(1.5f);

            // 3. 복귀 (위로)
            currentState = ClawState.Returning;
            while (clawRigidbody.position.y < maxY)
            {
                Vector3 nextPos = clawRigidbody.position + Vector3.up * dropSpeed * Time.fixedDeltaTime;
                clawRigidbody.MovePosition(nextPos);
                yield return new WaitForFixedUpdate();
            }

            // 4. 배출구로 이동
            Vector3 sPos = clawRigidbody.position;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * moveSpeed * 0.5f;
                clawRigidbody.MovePosition(Vector3.Lerp(sPos, dropZonePosition, t));
                yield return new WaitForFixedUpdate();
            }

            // 5. 놓기
            currentState = ClawState.Releasing;
            if (clawGripper != null)
            {
                clawGripper.OpenClaw();
            }
            yield return new WaitForSeconds(1.5f);

            // 6. 끝 (데이터베이스 판정 등 연동)
            currentState = ClawState.Done;
            activeRoutine = null;
            Debug.Log("게임 1회차 완료 - 판정 로직 시작");

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnClawRoutineFinished();
            }
        }

        private IEnumerator ResetRoutine()
        {
            Debug.Log("게임 리셋 시작...");
            if (moveAudioSource != null && moveAudioSource.isPlaying) moveAudioSource.Stop();

            // 1. 집게 열기
            if (clawGripper != null)
            {
                clawGripper.OpenClaw();
            }

            // 2. 수직으로 먼저 상승 (장애물 충돌 방지)
            if (clawRigidbody.position.y < maxY)
            {
                while (clawRigidbody.position.y < maxY)
                {
                    Vector3 nextPos = clawRigidbody.position + Vector3.up * dropSpeed * Time.fixedDeltaTime;
                    if (nextPos.y > maxY) nextPos.y = maxY;
                    clawRigidbody.MovePosition(nextPos);
                    yield return new WaitForFixedUpdate();
                }
            }

            // 3. 수평으로 시작 위치(Home) 이동
            Vector3 currentPos = clawRigidbody.position;
            Vector3 targetPos = new Vector3(startPosition.x, maxY, startPosition.z);
            float distance = Vector3.Distance(currentPos, targetPos);
            
            if (distance > 0.01f)
            {
                float t = 0f;
                float duration = distance / moveSpeed;
                if (duration < 0.1f) duration = 0.1f;

                while (t < 1f)
                {
                    t += Time.deltaTime / duration;
                    clawRigidbody.MovePosition(Vector3.Lerp(currentPos, targetPos, t));
                    yield return new WaitForFixedUpdate();
                }
            }

            clawRigidbody.position = targetPos;
            
            // 4. 상태 복구
            currentState = ClawState.Idle;
            activeRoutine = null;
            Debug.Log("게임 리셋 완료 - 다시 시도 가능");
        }

        private void OnDrawGizmos()
        {
            // 1. 이동 가능 영역(Boundaries) 상자 그리기
            Vector3 center = new Vector3((maxX_Z.x + minX_Z.x) / 2f, (maxY + minY) / 2f, (maxX_Z.y + minX_Z.y) / 2f);
            Vector3 size = new Vector3(maxX_Z.x - minX_Z.x, maxY - minY, maxX_Z.y - minX_Z.y);
            
            Gizmos.color = new Color(0, 1, 0, 0.1f); // 반투명 초록색 면
            Gizmos.DrawCube(center, size);
            
            Gizmos.color = Color.green; // 초록색 테두리
            Gizmos.DrawWireCube(center, size);

            // 2. 배출구(Drop Zone) 그리기
            Gizmos.color = new Color(1, 0, 0, 0.5f); // 반투명 빨간색 구
            Gizmos.DrawSphere(dropZonePosition, 0.3f);
            
            // 3. 배출구 위치 기둥(시각적 보조 선)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(dropZonePosition.x, maxY, dropZonePosition.z), dropZonePosition);
        }
    }
}
