using UnityEngine;

public class BoxEntryDetector : MonoBehaviour
{
    [Header("이 상자의 고유 번호")]
    public int boxID;
    private DataManager dataManager;
    private HandPoseLogger handPoseLogger;
    private ExperimentManager experimentManager;
    private ObjectSpawner objectSpawner;

    void Start()
    {
        dataManager = FindObjectOfType<DataManager>();
        experimentManager = FindObjectOfType<ExperimentManager>();
        handPoseLogger = FindObjectOfType<HandPoseLogger>();
        objectSpawner = FindObjectOfType<ObjectSpawner>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball") && experimentManager != null)
        {
            experimentManager.BallEnteredBox(boxID, other.gameObject);

            if (objectSpawner != null)
            {
                objectSpawner.NotifyBallEnteredBox();
            }
        }
    }
}