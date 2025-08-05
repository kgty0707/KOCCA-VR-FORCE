using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public enum ExperimentState { Tutorial, MainBlock, BreakTime, Finished }

public enum ExperimentCondition
{
    Base,
    Vision,
    Confusion
}

[System.Serializable]
public class ExperimentBlock
{
    public ExperimentCondition visualCondition;
}

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
    [Tooltip("씬에 있는 오른손의 HandPoseLogger를 연결하세요.")]
    public HandPoseLogger rightHandLogger;
    [Tooltip("씬에 있는 ObjectSpawner를 연결하세요.")]
    public ObjectSpawner objectSpawner;
    [Tooltip("씬에 있는 UIManager를 연결하세요.")]
    public UIManager uiManager;
    [Tooltip("씬에 있는 GrabManager를 연결하세요.")]
    public GrabManager grabManager;
    [Tooltip("씬에 있는 DataManager를 연결하세요.")]
    public DataManager dataManager;

    public ConveyorBeltSolid[] conveyorBelts;

    private AudioSource audioSource;
    private ExperimentState currentState;
    private List<ExperimentBlock> experimentSequence;
    private int currentBlockIndex = 0;
    private bool hasConfirmedTutorial = false;
    private bool isWaitingForConfidence = false;
    private int _currentBoxID;
    private GameObject _currentBall;
    private string _currentEntryTimestamp;

    public void OnTutorialConfirmed()
    {
        hasConfirmedTutorial = true;
    }


    void Start()
    {
        Debug.Log($"[ExperimentManager] Start 시작. DataManager: {(dataManager != null)}, GrabManager: {(grabManager != null)}, RightHandLogger: {(rightHandLogger != null)}");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) Debug.LogError("AudioSource 컴포넌트를 찾을 수 없습니다!");

        string rootDataPath = Path.Combine(Application.dataPath, "Data");
        string playerDataPath = Path.Combine(rootDataPath, playerName);
        string handPoseLogPath = Path.Combine(playerDataPath, $"{playerName}_Hand_Pose");

        if (!Directory.Exists(playerDataPath)) Directory.CreateDirectory(playerDataPath);
        if (!Directory.Exists(handPoseLogPath)) Directory.CreateDirectory(handPoseLogPath);

        dataManager.Initialize(playerName, playerDataPath);

        if (grabManager != null)
        {
            grabManager.Initialize(playerName, playerDataPath);
        }
        else
        {
            Debug.LogError("GrabManager가 연결되지 않았습니다!");
        }

        if (rightHandLogger != null)
        {
            rightHandLogger.Initialize(playerName, handPoseLogPath);
        }

        PrepareExperimentSequence();
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
            Debug.Log($"  {i + 1}. Visual: {block.visualCondition}");
        }
    }

    private IEnumerator ExperimentFlowRoutine()
    {
        // 실험 시작 시 컨베이어 벨트를 우선 정지 상태로 설정
        SetAllBeltsMoving(false, 0);

        // --- 1. 튜토리얼 단계 ---
        currentState = ExperimentState.Tutorial;

        // [수정] 튜토리얼 공을 '생성'하는 대신 '활성화'하고 Fade-in 효과
        objectSpawner.ActivateTutorialBalls();
        StartCoroutine(objectSpawner.FadeInTutorialBalls());

        uiManager.ShowInstruction("앞에 놓인 5개의 공은 각각 단단한 정도가 다릅니다.\n각 공의 강성 차이를 충분히 익힌 후,\n준비가 되면 오른쪽 '확인' 버튼을 눌러주세요.");
        if (tutorialStartClip != null) audioSource.PlayOneShot(tutorialStartClip);

        uiManager.ShowTutorialButtons();
        yield return new WaitUntil(() => hasConfirmedTutorial);

        // [수정] 튜토리얼 공을 '파괴'하는 대신 '비활성화'
        objectSpawner.DeactivateTutorialBalls();
        uiManager.HideAllButtons();

        // --- 2. 메인 실험 블록 반복 ---
        while (currentBlockIndex < experimentSequence.Count)
        {
            Debug.Log($"[ExperimentFlowRoutine] --- {currentBlockIndex+1}번째 블록 루프 진입 ---");

            ExperimentBlock currentBlock = experimentSequence[currentBlockIndex];
            string visualCondStr = currentBlock.visualCondition.ToString();

            SetAllBeltsMoving(true, conveyorBeltSpeed);

            currentState = ExperimentState.MainBlock;

            dataManager.SetCurrentBlockInfo(visualCondStr);
            dataManager.SetBlockNumberInfo(currentBlockIndex + 1, experimentSequence.Count); // <-- 추가

            if (grabManager != null)
            {
                grabManager.SetCurrentBlockInfo(visualCondStr);
            }

            if (rightHandLogger != null)
            {
                try
                {
                    rightHandLogger.SetBlockNumberInfo(currentBlockIndex + 1, experimentSequence.Count);
                    rightHandLogger.SetCurrentBlockInfo(visualCondStr);
                    rightHandLogger.StartLogging();
                    Debug.Log("손 데이터 기록을 시작합니다.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"!!!!!!!! HandPoseLogger에서 예외 발생 !!!!!!!!");
                    Debug.LogError(e.ToString()); 
                }
            }

            uiManager.ShowInstruction($"{currentBlockIndex + 1} / {experimentSequence.Count} 번째 블록을 시작합니다.");
            if (blockStartClip != null) audioSource.PlayOneShot(blockStartClip);

            uiManager.HideInstruction();

            // ★ 블록 시작 전에 ObjectSpawner의 blocked 상태를 명시적으로 해제
            objectSpawner.SetBlockedStatus(false);
            objectSpawner.StartSpawningForBlock(currentBlock.visualCondition, ballsPerBlock);

            yield return new WaitUntil(() => objectSpawner.IsAllBallsEntered());

            yield return new WaitUntil(() => !isWaitingForConfidence);

            yield return new WaitForSeconds(0.5f);

            objectSpawner.ClearAllSpawnedObjects();

            if (grabManager != null)
            {
                grabManager.ResetBlockCounts();
            }


            if (rightHandLogger != null)
            {
                rightHandLogger.StopLogging();
                Debug.Log("손 데이터가 저장됩니다.");
            }

            dataManager.ExportToCSV();

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
        SetAllBeltsMoving(false, 0);

        // --- 4. 실험 종료 ---
        currentState = ExperimentState.Finished;
        uiManager.HideAllButtons();
        uiManager.ShowEndOfExperimentScreen();
        if (experimentEndClip != null) audioSource.PlayOneShot(experimentEndClip);

        Debug.Log("모든 실험이 종료되었습니다. 데이터가 곧 저장됩니다.");
    }

    public void BallEnteredBox(int boxID, GameObject ball)
    {
        if (currentState != ExperimentState.MainBlock) return;

        _currentBoxID = boxID;
        _currentBall = ball;
        _currentEntryTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        isWaitingForConfidence = true;

        uiManager.ShowConfidencePanel();
    }

    public void OnConfidenceSelected(int confidence)
    {
        if (_currentBall == null) 
        {
            Debug.Log("_currentBall이 null입니다!");
            return;
        }

        uiManager.HideConfidencePanel();

        dataManager.RecordBallEntry(_currentBoxID, _currentBall, _currentEntryTimestamp, confidence);

        _currentBall = null;

        isWaitingForConfidence = false;

        SetSystemBlocked(false);
    }

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