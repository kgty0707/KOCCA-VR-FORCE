using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;

// [수정] 로그 데이터 구조에 부가 과제 결과 필드 추가
public struct LogEntry
{
    public string timestamp;
    public string participantName;
    public string visualCondition;
    public string loadCondition;

    // 이벤트 유형을 구분하기 위한 필드
    public string eventType; // "Ball_Sorted" 또는 "Cognitive_Task"

    // 주 과제 데이터
    public int boxID;
    public string ballName;

    // 부가 과제 데이터
    public bool cognitiveTaskCorrect;
    public float cognitiveTaskResponseTime;
}

public class DataManager : MonoBehaviour
{
    private List<LogEntry> logEntries = new List<LogEntry>();
    private string participantName;
    private string currentVisualCondition;
    private string currentLoadCondition;

    public void SetParticipantInfo(string name)
    {
        participantName = name;
    }

    public void SetCurrentBlockInfo(string visualCond, string loadCond)
    {
        currentVisualCondition = visualCond;
        currentLoadCondition = loadCond;
    }

    // 주 과제(공 분류) 기록 함수
    public void RecordBallEntry(int boxID, GameObject ball)
    {
        LogEntry newEntry = new LogEntry
        {
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            participantName = this.participantName,
            visualCondition = this.currentVisualCondition,
            loadCondition = this.currentLoadCondition,
            eventType = "Ball_Sorted", // 이벤트 유형: 공 분류
            boxID = boxID,
            ballName = ball.name
            // 부가 과제 필드는 기본값(false, 0)으로 둠
        };
        logEntries.Add(newEntry);
    }

    // [추가] 부가 과제(인지 부하) 기록 함수
    public void RecordCognitiveTask(bool isCorrect, float responseTime)
    {
        LogEntry newEntry = new LogEntry
        {
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            participantName = this.participantName,
            visualCondition = this.currentVisualCondition,
            loadCondition = this.currentLoadCondition,
            eventType = "Cognitive_Task", // 이벤트 유형: 인지 과제
            cognitiveTaskCorrect = isCorrect,
            cognitiveTaskResponseTime = responseTime
            // 주 과제 필드는 기본값(0, null)으로 둠
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

        // --- [수정] 파일 저장 경로 설정 ---
        // 1. "Assets" 폴더를 기준으로 "Data" 폴더 경로를 지정합니다.
        string dataFolderPath = Path.Combine(Application.dataPath, "Data");

        // 2. 만약 "Data" 폴더가 존재하지 않으면 새로 생성합니다.
        if (!Directory.Exists(dataFolderPath))
        {
            Directory.CreateDirectory(dataFolderPath);
        }

        // 3. 파일 이름과 전체 경로를 조합합니다.
        string fileTimestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{participantName}_output_{fileTimestamp}.csv";
        string path = Path.Combine(dataFolderPath, fileName);
        // --- 수정 끝 ---

        StringBuilder sb = new StringBuilder();
        // [수정] CSV 헤더에 부가 과제 결과 열 추가
        sb.AppendLine("Timestamp,ParticipantName,VisualCondition,LoadCondition,EventType,BoxID,BallName,CognitiveTaskCorrect,CognitiveTaskResponseTime");

        foreach (LogEntry entry in logEntries)
        {
            // [수정] 새로운 필드를 포함하여 한 줄 생성
            sb.AppendLine($"{entry.timestamp},{entry.participantName},{entry.visualCondition},{entry.loadCondition},{entry.eventType},{entry.boxID},\"{entry.ballName}\",{entry.cognitiveTaskCorrect},{entry.cognitiveTaskResponseTime}");
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
