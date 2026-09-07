using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using ClawMachine.Input;
using ClawMachine.Utils;

namespace ClawMachine.UI
{
    public class ClawMachineUIManager : MonoBehaviour
    {
        public static ClawMachineUIManager Instance { get; private set; }

        [Header("UI Document")]
        [Tooltip("씬에 배치된 UI Document 컴포넌트")]
        public UIDocument uiDocument;

        // UI Elements - HUD
        private Label instaCountText;
        private Label dollCountText;
        private Label winChanceText;
        private Label attemptCountText;
        private Label timerText;
        private VisualElement pityProgressBar;
        private Label pityStatusText;
        private VisualElement pityMaxBanner;
        private Button retryButton;
        private Button quitButton;

        // UI Elements - Overlays
        private VisualElement registerOverlay;
        private VisualElement successOverlay;
        private VisualElement failOverlay;
        private VisualElement quitConfirmOverlay;
        private VisualElement devModeOverlay;
        private VisualElement devDbOverlay;
        private VisualElement failRetryOverlay;

        // Registration Card Fields
        private TextField inputName;
        private TextField inputInsta;
        private TextField inputBio;
        private Button genderBtnMale;
        private Button genderBtnFemale;
        private Button registerSubmitBtn;
        private Label registerMaleCountText;
        private Label registerFemaleCountText;
        private Label registerWarningText;
        private Label registerAccountInfoText;
        private Label failAccountInfoText;

        // Success Card Fields
        private Label matchedName;
        private Label matchedInsta;
        private Label matchedBio;
        private Button successRetryBtn;
        private Button successExitBtn;
        private Label successSubTitle;
        private Label dollPickupNotice;

        // Coin System Fields
        private Label coinCountText;
        private Label popupCoinCountText;
        private Label registerCoinCountText;
        private Button btnCoinPack1;
        private Button btnCoinPack2;
        private Button btnCoinPack3;
        private Button btnCoinPack4;
        private Button regCoinPack1;
        private Button regCoinPack2;
        private Button regCoinPack3;
        private Button regCoinPack4;
        private Button btnCoinStart;
        private int currentCoins = 0;
        private int pendingCoinsToCharge = 0;
        private bool isRetryingFromSuccess = false;

        // Quit Confirm Retention Fields & state
        private Label quitConfirmTitle;
        private Button quitConfirmYesBtn;
        private Button quitConfirmNoBtn;
        private int exitConfirmCount = 0;

        // Fail Card Fields
        private Button failCloseBtn;
        private Button failRetryCloseBtn;
        private Button failRetryAgainBtn;
        private Label failCuteMsg;
        private Label failEmoji;

        // 랜덤 실패 귀여운 멘트 풀
        private static readonly string[] FailCuteMsgs = {
            "인형이 기다리고 있어요 ㅜㅜ",
            "한 번만 더 하면 잡을 수 있어요!",
            "집게가 아직 워밍업 중이에요 🔥",
            "이번엔 진짜 잡힐 것 같은데요?",
            "인형이 '나 여기 있어~' 하고 있어요",
            "다음엔 꼭 될 거예요, 응원할게요 💕",
            "아깝다! 손끝에서 놓쳤어요 😭",
            "인형이 도망갔지만 기다리고 있을 거예요",
            "한 번 더! 이번엔 집게가 각 잡았어요",
            "포기하면 인형이 슬퍼해요 🥺"
        };
        private static readonly string[] FailEmojis = {
            "🥺", "😭", "😢", "🤧", "💔", "😿", "🙈"
        };

        // Dev Mode Fields
        private Button devOpenDbViewBtn;
        private Button devResetDbBtn;
        private Slider devGripForceSlider;
        private Label devGripForceLabel;
        private TextField devGripForceInput;
        private Slider devProbDollInstaSlider;
        private Label devProbDollInstaLabel;
        private TextField devProbDollInstaInput;
        private Slider devProbIdOnlySlider;
        private Label devProbIdOnlyLabel;
        private TextField devProbIdOnlyInput;
        private Slider devProbDollOnlySlider;
        private Label devProbDollOnlyLabel;
        private TextField devProbDollOnlyInput;
        private Slider devProbCandySlider;
        private Label devProbCandyLabel;
        private TextField devProbCandyInput;
        private TextField devTotalDollsInput;
        private Button devSaveDollsBtn;
        private TextField devCoinsInput;
        private Button devSaveCoinsBtn;
        private Button devCloseBtn;

        // Dev Mode Stats Dashboard Labels
        private Label devStatRegistrations;
        private Label devStatPlays;
        private Label devStatSuccesses;
        private Label devStatRevenue;

        // Dev Mode Stats Manual Adjustments Fields
        private TextField devTotalRevenueInput;
        private Button devSaveRevenueBtn;
        private TextField devTotalRegistrationsInput;
        private Button devSaveRegistrationsBtn;
        private TextField devTotalPlaysInput;
        private Button devSavePlaysBtn;
        private TextField devTotalSuccessesInput;
        private Button devSaveSuccessesBtn;
        private Button devResetStatsBtn;

        // Dev DB View Fields
        private ScrollView devDbScrollView;
        private Button devDbRefreshBtn;
        private Button devDbCloseBtn;
        private TextField devDbSearchInput;
        private Button devReloadSceneBtn;
        private System.Collections.Generic.List<ClawMachine.Mechanics.ParticipantData> cachedDbData = new System.Collections.Generic.List<ClawMachine.Mechanics.ParticipantData>();

        private enum DevProbMode { Base, Male, Female }
        private DevProbMode currentDevProbMode = DevProbMode.Base;
        private Toggle devUseGenderToggle;
        private Toggle mainNoInstaToggle;

        // Dev DB 직접 등록 폼 Fields
        private TextField devAddName;
        private TextField devAddInsta;
        private TextField devAddBio;
        private Button devAddGenderMaleBtn;
        private Button devAddGenderFemaleBtn;
        private Button devAddSubmitBtn;
        private Label devAddStatusLabel;
        private string devAddSelectedGender = "남";

        // Current Player Registration Data
        [Header("Registered Player Info (Current Session)")]
        public string registeredName;
        public string registeredInsta;
        public string registeredBio;
        public string registeredGender = "남"; // "남" or "여"

        [Header("SFX")]
        public AudioClip buttonClickSound;
        public AudioClip popupSuccessSound;
        public AudioClip popupFailSound;

