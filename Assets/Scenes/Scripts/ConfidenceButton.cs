using UnityEngine;
using UnityEngine.Events; // UnityEvent를 사용하기 위해 필요

// UnityEvent에 int 매개변수를 넘겨주기 위한 커스텀 이벤트 클래스 정의
[System.Serializable]
public class OnTouchWithConfidence : UnityEvent<int> { }

[RequireComponent(typeof(AudioSource))]
public class ConfidenceButton : MonoBehaviour
{
    [Header("버튼의 고유 값 (1~5)")]
    [Tooltip("이 버튼이 나타내는 확신도 값을 설정하세요.")]
    public int confidenceValue;

    [Header("터치 시 재생할 사운드")]
    public AudioClip touchSound;

    [Header("터치 시 실행될 이벤트 (확신도 값 전달)")]
    public OnTouchWithConfidence onTouch;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Glove"))
        {
            if (touchSound != null)
            {
                AudioSource.PlayClipAtPoint(touchSound, transform.position);
            }
            
            Debug.Log($"확신도 {confidenceValue}번 버튼이 터치되었습니다!");
            
            onTouch.Invoke(confidenceValue);
        }
    }
}