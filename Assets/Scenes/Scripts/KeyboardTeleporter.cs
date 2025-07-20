using UnityEngine;

public class KeyboardTeleporter_Corrected : MonoBehaviour
{
    [Header("연결할 오브젝트")]
    // 플레이어 Rig (XR Rig)
    public Transform playerTransform;

    // 플레이어 카메라 (XR Rig 하위의 Main Camera)
    public Transform playerCamera;

    // 이동할 목표 지점
    public Transform teleportTarget;

    void Update()
    {
        // 'R' 키를 누르는 순간을 감지
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 모든 오브젝트가 할당되었는지 확인
            if (playerTransform != null && playerCamera != null && teleportTarget != null)
            {
                // --- 위치 이동 ---
                // 위치는 목표 지점의 위치로 그대로 이동합니다.
                playerTransform.position = teleportTarget.position;

                // --- 방향 보정 ---
                // 1. 목표 지점이 바라보는 방향 (Y축 기준)
                float targetYaw = teleportTarget.eulerAngles.y;

                // 2. 플레이어 카메라(HMD)가 몸(XR Rig)을 기준으로 얼마나 돌아가 있는지 (Y축 기준)
                float cameraYaw = playerCamera.localEulerAngles.y;

                // 3. 플레이어의 몸(XR Rig)을 (목표 방향 - HMD가 돌아간 각도) 만큼 회전시켜서
                //    최종적으로 플레이어의 시선이 목표 방향을 향하도록 만듭니다.
                playerTransform.rotation = Quaternion.Euler(0, targetYaw - cameraYaw, 0);

                Debug.Log(teleportTarget.name + " 위치로 시점을 보정하여 텔레포트했습니다.");
            }
            else
            {
                Debug.LogWarning("Player Transform, Player Camera, 또는 Teleport Target이 설정되지 않았습니다.");
            }
        }
    }
}