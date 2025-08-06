using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class LogEntry
{
    public string timestamp;
    public string participantName;
    public string visualCondition;
    public int boxID;
    public string ballName;
    public float stiffness;
    public int blockGrabCount;
    public int totalGrabCount;
    public string firstGrabTimestamp;
    public string lastGrabTimestamp;
    public int confidence;
}

public class DataManager : MonoBehaviour
{
    private List<LogEntry> logEntries = new List<LogEntry>();
    private string participantName;
    private string currentVisualCondition;
    // 이제 이 경로는 'C:/.../Data'와 같은 최상위 데이터 폴더를 가리킵니다.
    private string rootDataPath; 
    private GrabManager grabManager;
    // HandPoseLogger를 제어하기 위한 참조
    private HandPoseLogger handPoseLogger; 

    private int currentBlockNumber = 1;
    private int totalBlocks = 1;
    
    // dataFolderPath는 이제 플레이어별 폴더가 아닌 최상위 데이터 폴더 경로입니다.
    public void Initialize(string name, string dataFolderPath)
    {
        participantName = name;
        rootDataPath = dataFolderPath;
        Debug.Log($"DataManager가 초기화되었습니다. 루트 데이터 저장 경로: {rootDataPath}");
    }

    void Start()
    {
        grabManager = FindObjectOfType<GrabManager>();
        // HandPoseLogger 컴포넌트를 찾아서 참조를 저장합니다.
        handPoseLogger = FindObjectOfType<HandPoseLogger>();
        if (handPoseLogger == null)
        {
            Debug.LogError("오류: 씬에서 HandPoseLogger를 찾을 수 없습니다!");
        }
    }

    public void SetCurrentBlockInfo(string visualCond)
    {
        currentVisualCondition = visualCond;
    }

    public void SetBlockNumberInfo(int blockNum, int totalNum)
    {
        this.currentBlockNumber = blockNum;
        this.totalBlocks = totalNum;
    }

    public void RecordBallEntry(int boxID, GameObject ball, string entryTimestamp, int confidence)
    {
        if (!ball.name.Contains("_"))
        {
            Debug.Log($"튜토리얼 공 '{ball.name}' 감지. 데이터 기록을 건너뜁니다.");
            return;
        }

        float stiffness = -1f;
        int blockGrabCount = -1;
        int totalGrabCount = -1;
        string firstGrabTimestamp = "";
        string lastGrabTimestamp = "";

        if (grabManager != null && grabManager.lastGrabInfos.ContainsKey(ball.name))
        {
            var info = grabManager.lastGrabInfos[ball.name];
            stiffness = info.stiffness;
            blockGrabCount = info.blockGrabCount;
            totalGrabCount = info.totalGrabCount;
            firstGrabTimestamp = info.firstGrabTimestamp;
            lastGrabTimestamp = info.lastGrabTimestamp;
        }

        LogEntry newEntry = new LogEntry
        {
            timestamp = entryTimestamp,
            participantName = this.participantName,
            visualCondition = this.currentVisualCondition,
            boxID = boxID,
            ballName = ball.name,
            stiffness = stiffness,
            blockGrabCount = blockGrabCount,
            totalGrabCount = totalGrabCount,
            firstGrabTimestamp = firstGrabTimestamp,
            lastGrabTimestamp = lastGrabTimestamp,
            confidence = confidence
        };
        logEntries.Add(newEntry);
    }
    
    // 데이터(로그)를 CSV 파일로 내보내는 함수
    public void ExportToCSV()
    {
        if (logEntries.Count == 0)
        {
            Debug.LogWarning("기록된 데이터가 없어 MainLog 파일을 저장하지 않습니다.");
            // 데이터가 없어도 손 추적 데이터는 저장해야 할 수 있으므로 handPoseLogger 저장은 호출
            if (handPoseLogger != null)
            {
                // MainLog가 없어도 HandPoseLogger는 저장될 수 있도록 경로를 생성하고 호출합니다.
                CreateDirectoryAndTriggerSave();
            }
            return;
        }

        // 경로 생성 및 저장을 통합된 함수로 호출
        string blockDataPath = CreateDirectoryAndTriggerSave();
        
        // MainLog.csv 파일 저장 로직
        string fileName = "MainLog.csv";
        string path = Path.Combine(blockDataPath, fileName);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Timestamp,ParticipantName,VisualCondition,BoxID,BallName,Stiffness,BlockGrabCount,TotalGrabCount,FirstGrabTimestamp,LastGrabTimestamp,Confidence");

        foreach (LogEntry entry in logEntries)
        {
            sb.AppendLine($"{entry.timestamp},{entry.participantName},{entry.visualCondition},{entry.boxID},\"{entry.ballName}\",{entry.stiffness},{entry.blockGrabCount},{entry.totalGrabCount},{entry.firstGrabTimestamp},{entry.lastGrabTimestamp},{entry.confidence}");
        }

        try
        {
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log($"성공: MainLog 데이터가 저장되었습니다.\n{path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"오류: MainLog 파일 저장에 실패했습니다. \n{e.Message}");
        }
        finally
        {
            logEntries.Clear();
            Debug.Log("다음 블록을 위해 MainLog 데이터를 초기화했습니다.");
        }
    }

    // 경로를 생성하고 모든 로거의 저장을 트리거하는 새로운 메소드
    private string CreateDirectoryAndTriggerSave()
    {
        // 1. 플레이어 이름의 폴더 경로 생성
        string playerFolderPath = Path.Combine(rootDataPath, participantName);

        // 2. 세션 폴더 이름 생성 (타임스탬프 포함)
        string timeStampForFile = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string sessionFolderName = $"Block_{currentBlockNumber:D2}_of_{totalBlocks}_{participantName}_{currentVisualCondition}_{timeStampForFile}";
        
        // 3. 최종 세션 폴더 경로 결합
        string blockDataPath = Path.Combine(playerFolderPath, sessionFolderName);

        // 4. 폴더(디렉토리) 생성
        if (!Directory.Exists(blockDataPath))
        {
            Directory.CreateDirectory(blockDataPath);
        }

        // 5. HandPoseLogger에 최종 경로를 전달하여 파일 저장을 요청
        if (handPoseLogger != null)
        {
            // StopLogging은 로깅 루프만 중단하고, 실제 파일 저장은 이 곳에서 제어
            handPoseLogger.StopLogging();
            handPoseLogger.SaveLogToFile(blockDataPath);
        }

        return blockDataPath;
    }
}