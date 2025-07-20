using UnityEngine;
using UnityEngine.Events;

// AudioSource 컴포넌트가 자동으로 추가되도록 보장합니다.
[RequireComponent(typeof(AudioSource))] 
public class TouchableButton : MonoBehaviour
{
    [Header("터치 시 재생할 사운드")]
    public AudioClip touchSound; // 인스펙터에서 '딩동' 사운드 파일을 연결

    [Header("터치 시 실행될 이벤트")]
    public UnityEvent onTouch;

    // --- 내부 변수 ---
    private AudioSource audioSource;

    void Start()
    {
        // 이 오브젝트에 있는 AudioSource 컴포넌트를 자동으로 찾아 연결
        audioSource = GetComponent<AudioSource>();
        // 게임 시작 시 소리가 나지 않도록 Play On Awake를 비활성화
        audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // [수정] 충돌한 바로 그 오브젝트의 태그가 "Glove"인지 직접 확인합니다.
        if (other.CompareTag("Glove"))
        {
            // --- 오디오 재생 로직 ---
            if (audioSource != null && touchSound != null)
            {
                audioSource.PlayOneShot(touchSound);
            }
            
            Debug.Log(this.name + " 버튼이 터치되었습니다!");
            onTouch.Invoke();
        }
    }
}
