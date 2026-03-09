using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    // 单例实例
    public static AudioManager Instance;

    // BGM相关配置
    [Header("BGM设置")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip defaultBGM;
    [Range(0f, 1f)] public float bgmVolume = 0.8f;

    // 音效相关配置
    [Header("按钮音效设置")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickClip;
    [Range(0f, 1f)] public float clickVolume = 0.6f;

    // 音量滑块标签（用于场景加载时匹配）
    [Header("音量滑块匹配")]
    [Tooltip("BGM音量滑块的标签")]
    public string bgmSliderTag = "BGMVolumeSlider";
    [Tooltip("点击音效滑块的标签")]
    public string clickSliderTag = "ClickVolumeSlider";

    // 已绑定音效的按钮列表（防止重复绑定）
    private List<Button> boundButtons = new List<Button>();

    private void Awake()
    {
        // 单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化音频源
            InitAudioSources();

            // 播放默认BGM
            if (defaultBGM != null)
                PlayBGM(defaultBGM);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 初始化BGM和音效的音频源
    /// </summary>
    private void InitAudioSources()
    {
        // 初始化BGM音频源
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;

        // 初始化音效音频源
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.loop = false;
        sfxSource.volume = clickVolume;
        sfxSource.playOnAwake = false;
        sfxSource.priority = 64;
        sfxSource.clip = clickClip;
    }

    /// <summary>
    /// 场景加载完成回调
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 清空已绑定的按钮列表，重新绑定当前场景的按钮音效
        boundButtons.Clear();
        BindButtonsClickSound(scene);

        // 同步当前场景的音量滑块值
        SyncVolumeSliders();
    }

    /// <summary>
    /// 绑定当前场景所有按钮的点击音效
    /// </summary>
    private void BindButtonsClickSound(Scene targetScene)
    {
        Button[] allButtons = FindObjectsOfType<Button>(includeInactive: true);
        if (allButtons.Length == 0)
        {
            Debug.LogWarning($"当前场景「{targetScene.name}」未找到可绑定的按钮");
            return;
        }

        int bindCount = 0;
        foreach (Button btn in allButtons)
        {
            if (boundButtons.Contains(btn)) continue;

            btn.onClick.RemoveListener(PlayClickSound);
            btn.onClick.AddListener(PlayClickSound);

            boundButtons.Add(btn);
            bindCount++;
        }
    }

    /// <summary>
    /// 同步当前场景的音量滑块值（与管理器的音量保持一致）
    /// </summary>
    private void SyncVolumeSliders()
    {
        // 查找并绑定BGM音量滑块
        Slider bgmSlider = FindSliderByTag(bgmSliderTag);
        if (bgmSlider != null)
        {
            bgmSlider.value = bgmVolume;
            bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        // 查找并绑定点击音效音量滑块
        Slider clickSlider = FindSliderByTag(clickSliderTag);
        if (clickSlider != null)
        {
            clickSlider.value = clickVolume;
            clickSlider.onValueChanged.RemoveListener(SetClickVolume);
            clickSlider.onValueChanged.AddListener(SetClickVolume);
        }
    }

    /// <summary>
    /// 根据标签查找滑块
    /// </summary>
    private Slider FindSliderByTag(string tag)
    {
        GameObject[] sliderObjs = GameObject.FindGameObjectsWithTag(tag);
        if (sliderObjs.Length > 0)
            return sliderObjs[0].GetComponent<Slider>();

        Debug.LogWarning($"未找到标签为「{tag}」的音量滑块");
        return null;
    }

    /// <summary>
    /// 播放BGM
    /// </summary>
    public void PlayBGM(AudioClip bgmClip)
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("播放BGM失败：传入的音频剪辑为空");
            return;
        }

        if (bgmSource.clip == bgmClip && bgmSource.isPlaying)
            return;

        bgmSource.clip = bgmClip;
        bgmSource.Play();
    }

    /// <summary>
    /// 暂停BGM
    /// </summary>
    public void PauseBGM()
    {
        if (bgmSource.isPlaying)
            bgmSource.Pause();
    }

    /// <summary>
    /// 恢复BGM播放
    /// </summary>
    public void ResumeBGM()
    {
        if (!bgmSource.isPlaying && bgmSource.clip != null)
            bgmSource.UnPause();
    }

    /// <summary>
    /// 设置BGM音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }

    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    public void PlayClickSound()
    {
        if (clickClip == null)
        {
            Debug.LogWarning("点击音频没有放");
            return;
        }

        sfxSource.PlayOneShot(clickClip, clickVolume);
    }

    /// <summary>
    /// 设置点击音效音量
    /// </summary>
    public void SetClickVolume(float volume)
    {
        clickVolume = Mathf.Clamp01(volume);
        sfxSource.volume = clickVolume;
    }

    /// <summary>
    /// 获取BGM播放进度（秒）
    /// </summary>
    public float GetBGMProgress()
    {
        return bgmSource.time;
    }
}