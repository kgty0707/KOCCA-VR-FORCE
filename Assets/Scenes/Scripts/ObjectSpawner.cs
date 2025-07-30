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

    // --- [수정된 부분 1] ---
    // 프리팹 배열 대신 씬에 있는 오브젝트를 직접 연결합니다.
    [Header("튜토리얼 공 설정")]
    [Tooltip("씬에 미리 배치된 튜토리얼 공 오브젝트들을 여기에 연결하세요.")]
    public GameObject[] tutorialBallObjects; // 기존 standardBallPrefabs 대신 사용

    public Transform tutorialSpawnPoint;

    // --- 내부 변수 ---
    private bool isBlocked = false;
    private GameObject lastSpawnedObject;
    private List<GameObject> spawnQueue = new List<GameObject>();
    private int spawnIndex = 0;

    // [추가] 블록 내에서 생성된 공의 순서를 기록할 카운터
    private int ballSpawnCounter = 0;

    // 이 리스트는 이제 씬에 있는 공들의 참조를 임시로 담는 역할을 합니다.
    private List<GameObject> activeTutorialBalls = new List<GameObject>();
    private List<GameObject> mainBlockSpawnedBalls = new List<GameObject>();


    // --- [수정된 부분 2] ---
    // 튜토리얼 공을 '생성'하는 대신 '활성화하고 보여주는' 함수
    public void ActivateTutorialBalls()
    {
        activeTutorialBalls.Clear(); // 리스트를 먼저 비웁니다.
        foreach (var ball in tutorialBallObjects)
        {
            if (ball != null)
            {
                ball.SetActive(true);  // 오브젝트를 활성화합니다.
                SetAlpha(ball, 0);     // 투명하게 만들어 Fade-in을 준비합니다.
                activeTutorialBalls.Add(ball); // 관리 리스트에 추가합니다.
            }
        }
    }

    // Fade-in 기능은 그대로 사용합니다. 대상이 activeTutorialBalls 리스트입니다.
    public IEnumerator FadeInTutorialBalls()
    {
        float duration = 1.5f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(0, 1, elapsedTime / duration);
            foreach (var ball in activeTutorialBalls)
            {
                SetAlpha(ball, alpha);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // --- [수정된 부분 3] ---
    // 튜토리얼 공을 '파괴'하는 대신 '비활성화'하는 함수
    public void DeactivateTutorialBalls()
    {
        foreach (var ball in activeTutorialBalls)
        {
            if (ball != null)
            {
                ball.SetActive(false); // 오브젝트를 파괴하는 대신 비활성화합니다.
            }
        }
        activeTutorialBalls.Clear(); // 관리 리스트를 비웁니다.
    }


    private void SetAlpha(GameObject obj, float alpha)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            // 모든 머티리얼의 색상을 변경하도록 수정 (투명 쉐이더 사용 시)
            foreach (var mat in renderer.materials)
            {
                Color newColor = mat.color;
                newColor.a = alpha;
                mat.color = newColor;
            }
        }
    }

    // --- 메인 블록 관련 함수들 ---
    public void StartSpawningForBlock(ExperimentCondition condition, int requiredBallCount)
    {
        // [추가] 새로운 블록이 시작될 때 스폰 카운터를 초기화합니다.
        ballSpawnCounter = 0;

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

            GameObject newBall = Instantiate(spawnQueue[spawnIndex], spawnPoint.position, spawnPoint.rotation);

            // --- [수정] 생성된 공에 고유 이름 부여 ---
            ballSpawnCounter++; // 카운터 증가
            string originalName = spawnQueue[spawnIndex].name; // 원본 프리팹 이름 (예: "BallA")
            newBall.name = $"{originalName}-{ballSpawnCounter}"; // 새 이름 할당 (예: "BallA-1")
            Debug.Log($"공 생성: {newBall.name}");
            // --- 수정 끝 ---

            lastSpawnedObject = newBall;
            mainBlockSpawnedBalls.Add(newBall);

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
