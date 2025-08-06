using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public enum ExperimentState { 
    Exploration,
    RealTutorial,
    MainBlock, 
    BreakTime, 
    Finished 
}

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
    private bool isProcessingConfidence = false;
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
        dataManager.Initialize(playerName, rootDataPath);

        if (grabManager != null)
        {
            string playerDataPath = Path.Combine(rootDataPath, playerName);
            if (!Directory.Exists(playerDataPath)) Directory.CreateDirectory(playerDataPath);
            grabManager.Initialize(playerName, playerDataPath);
        }
        else
        {
            Debug.LogError("GrabManager가 연결되지 않았습니다!");
        }

        if (rightHandLogger != null)
        {
            rightHandLogger.Initialize(playerName, rootDataPath);
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
        SetAllBeltsMoving(false, 0);

        // --- 1. 탐구 세션 ---
        currentState = ExperimentState.Exploration;
        objectSpawner.ActivateTutorialBalls(); 
        StartCoroutine(objectSpawner.FadeInTutorialBalls());

        uiManager.ShowInstruction("앞에 놓인 5개의 공은 각각 단단한 정도가 다릅니다.\n각 공의 강성 차이를 충분히 익혀주세요.\n준비가 되면 오른쪽 '확인' 버튼을 눌러 다음 단계로 진행합니다.");
        if (tutorialStartClip != null) audioSource.PlayOneShot(tutorialStartClip);

        uiManager.ShowTutorialButtons();
        yield return new WaitUntil(() => hasConfirmedTutorial);
        
        objectSpawner.DeactivateTutorialBalls();
        uiManager.HideAllButtons();
        hasConfirmedTutorial = false;

        // --- 2. 튜토리얼 세션 ---
        currentState = ExperimentState.RealTutorial;

        // 튜토리얼용 공 개수
        int tutorialBallCount = 5; 
        
        uiManager.ShowInstruction("이제 본 실험과 동일한 방식의 튜토리얼을 시작하겠습니다.\n컨베이어 벨트에서 나오는 공을 잡고 강성을 판단한 후, \n알맞은 상자에 넣어주세요.");
        yield return new WaitForSeconds(5f);
        uiManager.HideInstruction();

        SetAllBeltsMoving(true, conveyorBeltSpeed);
        
        objectSpawner.StartSpawningForBlock(ExperimentCondition.Base, tutorialBallCount);
        
        yield return new WaitUntil(() => objectSpawner.IsAllBallsEntered());
        yield return new WaitUntil(() => !isWaitingForConfidence);
        yield return new WaitForSeconds(0.5f);

        objectSpawner.ClearAllSpawnedObjects();

        // --- 튜토리얼 종료 안내 ---
        uiManager.ShowInstruction("튜토리얼이 종료되었습니다.\n준비가 되면 '확인' 버튼을 눌러 본 실험을 시작하세요.");
        uiManager.ShowTutorialButtons();
        yield return new WaitUntil(() => hasConfirmedTutorial);
        uiManager.HideAllButtons();
        hasConfirmedTutorial = false;

        // --- 3. 메인 실험 블록 반복 ---
        while (currentBlockIndex < experimentSequence.Count)
        {
            Debug.Log($"[ExperimentFlowRoutine] --- {currentBlockIndex+1}번째 블록 루프 진입 ---");

            ExperimentBlock currentBlock = experimentSequence[currentBlockIndex];
            string visualCondStr = currentBlock.visualCondition.ToString();

            SetAllBeltsMoving(true, conveyorBeltSpeed);

            currentState = ExperimentState.MainBlock;

            dataManager.SetCurrentBlockInfo(visualCondStr);
            dataManager.SetBlockNumberInfo(currentBlockIndex + 1, experimentSequence.Count); 

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
            yield return new WaitForSeconds(3f);
            uiManager.HideInstruction();

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
        // [수정됨] RealTutorial 또는 MainBlock 상태일 때 로직을 실행하도록 변경
        if (currentState != ExperimentState.MainBlock && currentState != ExperimentState.RealTutorial) return;

        _currentBoxID = boxID;
        _currentBall = ball;
        _currentEntryTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        isWaitingForConfidence = true;
        isProcessingConfidence = false;
        uiManager.ShowConfidencePanel();
    }

    public void OnConfidenceSelected(int confidence)
    {
        if (isProcessingConfidence)
        {
            Debug.LogWarning("이미 확신도 처리가 진행 중이므로 중복 호출을 무시합니다.");
            return;
        }
        
        isProcessingConfidence = true;
        uiManager.HideConfidencePanel();

        if (_currentBall == null)
        {
            Debug.Log("_currentBall이 null입니다!");
            return;
        }
        
        // RealTutorial에서는 데이터를 기록하지 않고, 공이 들어갔다는 사실만 알림
        if (currentState == ExperimentState.RealTutorial)
        {
            // objectSpawner.NotifyBallEnteredBox();
        }
        // MainBlock에서는 데이터를 기록
        else if (currentState == ExperimentState.MainBlock)
        {
            dataManager.RecordBallEntry(_currentBoxID, _currentBall, _currentEntryTimestamp, confidence);
        }

        Destroy(_currentBall);
        _currentBall = null;

        isWaitingForConfidence = false;

        SetSystemBlocked(false);
    }

    public void SetSystemBlocked(bool isBlocked)
    {
        // [수정됨] RealTutorial 또는 MainBlock 상태일 때 시스템을 멈추도록 변경
        if (currentState == ExperimentState.MainBlock || currentState == ExperimentState.RealTutorial)
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
