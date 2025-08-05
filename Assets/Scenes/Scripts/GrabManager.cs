using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;


public class GrabManager : MonoBehaviour
{
    private Dictionary<string, int> blockGrabCounts = new Dictionary<string, int>();
    private int totalGrabCount = 0;
    private string playerName;
    private string currentVisualCondition;
    public Dictionary<string, GrabInfo> lastGrabInfos = new Dictionary<string, GrabInfo>();

    #region

    public void Initialize(string pName, string dataFolderPath)
    {
        this.playerName = pName;
        SG.SG_Grabable.OnObjectGrabbed += HandleObjectGrabbed;
    }

    public void SetCurrentBlockInfo(string visualCond)
    {
        currentVisualCondition = visualCond;
    }

    #endregion

    #region

    private void HandleObjectGrabbed(SG.SG_Grabable grabable)
    {
        if (grabable.isTutorialObject) return;

        totalGrabCount++;
        string objectName = grabable.name;
        string nowTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        if (!blockGrabCounts.ContainsKey(objectName))
        {
            blockGrabCounts[objectName] = 0;
        }
        blockGrabCounts[objectName]++;

        if (!lastGrabInfos.ContainsKey(objectName))
        {
            lastGrabInfos[objectName] = new GrabInfo
            {
                objectName = objectName,
                stiffness = -1f,
                blockGrabCount = blockGrabCounts[objectName],
                totalGrabCount = totalGrabCount,
                firstGrabTimestamp = nowTimestamp,
                lastGrabTimestamp = nowTimestamp
            };
        }
        else
        {
            var info = lastGrabInfos[objectName];
            info.blockGrabCount = blockGrabCounts[objectName];
            info.totalGrabCount = totalGrabCount;
            info.lastGrabTimestamp = nowTimestamp;
            lastGrabInfos[objectName] = info;
        }

        LogGrabEvent(grabable, blockGrabCounts[objectName]);
    }

    private void LogGrabEvent(SG.SG_Grabable grabable, int blockCount)
    {
        float stiffness = -1f;
        var material = grabable.GetComponent<SG.SG_Material>();
        if (material != null && material.materialProperties != null)
        {
            stiffness = material.materialProperties.maxForce;
        }

        string objectName = grabable.name;

        if (lastGrabInfos.ContainsKey(objectName))
        {
            var info = lastGrabInfos[objectName];
            info.stiffness = stiffness;
            info.blockGrabCount = blockCount;
            info.totalGrabCount = totalGrabCount;
            lastGrabInfos[objectName] = info;
        }
        else
        {
            lastGrabInfos[objectName] = new GrabInfo
            {
                objectName = objectName,
                stiffness = stiffness,
                blockGrabCount = blockCount,
                totalGrabCount = totalGrabCount,
                firstGrabTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                lastGrabTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };
        }
    }

    #endregion

    #region

    public void ResetBlockCounts()
    {
        blockGrabCounts.Clear();
        Debug.Log("블록이 종료되어 잡기 횟수(BlockGrabCounts)를 초기화했습니다.");
    }

    #endregion

    private void OnDestroy()
    {
        SG.SG_Grabable.OnObjectGrabbed -= HandleObjectGrabbed;
    }
}

public class GrabInfo
{
    public string objectName;
    public float stiffness;
    public int blockGrabCount;
    public int totalGrabCount;
    public string firstGrabTimestamp;
    public string lastGrabTimestamp;
}