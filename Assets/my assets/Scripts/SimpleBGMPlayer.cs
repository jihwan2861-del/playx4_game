using UnityEngine;

/// <summary>
/// 메인 카메라나 씬 내 임의의 오브젝트에 붙여 간단히 배경음악(BGM)을 재생하고 루프시키는 스크립트입니다.
/// 허브 씬(Hub_Scene) 등 레벨 컨트롤러가 없는 씬에서 메인 카메라 등에 붙여 유용하게 사용할 수 있습니다.
/// </summary>
public class SimpleBGMPlayer : MonoBehaviour
{
    [Header("🎵 BGM 설정")]
    [Tooltip("이 씬에서 재생할 배경음악 BGM 클립입니다.")]
    public AudioClip bgmClip;

    [Tooltip("배경음악을 무한 루프로 반복 재생할지 여부")]
    public bool loop = true;

    [Range(0f, 1f)]
    [Tooltip("배경음악 볼륨")]
    public float volume = 0.5f;

    private AudioSource audioSource;

    private void Start()
    {
        if (bgmClip != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = bgmClip;
            audioSource.loop = loop;
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.spatialBlend = 0f; // 2D BGM
            audioSource.Play();
            Debug.Log($"🎵 [SimpleBGMPlayer] '{gameObject.name}'에서 배경 음악 '{bgmClip.name}' 재생 시작! (루프 여부: {loop})");
        }
    }
}

