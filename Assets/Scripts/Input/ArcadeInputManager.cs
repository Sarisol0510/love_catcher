using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace ClawMachine.Input
{
    public class ArcadeInputManager : MonoBehaviour
    {
        public static ArcadeInputManager Instance { get; private set; }

        [Header("Current Input State")]
        [Tooltip("조이스틱의 현재 방향 입력값 (-1.0 ~ 1.0)")]
        public Vector2 JoystickInput { get; private set; }
        
        [Tooltip("버튼이 현재 눌려있는지 여부")]
        public bool IsActionPressed { get; private set; }

        /// <summary>
        /// 버튼을 누르는 순간 발생하는 이벤트 (집게 하강 등에 사용)
        /// </summary>
        public event Action OnActionDown;

        /// <summary>
        /// R 키를 누르거나 리셋을 요청할 때 발생하는 이벤트 (테스트용)
        /// </summary>
        public event Action OnResetPressed;

        private InputAction moveAction;
        private InputAction actionButton;
        private InputAction resetAction;

        private void Awake()
        {
            // 싱글톤 패턴 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            SetupInputActions();
        }

        private void SetupInputActions()
        {
            // 코드 기반으로 ActionMap 생성 (별도의 .inputactions 파일 설정 불필요)
            var actionMap = new InputActionMap("ArcadeClaw");

            // 1. 조이스틱(이동) 액션 세팅
            moveAction = actionMap.AddAction("Move", type: InputActionType.Value);
            
            // 키보드(WASD), 게임패드(왼쪽 스틱), 범용 조이스틱(아케이드 인코더) 지원
            moveAction.AddCompositeBinding("2DVector(mode=2)")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Up", "<Gamepad>/leftStick/up")
                .With("Down", "<Gamepad>/leftStick/down")
                .With("Left", "<Gamepad>/leftStick/left")
                .With("Right", "<Gamepad>/leftStick/right")
                .With("Up", "<Joystick>/stick/up")
                .With("Down", "<Joystick>/stick/down")
                .With("Left", "<Joystick>/stick/left")
                .With("Right", "<Joystick>/stick/right");

            // 2. 버튼 액션 세팅 (스페이스바, 게임패드 A버튼, 조이스틱 버튼 지원)
            actionButton = actionMap.AddAction("Catch", type: InputActionType.Button);
            actionButton.AddBinding("<Keyboard>/space");
            actionButton.AddBinding("<Gamepad>/buttonSouth");
            actionButton.AddBinding("<Joystick>/trigger"); // 인코더 보드의 기본 버튼 1

            // 이벤트 연결
            actionButton.performed += ctx => 
            {
                IsActionPressed = true;
                OnActionDown?.Invoke();
            };
            
            actionButton.canceled += ctx => 
            {
                IsActionPressed = false;
            };

            // 3. 리셋/재시도 액션 세팅 (R 키)
            resetAction = actionMap.AddAction("Reset", type: InputActionType.Button);
            resetAction.AddBinding("<Keyboard>/r");

            resetAction.performed += ctx => 
            {
                OnResetPressed?.Invoke();
            };

            // 액션 활성화
            moveAction.Enable();
            actionButton.Enable();
            resetAction.Enable();
        }

        private void Update()
        {
            // 매 프레임마다 조이스틱 방향 업데이트
            JoystickInput = moveAction.ReadValue<Vector2>();
        }

        private void OnDestroy()
        {
            if (moveAction != null)
            {
                moveAction.Disable();
                moveAction.Dispose();
            }
            
            if (actionButton != null)
            {
                actionButton.Disable();
                actionButton.Dispose();
            }

            if (resetAction != null)
            {
                resetAction.Disable();
                resetAction.Dispose();
            }
        }
    }
}
