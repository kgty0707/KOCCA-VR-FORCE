using UnityEngine;

public class NormalizeBallsInScene : MonoBehaviour
{
    [Header("정규화 기준 반지름(월드 단위)")]
    public float targetRadius = 0.5f; // 원하는 반지름

    [Header("대상 태그 (없으면 모든 Mesh 처리)")]
    public string targetTag = "Ball"; // 태그로 필터, 공에 "Ball" 태그를 붙이면 그 오브젝트만

    [ContextMenu("Normalize All Balls")]
    void NormalizeAllBalls()
    {
        // 태그로 찾거나, 전체 MeshFilter 오브젝트 찾기
        GameObject[] objects = 
            string.IsNullOrEmpty(targetTag) ?
            FindObjectsOfType<GameObject>() :
            GameObject.FindGameObjectsWithTag(targetTag);

        int count = 0;
        foreach (var go in objects)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            // 메시 바운드 기준 최대 크기 구하기
            var bounds = mf.sharedMesh.bounds;
            float meshMax = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            if (meshMax > 0f)
            {
                float scale = (targetRadius * 2f) / meshMax; // 2x = diameter
                go.transform.localScale = Vector3.one * scale;
                count++;
            }
        }
        Debug.Log($"{count}개 오브젝트(공)가 반지름 {targetRadius}로 정규화됨!");
    }
}
