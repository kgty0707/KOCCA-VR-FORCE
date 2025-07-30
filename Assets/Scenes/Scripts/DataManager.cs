using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;

public struct LogEntry
{
    public string timestamp;
    public string participantName;
    public string visualCondition;
    public string eventType;
    public int boxID;
    public string ballName;
}

public class DataManager : MonoBehaviour
{
    private List<LogEntry> logEntries = new List<LogEntry>();
    private string participantName;
    private string currentVisualCondition;
    private string playerDataPath; // [추가] 파일 저장 경로를 저장할 변수

    // [수정] SetParticipantInfo -> Initialize로 변경하고 경로를 받도록 함
    public void Initialize(string name, string dataFolderPath)
    {
        participantName = name;
        playerDataPath = dataFolderPath;
        Debug.Log($"DataManager가 초기화되었습니다. 데이터 저장 경로: {playerDataPath}");
    }

    public void SetCurrentBlockInfo(string visualCond)
    {
        currentVisualCondition = visualCond;
    }

    // 주 과제(공 분류) 기록 함수
    public void RecordBallEntry(int boxID, GameObject ball)
    {
        // [수정] 튜토리얼 공(이름에 "(Clone)"이 없음)은 기록하지 않고 함수를 종료합니다.
        if (!ball.name.Contains("(Clone)"))
        {
            Debug.Log($"튜토리얼 공 '{ball.name}' 감지. 데이터 기록을 건너뜁니다.");
            return;
        }

        LogEntry newEntry = new LogEntry
        {
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            participantName = this.participantName,
            visualCondition = this.currentVisualCondition,
            eventType = "Ball_Sorted",
            boxID = boxID,
            ballName = ball.name
        };
        logEntries.Add(newEntry);
    }

    private void OnApplicationQuit()
    {
        ExportToCSV();
    }

    public void ExportToCSV()
    {
        if (logEntries.Count == 0) return;

        // --- [수정] 파일 저장 경로를 전달받은 playerDataPath로 변경 ---
        string fileTimestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{participantName}_MainLog_{fileTimestamp}.csv";
        string path = Path.Combine(playerDataPath, fileName); // 저장 위치 변경

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Timestamp,ParticipantName,VisualCondition,EventType,BoxID,BallName");

        foreach (LogEntry entry in logEntries)
        {
            sb.AppendLine($"{entry.timestamp},{entry.participantName},{entry.visualCondition},{entry.eventType},{entry.boxID},\"{entry.ballName}\"");
        }

        try
        {
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log($"CSV 파일 저장 성공! 경로: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CSV 파일 저장 실패: {e.Message}");
        }
    }
}
