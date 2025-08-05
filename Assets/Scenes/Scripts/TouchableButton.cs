using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))] 
public class TouchableButton : MonoBehaviour
{
    [Header("터치 시 재생할 사운드")]
    public AudioClip touchSound;

    [Header("터치 시 실행될 이벤트")]
    public UnityEvent onTouch;

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
            
            Debug.Log(this.name + " 버튼이 터치되었습니다!");
            onTouch.Invoke();
        }
    }
}
