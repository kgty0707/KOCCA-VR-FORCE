using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ConditionBallSet
{
    public string conditionName;
    public GameObject[] ballPrefabs;
}

public class ObjectSpawner : MonoBehaviour
{
    [Header("조건별 공 프리팹 목록")]
    public List<ConditionBallSet> conditionSets;

    [Header("생성 위치 설정")]
    public Transform spawnPoint;
    public float minimumSpacing = 1.5f;

    [Header("튜토리얼 공 설정")]
    public GameObject[] standardBallPrefabs; // 5개의 표준 공 프리팹 배열
    public Transform tutorialSpawnPoint;     // 튜토리얼 공이 생성될 위치

    // --- 내부 변수 ---
    private bool isBlocked = false;
    private GameObject lastSpawnedObject;
    private List<GameObject> spawnQueue = new List<GameObject>();
    private int spawnIndex = 0;
    private List<GameObject> tutorialBalls = new List<GameObject>();

    // [추가] 메인 블록에서 생성된 공들을 추적하기 위한 리스트
    private List<GameObject> mainBlockSpawnedBalls = new List<GameObject>();

    // --- 튜토리얼 관련 함수들 ---
    public void SpawnTutorialBalls()
    {
        foreach (var prefab in standardBallPrefabs)
        {
            Transform spawnLocation = tutorialSpawnPoint != null ? tutorialSpawnPoint : this.transform;
            GameObject ball = Instantiate(prefab, spawnLocation.position, Quaternion.identity);
            SetAlpha(ball, 0);
            tutorialBalls.Add(ball);
        }
    }

    public IEnumerator FadeInTutorialBalls()
    {
        float duration = 1.5f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(0, 1, elapsedTime / duration);
            foreach (var ball in tutorialBalls)
            {
                SetAlpha(ball, alpha);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public void ClearTutorialBalls()
    {
        foreach (var ball in tutorialBalls)
        {
            Destroy(ball);
        }
        tutorialBalls.Clear();
    }

    private void SetAlpha(GameObject obj, float alpha)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color newColor = renderer.material.color;
            newColor.a = alpha;
            renderer.material.color = newColor;
        }
    }

    // --- 메인 블록 관련 함수들 ---
    public void StartSpawningForBlock(ExperimentCondition condition, int requiredBallCount)
    {
        string conditionName = condition.ToString();
        ConditionBallSet currentSet = conditionSets.Find(set => set.conditionName == conditionName);
        if (currentSet != null && currentSet.ballPrefabs.Length > 0)
        {
            int totalSpawnCount = requiredBallCount + 3;
            PrepareSpawnQueue(currentSet.ballPrefabs, totalSpawnCount, requiredBallCount);
            StartCoroutine(SpawnObjectRoutine());
        }
        else
        {
            Debug.LogError($"'{conditionName}' 조건을 찾을 수 없거나 프리팹이 없습니다!");
        }
    }

    void PrepareSpawnQueue(GameObject[] prefabs, int totalSpawnCount, int requiredBallCount)
    {
        spawnQueue.Clear();
        spawnIndex = 0;
        if (requiredBallCount % prefabs.Length != 0)
        {
            Debug.LogWarning("경고: 목표 생성 개수가 공 종류의 배수가 아닙니다.");
        }
        int countPerPrefab = requiredBallCount / prefabs.Length;
        foreach (GameObject prefab in prefabs)
        {
            for (int i = 0; i < countPerPrefab; i++)
            {
                spawnQueue.Add(prefab);
            }
        }
        for (int i = 0; i < totalSpawnCount - requiredBallCount; i++)
        {
            spawnQueue.Add(prefabs[Random.Range(0, prefabs.Length)]);
        }
        var random = new System.Random();
        spawnQueue = spawnQueue.OrderBy(x => random.Next()).ToList();
    }

    public void SetBlockedStatus(bool status)
    {
        isBlocked = status;
    }

    private IEnumerator SpawnObjectRoutine()
    {
        while (spawnIndex < spawnQueue.Count)
        {
            while (isBlocked || !IsSpaceAvailable())
            {
                yield return null;
            }
            // [수정] 생성된 오브젝트를 변수에 담고, 추적 리스트에 추가
            GameObject newBall = Instantiate(spawnQueue[spawnIndex], spawnPoint.position, spawnPoint.rotation);
            lastSpawnedObject = newBall;
            mainBlockSpawnedBalls.Add(newBall); // 리스트에 추가

            spawnIndex++;
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("현재 블록의 모든 공 생성이 완료되었습니다.");
    }

    public bool IsBlockFinished()
    {
        return spawnIndex >= spawnQueue.Count;
    }

    private bool IsSpaceAvailable()
    {
        if (lastSpawnedObject == null) return true;
        float distance = Vector3.Distance(lastSpawnedObject.transform.position, spawnPoint.position);
        return distance > minimumSpacing;
    }
    
    // [추가] 메인 블록에서 생성된 모든 공을 삭제하는 함수
    public void ClearAllSpawnedObjects()
    {
        // 리스트에 있는 모든 게임오브젝트를 파괴
        foreach (GameObject ball in mainBlockSpawnedBalls)
        {
            // 오브젝트가 이미 다른 이유로 파괴되었을 경우를 대비해 null 체크
            if (ball != null)
            {
                Destroy(ball);
            }
        }
        
        // 리스트를 비워서 다음 블록을 준비
        mainBlockSpawnedBalls.Clear();
        
        Debug.Log("메인 블록에서 생성된 모든 공이 삭제되었습니다.");
    }
}
