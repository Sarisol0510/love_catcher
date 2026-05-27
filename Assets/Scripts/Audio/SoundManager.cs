using UnityEngine;

namespace ClawMachine.Audio
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("BGM Settings")]
        [Tooltip("여기에 BGM 파일을 넣으면 게임 시작 시 자동으로 재생됩니다.")]
        public AudioClip mainBGM;
        [Range(0f, 1f)] public float bgmVolume = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 컴포넌트가 안 붙어있으면 자동으로 붙여줌
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }
            
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            if (mainBGM != null)
            {
                PlayBGM(mainBGM, bgmVolume);
            }
        }

        /// <summary>
        /// 배경음악(BGM)을 재생합니다.
        /// </summary>
        public void PlayBGM(AudioClip clip, float volume = 0.5f)
        {
            if (clip == null) return;
            bgmSource.clip = clip;
            bgmSource.volume = volume;
            bgmSource.Play();
        }

        /// <summary>
        /// 배경음악(BGM)을 정지합니다.
        /// </summary>
        public void StopBGM()
        {
            if (bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }
        }

        /// <summary>
        /// 효과음(SFX)을 1회 재생합니다. (여러 소리 겹치기 가능)
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1.0f)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
