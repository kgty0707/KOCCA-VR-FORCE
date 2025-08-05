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
    private string playerDataPath;
    private GrabManager grabManager;
    private int currentBlockNumber = 1;
    private int totalBlocks = 1;
    public void Initialize(string name, string dataFolderPath)
    {
        participantName = name;
        playerDataPath = dataFolderPath;
        Debug.Log($"DataManager가 초기화되었습니다. 데이터 저장 경로: {playerDataPath}");
    }

    void Start()
    {
        grabManager = FindObjectOfType<GrabManager>();
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
    public void ExportToCSV()
    {
        if (logEntries.Count == 0)
        {
            Debug.LogWarning("기록된 데이터가 없어 MainLog 파일을 저장하지 않습니다.");
            return;
        }

        string timeStampForFile = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folderName = $"Block_{currentBlockNumber:D2}_of_{totalBlocks}_{participantName}_{currentVisualCondition}_{timeStampForFile}";
        
        string blockDataPath = Path.Combine(playerDataPath, folderName);

        if (!Directory.Exists(blockDataPath))
        {
            Directory.CreateDirectory(blockDataPath);
        }

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
}
