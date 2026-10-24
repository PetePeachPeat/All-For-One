using System.Collections.Generic;
using UnityEngine;

namespace SecurityCenter.MainMenu
{
    [System.Serializable]
    public class NamedAudioClip
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float defaultVolume = 0.8f;
        public bool isGhostChannel;
    }

    public class MenuAudioManager : MonoBehaviour
    {
        public static MenuAudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource humLoopSource;
        [SerializeField] private AudioSource musicLoopSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ghostSource;

        [Header("Clips")]
        [SerializeField] private List<NamedAudioClip> clips = new List<NamedAudioClip>();

        private readonly Dictionary<string, NamedAudioClip> _clipMap = new Dictionary<string, NamedAudioClip>();

        private void Awake()
        {
            Instance = this;
            RebuildMap();
        }

        public void RebuildMap()
        {
            _clipMap.Clear();
            foreach (var c in clips)
            {
                if (c != null && !string.IsNullOrEmpty(c.key) && c.clip != null)
                    _clipMap[c.key] = c;
            }
        }

        private void Start()
        {
            ApplyVolumesFromPrefs();
            if (humLoopSource != null && humLoopSource.clip != null && !humLoopSource.isPlaying)
                humLoopSource.Play();
            if (musicLoopSource != null && musicLoopSource.clip != null && !musicLoopSource.isPlaying)
                musicLoopSource.Play();
        }

        public void ApplyVolumesFromPrefs()
        {
            float master = PlayerPrefs.GetInt("MM_VolMaster", 80) / 100f;
            float music = PlayerPrefs.GetInt("MM_VolMusic", 70) / 100f;
            float sfx = PlayerPrefs.GetInt("MM_VolSFX", 80) / 100f;
            float ghost = PlayerPrefs.GetInt("MM_VolGhost", 90) / 100f;
            bool jumpCap = PlayerPrefs.GetInt("MM_JumpCap", 0) == 1;
            if (jumpCap) ghost = Mathf.Min(ghost, 0.45f);

            if (humLoopSource != null) humLoopSource.volume = master * sfx * 0.32f;
            if (musicLoopSource != null) musicLoopSource.volume = master * music * 0.42f;
            if (sfxSource != null) sfxSource.volume = master * sfx;
            if (ghostSource != null) ghostSource.volume = master * ghost;
        }

        public void Play(string key, float pitchJitter = 0.03f)
        {
            if (_clipMap.Count == 0) RebuildMap();
            if (!_clipMap.TryGetValue(key, out var item) || item.clip == null) return;

            AudioSource target = item.isGhostChannel && ghostSource != null ? ghostSource : sfxSource;
            if (target == null) return;

            target.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            target.PlayOneShot(item.clip, item.defaultVolume);
        }
    }
}
