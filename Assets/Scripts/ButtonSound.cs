using UnityEngine;
using UnityEngine.UI;

public class ButtonSound : MonoBehaviour
{
    [SerializeField] private Button _targetButton;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _clickSound;

    private void Awake()
    {
        if (_targetButton != null && _audioSource != null && _clickSound != null)
        {
            _targetButton.onClick.AddListener(PlayClickSound);
        }
    }
    public void Update()
    {
        _audioSource.volume = AudioManager.Instance.clickVolume;
    }
    public void PlayClickSound()
    {
        _audioSource.PlayOneShot(_clickSound);
    }
}