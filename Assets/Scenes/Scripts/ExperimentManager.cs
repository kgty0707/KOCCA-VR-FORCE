using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // OrderBy (셔플) 기능을 사용하기 위해 필요

// --- 데이터 구조 정의 ---

// 실험의 현재 상태를 나타내는 열거형
public enum ExperimentState { Tutorial, MainBlock, BreakTime, Finished }

// 시각-촉각 조건을 위한 열거형
public enum ExperimentCondition
{
    Base,    // 촉각만 또는 일치 조건
    Vision,  // 시각적 단서가 있는 조건
    Confusion // 시각-촉각 불일치 조건
}

// 인지 부하 조건을 위한 열거형
public enum CognitiveLoadCondition { Low, High }

// 하나의 실험 블록에 대한 설정을 담는 클래스
[System.Serializable]
public class ExperimentBlock
{
    public ExperimentCondition visualCondition;
    public CognitiveLoadCondition loadCondition;
}


// --- 메인 컨트롤러 ---

public class ExperimentManager : MonoBehaviour
{
    [Header("--- 참가자 정보 ---")]
    public string playerName = "Player01";

    [Header("--- 실험 블록 및 진행 설정 ---")]
    [Tooltip("실험에 사용할 모든 블록 조합을 설정합니다.")]
    public List<ExperimentBlock> experimentBlocks;
    [Tooltip("한 블록에서 생성할 공의 총 개수. 공 종류의 배수여야 합니다.")]
    public int ballsPerBlock = 20;
    [Tooltip("블록 사이의 휴식 시간 (초)")]
    public float breakTimeDuration = 10f;

    [Header("--- 컨베이어 벨트 설정 ---")]
    public float conveyorBeltSpeed = 1.5f;

    [Header("--- 오디오 설정 ---")]
    public AudioClip tutorialStartClip;
    public AudioClip blockStartClip;
    public AudioClip experimentEndClip;

    [Header("--- 필수 연결 요소 ---")]
    [Tooltip("씬에 있는 ObjectSpawner를 연결하세요.")]
    public ObjectSpawner objectSpawner;
    [Tooltip("씬에 있는 UIManager를 연결하세요.")]
    public UIManager uiManager;
    [Tooltip("씬에 있는 DataManager를 연결하세요.")]
    public DataManager dataManager;
    [Tooltip("씬에 있는 CognitiveLoadManager를 연결하세요.")]
    public CognitiveLoadManager cognitiveLoadManager;
    [Tooltip("씬에 있는 모든 컨베이어 벨트를 연결하세요.")]
    public ConveyorBeltSolid[] conveyorBelts;

    // --- 내부 변수 ---
    private AudioSource audioSource;
    private ExperimentState currentState;
    private List<ExperimentBlock> experimentSequence; // 셔플된 최종 블록 순서
    private int currentBlockIndex = 0;
    private bool hasConfirmedTutorial = false;

    // --- 확인 버튼이 호출할 함수 ---
    public void OnTutorialConfirmed()
    {
        hasConfirmedTutorial = true;
    }


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // --- 디버깅용 로그 추가 ---
        if (audioSource == null)
        {
            Debug.LogError("AudioSource 컴포넌트를 찾을 수 없습니다!");
        }
        if (tutorialStartClip == null)
        {
            Debug.LogError("!!! Tutorial Start Clip 변수가 비어있습니다(null)!");
        }
        PrepareExperimentSequence();
        dataManager.SetParticipantInfo(playerName);
        StartCoroutine(ExperimentFlowRoutine());
    }

    void PrepareExperimentSequence()
    {
        var random = new System.Random();
        experimentSequence = experimentBlocks.OrderBy(x => random.Next()).ToList();

        Debug.Log("생성된 블록 순서:");
        for (int i = 0; i < experimentSequence.Count; i++)
        {
            var block = experimentSequence[i];
            Debug.Log($"  {i + 1}. Visual: {block.visualCondition} / Load: {block.loadCondition}");
        }
    }

    private IEnumerator ExperimentFlowRoutine()
    {
        // 실험 시작 시 컨베이어 벨트를 우선 정지 상태로 설정
        SetAllBeltsMoving(false, 0);

        // --- 1. 튜토리얼 단계 ---
        currentState = ExperimentState.Tutorial;
        uiManager.ShowInstruction("앞에 놓인 5개의 공은 각각 단단한 정도가 다릅니다.\n각 공의 강성 차이를 충분히 익힌 후,\n준비가 되면 오른쪽 '확인' 버튼을 눌러주세요.");
        if (tutorialStartClip != null) audioSource.PlayOneShot(tutorialStartClip);

        uiManager.ShowTutorialButtons();
        yield return new WaitUntil(() => hasConfirmedTutorial);

        // --- 2. 메인 실험 블록 반복 ---
        uiManager.ShowMainGameButtons();

        while (currentBlockIndex < experimentSequence.Count)
        {
            ExperimentBlock currentBlock = experimentSequence[currentBlockIndex];

            SetAllBeltsMoving(true, conveyorBeltSpeed);

            currentState = ExperimentState.MainBlock;
            dataManager.SetCurrentBlockInfo(currentBlock.visualCondition.ToString(), currentBlock.loadCondition.ToString());
            uiManager.ShowInstruction($"{currentBlockIndex + 1} / {experimentSequence.Count} 번째 블록을 시작합니다.");
            if (blockStartClip != null) audioSource.PlayOneShot(blockStartClip);

            yield return new WaitForSeconds(3);
            uiManager.HideInstruction();
            uiManager.ShowMainGameButtons();

            // 관리자들에게 현재 블록 시작 명령
            objectSpawner.StartSpawningForBlock(currentBlock.visualCondition, ballsPerBlock);
            cognitiveLoadManager.StartBlock(currentBlock.loadCondition);
            SetAllBeltsMoving(true, conveyorBeltSpeed);

            // Spawner가 모든 공 생성을 마칠 때까지 대기
            yield return new WaitUntil(() => objectSpawner.IsBlockFinished());
            yield return new WaitForSeconds(2); // 마지막 공이 처리될 시간 확보

            // 현재 블록의 부가 과제 종료
            cognitiveLoadManager.StopBlock();

            objectSpawner.ClearAllSpawnedObjects();

            // --- 3. 블록 간 휴식 ---
            currentBlockIndex++;
            if (currentBlockIndex < experimentSequence.Count)
            {
                currentState = ExperimentState.BreakTime;
                SetAllBeltsMoving(false, 0);
                uiManager.HideAllButtons();
                uiManager.ShowBreakScreen(breakTimeDuration);
                yield return new WaitForSeconds(breakTimeDuration);
            }
        }

        // --- 4. 실험 종료 ---
        currentState = ExperimentState.Finished;
        uiManager.HideAllButtons();
        uiManager.ShowEndOfExperimentScreen();
        if (experimentEndClip != null) audioSource.PlayOneShot(experimentEndClip);

        Debug.Log("모든 실험이 종료되었습니다. 데이터가 곧 저장됩니다.");
    }

    // DetectionZone이 호출하여 시스템 전체를 멈추거나 재개하는 함수
    public void SetSystemBlocked(bool isBlocked)
    {
        if (currentState == ExperimentState.MainBlock)
        {
            objectSpawner.SetBlockedStatus(isBlocked);
            SetAllBeltsMoving(!isBlocked, conveyorBeltSpeed);
        }
    }

    private void SetAllBeltsMoving(bool isMoving, float speed)
    {
        foreach (var belt in conveyorBelts)
        {
            if (belt != null) belt.SetMoving(isMoving, speed);
        }
    }
}