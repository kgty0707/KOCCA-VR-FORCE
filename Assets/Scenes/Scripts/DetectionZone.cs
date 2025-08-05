using UnityEngine;

public class DetectionZone : MonoBehaviour
{
    private ExperimentManager experimentManager;

    void Start()
    {
        experimentManager = FindObjectOfType<ExperimentManager>();
        if (experimentManager == null)
        {
            Debug.LogError("씬에 ExperimentManager가 없습니다!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball") && experimentManager != null)
        {
            experimentManager.SetSystemBlocked(true);
        }
    }
}
