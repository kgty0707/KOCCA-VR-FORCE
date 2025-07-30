using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 물체 잡기(Grab) 이벤트와 관련된 데이터 및 로그를 중앙에서 관리합니다.
/// </summary>
public class GrabManager : MonoBehaviour
{
    // --- 내부 변수 ---
    private Dictionary<string, int> blockGrabCounts = new Dictionary<string, int>();
    private int totalGrabCount = 0;

    private string playerName;
    private string currentVisualCondition;
    private string logFilePath;
    private StringBuilder logBuilder = new StringBuilder();


    #region --- 초기화 및 설정 ---

    // [수정] Initialize 메서드가 데이터 폴더 경로를 받도록 변경
    public void Initialize(string pName, string dataFolderPath)
    {
        this.playerName = pName;
        // [수정] 전달받은 경로로 로그 파일 설정
        SetupLogFile(dataFolderPath);

        // 이벤트 구독
        SG.SG_Grabable.OnObjectGrabbed += HandleObjectGrabbed;
    }


    /// <summary>
    /// 로그 파일의 경로와 헤더를 설정합니다.
    /// </summary>
    // [수정] SetupLogFile 메서드가 경로를 인자로 받도록 변경
    private void SetupLogFile(string dataFolderPath)
    {
        string fileTimestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{playerName}_GrabLog_{fileTimestamp}.csv";
        logFilePath = Path.Combine(dataFolderPath, fileName);

        // [수정] CSV 헤더에 블록 정보 추가
        logBuilder.AppendLine("Timestamp,PlayerName,VisualCondition,ObjectName,Stiffness,BlockGrabCount,TotalGrabCount");
        File.WriteAllText(logFilePath, logBuilder.ToString(), Encoding.UTF8);
        logBuilder.Clear();
    }

    /// <summary>
    /// 현재 블록의 정보를 업데이트합니다.
    /// </summary>
    public void SetCurrentBlockInfo(string visualCond)
    {
        currentVisualCondition = visualCond;
    }

    #endregion


    #region --- 이벤트 핸들링 및 로깅 ---

    /// <summary>
    /// SG_Grabable이 잡혔을 때 호출되는 이벤트 핸들러입니다.
    /// </summary>
    private void HandleObjectGrabbed(SG.SG_Grabable grabable)
    {
        if (grabable.isTutorialObject) return;

        totalGrabCount++;
        string objectName = grabable.name;
        if (!blockGrabCounts.ContainsKey(objectName))
        {
            blockGrabCounts[objectName] = 0;
        }
        blockGrabCounts[objectName]++;

        LogGrabEvent(grabable, blockGrabCounts[objectName]);
    }
    /// <summary>
    /// 잡기 이벤트 정보를 CSV 파일에 기록합니다.
    /// </summary>
    private void LogGrabEvent(SG.SG_Grabable grabable, int blockCount)
    {
        float stiffness = -1f;
        var material = grabable.GetComponent<SG.SG_Material>();
        if (material != null && material.materialProperties != null)
        {
            stiffness = material.materialProperties.maxForce;
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        // [수정] 로그 라인에 블록 정보(VisualCondition) 추가
        logBuilder.Append(timestamp).Append(",");
        logBuilder.Append(playerName).Append(",");
        logBuilder.Append(currentVisualCondition).Append(","); // 블록 정보
        logBuilder.Append(grabable.name).Append(",");
        logBuilder.Append(stiffness).Append(",");
        logBuilder.Append(blockCount).Append(",");
        logBuilder.Append(totalGrabCount).AppendLine();

        File.AppendAllText(logFilePath, logBuilder.ToString(), Encoding.UTF8);
        logBuilder.Clear();
    }

    #endregion


    #region --- 제어 함수 ---

    /// <summary>
    /// 새로운 블록이 시작될 때 카운트를 초기화합니다.
    /// </summary>
    public void ResetBlockCounts()
    {
        blockGrabCounts.Clear();
        Debug.Log("블록이 종료되어 잡기 횟수(BlockGrabCounts)를 초기화했습니다.");
    }

    #endregion


    /// <summary>
    /// 오브젝트가 파괴될 때 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDestroy()
    {
        SG.SG_Grabable.OnObjectGrabbed -= HandleObjectGrabbed;
    }
}