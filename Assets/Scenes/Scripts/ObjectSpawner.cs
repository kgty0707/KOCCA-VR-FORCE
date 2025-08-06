using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SG; // SenseGlove 네임스페이스

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
    public GameObject[] tutorialBallObjects;
    public Transform tutorialSpawnPoint;

    // --- 외부 참조 변수 ---
    public int enteredBallCount { get; private set; }
    public int totalBallsToEnter { get; private set; }

    // --- 내부 관리 변수 ---
    private List<GameObject> spawnQueue = new List<GameObject>();
    private List<float> forceQueue = new List<float>();
    private int spawnIndex = 0;
    private Coroutine spawnRoutine;
    private bool isBlocked = false;
    private GameObject lastSpawnedObject;
    private List<GameObject> mainBlockSpawnedBalls = new List<GameObject>();
    private HashSet<GameObject> enteredBalls = new HashSet<GameObject>();

    public void NotifyBallEnteredBox(GameObject ball = null)
    {
        // 중복 진입 방지
        if (ball != null && enteredBalls.Contains(ball))
        {
            Debug.LogWarning($"[ObjectSpawner] 이미 카운트된 공입니다: {ball.name}");
            return;
        }
        
        if (ball != null) enteredBalls.Add(ball);
        
        enteredBallCount++;
        Debug.Log($"[ObjectSpawner] 공이 상자에 들어옴. 현재 카운트: {enteredBallCount} / {totalBallsToEnter}");
    }

    void Start()
    {
        // 특별한 초기화가 필요 없으므로 비워둡니다.
    }

    // =====================================================================================
    // [공용 함수] ExperimentManager, BoxEntryDetector 등 외부 스크립트에서 호출하는 함수들
    // =====================================================================================

    /// <summary>
    /// 새로운 블록의 공 생성을 시작합니다.
    /// </summary>
    public void StartSpawningForBlock(ExperimentCondition condition, int requiredBallCount)
    {
        Debug.Log($"[ObjectSpawner] 새로운 블록 시작 요청. requiredBallCount = {requiredBallCount}");

        this.totalBallsToEnter = requiredBallCount;
        this.enteredBallCount = 0;
        spawnQueue.Clear();
        forceQueue.Clear();
        spawnIndex = 0;

        Debug.Log($"[ObjectSpawner] totalBallsToEnter 변수를 '{this.totalBallsToEnter}'로 설정했습니다.");


        ConditionBallSet currentSet = conditionSets.FirstOrDefault(cs => cs.conditionName == condition.ToString());
        if (currentSet == null || currentSet.ballPrefabs.Length == 0)
        {
            Debug.LogError($"'{condition}' 조건에 대한 공 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 규칙 1: 균등 분배 목록 생성
        var basePrefabs = new List<GameObject>();
        int numBallTypes = currentSet.ballPrefabs.Length;
        int countPerBall = requiredBallCount / numBallTypes;
        int remainder = requiredBallCount % numBallTypes;

        foreach (var prefab in currentSet.ballPrefabs)
        {
            for (int i = 0; i < countPerBall; i++) basePrefabs.Add(prefab);
        }
        var shuffledPrefabsForRemainder = currentSet.ballPrefabs.OrderBy(x => Guid.NewGuid()).ToList();
        for (int i = 0; i < remainder; i++)
        {
            basePrefabs.Add(shuffledPrefabsForRemainder[i]);
        }
        
        spawnQueue = basePrefabs.OrderBy(x => Guid.NewGuid()).ToList();
        Debug.Log($"[ObjectSpawner] 생성 큐에 {spawnQueue.Count}개의 공을 준비했습니다.");


        // 규칙 2: Confusion 조건일 경우, 추가 규칙 적용
        if (condition == ExperimentCondition.Confusion)
        {
            PrepareConfusionForces(spawnQueue);
        }

        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }
    
    /// <summary>
    /// 모든 공이 상자에 들어갔는지 확인합니다.
    /// </summary>
    public bool IsAllBallsEntered()
    {
        return enteredBallCount >= totalBallsToEnter;
    }

    /// <summary>
    /// BoxEntryDetector가 호출하여 공이 상자에 들어왔음을 알립니다.
    /// </summary>
    public void NotifyBallEnteredBox()
    {
        enteredBallCount++;
    }
    /// <summary>
    /// 튜토리얼 공들을 활성화합니다.
    /// </summary>
    public void ActivateTutorialBalls()
    {
        foreach (var ball in tutorialBallObjects)
        {
            if (ball != null) ball.SetActive(true);
        }
    }
    
    /// <summary>
    /// 튜토리얼 공들을 비활성화합니다.
    /// </summary>
    public void DeactivateTutorialBalls()
    {
        foreach (var ball in tutorialBallObjects)
        {
            if (ball != null) ball.SetActive(false);
        }
    }

    /// <summary>
    /// 튜토리얼 공들이 서서히 나타나는 효과 (코루틴)
    /// </summary>
    public IEnumerator FadeInTutorialBalls()
    {
        Debug.Log("튜토리얼 공 페이드 인 시작");
        yield return new WaitForSeconds(1f); // 임시 대기, 필요시 로직 구현
    }
    
    /// <summary>
    /// 현재 블록에서 생성된 모든 공을 제거합니다.
    /// </summary>
    public void ClearAllSpawnedObjects()
    {
        foreach (var ball in mainBlockSpawnedBalls)
        {
            if (ball != null) Destroy(ball);
        }
        mainBlockSpawnedBalls.Clear();
        enteredBalls.Clear(); // 추가
    }
    
    /// <summary>
    /// 공 생성이 잠시 막혔는지 상태를 설정합니다.
    /// </summary>
    public void SetBlockedStatus(bool blocked)
    {
        isBlocked = blocked;
    }

    // ======================================================================
    // [수정된 핵심 로직] 내부 로직 함수들
    // ======================================================================

    /// <summary>
    /// Confusion 조건의 Stiffness 목록을 준비하고, 원래 값과 겹치지 않도록 보정합니다.
    /// </summary>
    private void PrepareConfusionForces(List<GameObject> currentSpawnQueue)
    {
        var originalForces = currentSpawnQueue.Select(p => p.GetComponent<SG_Material>().materialProperties.maxForce).ToList();
        
        int attempts = 0;
        while (attempts < 100) // 무한 루프 방지 장치
        {
            forceQueue = originalForces.OrderBy(x => Guid.NewGuid()).ToList();

            if (!HasMatches(currentSpawnQueue, forceQueue))
            {
                Debug.Log($"Confusion 조건의 완벽한 교란 순열 생성 성공! (시도 횟수: {attempts + 1})");
                return; 
            }
            attempts++;
        }
        Debug.LogError("100번 이상 시도했지만 완벽한 교란 순열을 만들지 못했습니다. 공의 종류나 개수에 문제가 있을 수 있습니다.");
    }
    
    /// <summary>
    /// 두 목록 사이에 같은 위치에 같은 값이 있는지 검사하는 헬퍼 함수
    /// </summary>
    private bool HasMatches(List<GameObject> spawnQueue, List<float> forceQueue)
    {
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            float originalForce = spawnQueue[i].GetComponent<SG_Material>().materialProperties.maxForce;
            if (Mathf.Approximately(originalForce, forceQueue[i]))
            {
                return true; // 하나라도 겹치면 true 반환
            }
        }
        return false; // 모두 다르면 false 반환
    }

    /// <summary>
    /// 설정된 큐에 따라 공을 생성하는 코루틴
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        while (spawnIndex < spawnQueue.Count)
        {
            yield return new WaitUntil(() => !isBlocked);

            GameObject prefabToSpawn = spawnQueue[spawnIndex];
            GameObject newBall = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            newBall.name = $"{prefabToSpawn.name}_{spawnIndex + 1}"; // 로그 기록을 위한 이름 부여
            mainBlockSpawnedBalls.Add(newBall);

            // Confusion 조건일 경우에만 값을 덮어씁니다.
            if (forceQueue.Count > 0)
            {
                var material = newBall.GetComponent<SG_Material>();
                if (material != null && material.materialProperties != null)
                {
                    // [최종 핵심 수정] 원본 Material 애셋이 영구적으로 변경되는 것을 방지합니다.
                    // 1. 원본 Material Properties 애셋을 복제(Instantiate)하여 메모리에 새로운 인스턴스를 만듭니다.
                    material.materialProperties = Instantiate(material.materialProperties);
                    
                    // 2. 이제 복제된 인스턴스의 maxForce 값을 변경합니다.
                    material.materialProperties.maxForce = forceQueue[spawnIndex];
                }
            }
            
            lastSpawnedObject = newBall;
            spawnIndex++;
            yield return new WaitForSeconds(minimumSpacing);
        }
    }

    // --- Unity 메시지 함수 ---
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == lastSpawnedObject)
        {
            isBlocked = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == lastSpawnedObject)
        {
            isBlocked = false;
        }
    }
}
