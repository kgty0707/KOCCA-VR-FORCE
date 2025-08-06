using UnityEngine;
using SG;
using System.Text;
using System.IO;
using System;

public class HandPoseLogger : MonoBehaviour
{
    [Header("필수 연결 요소")]
    public SG_TrackedHand trackedHand;

    // 이 변수들은 이제 DataManager가 경로를 생성하므로 사용되지 않습니다.
    // [Header("파일 설정")]
    // public string filePrefix = "HandPoseLog_";

    private bool isLogging = false;
    // private float logStartTime; // 사용되지 않으므로 주석 처리
    // private StringBuilder csvData; // 사용되지 않으므로 주석 처리
    private string participantName = "UnknownPlayer";
    // private string handPoseLogPath; // DataManager로부터 경로를 받으므로 불필요
    private StringBuilder realCsvData;
    private StringBuilder virtualCsvData;
    private int currentBlockNumber = 1;
    private int totalBlocks = 1;

    public void SetBlockNumberInfo(int blockNum, int totalNum)
    {
        this.currentBlockNumber = blockNum;
        this.totalBlocks = totalNum;
    }

    public void SetParticipantName(string name)
    {
        this.participantName = name;
        Debug.Log($"Logger's participant name set to: {this.participantName}");
    }
    private string currentVisualCondition = "N/A";
    void Awake()
    {
        if (trackedHand == null)
        {
            Debug.LogError($"[HandPoseLogger ERROR] {gameObject.name} 오브젝트의 HandPoseLogger에 'Tracked Hand' 필드가 연결되지 않았습니다!");
        }
        else
        {
            Debug.Log($"[HandPoseLogger] {gameObject.name}의 'Tracked Hand' 연결 확인 완료: {trackedHand.name}");
        }
    }
    
    // Initialize 함수에서 경로 설정 부분을 제거하거나 비워둡니다.
    public void Initialize(string name, string logFolderPath)
    {
        Debug.Log($"[HandPoseLogger] Initialize 함수 호출됨. Player: {name}");
        this.participantName = name;
        // this.handPoseLogPath = logFolderPath; // 더 이상 이 스크립트에서 경로를 관리하지 않음
    }
    
    public void SetCurrentBlockInfo(string visual)
    {
        Debug.Log($"[HandPoseLogger] SetCurrentBlockInfo 호출됨. 현재 조건: {visual}");
        this.currentVisualCondition = visual;
    }
    
    void Start()
    {
        if (trackedHand == null)
        {
            Debug.LogError("오류: HandPoseLogger에 SG_TrackedHand가 연결되지 않았습니다!");
            this.enabled = false;
            return;
        }

        Debug.Log("HandPoseLogger가 초기화되었습니다.");
    }

    public void StartLogging()
    {
        if (currentVisualCondition == "N/A")
        {
            Debug.Log("튜토리얼 단계에서는 손 추적 데이터를 기록하지 않습니다.");
            return;
        }
        isLogging = true;
        // logStartTime = Time.time; // 사용되지 않으므로 주석 처리

        realCsvData = new StringBuilder();
        virtualCsvData = new StringBuilder();

        string header = "Timestamp,Frame,PoseType,JointName,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW";

        realCsvData.AppendLine(header);
        virtualCsvData.AppendLine(header);

        Debug.Log($"로깅 시작, 조건: {currentVisualCondition}");
    }   

    public void StopLogging()
    {
        Debug.Log($"[HandPoseLogger] StopLogging 호출됨. isLogging: {isLogging}");
        if (!isLogging) return;
        isLogging = false;

        // 마지막 프레임 데이터가 누락되지 않도록 로깅 중지 시 한 번 더 기록
        LogCurrentFrameData(); 
        Debug.Log("[HandPoseLogger] 로깅 루프 중지됨.");
        
        // 파일 저장은 DataManager가 외부에서 SaveLogToFile(path)를 호출하여 수행
        // SaveLogToFile(); // 여기서 직접 호출하지 않음
    }
    
    void Update()
    {
        if (isLogging)
        {
            LogCurrentFrameData();
        }
    }
    
    private void LogCurrentFrameData()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        LogPoseData(timestamp, SG_TrackedHand.TrackingLevel.RealHandPose, "Real");
        LogPoseData(timestamp, SG_TrackedHand.TrackingLevel.VirtualPose, "Virtual");
    }

    private void LogPoseData(string timestamp, SG_TrackedHand.TrackingLevel level, string poseTypeName)
    {
        SG_HandPoser3D poser = trackedHand.GetPoser(level);
        if (poser == null) return;
        foreach (HandJoint jointId in Enum.GetValues(typeof(HandJoint)))
        {
            Transform jointTransform = poser.GetTransform(jointId);
            if (jointTransform != null)
            {
                Vector3 pos = jointTransform.position;
                Quaternion rot = jointTransform.rotation;
                string line = $"{timestamp},{Time.frameCount},{poseTypeName},{jointId},{pos.x:F6},{pos.y:F6},{pos.z:F6},{rot.x:F6},{rot.y:F6},{rot.z:F6},{rot.w:F6}";
                if (poseTypeName == "Real")
                    realCsvData.AppendLine(line);
                else if (poseTypeName == "Virtual")
                    virtualCsvData.AppendLine(line);
            }
        }
    }

    public void SaveLogToFile(string sessionFolderPath)
    {

        if (realCsvData != null && realCsvData.Length > realCsvData.ToString().Split('\n')[0].Length + 5) // 헤더만 있는지 체크
        {
            string realFilePath = Path.Combine(sessionFolderPath, "Real.csv");
            try
            {
                File.WriteAllText(realFilePath, realCsvData.ToString(), Encoding.UTF8);
                Debug.Log($"성공: [Real] 손 포즈 데이터가 저장되었습니다.\n{realFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"오류: [Real] 파일 저장에 실패했습니다. \n{e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[Real] 손 추적 데이터가 없어 파일을 저장하지 않습니다.");
        }


        if (virtualCsvData != null && virtualCsvData.Length > virtualCsvData.ToString().Split('\n')[0].Length + 5) // 헤더만 있는지 체크
        {
            string virtualFilePath = Path.Combine(sessionFolderPath, "Virtual.csv");
            try
            {
                File.WriteAllText(virtualFilePath, virtualCsvData.ToString(), Encoding.UTF8);
                Debug.Log($"성공: [Virtual] 손 포즈 데이터가 저장되었습니다.\n{virtualFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"오류: [Virtual] 파일 저장에 실패했습니다. \n{e.Message}");
            }
        }
        else
        {
             Debug.LogWarning("[Virtual] 손 추적 데이터가 없어 파일을 저장하지 않습니다.");
        }
        
        realCsvData?.Clear();
        virtualCsvData?.Clear();
    }
}