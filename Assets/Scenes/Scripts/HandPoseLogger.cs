using UnityEngine;
using SG;
using System.Text;
using System.IO;
using System;
using System.Collections.Generic;

public class HandPoseLogger : MonoBehaviour
{
    [Header("필수 연결 요소")]
    public SG_TrackedHand trackedHand;

    [Header("파일 설정")]
    public string filePrefix = "HandPoseLog_";

    private SG_GrabScript grabScript;
    private bool isLogging = false;
    private float logStartTime;
    private StringBuilder csvData;
    private string participantName = "UnknownPlayer";
    private string handPoseLogPath;

    public void SetParticipantName(string name)
    {
        this.participantName = name;
        Debug.Log($"Logger's participant name set to: {this.participantName}");
    }

    // [추가] 현재 블록 정보를 저장할 변수
    private string currentVisualCondition = "N/A";
    private string currentGrabbedObjectName = "N/A";

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

    // [수정] SetParticipantName이 폴더 경로도 함께 받도록 변경
    public void Initialize(string name, string logFolderPath)
    {
        Debug.Log($"[HandPoseLogger] Initialize 함수 호출됨. Player: {name}, Path: {logFolderPath}");

        this.participantName = name;
        this.handPoseLogPath = logFolderPath; // 경로 설정
        // [디버깅] 전달받은 경로가 비어있는지 확인합니다.
        if (string.IsNullOrEmpty(this.handPoseLogPath))
        {
            Debug.LogError("[HandPoseLogger ERROR] Initialize를 통해 전달받은 폴더 경로(logFolderPath)가 비어있습니다!");
        }
    }

    // [추가] 현재 블록 정보를 업데이트하는 함수
    public void SetCurrentBlockInfo(string visual)
    {
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

        grabScript = trackedHand.grabScript;
        if (grabScript == null)
        {
            Debug.LogError("오류: " + trackedHand.name + "에 SG_GrabScript가 연결되어 있지 않습니다.");
            this.enabled = false;
            return;
        }

        grabScript.GrabbedObject.AddListener(HandleObjectGrabbed);
        grabScript.ReleasedObject.AddListener(HandleObjectReleased);
        Debug.Log("HandPoseLogger가 초기화되었습니다. 잡기/놓기 이벤트를 기다립니다.");
    }

    private void HandleObjectGrabbed(SG_Interactable grabbedObject, SG_GrabScript script)
    {
        if (currentVisualCondition == "N/A")
        {
            Debug.Log("튜토리얼 단계에서는 손 추적 데이터를 기록하지 않습니다.");
            return;
        }

        isLogging = true;
        logStartTime = Time.time;
        csvData = new StringBuilder();
        currentGrabbedObjectName = grabbedObject.name;

        csvData.AppendLine($"PlayerName,{this.participantName}");
        csvData.AppendLine($"VisualCondition,{this.currentVisualCondition}");
        csvData.AppendLine($"GrabbedObject,{this.currentGrabbedObjectName}");
        csvData.AppendLine("Timestamp,Frame,PoseType,JointName,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW");

        Debug.Log($"로깅 시작: {currentGrabbedObjectName} / 조건: {currentVisualCondition}");
    }

    private void HandleObjectReleased(SG_Interactable releasedObject, SG_GrabScript script)
    {
        if (!isLogging) return;
        isLogging = false;
        Debug.Log($"로깅 중지: {releasedObject.name}을(를) 놓았습니다. 파일 저장 중...");
        SaveLogToFile();
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
        float timestamp = Time.time - logStartTime;
        LogPoseData(timestamp, SG_TrackedHand.TrackingLevel.RealHandPose, "Real");
        LogPoseData(timestamp, SG_TrackedHand.TrackingLevel.VirtualPose, "Virtual");
        LogPoseData(timestamp, SG_TrackedHand.TrackingLevel.RenderPose, "Render");
    }

    private void LogPoseData(float timestamp, SG_TrackedHand.TrackingLevel level, string poseTypeName)
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
                string line = $"{timestamp:F4},{Time.frameCount},{poseTypeName},{jointId},{pos.x:F6},{pos.y:F6},{pos.z:F6},{rot.x:F6},{rot.y:F6},{rot.z:F6},{rot.w:F6}";
                csvData.AppendLine(line);
            }
        }
    }

    private void SaveLogToFile()
    {
        if (csvData == null || csvData.Length == 0) return;

        string timeStampForFile = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        // 파일 이름에 유효하지 않은 문자(예: "(Clone)") 제거
        string cleanObjectName = currentGrabbedObjectName.Replace("(Clone)", "").Trim();
        string fileName = $"{participantName}_{currentVisualCondition}_{cleanObjectName}_{timeStampForFile}.csv";

        // 전달받은 handPoseLogPath 사용
        string filePath = Path.Combine(handPoseLogPath, fileName);

        try
        {
            File.WriteAllText(filePath, csvData.ToString(), Encoding.UTF8);
            Debug.Log($"성공: 손 포즈 데이터가 다음 경로에 저장되었습니다. \n{filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"오류: 파일 저장에 실패했습니다. \n{e.Message}");
        }
    }

    void OnDisable()
    {
        if (grabScript != null)
        {
            // [수정] 올바른 이벤트 이름으로 변경
            grabScript.GrabbedObject.RemoveListener(HandleObjectGrabbed);
            grabScript.ReleasedObject.RemoveListener(HandleObjectReleased); // ObjectReleased -> ReleasedObject
        }
    }
}