        // Events
        public event Action<string, string, string, string, bool> OnPlayerRegistered;
        public event Action OnRetryClicked;
        public event Action OnNextPlayerReady;
        public event Action OnContinueSession;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null)
            {
                InitializeUIElements();
            }
        }

        private void Start()
        {
            if (ArcadeInputManager.Instance != null)
            {
                ArcadeInputManager.Instance.OnActionDown += HandleJoystickActionPressed;
            }

            // 코인 정보 로드 및 UI 초기 갱신
            currentCoins = PlayerPrefs.GetInt("LoveCatcher_Coins", 0);
            UpdateCoinUI();
        }

        private void OnDestroy()
        {
            if (ArcadeInputManager.Instance != null)
            {
                ArcadeInputManager.Instance.OnActionDown -= HandleJoystickActionPressed;
            }
        }
        private void InitializeUIElements()
        {
            var root = uiDocument.rootVisualElement;

            // HUD
            instaCountText = root.Q<Label>("InstaCountText");
            dollCountText = root.Q<Label>("DollCountText");
            winChanceText = root.Q<Label>("WinChanceText");
            attemptCountText = root.Q<Label>("AttemptCountText");
            timerText = root.Q<Label>("TimerText");
            pityProgressBar = root.Q<VisualElement>("PityProgressBar");
            pityStatusText = root.Q<Label>("PityStatusText");
            pityMaxBanner = root.Q<VisualElement>("PityMaxBanner");
            retryButton = root.Q<Button>("RetryButton");
            quitButton = root.Q<Button>("QuitButton");

            // Overlays
            registerOverlay = root.Q<VisualElement>("RegisterOverlay");
            successOverlay = root.Q<VisualElement>("SuccessOverlay");
            failOverlay = root.Q<VisualElement>("FailOverlay");
            quitConfirmOverlay = root.Q<VisualElement>("QuitConfirmOverlay");
            devModeOverlay = root.Q<VisualElement>("DevModeOverlay");
            devDbOverlay = root.Q<VisualElement>("DevDbOverlay");
            failRetryOverlay = root.Q<VisualElement>("FailRetryOverlay");

            // Register Panel Fields
            inputName = root.Q<TextField>("InputName");
            inputInsta = root.Q<TextField>("InputInsta");
            inputBio = root.Q<TextField>("InputBio");
            genderBtnMale = root.Q<Button>("GenderBtnMale");
            genderBtnFemale = root.Q<Button>("GenderBtnFemale");
            registerSubmitBtn = root.Q<Button>("RegisterSubmitBtn");
            registerMaleCountText = root.Q<Label>("RegisterMaleCountText");
            registerFemaleCountText = root.Q<Label>("RegisterFemaleCountText");
            registerWarningText = root.Q<Label>("RegisterWarningText");
            registerAccountInfoText = root.Q<Label>("RegisterAccountInfoText");
            failAccountInfoText = root.Q<Label>("FailAccountInfoText");

            // .env 환경변수에서 부스 결제 계좌 안내 문구 로드 및 UI 적용
            string bankAccount = EnvLoader.Get("BANK_ACCOUNT_INFO");
            if (!string.IsNullOrEmpty(bankAccount))
            {
                if (registerAccountInfoText != null) registerAccountInfoText.text = bankAccount;
                if (failAccountInfoText != null) failAccountInfoText.text = bankAccount;
            }

            // Success Panel Fields
            matchedName = root.Q<Label>("MatchedName");
            matchedInsta = root.Q<Label>("MatchedInsta");
            matchedBio = root.Q<Label>("MatchedBio");
            successRetryBtn = root.Q<Button>("SuccessRetryBtn");
            successExitBtn = root.Q<Button>("SuccessExitBtn");
            successSubTitle = root.Q<Label>("SuccessSubTitle");
            dollPickupNotice = root.Q<Label>("DollPickupNotice");

            // Coin System Fields
            coinCountText = root.Q<Label>("CoinCountText");
            popupCoinCountText = root.Q<Label>("PopupCoinCountText");
            registerCoinCountText = root.Q<Label>("RegisterCoinCountText");
            btnCoinPack1 = root.Q<Button>("BtnCoinPack1");
            btnCoinPack2 = root.Q<Button>("BtnCoinPack2");
            btnCoinPack3 = root.Q<Button>("BtnCoinPack3");
            btnCoinPack4 = root.Q<Button>("BtnCoinPack4");
            regCoinPack1 = root.Q<Button>("RegCoinPack1");
            regCoinPack2 = root.Q<Button>("RegCoinPack2");
            regCoinPack3 = root.Q<Button>("RegCoinPack3");
            regCoinPack4 = root.Q<Button>("RegCoinPack4");
            btnCoinStart = root.Q<Button>("BtnCoinStart");

            // Quit Confirm Panel Fields
            quitConfirmTitle = root.Q<Label>("QuitConfirmTitle");
            quitConfirmYesBtn = root.Q<Button>("QuitConfirmYesBtn");
            quitConfirmNoBtn = root.Q<Button>("QuitConfirmNoBtn");

            // Fail Panel Fields
            failCloseBtn = root.Q<Button>("FailCloseBtn");
            failRetryCloseBtn = root.Q<Button>("FailRetryCloseBtn");
            failRetryAgainBtn = root.Q<Button>("FailRetryAgainBtn");
            failCuteMsg = root.Q<Label>("FailCuteMsg");
            failEmoji = root.Q<Label>("FailEmoji");

            // Dev Mode Fields
            devOpenDbViewBtn = root.Q<Button>("DevOpenDbViewBtn");
            devResetDbBtn = root.Q<Button>("DevResetDbBtn");
            devGripForceSlider = root.Q<Slider>("DevGripForceSlider");
            devGripForceLabel = root.Q<Label>("DevGripForceLabel");
            devGripForceInput = root.Q<TextField>("DevGripForceInput");
            devProbDollInstaSlider = root.Q<Slider>("DevProbDollInstaSlider");
            devProbDollInstaLabel = root.Q<Label>("DevProbDollInstaLabel");
            devProbDollInstaInput = root.Q<TextField>("DevProbDollInstaInput");
            devProbIdOnlySlider = root.Q<Slider>("DevProbIdOnlySlider");
            devProbIdOnlyLabel = root.Q<Label>("DevProbIdOnlyLabel");
            devProbIdOnlyInput = root.Q<TextField>("DevProbIdOnlyInput");
            devProbDollOnlySlider = root.Q<Slider>("DevProbDollOnlySlider");
            devProbDollOnlyLabel = root.Q<Label>("DevProbDollOnlyLabel");
            devProbDollOnlyInput = root.Q<TextField>("DevProbDollOnlyInput");
            devProbCandySlider = root.Q<Slider>("DevProbCandySlider");
            devProbCandyLabel = root.Q<Label>("DevProbCandyLabel");
            devProbCandyInput = root.Q<TextField>("DevProbCandyInput");
            devTotalDollsInput = root.Q<TextField>("DevTotalDollsInput");
            devSaveDollsBtn = root.Q<Button>("DevSaveDollsBtn");
            devCoinsInput = root.Q<TextField>("DevCoinsInput");
            devSaveCoinsBtn = root.Q<Button>("DevSaveCoinsBtn");
            devCloseBtn = root.Q<Button>("DevCloseBtn");

            // Bind Stat Labels
            devStatRegistrations = root.Q<Label>("DevStatRegistrations");
            devStatPlays = root.Q<Label>("DevStatPlays");
            devStatSuccesses = root.Q<Label>("DevStatSuccesses");
            devStatRevenue = root.Q<Label>("DevStatRevenue");

            // Bind Stat Adjustments Input Fields and Buttons
            devTotalRevenueInput = root.Q<TextField>("DevTotalRevenueInput");
            devSaveRevenueBtn = root.Q<Button>("DevSaveRevenueBtn");
            devTotalRegistrationsInput = root.Q<TextField>("DevTotalRegistrationsInput");
            devSaveRegistrationsBtn = root.Q<Button>("DevSaveRegistrationsBtn");
            devTotalPlaysInput = root.Q<TextField>("DevTotalPlaysInput");
            devSavePlaysBtn = root.Q<Button>("DevSavePlaysBtn");
            devTotalSuccessesInput = root.Q<TextField>("DevTotalSuccessesInput");
            devSaveSuccessesBtn = root.Q<Button>("DevSaveSuccessesBtn");
            devResetStatsBtn = root.Q<Button>("DevResetStatsBtn");

            // Dev DB View Fields
            devDbScrollView = root.Q<ScrollView>("DevDbScrollView");
            devDbRefreshBtn = root.Q<Button>("DevDbRefreshBtn");
            devDbCloseBtn = root.Q<Button>("DevDbCloseBtn");

            // Dev DB 직접 등록 폼 Fields
            devAddName = root.Q<TextField>("DevAddName");
            devAddInsta = root.Q<TextField>("DevAddInsta");
            devAddBio = root.Q<TextField>("DevAddBio");
            devAddGenderMaleBtn = root.Q<Button>("DevAddGenderMaleBtn");
            devAddGenderFemaleBtn = root.Q<Button>("DevAddGenderFemaleBtn");
            devAddSubmitBtn = root.Q<Button>("DevAddSubmitBtn");
            devAddStatusLabel = root.Q<Label>("DevAddStatusLabel");

            // UI Button SFX Helper
            Action playBtnSound = () => 
            { 
                if (buttonClickSound != null && ClawMachine.Audio.SoundManager.Instance != null) 
                    ClawMachine.Audio.SoundManager.Instance.PlaySFX(buttonClickSound); 
            };

            // Register Event Listeners
            genderBtnMale.clicked += () => { playBtnSound(); SelectGender("남"); };
            genderBtnFemale.clicked += () => { playBtnSound(); SelectGender("여"); };
            registerSubmitBtn.clicked += () => { playBtnSound(); SubmitRegistration(); };
            
            if (registerSubmitBtn != null && registerSubmitBtn.parent != null)
            {
                // The mainNoInstaToggle has been moved to the developer mode screen.
            }
            
            if (retryButton != null)
            {
                retryButton.clicked += () => 
                { 
                    playBtnSound(); 
                    isRetryingFromSuccess = false; 
                    ExecuteRetryAction(); 
                };
            }

            if (quitButton != null)
            {
                quitButton.clicked += () => { playBtnSound(); ResetToRegistration(); };
            }

            if (successRetryBtn != null)
            {
                successRetryBtn.clicked += () => 
                { 
                    playBtnSound(); 
                    ExecuteSuccessRetryClick(); 
                };
            }

            if (successExitBtn != null)
            {
                successExitBtn.clicked += () => { playBtnSound(); HandleSuccessExitClick(); };
            }

            if (quitConfirmNoBtn != null)
            {
                quitConfirmNoBtn.clicked += () => { playBtnSound(); HandleQuitConfirmNoClick(); };
            }

            if (quitConfirmYesBtn != null)
            {
                quitConfirmYesBtn.clicked += () => { playBtnSound(); HandleQuitConfirmYesClick(); };
            }

            failCloseBtn.clicked += () => { playBtnSound(); ResetToRegistration(); };
            if (failRetryCloseBtn != null)
            {
                failRetryCloseBtn.clicked += () => 
                { 
                    playBtnSound(); 
                    HideOverlay(failRetryOverlay); 
                    if (isRetryingFromSuccess)
                    {
                        ShowOverlay(successOverlay);
                    }
                    else
                    {
                        ShowRetryButton(true);
                    }
                };
            }
            if (failRetryAgainBtn != null)
            {
                failRetryAgainBtn.clicked += () => 
                { 
                    playBtnSound(); 
                    ExecuteFailRetryAgainClick(); 
                };
            }

            // Coin Package Button Events
            Button[] regPacks = { regCoinPack1, regCoinPack2, regCoinPack3, regCoinPack4 };
            Button[] failPacks = { btnCoinPack1, btnCoinPack2, btnCoinPack3, btnCoinPack4 };

            if (btnCoinPack1 != null) btnCoinPack1.clicked += () => { SelectCoinPackage(1, btnCoinPack1, failPacks); };
            if (btnCoinPack2 != null) btnCoinPack2.clicked += () => { SelectCoinPackage(3, btnCoinPack2, failPacks); };
            if (btnCoinPack3 != null) btnCoinPack3.clicked += () => { SelectCoinPackage(6, btnCoinPack3, failPacks); };
            if (btnCoinPack4 != null) btnCoinPack4.clicked += () => { SelectCoinPackage(13, btnCoinPack4, failPacks); };

            if (regCoinPack1 != null) regCoinPack1.clicked += () => { SelectCoinPackage(1, regCoinPack1, regPacks); };
            if (regCoinPack2 != null) regCoinPack2.clicked += () => { SelectCoinPackage(3, regCoinPack2, regPacks); };
            if (regCoinPack3 != null) regCoinPack3.clicked += () => { SelectCoinPackage(6, regCoinPack3, regPacks); };
            if (regCoinPack4 != null) regCoinPack4.clicked += () => { SelectCoinPackage(13, regCoinPack4, regPacks); };

            if (btnCoinStart != null)
            {
                btnCoinStart.clicked += () =>
                {
                    playBtnSound();
                    ExecuteCoinStartClick();
                };
            }

            // Dev Mode Events
            if (devCloseBtn != null) devCloseBtn.clicked += () => { playBtnSound(); HideOverlay(devModeOverlay); };
            if (devResetDbBtn != null) devResetDbBtn.clicked += () => { playBtnSound(); HandleDevResetDb(); };
            if (devOpenDbViewBtn != null) devOpenDbViewBtn.clicked += () => { playBtnSound(); OpenDbView(); };

            // Dev DB View Events
            if (devDbCloseBtn != null) devDbCloseBtn.clicked += () => { playBtnSound(); CloseDbView(); };
            if (devDbRefreshBtn != null) devDbRefreshBtn.clicked += () => { playBtnSound(); RefreshDbView(); };

            // Dev DB 직접 등록 폼 Events
            if (devAddGenderMaleBtn != null)
                devAddGenderMaleBtn.clicked += () => { playBtnSound(); SetDevAddGender("남"); };
            if (devAddGenderFemaleBtn != null)
                devAddGenderFemaleBtn.clicked += () => { playBtnSound(); SetDevAddGender("여"); };
            if (devAddSubmitBtn != null)
                devAddSubmitBtn.clicked += () => { playBtnSound(); HandleDevAddSubmit(); };



            if (devGripForceSlider != null)
            {
                devGripForceSlider.RegisterValueChangedCallback(evt => {
                    if (devGripForceLabel != null) devGripForceLabel.text = $"집게 힘 (악력): {evt.newValue:F1}";
                    if (devGripForceInput != null && rootVisualElement().focusController.focusedElement != devGripForceInput)
                        devGripForceInput.value = evt.newValue.ToString("F1");
                    
                    if (ClawMachine.Mechanics.GameFlowManager.Instance != null && ClawMachine.Mechanics.GameFlowManager.Instance.clawController != null)
                    {
                        var gripper = ClawMachine.Mechanics.GameFlowManager.Instance.clawController.clawGripper;
                        if (gripper != null)
                        {
                            gripper.baseGripForce = evt.newValue;
                            gripper.gripForce = evt.newValue;
                        }
                    }
                });
            }

            if (devGripForceInput != null)
            {
                devGripForceInput.RegisterValueChangedCallback(evt => {
                    if (float.TryParse(evt.newValue, out float val))
                    {
                        val = Mathf.Clamp(val, 10f, 150f);
                        if (devGripForceSlider != null && !Mathf.Approximately(devGripForceSlider.value, val))
                        {
                            devGripForceSlider.value = val;
                        }
                    }
                });
            }

            if (devProbDollInstaSlider != null && devProbDollInstaSlider.parent != null)
            {
                var toggleContainer = new VisualElement();
                toggleContainer.style.flexDirection = FlexDirection.Row;
                toggleContainer.style.marginBottom = 10;
                toggleContainer.style.alignItems = Align.Center;

                devUseGenderToggle = new Toggle("성별에 따른 확률 사용");
                devUseGenderToggle.style.color = new Color(1f, 1f, 1f);
                if (ClawMachine.Mechanics.GameFlowManager.Instance != null) {
                    devUseGenderToggle.value = ClawMachine.Mechanics.GameFlowManager.Instance.useGenderSpecificProbability;
                }
                devUseGenderToggle.RegisterValueChangedCallback(evt => {
                    if (ClawMachine.Mechanics.GameFlowManager.Instance != null) {
                        ClawMachine.Mechanics.GameFlowManager.Instance.useGenderSpecificProbability = evt.newValue;
                    }
                });
                toggleContainer.Add(devUseGenderToggle);

                mainNoInstaToggle = new Toggle("인스타 등록 없이 진행 모드");
                mainNoInstaToggle.style.color = new Color(1f, 1f, 1f);
                mainNoInstaToggle.style.marginLeft = 20;
                if (ClawMachine.Mechanics.GameFlowManager.Instance != null) {
                    mainNoInstaToggle.value = ClawMachine.Mechanics.GameFlowManager.Instance.noInstaMode;
                }
                mainNoInstaToggle.RegisterValueChangedCallback(evt => {
                    if (ClawMachine.Mechanics.GameFlowManager.Instance != null) {
                        ClawMachine.Mechanics.GameFlowManager.Instance.noInstaMode = evt.newValue;
                    }
                });
                toggleContainer.Add(mainNoInstaToggle);

                var probModeContainer = new VisualElement();
                probModeContainer.style.flexDirection = FlexDirection.Row;
                probModeContainer.style.marginBottom = 10;
                
                var btnBase = new Button { text = "기본 설정" };
                var btnMale = new Button { text = "남성용 설정" };
                var btnFemale = new Button { text = "여성용 설정" };
                
                Action updateBtnColors = () => {
                    btnBase.style.backgroundColor = currentDevProbMode == DevProbMode.Base ? new Color(0, 0.4f, 0) : new Color(0.2f, 0.2f, 0.2f);
                    btnMale.style.backgroundColor = currentDevProbMode == DevProbMode.Male ? new Color(0, 0, 0.5f) : new Color(0.2f, 0.2f, 0.2f);
                    btnFemale.style.backgroundColor = currentDevProbMode == DevProbMode.Female ? new Color(0.5f, 0, 0) : new Color(0.2f, 0.2f, 0.2f);
                };

                btnBase.clicked += () => { currentDevProbMode = DevProbMode.Base; updateBtnColors(); RefreshDevProbSliders(); };
                btnMale.clicked += () => { currentDevProbMode = DevProbMode.Male; updateBtnColors(); RefreshDevProbSliders(); };
                btnFemale.clicked += () => { currentDevProbMode = DevProbMode.Female; updateBtnColors(); RefreshDevProbSliders(); };
                
                probModeContainer.Add(btnBase); probModeContainer.Add(btnMale); probModeContainer.Add(btnFemale);
                updateBtnColors();

                // Add above the probability label
                var parent = devProbDollInstaSlider.parent;
                var indexOfLabel = parent.IndexOf(devProbDollInstaSlider) - 1;
                if (indexOfLabel >= 0) {
                    parent.Insert(indexOfLabel, probModeContainer);
                    parent.Insert(indexOfLabel, toggleContainer);
                }
            }

            if (devProbDollInstaSlider != null)
            {
                devProbDollInstaSlider.RegisterValueChangedCallback(evt => {
                    if (devProbDollInstaLabel != null) devProbDollInstaLabel.text = $"[인형 + 인스타] 확률: {evt.newValue:F1}%";
                    if (devProbDollInstaInput != null && rootVisualElement().focusController.focusedElement != devProbDollInstaInput)
                        devProbDollInstaInput.value = evt.newValue.ToString("F1");
                    
                    if (ClawMachine.Mechanics.GameFlowManager.Instance != null) {
                        var gm = ClawMachine.Mechanics.GameFlowManager.Instance;
                        if (currentDevProbMode == DevProbMode.Base) gm.probDollAndInsta = evt.newValue;
                        else if (currentDevProbMode == DevProbMode.Male) gm.maleProbDollAndInsta = evt.newValue;
                        else if (currentDevProbMode == DevProbMode.Female) gm.femaleProbDollAndInsta = evt.newValue;
                    }
                });
            }

            if (devProbDollInstaInput != null)
            {
                devProbDollInstaInput.RegisterValueChangedCallback(evt => {
                    if (float.TryParse(evt.newValue, out float val))
                    {
                        val = Mathf.Clamp(val, 0f, 100f);
                        if (devProbDollInstaSlider != null && !Mathf.Approximately(devProbDollInstaSlider.value, val))
                        {
                            devProbDollInstaSlider.value = val;
                        }
                    }
                });
            }

            if (devProbIdOnlySlider != null)
            {
                devProbIdOnlySlider.RegisterValueChangedCallback(evt => {
                    if (devProbIdOnlyLabel != null) devProbIdOnlyLabel.text = $"[아이디만] 확률: {evt.newValue:F1}%";
                    if (devProbIdOnlyInput != null && rootVisualElement().focusController.focusedElement != devProbIdOnlyInput)
                        devProbIdOnlyInput.value = evt.newValue.ToString("F1");
                    
                    if (ClawMachine.Mechanics.GameFlowManager.Instance != null) {
                        var gm = ClawMachine.Mechanics.GameFlowManager.Instance;
                        if (currentDevProbMode == DevProbMode.Base) gm.probIdOnly = evt.newValue;
                        else if (currentDevProbMode == DevProbMode.Male) gm.maleProbIdOnly = evt.newValue;
                        else if (currentDevProbMode == DevProbMode.Female) gm.femaleProbIdOnly = evt.newValue;
                    }
                });
            }

            if (devProbIdOnlyInput != null)
            {
                devProbIdOnlyInput.RegisterValueChangedCallback(evt => {
                    if (float.TryParse(evt.newValue, out float val))
                    {
                        val = Mathf.Clamp(val, 0f, 100f);
                        if (devProbIdOnlySlider != null && !Mathf.Approximately(devProbIdOnlySlider.value, val))
                        {
                            devProbIdOnlySlider.value = val;
                        }
                    }
                });
            }

            if (devProbDollOnlySlider != null)
            {
                devProbDollOnlySlider.RegisterValueChangedCallback(evt => {
                    if (devProbDollOnlyLabel != null) devProbDollOnlyLabel.text = $"[실물 인형만] 확률: {evt.newValue:F1}%";
                    if (devProbDollOnlyInput != null && rootVisualElement().focusController.focusedElement != devProbDollOnlyInput)
                        devProbDollOnlyInput.value = evt.newValue.ToString("F1");
                    
                    if (ClawMachine.Mechanics.GameFlowManager.Instance != null) {
                        var gm = ClawMachine.Mechanics.GameFlowManager.Instance;
                        if (currentDevProbMode == DevProbMode.Base) gm.probDollOnly = evt.newValue;
                        else if (currentDevProbMode == DevProbMode.Male) gm.maleProbDollOnly = evt.newValue;
                        else if (currentDevProbMode == DevProbMode.Female) gm.femaleProbDollOnly = evt.newValue;
                    }
                });
            }

            if (devProbDollOnlyInput != null)
            {
                devProbDollOnlyInput.RegisterValueChangedCallback(evt => {
                    if (float.TryParse(evt.newValue, out float val))
                    {
                        val = Mathf.Clamp(val, 0f, 100f);
                        if (devProbDollOnlySlider != null && !Mathf.Approximately(devProbDollOnlySlider.value, val))
                        {
                            devProbDollOnlySlider.value = val;
                        }
                    }
                });
            }

            if (devProbCandySlider != null)
            {
                devProbCandySlider.RegisterValueChangedCallback(evt => {
                    if (devProbCandyLabel != null) devProbCandyLabel.text = $"[꽝(사탕)] 확률: {evt.newValue:F1}%";
                    if (devProbCandyInput != null && rootVisualElement().focusController.focusedElement != devProbCandyInput)
                        devProbCandyInput.value = evt.newValue.ToString("F1");
                    
                    if (ClawMachine.Mechanics.GameFlowManager.Instance != null) {
                        var gm = ClawMachine.Mechanics.GameFlowManager.Instance;
                        if (currentDevProbMode == DevProbMode.Base) gm.probCandy = evt.newValue;
                        else if (currentDevProbMode == DevProbMode.Male) gm.maleProbCandy = evt.newValue;
                        else if (currentDevProbMode == DevProbMode.Female) gm.femaleProbCandy = evt.newValue;
                    }
                });
            }

            if (devProbCandyInput != null)
            {
                devProbCandyInput.RegisterValueChangedCallback(evt => {
                    if (float.TryParse(evt.newValue, out float val))
                    {
                        val = Mathf.Clamp(val, 0f, 100f);
                        if (devProbCandySlider != null && !Mathf.Approximately(devProbCandySlider.value, val))
                        {
                            devProbCandySlider.value = val;
                        }
                    }
                });
            }

            if (devSaveDollsBtn != null)
            {
                devSaveDollsBtn.clicked += () => {
                    playBtnSound();
                    if (devTotalDollsInput != null && int.TryParse(devTotalDollsInput.value, out int newCount))
                    {
                        devSaveDollsBtn.text = "...";
                        devSaveDollsBtn.SetEnabled(false);
                        if (ClawMachine.Mechanics.FirebaseRESTService.Instance != null)
                        {
                            StartCoroutine(ClawMachine.Mechanics.FirebaseRESTService.Instance.UpdateTotalDolls(newCount, success => {
                                if (success)
                                {
                                    devSaveDollsBtn.text = "완료!";
                                    if (ClawMachine.Mechanics.GameFlowManager.Instance != null)
                                    {
                                        ClawMachine.Mechanics.GameFlowManager.Instance.totalDolls = newCount;
                                        ClawMachine.Mechanics.GameFlowManager.Instance.UpdateStatsUI();
                                    }
                                }
                                else
                                {
                                    devSaveDollsBtn.text = "실패";
                                }
                                devSaveDollsBtn.SetEnabled(true);
                            }));
                        }
                    }
                };
            }

            if (devSaveCoinsBtn != null)
            {
                devSaveCoinsBtn.clicked += () => {
                    playBtnSound();
                    if (devCoinsInput != null && int.TryParse(devCoinsInput.value, out int newCoins))
                    {
                        devSaveCoinsBtn.text = "...";
                        devSaveCoinsBtn.SetEnabled(false);
                        
                        currentCoins = newCoins;
                        PlayerPrefs.SetInt("LoveCatcher_Coins", currentCoins);
                        PlayerPrefs.Save();
                        UpdateCoinUI();
                        
                        devSaveCoinsBtn.text = "완료!";
                        devSaveCoinsBtn.SetEnabled(true);
                        
                        Invoke(nameof(RestoreDevSaveCoinsBtnText), 1.5f);
                    }
                };
            }

            if (devSaveRevenueBtn != null)
            {
                devSaveRevenueBtn.clicked += () => {
                    playBtnSound();
                    if (devTotalRevenueInput != null && int.TryParse(devTotalRevenueInput.value, out int newVal))
                    {
                        devSaveRevenueBtn.text = "...";
                        devSaveRevenueBtn.SetEnabled(false);
                        StartCoroutine(SaveSingleStatCoroutine("totalRevenue", newVal, success => {
                            devSaveRevenueBtn.text = success ? "완료!" : "실패";
                            devSaveRevenueBtn.SetEnabled(true);
                            RefreshDevModeStats();
                        }));
                    }
                };
            }

            if (devSaveRegistrationsBtn != null)
            {
                devSaveRegistrationsBtn.clicked += () => {
                    playBtnSound();
                    if (devTotalRegistrationsInput != null && int.TryParse(devTotalRegistrationsInput.value, out int newVal))
                    {
                        devSaveRegistrationsBtn.text = "...";
                        devSaveRegistrationsBtn.SetEnabled(false);
                        StartCoroutine(SaveSingleStatCoroutine("totalRegistrations", newVal, success => {
                            devSaveRegistrationsBtn.text = success ? "완료!" : "실패";
                            devSaveRegistrationsBtn.SetEnabled(true);
                            RefreshDevModeStats();
                        }));
                    }
                };
            }

            if (devSavePlaysBtn != null)
            {
                devSavePlaysBtn.clicked += () => {
                    playBtnSound();
                    if (devTotalPlaysInput != null && int.TryParse(devTotalPlaysInput.value, out int newVal))
                    {
                        devSavePlaysBtn.text = "...";
                        devSavePlaysBtn.SetEnabled(false);
                        StartCoroutine(SaveSingleStatCoroutine("totalPlays", newVal, success => {
                            devSavePlaysBtn.text = success ? "완료!" : "실패";
                            devSavePlaysBtn.SetEnabled(true);
                            RefreshDevModeStats();
                        }));
                    }
                };
            }

            if (devSaveSuccessesBtn != null)
            {
                devSaveSuccessesBtn.clicked += () => {
                    playBtnSound();
                    if (devTotalSuccessesInput != null && int.TryParse(devTotalSuccessesInput.value, out int newVal))
                    {
                        devSaveSuccessesBtn.text = "...";
                        devSaveSuccessesBtn.SetEnabled(false);
                        StartCoroutine(SaveSingleStatCoroutine("totalSuccesses", newVal, success => {
                            devSaveSuccessesBtn.text = success ? "완료!" : "실패";
                            devSaveSuccessesBtn.SetEnabled(true);
                            RefreshDevModeStats();
                        }));
                    }
                };
            }

            if (devResetStatsBtn != null)
            {
                devResetStatsBtn.clicked += () => {
                    playBtnSound();
                    devResetStatsBtn.text = "초기화 진행 중...";
                    devResetStatsBtn.SetEnabled(false);
                    
                    var zeroStats = new ClawMachine.Mechanics.GameStatsData
                    {
                        totalRevenue = 0,
                        totalRegistrations = 0,
                        totalPlays = 0,
                        totalSuccesses = 0,
                        totalDolls = (ClawMachine.Mechanics.GameFlowManager.Instance != null) ? ClawMachine.Mechanics.GameFlowManager.Instance.totalDolls : 100
                    };

                    StartCoroutine(ClawMachine.Mechanics.FirebaseRESTService.Instance.UpdateGameStats(
                        zeroStats, 
                        new System.Collections.Generic.List<string> { "totalRevenue", "totalRegistrations", "totalPlays", "totalSuccesses" }, 
                        success => {
                            devResetStatsBtn.text = success ? "통계 초기화 완료!" : "통계 초기화 실패";
                            devResetStatsBtn.SetEnabled(true);
                            RefreshDevModeStats();
                            Invoke(nameof(RestoreDevResetStatsBtnText), 2f);
                        }));
                };
            }

            // Create Dev DB Search Input dynamically
            devDbSearchInput = new TextField("이름 검색");
            devDbSearchInput.AddToClassList("form-input");
            devDbSearchInput.style.marginBottom = 10;
            devDbSearchInput.style.color = Color.white;
            devDbSearchInput.RegisterValueChangedCallback(evt => {
                FilterDbView(evt.newValue);
            });
            if (devDbOverlay != null)
            {
                var devDbCard = devDbOverlay.Q<VisualElement>("DevDbCard");
                if (devDbCard != null && devDbScrollView != null)
                {
                    devDbCard.Insert(devDbCard.IndexOf(devDbScrollView), devDbSearchInput);
                }
            }

            // Create Scene Reload Button dynamically
            devReloadSceneBtn = new Button() { text = "Scene 리로드 (씬 초기화)" };
            devReloadSceneBtn.AddToClassList("btn-primary");
            devReloadSceneBtn.style.backgroundColor = new Color(0.8f, 0f, 0f);
            devReloadSceneBtn.style.marginTop = 10;
            devReloadSceneBtn.style.width = new Length(100, LengthUnit.Percent);
            devReloadSceneBtn.clicked += () => { 
                playBtnSound(); 
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); 
            };
            if (devModeOverlay != null)
            {
                var devPanel = devModeOverlay.Q<VisualElement>("DevModeCard");
                if (devPanel == null && devModeOverlay.childCount > 0)
                {
                    devPanel = devModeOverlay.ElementAt(0);
                }
                if (devPanel != null)
                {
                    devPanel.Add(devReloadSceneBtn);
                }
            }

            // Initial UI State
            SelectGender("남"); // 기본 성별은 남성
            HideAllOverlays();
            ShowOverlay(registerOverlay);
        }

        private void SelectGender(string gender)
        {
            registeredGender = gender;
            if (gender == "남")
            {
                genderBtnMale.AddToClassList("gender-button-male-selected");
                genderBtnFemale.RemoveFromClassList("gender-button-female-selected");
                
                // 카드에 테두리 색을 Cyan으로 설정
                rootVisualElement().Q<VisualElement>("RegisterCard").RemoveFromClassList("popup-card-female");
                registerSubmitBtn.RemoveFromClassList("btn-primary-female");
            }
            else
            {
                genderBtnMale.RemoveFromClassList("gender-button-male-selected");
                genderBtnFemale.AddToClassList("gender-button-female-selected");
                
                // 카드에 테두리 색을 Pink로 설정
                rootVisualElement().Q<VisualElement>("RegisterCard").AddToClassList("popup-card-female");
                registerSubmitBtn.AddToClassList("btn-primary-female");
            }
        }

        private bool isDuplicateRegistration = false;

        private void SubmitRegistration()
        {
            // 포커스 해제하여 커서 인덱스 예외(ArgumentOutOfRangeException) 방지
            inputName?.Blur();
            inputInsta?.Blur();
            inputBio?.Blur();

            isDuplicateRegistration = false;
            registeredName = inputName.value;
            registeredInsta = inputInsta.value;
            registeredBio = inputBio.value;

            bool isInstaRequired = true;
            if (ClawMachine.Mechanics.GameFlowManager.Instance != null && ClawMachine.Mechanics.GameFlowManager.Instance.noInstaMode)
            {
                isInstaRequired = false;
            }

            if (string.IsNullOrEmpty(registeredName) || (isInstaRequired && string.IsNullOrEmpty(registeredInsta)))
            {
                if (registerWarningText != null)
                {
                    registerWarningText.text = isInstaRequired ? "이름과 인스타 아이디는 필수 입력 항목입니다." : "이름은 필수 입력 항목입니다.";
                    registerWarningText.style.display = DisplayStyle.Flex;
                }
                Debug.LogWarning("필수 입력 항목 누락.");
                return;
            }

            if (registerWarningText != null) registerWarningText.style.display = DisplayStyle.None;

            if (registeredGender == "남" && ClawMachine.Mechanics.FirebaseRESTService.Instance != null)
            {
                if (registerSubmitBtn != null) registerSubmitBtn.SetEnabled(false);
                
                StartCoroutine(ClawMachine.Mechanics.FirebaseRESTService.Instance.CheckInstaIdExists(registeredInsta, (exists) => {
                    isDuplicateRegistration = exists;
                    if (registerSubmitBtn != null) registerSubmitBtn.SetEnabled(true);
                    CheckCoinAndProceed();
                }));
            }
            else
            {
                CheckCoinAndProceed();
            }
        }

        private void CheckCoinAndProceed()
        {
            int totalAvailable = currentCoins + pendingCoinsToCharge;
            if (totalAvailable > 0)
            {
                int revenue = GetRevenueFromStagedCoins(pendingCoinsToCharge);
                currentCoins = totalAvailable - 1;
                pendingCoinsToCharge = 0;

                // Clear highlights from all pack buttons
                Button[] allPackButtons = { btnCoinPack1, btnCoinPack2, btnCoinPack3, btnCoinPack4, regCoinPack1, regCoinPack2, regCoinPack3, regCoinPack4 };
                foreach (var btn in allPackButtons)
                {
                    if (btn != null)
                    {
                        btn.RemoveFromClassList("gender-button-male-selected");
                    }
                }

                PlayerPrefs.SetInt("LoveCatcher_Coins", currentCoins);
                PlayerPrefs.Save();
                UpdateCoinUI();

                // Record the play session
                RecordPlaySession(revenue);
                
                if (registerWarningText != null) registerWarningText.style.display = DisplayStyle.None;
                ProceedWithRegistration();
            }
            else
            {
                if (registerWarningText != null)
                {
                    registerWarningText.text = "코인을 먼저 충전해주세요! (우측 카드 이용)";
                    registerWarningText.style.display = DisplayStyle.Flex;
                }
            }
        }

        private void ProceedWithRegistration()
        {
            // 오버레이 숨기기
            HideOverlay(registerOverlay);
            
            // 이벤트 발행 -> Game Flow Manager에서 수신하여 타이머 시작 및 게임 가능 모드 전환
            OnPlayerRegistered?.Invoke(registeredName, registeredInsta, registeredBio, registeredGender, isDuplicateRegistration);
        }

        private void ResetToRegistration()
        {
            // Clear coin selection state
            ClearStagedCoins();

            // Reset actual coins to 0 upon quitting
            currentCoins = 0;
            PlayerPrefs.SetInt("LoveCatcher_Coins", 0);
            PlayerPrefs.Save();
            UpdateCoinUI();

            // 포커스 해제
            inputName?.Blur();
            inputInsta?.Blur();
            inputBio?.Blur();

            // 입력 필드 비우기
            inputName.value = "";
            inputInsta.value = "";
            inputBio.value = "";
            
            if (registerWarningText != null) registerWarningText.style.display = DisplayStyle.None;
            SelectGender("남");

            HideAllOverlays();
            ShowOverlay(registerOverlay);

            OnNextPlayerReady?.Invoke();
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame &&
                    UnityEngine.InputSystem.Keyboard.current.ctrlKey.isPressed &&
                    UnityEngine.InputSystem.Keyboard.current.altKey.isPressed)
                {
                    ToggleDevMode();
                }

                // Operator R key retry override when bottom bar, fail overlay, or payment overlay is visible
                if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                {
                    bool isRetryEligible = (retryButton != null && retryButton.resolvedStyle.display == DisplayStyle.Flex) ||
                                           (failRetryOverlay != null && failRetryOverlay.resolvedStyle.display == DisplayStyle.Flex) ||
                                           (failOverlay != null && failOverlay.resolvedStyle.display == DisplayStyle.Flex);

                    if (isRetryEligible && ClawMachine.Mechanics.GameFlowManager.Instance != null)
                    {
                        HideAllOverlays();
                        ClawMachine.Mechanics.GameFlowManager.Instance.RequestRetry();
                    }
                }
            }

            // UI Navigation Update
            if (currentNavGroup != NavGroup.None && currentButtons != null && currentButtons.Length > 0)
            {
                // Support keyboard Arrow keys or WASD for UI navigation
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
                    {
                        ChangeSelection(1);
                    }
                    else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                    {
                        ChangeSelection(-1);
                    }
                }

                // Support Arcade Joystick
                Vector2 joystick = Vector2.zero;
                if (ArcadeInputManager.Instance != null)
                {
                    joystick = ArcadeInputManager.Instance.JoystickInput;
                }

                float moveThreshold = 0.5f;
                if (!isJoystickDpadPressed)
                {
                    if (joystick.x > moveThreshold || joystick.y > moveThreshold)
                    {
                        isJoystickDpadPressed = true;
                        ChangeSelection(-1);
                    }
                    else if (joystick.x < -moveThreshold || joystick.y < -moveThreshold)
                    {
                        isJoystickDpadPressed = true;
                        ChangeSelection(1);
                    }
                }
                else
                {
                    if (Mathf.Abs(joystick.x) < 0.15f && Mathf.Abs(joystick.y) < 0.15f)
                    {
                        isJoystickDpadPressed = false;
                    }
                }
            }
        }

        private void ToggleDevMode()
        {
            if (devModeOverlay == null) return;

            if (devModeOverlay.style.display == DisplayStyle.Flex)
            {
                HideOverlay(devModeOverlay);
            }
            else
            {
                // 열릴 때 현재 값으로 동기화
                RefreshDevProbSliders();
                if (ClawMachine.Mechanics.GameFlowManager.Instance != null)
                {
                    if (ClawMachine.Mechanics.GameFlowManager.Instance.clawController != null && ClawMachine.Mechanics.GameFlowManager.Instance.clawController.clawGripper != null)
                    {
                        if (devGripForceSlider != null) devGripForceSlider.value = ClawMachine.Mechanics.GameFlowManager.Instance.clawController.clawGripper.baseGripForce;
                    }
                }
                ShowOverlay(devModeOverlay);
            }
        }

        private void HandleDevResetDb()
        {
            if (ClawMachine.Mechanics.FirebaseRESTService.Instance != null && devResetDbBtn != null)
            {
                devResetDbBtn.text = "초기화 진행 중...";
                devResetDbBtn.SetEnabled(false);
                StartCoroutine(ClawMachine.Mechanics.FirebaseRESTService.Instance.ResetAllPickedStatus((success) => {
                    devResetDbBtn.text = success ? "초기화 완료!" : "초기화 실패!";
                    Invoke(nameof(RestoreDevResetBtnText), 2f);
                }));
            }
        }

        private void RestoreDevResetBtnText()
        {
            if (devResetDbBtn != null)
            {
                devResetDbBtn.text = "데이터베이스 유저 전원 상태 초기화 (isPicked = false)";
                devResetDbBtn.SetEnabled(true);
            }
        }

        private void RestoreDevSaveCoinsBtnText()
        {
            if (devSaveCoinsBtn != null)
            {
                devSaveCoinsBtn.text = "저장";
            }
        }

        private void OpenDbView()
        {
            HideOverlay(devModeOverlay);
            ShowOverlay(devDbOverlay);
            ResetDevAddForm();
            RefreshDbView();
        }

        private void ResetDevAddForm()
        {
            if (devAddName != null) devAddName.value = "";
            if (devAddInsta != null) devAddInsta.value = "";
            if (devAddBio != null) devAddBio.value = "";
            if (devAddStatusLabel != null) devAddStatusLabel.style.display = DisplayStyle.None;
            devAddSelectedGender = "남";
            SetDevAddGender("남");
        }

        private void SetDevAddGender(string gender)
        {
            devAddSelectedGender = gender;
            if (devAddGenderMaleBtn != null)
            {
                devAddGenderMaleBtn.style.backgroundColor = gender == "남"
                    ? new StyleColor(new Color(0f, 0.55f, 1f))
                    : new StyleColor(new Color(0.31f, 0.31f, 0.31f, 0.5f));
                devAddGenderMaleBtn.style.borderTopColor = gender == "남"
                    ? new StyleColor(new Color(0f, 0.93f, 1f))
                    : new StyleColor(new Color(1f, 1f, 1f, 0.2f));
                devAddGenderMaleBtn.style.borderBottomColor = devAddGenderMaleBtn.style.borderTopColor;
                devAddGenderMaleBtn.style.borderLeftColor = devAddGenderMaleBtn.style.borderTopColor;
                devAddGenderMaleBtn.style.borderRightColor = devAddGenderMaleBtn.style.borderTopColor;
            }
            if (devAddGenderFemaleBtn != null)
            {
                devAddGenderFemaleBtn.style.backgroundColor = gender == "여"
                    ? new StyleColor(new Color(1f, 0f, 0.5f))
                    : new StyleColor(new Color(0.31f, 0.31f, 0.31f, 0.5f));
                devAddGenderFemaleBtn.style.borderTopColor = gender == "여"
                    ? new StyleColor(new Color(1f, 0.4f, 0.8f))
                    : new StyleColor(new Color(1f, 1f, 1f, 0.2f));
                devAddGenderFemaleBtn.style.borderBottomColor = devAddGenderFemaleBtn.style.borderTopColor;
                devAddGenderFemaleBtn.style.borderLeftColor = devAddGenderFemaleBtn.style.borderTopColor;
                devAddGenderFemaleBtn.style.borderRightColor = devAddGenderFemaleBtn.style.borderTopColor;
            }
        }

        private void HandleDevAddSubmit()
        {
            if (devAddName == null || devAddInsta == null) return;

            string name = devAddName.value.Trim();
            string insta = devAddInsta.value.Trim();
            string bio = devAddBio != null ? devAddBio.value.Trim() : "";

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(insta))
            {
                ShowDevAddStatus("⚠ 이름과 인스타 아이디는 필수입니다.", new Color(1f, 0.4f, 0.2f));
                return;
            }

            if (ClawMachine.Mechanics.FirebaseRESTService.Instance == null)
            {
                ShowDevAddStatus("⚠ Firebase 연결 없음", new Color(1f, 0.4f, 0.2f));
                return;
            }

            devAddSubmitBtn.SetEnabled(false);
            devAddSubmitBtn.text = "...";

            StartCoroutine(ClawMachine.Mechanics.FirebaseRESTService.Instance.RegisterPlayer(
                name, insta, bio, devAddSelectedGender, 0, success =>
                {
                    if (success)
                    {
                        ShowDevAddStatus($"✅ '{name}' ({devAddSelectedGender}) 등록 완료!", new Color(0f, 1f, 0.5f));
                        devAddName.value = "";
                        devAddInsta.value = "";
                        devAddBio.value = "";
                        RefreshDbView(); // 목록 갱신
                    }
                    else
                    {
                        ShowDevAddStatus("❌ 등록 실패. Firebase 오류를 확인하세요.", new Color(1f, 0.3f, 0.3f));
                    }
                    devAddSubmitBtn.SetEnabled(true);
                    devAddSubmitBtn.text = "등록";
                }));
        }

        private void ShowDevAddStatus(string msg, Color color)
        {
            if (devAddStatusLabel == null) return;
            devAddStatusLabel.text = msg;
            devAddStatusLabel.style.color = new StyleColor(color);
            devAddStatusLabel.style.display = DisplayStyle.Flex;
        }

        private void CloseDbView()
        {
            HideOverlay(devDbOverlay);
            ShowOverlay(devModeOverlay);
        }

        private void RefreshDbView()
        {
            if (devDbScrollView == null || ClawMachine.Mechanics.FirebaseRESTService.Instance == null) return;
            
            devDbScrollView.Clear();
            var loadingLabel = new Label("데이터를 불러오는 중...");
            loadingLabel.style.color = Color.white;
            devDbScrollView.Add(loadingLabel);

            StartCoroutine(ClawMachine.Mechanics.FirebaseRESTService.Instance.GetAllParticipants(dataList => {
                cachedDbData = dataList ?? new System.Collections.Generic.List<ClawMachine.Mechanics.ParticipantData>();
                string query = devDbSearchInput != null ? devDbSearchInput.value : "";
                FilterDbView(query);
            }));
        }

        private void FilterDbView(string query)
        {
            if (devDbScrollView == null) return;
            devDbScrollView.Clear();

            var filteredData = cachedDbData;
            if (!string.IsNullOrEmpty(query))
            {
                string lowerQuery = query.ToLower();
                filteredData = cachedDbData.FindAll(d => d.name != null && d.name.ToLower().Contains(lowerQuery));
            }

            if (filteredData == null || filteredData.Count == 0)
            {
                var emptyLabel = new Label(string.IsNullOrEmpty(query) ? "등록된 참가자가 없습니다." : "검색 결과가 없습니다.");
                emptyLabel.style.color = Color.white;
                devDbScrollView.Add(emptyLabel);
                return;
            }

            foreach (var data in filteredData)
            {
                devDbScrollView.Add(CreateDbRow(data));
            }
        }

        private VisualElement CreateDbRow(ClawMachine.Mechanics.ParticipantData data)
        {
            var row = new VisualElement();
            row.AddToClassList("db-row");

            var nameInput = new TextField() { value = data.name };
            nameInput.AddToClassList("db-input");
            nameInput.style.width = 60;

            var instaInput = new TextField() { value = data.insta };
            instaInput.AddToClassList("db-input");
            instaInput.style.width = 120;

            var genderInput = new TextField() { value = data.gender };
            genderInput.AddToClassList("db-input");
            genderInput.style.width = 40;

            var bioInput = new TextField() { value = data.bio };
            bioInput.AddToClassList("db-input");
            bioInput.style.width = 150;

            var isPickedToggle = new Toggle("뽑힘") { value = data.isPicked };
            isPickedToggle.AddToClassList("db-toggle");
            isPickedToggle.style.color = Color.white;

            var saveBtn = new Button() { text = "저장" };
            saveBtn.AddToClassList("db-btn");
            saveBtn.style.backgroundColor = new Color(0, 0.5f, 0);

            var deleteBtn = new Button() { text = "삭제" };
            deleteBtn.AddToClassList("db-btn");
            deleteBtn.style.backgroundColor = new Color(0.8f, 0, 0);

            saveBtn.clicked += () => {
                saveBtn.text = "...";
                saveBtn.SetEnabled(false);
                var newData = new ClawMachine.Mechanics.ParticipantData
                {
                    documentId = data.documentId,
                    name = nameInput.value,
                    insta = instaInput.value,
                    gender = genderInput.value,
                    bio = bioInput.value,
                    isPicked = isPickedToggle.value,
                    attempts = data.attempts
                };
                StartCoroutine(ClawMachine.Mechanics.FirebaseRESTService.Instance.UpdateParticipantFullData(newData, success => {
                    saveBtn.text = success ? "완료!" : "실패";
                    saveBtn.SetEnabled(true);
                }));
            };

            deleteBtn.clicked += () => {
                deleteBtn.text = "...";
                deleteBtn.SetEnabled(false);
                StartCoroutine(ClawMachine.Mechanics.FirebaseRESTService.Instance.DeleteParticipant(data.documentId, success => {
                    if (success)
                    {
                        row.RemoveFromHierarchy();
                    }
                    else
                    {
                        deleteBtn.text = "실패";
                        deleteBtn.SetEnabled(true);
                    }
                }));
            };

            row.Add(nameInput);
            row.Add(instaInput);
            row.Add(genderInput);
            row.Add(bioInput);
            row.Add(isPickedToggle);
            row.Add(saveBtn);
            row.Add(deleteBtn);

            return row;
        }

        public bool IsUserTyping()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return false;
            var focused = uiDocument.rootVisualElement.focusController.focusedElement;
            if (focused != null)
            {
                var type = focused.GetType();
                if (type.Name.Contains("TextField") || type.Name.Contains("TextInput") || focused is TextField)
                {
                    return true;
                }
            }
            return false;
        }

        public enum NavGroup
        {
            None,
            BottomBar,
            Success,
            QuitConfirm,
            Fail
        }

        private NavGroup currentNavGroup = NavGroup.None;
        private Button[] currentButtons = null;
        private int selectedIndex = 0;
        private bool isJoystickDpadPressed = false;

        private void SetNavigationGroup(NavGroup group, Button[] buttons)
        {
            // Clean up old focus
            if (currentButtons != null)
            {
                foreach (var btn in currentButtons)
                {
                    if (btn != null) btn.RemoveFromClassList("arcade-focus");
                }
            }

            currentNavGroup = group;
            currentButtons = buttons;
            selectedIndex = 0;

            // Set new focus
            if (currentButtons != null && currentButtons.Length > 0 && currentButtons[0] != null)
            {
                currentButtons[0].AddToClassList("arcade-focus");
            }
        }

        private void ChangeSelection(int direction)
        {
            if (currentButtons == null || currentButtons.Length <= 1) return;

            // Remove focus from old
            if (selectedIndex >= 0 && selectedIndex < currentButtons.Length && currentButtons[selectedIndex] != null)
            {
                currentButtons[selectedIndex].RemoveFromClassList("arcade-focus");
            }

            // Calculate new index
            selectedIndex = (selectedIndex + direction + currentButtons.Length) % currentButtons.Length;

            // Add focus to new
            if (selectedIndex >= 0 && selectedIndex < currentButtons.Length && currentButtons[selectedIndex] != null)
            {
                currentButtons[selectedIndex].AddToClassList("arcade-focus");
                
                // Play subtle selection tick SFX
                if (buttonClickSound != null && ClawMachine.Audio.SoundManager.Instance != null)
                {
                    ClawMachine.Audio.SoundManager.Instance.PlaySFX(buttonClickSound);
                }
            }
        }

        private void HandleJoystickActionPressed()
        {
            if (currentNavGroup != NavGroup.None && currentButtons != null)
            {
                if (selectedIndex >= 0 && selectedIndex < currentButtons.Length && currentButtons[selectedIndex] != null)
                {
                    var button = currentButtons[selectedIndex];
                    if (button.enabledSelf && button.resolvedStyle.display != DisplayStyle.None)
                    {
                        TriggerButtonAction(button);
                    }
                }
            }
        }

        private void TriggerButtonAction(Button button)
        {
            if (button == null) return;
            
            // Play click sound
            if (buttonClickSound != null && ClawMachine.Audio.SoundManager.Instance != null) 
                ClawMachine.Audio.SoundManager.Instance.PlaySFX(buttonClickSound);

            if (button == retryButton)
            {
                isRetryingFromSuccess = false;
                ExecuteRetryAction();
            }
            else if (button == quitButton)
            {
                ResetToRegistration();
            }
            else if (button == successRetryBtn)
            {
                ExecuteSuccessRetryClick();
            }
            else if (button == successExitBtn)
            {
                HandleSuccessExitClick();
            }
            else if (button == quitConfirmNoBtn)
            {
                HandleQuitConfirmNoClick();
            }
            else if (button == quitConfirmYesBtn)
            {
                HandleQuitConfirmYesClick();
            }
            else if (button == failCloseBtn)
            {
                ResetToRegistration();
            }
            else if (button == failRetryAgainBtn)
            {
                ExecuteFailRetryAgainClick();
            }
            else if (button == failRetryCloseBtn)
            {
                HideOverlay(failRetryOverlay);
                if (isRetryingFromSuccess)
                {
                    ShowOverlay(successOverlay);
                }
                else
                {
                    ShowRetryButton(true);
                }
            }
            else if (button == btnCoinPack1)
            {
                SelectCoinPackage(1, btnCoinPack1, new Button[] { btnCoinPack1, btnCoinPack2, btnCoinPack3, btnCoinPack4 });
            }
            else if (button == btnCoinPack2)
            {
                SelectCoinPackage(3, btnCoinPack2, new Button[] { btnCoinPack1, btnCoinPack2, btnCoinPack3, btnCoinPack4 });
            }
            else if (button == btnCoinPack3)
            {
                SelectCoinPackage(6, btnCoinPack3, new Button[] { btnCoinPack1, btnCoinPack2, btnCoinPack3, btnCoinPack4 });
            }
            else if (button == btnCoinPack4)
            {
                SelectCoinPackage(13, btnCoinPack4, new Button[] { btnCoinPack1, btnCoinPack2, btnCoinPack3, btnCoinPack4 });
            }
            else if (button == regCoinPack1)
            {
                SelectCoinPackage(1, regCoinPack1, new Button[] { regCoinPack1, regCoinPack2, regCoinPack3, regCoinPack4 });
            }
            else if (button == regCoinPack2)
            {
                SelectCoinPackage(3, regCoinPack2, new Button[] { regCoinPack1, regCoinPack2, regCoinPack3, regCoinPack4 });
            }
            else if (button == regCoinPack3)
            {
                SelectCoinPackage(6, regCoinPack3, new Button[] { regCoinPack1, regCoinPack2, regCoinPack3, regCoinPack4 });
            }
            else if (button == regCoinPack4)
            {
                SelectCoinPackage(13, regCoinPack4, new Button[] { regCoinPack1, regCoinPack2, regCoinPack3, regCoinPack4 });
            }
            else if (button == btnCoinStart)
            {
                ExecuteCoinStartClick();
            }
        }

        // ================= COIN SYSTEM HELPERS =================

        private void SelectCoinPackage(int amount, Button clickedButton, Button[] siblingButtons)
        {
            // Play button sound
            if (buttonClickSound != null && ClawMachine.Audio.SoundManager.Instance != null) 
                ClawMachine.Audio.SoundManager.Instance.PlaySFX(buttonClickSound);

            pendingCoinsToCharge = amount;

            // Highlight selected button
            foreach (var btn in siblingButtons)
            {
                if (btn != null)
                {
                    btn.RemoveFromClassList("gender-button-male-selected");
                }
            }

            if (clickedButton != null)
            {
                clickedButton.AddToClassList("gender-button-male-selected");
            }

            UpdateCoinUI();
            Debug.Log($"[코인 선택] {amount}개 패키지 선택됨. 결제 시작 시 반영 대기중.");
        }

        private void ClearStagedCoins()
        {
            pendingCoinsToCharge = 0;

            // Clear highlights from all pack buttons
            Button[] allPackButtons = { btnCoinPack1, btnCoinPack2, btnCoinPack3, btnCoinPack4, regCoinPack1, regCoinPack2, regCoinPack3, regCoinPack4 };
            foreach (var btn in allPackButtons)
            {
                if (btn != null)
                {
                    btn.RemoveFromClassList("gender-button-male-selected");
                }
            }

            UpdateCoinUI();
        }

        public void UpdateCoinUI()
        {
            if (coinCountText != null)
            {
                coinCountText.text = $"{currentCoins}개";
            }
            if (popupCoinCountText != null)
            {
                popupCoinCountText.text = $"보유 코인: {currentCoins}개";
            }
            if (registerCoinCountText != null)
            {
                registerCoinCountText.text = $"보유 코인: {currentCoins}개";
            }

            if (btnCoinStart != null)
            {
                btnCoinStart.style.display = (currentCoins > 0 || pendingCoinsToCharge > 0) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (failRetryAgainBtn != null)
            {
                failRetryAgainBtn.text = (currentCoins > 0 || pendingCoinsToCharge > 0) ? "🎯 한 번만 더! (1코인 차감)" : "🎯 한 번만 더! (500원)";
            }

            // 충전 팝업이 활성화되어 있는 경우 네비게이션 그룹 동적 갱신
            if (failRetryOverlay != null && failRetryOverlay.resolvedStyle.display == DisplayStyle.Flex)
            {
                var buttonsList = new System.Collections.Generic.List<Button>();
                if (btnCoinPack1 != null) buttonsList.Add(btnCoinPack1);
                if (btnCoinPack2 != null) buttonsList.Add(btnCoinPack2);
                if (btnCoinPack3 != null) buttonsList.Add(btnCoinPack3);
                if (btnCoinPack4 != null) buttonsList.Add(btnCoinPack4);
                
                if (btnCoinStart != null && (currentCoins > 0 || pendingCoinsToCharge > 0))
                {
                    buttonsList.Add(btnCoinStart);
                }
                
                if (failRetryCloseBtn != null) buttonsList.Add(failRetryCloseBtn);
                
                SetNavigationGroup(NavGroup.Fail, buttonsList.ToArray());
            }
        }

        public bool HasCoins()
        {
            return currentCoins > 0;
        }

        public void DeductCoinDirectly()
        {
            if (currentCoins > 0)
            {
                currentCoins--;
                PlayerPrefs.SetInt("LoveCatcher_Coins", currentCoins);
                PlayerPrefs.Save();
                UpdateCoinUI();
                
                // Record play session
                RecordPlaySession(0);

                HideAllOverlays();
                
                if (ClawMachine.Mechanics.GameFlowManager.Instance != null)
                {
                    ClawMachine.Mechanics.GameFlowManager.Instance.RequestRetry();
                }
            }
        }

        private void AddCoins(int amount)
        {
            currentCoins += amount;
            PlayerPrefs.SetInt("LoveCatcher_Coins", currentCoins);
            PlayerPrefs.Save();
            UpdateCoinUI();
            Debug.Log($"[코인 충전] {amount}개 코인 충전 완료. 현재 코인: {currentCoins}개");
        }

        private void ExecuteRetryAction()
        {
            if (currentCoins > 0)
            {
                currentCoins--;
                PlayerPrefs.SetInt("LoveCatcher_Coins", currentCoins);
                PlayerPrefs.Save();
                UpdateCoinUI();
                
                // Record play session
                RecordPlaySession(0);

                HideAllOverlays();
                if (isRetryingFromSuccess)
                {
                    exitConfirmCount = 0;
                    OnContinueSession?.Invoke();
                }
                else
                {
                    if (ClawMachine.Mechanics.GameFlowManager.Instance != null)
                    {
                        ClawMachine.Mechanics.GameFlowManager.Instance.RequestRetry();
                    }
                }
            }
            else
            {
                HideAllOverlays();
                ShowOverlay(failRetryOverlay);
            }
        }

        private void ExecuteSuccessRetryClick()
        {
            if (currentCoins > 0)
            {
                currentCoins--;
                PlayerPrefs.SetInt("LoveCatcher_Coins", currentCoins);
                PlayerPrefs.Save();
                UpdateCoinUI();
                
                // Record play session
                RecordPlaySession(0);

                exitConfirmCount = 0;
                OnContinueSession?.Invoke();
            }
            else
            {
                isRetryingFromSuccess = true;
                HideAllOverlays();
                ShowOverlay(failRetryOverlay);
            }
        }

        private void ExecuteFailRetryAgainClick()
        {
            if (currentCoins > 0)
            {
                currentCoins--;
                PlayerPrefs.SetInt("LoveCatcher_Coins", currentCoins);
                PlayerPrefs.Save();
                UpdateCoinUI();
                
                // Record play session
                RecordPlaySession(0);

                HideAllOverlays();
                if (ClawMachine.Mechanics.GameFlowManager.Instance != null)
                {
                    ClawMachine.Mechanics.GameFlowManager.Instance.RequestRetry();
                }
            }
            else
            {
                isRetryingFromSuccess = false;
                HideAllOverlays();
                ShowOverlay(failRetryOverlay);
            }
        }

        private void ExecuteCoinStartClick()
        {
            int totalAvailable = currentCoins + pendingCoinsToCharge;
            if (totalAvailable > 0)
            {
                int revenue = GetRevenueFromStagedCoins(pendingCoinsToCharge);
                currentCoins = totalAvailable - 1;
                pendingCoinsToCharge = 0;

                // Clear highlights from all pack buttons
                Button[] allPackButtons = { btnCoinPack1, btnCoinPack2, btnCoinPack3, btnCoinPack4, regCoinPack1, regCoinPack2, regCoinPack3, regCoinPack4 };
                foreach (var btn in allPackButtons)
                {
                    if (btn != null)
                    {
                        btn.RemoveFromClassList("gender-button-male-selected");
                    }
                }

                PlayerPrefs.SetInt("LoveCatcher_Coins", currentCoins);
                PlayerPrefs.Save();
                UpdateCoinUI();

                // Record play session
                RecordPlaySession(revenue);

                HideAllOverlays();
                
                if (isRetryingFromSuccess)
                {
                    OnContinueSession?.Invoke();
                }
                else
                {
                    if (ClawMachine.Mechanics.GameFlowManager.Instance != null)
                    {
                        ClawMachine.Mechanics.GameFlowManager.Instance.RequestRetry();
                    }
                }
            }
        }

        // ================= PUBLIC UI CONTROLLERS =================

        public void SetStats(int instaCount, int dollCount, float winChance)
        {
            if (instaCountText != null) instaCountText.text = $"{instaCount}개";
            if (dollCountText != null) dollCountText.text = $"{dollCount}개";
            if (winChanceText != null) winChanceText.text = $"{winChance:F1}%";
        }

        public void UpdateRegisterPoolCount(int maleCount, int femaleCount)
        {
            if (registerMaleCountText != null) registerMaleCountText.text = $"남성 아이디: {maleCount}개";
            if (registerFemaleCountText != null) registerFemaleCountText.text = $"여성 아이디: {femaleCount}개";
        }

        public void SetTimer(float seconds)
        {
            if (timerText != null)
            {
                timerText.text = $"{Mathf.CeilToInt(seconds)}s";
            }
        }

        public void SetAttempts(int currentAttempts, int maxPity = 5)
        {
            if (attemptCountText != null) attemptCountText.text = $"{currentAttempts}회";
            
            if (pityProgressBar != null)
            {
                float percent = (float)currentAttempts / maxPity * 100f;
                pityProgressBar.style.width = new StyleLength(Length.Percent(Mathf.Min(percent, 100f)));
            }

            if (pityStatusText != null)
            {
                pityStatusText.text = $"{maxPity}회차에 MAX 파워 발동! ({currentAttempts}/{maxPity})";
            }

            if (pityMaxBanner != null)
            {
                bool isMax = currentAttempts >= maxPity;
                pityMaxBanner.style.display = isMax ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void ShowRetryButton(bool show)
        {
            if (retryButton != null)
            {
                retryButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (quitButton != null)
            {
                quitButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (show)
            {
                SetNavigationGroup(NavGroup.BottomBar, new Button[] { retryButton, quitButton });
            }
            else
            {
                if (currentNavGroup == NavGroup.BottomBar)
                {
                    SetNavigationGroup(NavGroup.None, null);
                }
                HideOverlay(failRetryOverlay);
            }
        }

        public void ShowRewardPopup(ClawMachine.Mechanics.RewardType reward, string name, string gender, string insta, string bio)
        {
            HideAllOverlays();
            
            // 끈질긴 그만두기 버튼 카운트 초기화
            exitConfirmCount = 0;

            var successCard = rootVisualElement().Q<VisualElement>("SuccessCard");
            if (gender == "여")
            {
                successCard.AddToClassList("popup-card-female");
                matchedName.style.color = new StyleColor(new Color(1f, 0f, 0.5f)); // Pink
            }
            else
            {
                successCard.RemoveFromClassList("popup-card-female");
                matchedName.style.color = new StyleColor(new Color(0f, 0.93f, 1f)); // Cyan
            }

            if (reward == ClawMachine.Mechanics.RewardType.Candy)
            {
                // 뽑기 성공 but 사탕 당첨 (보상이 사탕)
                if (popupFailSound != null && ClawMachine.Audio.SoundManager.Instance != null)
                    ClawMachine.Audio.SoundManager.Instance.PlaySFX(popupFailSound);

                matchedName.text = "사탕 당첨! 🍬";
                matchedInsta.text = "달콤한 위로상";
                matchedBio.text = "“데스크에서 사탕을 받아가세요!”";

                if (successSubTitle != null)
                    successSubTitle.text = "축하합니다! 달콤한 사탕 보상이 당첨되었습니다.";
                if (dollPickupNotice != null)
                    dollPickupNotice.style.display = DisplayStyle.None;
            }
            else if (reward == ClawMachine.Mechanics.RewardType.IdOnly)
            {
                // 아이디만
                if (popupSuccessSound != null && ClawMachine.Audio.SoundManager.Instance != null)
                    ClawMachine.Audio.SoundManager.Instance.PlaySFX(popupSuccessSound);

                matchedName.text = $"[아이디] {name} ({gender})";
                matchedInsta.text = insta;
                matchedBio.text = string.IsNullOrEmpty(bio) ? "“인스타 친구해요!”" : $"“{bio}”";

                if (successSubTitle != null)
                    successSubTitle.text = "축하합니다! 당신과 어울리는 이성의 인스타 카드입니다.";
                if (dollPickupNotice != null)
                    dollPickupNotice.style.display = DisplayStyle.None;
            }
            else if (reward == ClawMachine.Mechanics.RewardType.DollOnly)
            {
                // 실물 인형만 당첨
                if (popupSuccessSound != null && ClawMachine.Audio.SoundManager.Instance != null)
                    ClawMachine.Audio.SoundManager.Instance.PlaySFX(popupSuccessSound);

                matchedName.text = "인형 당첨! 🎁";
                matchedInsta.text = "귀여운 실물 인형";
                matchedBio.text = "“데스크에서 실물 인형을 받아가세요!”";

                if (successSubTitle != null)
                    successSubTitle.text = "축하합니다! 귀여운 실물 인형 보상이 당첨되었습니다.";
                if (dollPickupNotice != null)
                    dollPickupNotice.style.display = DisplayStyle.Flex;
            }
            else
            {
                // 인형 + 인스타
                if (popupSuccessSound != null && ClawMachine.Audio.SoundManager.Instance != null)
                    ClawMachine.Audio.SoundManager.Instance.PlaySFX(popupSuccessSound);

                matchedName.text = $"[인형+아이디] {name} ({gender})";
                matchedInsta.text = insta;
                matchedBio.text = string.IsNullOrEmpty(bio) ? "“인스타 친구해요!”" : $"“{bio}”";

                if (successSubTitle != null)
                    successSubTitle.text = "축하합니다! 당신과 어울리는 이성의 인스타 카드입니다.";
                if (dollPickupNotice != null)
                    dollPickupNotice.style.display = DisplayStyle.Flex;
            }

            if (successRetryBtn != null)
            {
                successRetryBtn.text = currentCoins > 0 ? "다시하기 (코인 차감)" : "또 뽑기 (결제 필요)";
            }

            ShowOverlay(successOverlay);
        }

        private void HandleSuccessRetryClick()
        {
            exitConfirmCount = 0;
            OnContinueSession?.Invoke();
        }

        private void HandleSuccessExitClick()
        {
            exitConfirmCount = 1;
            ShowQuitConfirmPopup(1);
        }

        private void ShowQuitConfirmPopup(int stage)
        {
            HideAllOverlays();
            
            if (quitConfirmTitle != null)
            {
                if (stage == 1)
                {
                    quitConfirmTitle.text = "진짜 그만둘래요? 🥺";
                }
                else if (stage == 2)
                {
                    quitConfirmTitle.text = "진짜진짜 갈 거예요? 💔";
                }
                else if (stage == 3)
                {
                    quitConfirmTitle.text = "한 판만 더 해요😭";
                }
            }

            ShowOverlay(quitConfirmOverlay);
        }

        private void HandleQuitConfirmNoClick()
        {
            exitConfirmCount = 0;
            HideOverlay(quitConfirmOverlay);
            OnContinueSession?.Invoke();
        }

        private void HandleQuitConfirmYesClick()
        {
            exitConfirmCount++;
            if (exitConfirmCount <= 3)
            {
                ShowQuitConfirmPopup(exitConfirmCount);
            }
            else
            {
                exitConfirmCount = 0;
                ResetToRegistration();
            }
        }

        public void HideAllOverlaysPublic()
        {
            HideAllOverlays();
        }

        public void ShowFailPopup()
        {
            HideAllOverlays();

            // 랜덤 귀여운 멘트 & 이모지 선택
            if (failCuteMsg != null)
                failCuteMsg.text = FailCuteMsgs[UnityEngine.Random.Range(0, FailCuteMsgs.Length)];
            if (failEmoji != null)
                failEmoji.text = FailEmojis[UnityEngine.Random.Range(0, FailEmojis.Length)];

            if (popupFailSound != null && ClawMachine.Audio.SoundManager.Instance != null)
            {
                ClawMachine.Audio.SoundManager.Instance.PlaySFX(popupFailSound);
            }

            ShowOverlay(failOverlay);
        }

        // ================= HELPERS =================

        private VisualElement rootVisualElement()
        {
            return uiDocument.rootVisualElement;
        }

        private void HideAllOverlays()
        {
            SetNavigationGroup(NavGroup.None, null);

            HideOverlay(registerOverlay);
            HideOverlay(successOverlay);
            HideOverlay(failOverlay);
            HideOverlay(quitConfirmOverlay);
            HideOverlay(devModeOverlay);
            HideOverlay(devDbOverlay);
            HideOverlay(failRetryOverlay);
        }

        private void ShowOverlay(VisualElement overlay)
        {
            if (overlay != null)
            {
                overlay.style.display = DisplayStyle.Flex;

                if (overlay == devModeOverlay)
                {
                    if (devTotalDollsInput != null && ClawMachine.Mechanics.GameFlowManager.Instance != null)
                    {
                        devTotalDollsInput.value = ClawMachine.Mechanics.GameFlowManager.Instance.totalDolls.ToString();
                        if (devSaveDollsBtn != null) devSaveDollsBtn.text = "저장";
                    }
                    if (devCoinsInput != null)
                    {
                        devCoinsInput.value = currentCoins.ToString();
                        if (devSaveCoinsBtn != null) devSaveCoinsBtn.text = "저장";
                    }

                    // Populate stats fields immediately when opening DevModeOverlay
                    RefreshDevModeStats();
                }

                // UI Navigation Group Setup
                if (overlay == successOverlay)
                {
                    SetNavigationGroup(NavGroup.Success, new Button[] { successRetryBtn, successExitBtn });
                }
                else if (overlay == quitConfirmOverlay)
                {
                    SetNavigationGroup(NavGroup.QuitConfirm, new Button[] { quitConfirmNoBtn, quitConfirmYesBtn });
                }
                else if (overlay == failOverlay)
                {
                    SetNavigationGroup(NavGroup.Fail, new Button[] { failRetryAgainBtn, failCloseBtn });
                }
                else if (overlay == failRetryOverlay)
                {
                    // 보유 코인에 맞춰 패키지 버튼 및 도전 시작 버튼을 동적으로 아케이드 조이스틱 내비게이션 그룹에 배정
                    var buttonsList = new System.Collections.Generic.List<Button>();
                    if (btnCoinPack1 != null) buttonsList.Add(btnCoinPack1);
                    if (btnCoinPack2 != null) buttonsList.Add(btnCoinPack2);
                    if (btnCoinPack3 != null) buttonsList.Add(btnCoinPack3);
                    if (btnCoinPack4 != null) buttonsList.Add(btnCoinPack4);
                    
                    if (btnCoinStart != null && currentCoins > 0)
                    {
                        buttonsList.Add(btnCoinStart);
                    }
                    
                    if (failRetryCloseBtn != null) buttonsList.Add(failRetryCloseBtn);
                    
                    SetNavigationGroup(NavGroup.Fail, buttonsList.ToArray());
                }
                else if (overlay == registerOverlay)
                {
                    SetNavigationGroup(NavGroup.None, null);
                }
            }
        }

        private void RefreshDevProbSliders()
        {
            if (ClawMachine.Mechanics.GameFlowManager.Instance == null) return;
            var gm = ClawMachine.Mechanics.GameFlowManager.Instance;
            
            if (devUseGenderToggle != null) {
                devUseGenderToggle.value = gm.useGenderSpecificProbability;
            }
            if (mainNoInstaToggle != null) {
                mainNoInstaToggle.value = gm.noInstaMode;
            }

            float pDollInsta = 0, pId = 0, pDoll = 0, pCandy = 0;
            switch(currentDevProbMode) {
                case DevProbMode.Base:
                    pDollInsta = gm.probDollAndInsta; pId = gm.probIdOnly; pDoll = gm.probDollOnly; pCandy = gm.probCandy; break;
                case DevProbMode.Male:
                    pDollInsta = gm.maleProbDollAndInsta; pId = gm.maleProbIdOnly; pDoll = gm.maleProbDollOnly; pCandy = gm.maleProbCandy; break;
                case DevProbMode.Female:
                    pDollInsta = gm.femaleProbDollAndInsta; pId = gm.femaleProbIdOnly; pDoll = gm.femaleProbDollOnly; pCandy = gm.femaleProbCandy; break;
            }

            if (devProbDollInstaSlider != null) devProbDollInstaSlider.value = pDollInsta;
            if (devProbIdOnlySlider != null) devProbIdOnlySlider.value = pId;
            if (devProbDollOnlySlider != null) devProbDollOnlySlider.value = pDoll;
            if (devProbCandySlider != null) devProbCandySlider.value = pCandy;
        }

        private void HideOverlay(VisualElement overlay)
        {
            if (overlay != null) overlay.style.display = DisplayStyle.None;
        }

        // ================= STATS HELPERS & ADJUSTMENTS =================

        private void RefreshDevModeStats()
        {
            if (ClawMachine.Mechanics.FirebaseRESTService.Instance == null) return;

            StartCoroutine(ClawMachine.Mechanics.FirebaseRESTService.Instance.GetGameStats(stats => {
                if (devStatPlays != null) devStatPlays.text = $"총 플레이 횟수: {stats.totalPlays}회";
                if (devStatSuccesses != null) devStatSuccesses.text = $"총 성공(뽑기) 횟수: {stats.totalSuccesses}회";
                if (devStatRevenue != null) devStatRevenue.text = $"총 누적 수입: {stats.totalRevenue:N0}원";

                if (devTotalRevenueInput != null && !IsUserTyping()) devTotalRevenueInput.value = stats.totalRevenue.ToString();
                if (devTotalPlaysInput != null && !IsUserTyping()) devTotalPlaysInput.value = stats.totalPlays.ToString();
                if (devTotalSuccessesInput != null && !IsUserTyping()) devTotalSuccessesInput.value = stats.totalSuccesses.ToString();

                // Firebase의 실제 Participants 문서 개수를 조회하여 '등록된 총 인스타 ID 수'로 갱신
                StartCoroutine(ClawMachine.Mechanics.FirebaseRESTService.Instance.GetAllParticipants(list => {
                    int actualCount = (list != null) ? list.Count : stats.totalRegistrations;
                    if (devStatRegistrations != null) devStatRegistrations.text = $"등록된 총 인스타 ID 수: {actualCount}명";
                    if (devTotalRegistrationsInput != null && !IsUserTyping()) devTotalRegistrationsInput.value = actualCount.ToString();
                }));
            }));
        }

        private void RestoreDevResetStatsBtnText()
        {
            if (devResetStatsBtn != null)
            {
                devResetStatsBtn.text = "📈 통계 데이터 전체 초기화 (0으로 리셋) 📈";
            }
        }

        private IEnumerator SaveSingleStatCoroutine(string fieldName, int value, Action<bool> callback)
        {
            if (ClawMachine.Mechanics.FirebaseRESTService.Instance == null)
            {
                callback?.Invoke(false);
                yield break;
            }

            ClawMachine.Mechanics.GameStatsData currentStats = new ClawMachine.Mechanics.GameStatsData();
            bool fetchDone = false;
            yield return ClawMachine.Mechanics.FirebaseRESTService.Instance.GetGameStats(stats => {
                currentStats = stats;
                fetchDone = true;
            });
            yield return new WaitUntil(() => fetchDone);

            if (fieldName == "totalRevenue") currentStats.totalRevenue = value;
            else if (fieldName == "totalRegistrations") currentStats.totalRegistrations = value;
            else if (fieldName == "totalPlays") currentStats.totalPlays = value;
            else if (fieldName == "totalSuccesses") currentStats.totalSuccesses = value;

            yield return ClawMachine.Mechanics.FirebaseRESTService.Instance.UpdateGameStats(
                currentStats, 
                new System.Collections.Generic.List<string> { fieldName }, 
                callback
            );
        }

        private int GetRevenueFromStagedCoins(int coins)
        {
            switch (coins)
            {
                case 1: return 500;
                case 3: return 1500;
                case 6: return 2500;
                case 13: return 5000;
                default: return 0;
            }
        }

        private void RecordPlaySession(int revenue)
        {
            if (ClawMachine.Mechanics.FirebaseRESTService.Instance != null)
            {
                ClawMachine.Mechanics.FirebaseRESTService.Instance.IncrementPlayCountAndRevenue(revenue);
            }
        }
    }
}
