using UnityEngine;

public class BoxEntryDetector : MonoBehaviour
{
    [Header("이 상자의 고유 번호")]
    public int boxID;

    // 데이터 매니저를 참조할 변수
    private DataManager dataManager;

    void Start()
    {
        // 씬에 있는 DataManager를 자동으로 찾아 연결
        dataManager = FindObjectOfType<DataManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 공이 들어왔고, 데이터 매니저가 연결되어 있다면
        if (other.CompareTag("Ball") && dataManager != null)
        {
            // 데이터 매니저에게 자신의 상자 번호와 공 오브젝트를 전달
            dataManager.RecordBallEntry(boxID, other.gameObject);
        }
    }
}