using UnityEngine;

public class ConveyorBeltSolid : MonoBehaviour
{
    // [수정] speed 변수를 private으로 변경 (더 이상 인스펙터에서 설정 안 함)
    private float speed = 1f;

    private Renderer beltRenderer;
    private bool isMoving = true;
    private Rigidbody selfRigidbody;

    void Start()
    {
        beltRenderer = GetComponent<Renderer>();
        selfRigidbody = GetComponent<Rigidbody>();
    }

    // [수정] isMoving과 함께 newSpeed 값을 받도록 함수 변경
    public void SetMoving(bool status, float newSpeed)
    {
        if (isMoving == false && status == true)
        {
            if (selfRigidbody != null)
            {
                Vector3 currentPos = selfRigidbody.position;
                selfRigidbody.position += Vector3.up * 0.001f;
                selfRigidbody.position = currentPos;
            }
        }

        isMoving = status;
        speed = newSpeed; // 전달받은 속도로 업데이트
    }

    // --- 나머지 코드는 그대로 유지 ---
    void Update()
    {
        if (isMoving)
        {
            float offset = Time.time * speed;
            if (beltRenderer.materials.Length > 1)
            {
                beltRenderer.materials[1].mainTextureOffset = new Vector2(offset, 0);
            }
        }
    }

    void OnCollisionStay(Collision other)
    {
        if (isMoving)
        {
            Rigidbody otherRigidbody = other.gameObject.GetComponent<Rigidbody>();
            if (otherRigidbody != null)
            {
                // speed 변수는 이제 Manager가 설정해준 값을 사용
                Vector3 moveDirection = transform.right * speed * 5f;
                otherRigidbody.velocity = new Vector3(moveDirection.x, otherRigidbody.velocity.y, moveDirection.z);
            }
        }
        else
        {
            Rigidbody otherRigidbody = other.gameObject.GetComponent<Rigidbody>();
            if (otherRigidbody != null)
            {
                otherRigidbody.velocity = new Vector3(0, otherRigidbody.velocity.y, 0);
            }
        }
    }

    void OnCollisionExit(Collision other)
    {
        Rigidbody otherRigidbody = other.gameObject.GetComponent<Rigidbody>();
        if (otherRigidbody != null)
        {
            otherRigidbody.velocity = new Vector3(0, otherRigidbody.velocity.y, 0);
        }
    }
}
