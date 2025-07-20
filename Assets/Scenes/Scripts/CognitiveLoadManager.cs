using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 단어-숫자 규칙을 위한 데이터 구조 (이전과 동일)
[System.Serializable]
public struct WordRule
{
    public string wordName;
    public AudioClip wordClip;
    public int correctNumber;
}

public class CognitiveLoadManager : MonoBehaviour
{
    [Header("--- 규칙 설정 ---")]
    [Tooltip("저부하 조건일 때 사용할 규칙 리스트")]
    public List<WordRule> lowLoadRules;
    [Tooltip("고부하 조건일 때 사용할 규칙 리스트")]
    public List<WordRule> highLoadRules;

    [Header("--- 과제 밀도 설정 ---")]
    [Tooltip("저부하 조건일 때 1초당 단어가 나타날 확률 (예: 0.1은 10%)")]
    [Range(0f, 1f)]
    public float lowLoadChancePerSecond = 0.1f; // 10초에 한번 꼴
    [Tooltip("고부하 조건일 때 1초당 단어가 나타날 확률 (예: 0.2는 20%)")]
    [Range(0f, 1f)]
    public float highLoadChancePerSecond = 0.2f; // 5초에 한번 꼴

    [Header("--- 필수 연결 요소 ---")]
    public AudioSource audioSource;
    public DataManager dataManager;

    // --- 내부 변수 ---
    private List<WordRule> currentRules;
    private float currentChance;
    private WordRule currentTask;
    private float taskStartTime;
    private bool isWaitingForInput = false;
    private Coroutine presentWordCoroutine;

    // ExperimentManager가 블록 시작 시 호출
    public void StartBlock(CognitiveLoadCondition loadCondition)
    {
        // 현재 블록의 규칙과 확률 설정
        if (loadCondition == CognitiveLoadCondition.Low)
        {
            currentRules = lowLoadRules;
            currentChance = lowLoadChancePerSecond;
        }
        else
        {
            currentRules = highLoadRules;
            currentChance = highLoadChancePerSecond;
        }

        // 이전에 실행 중인 코루틴이 있다면 중지하고 새로 시작
        if (presentWordCoroutine != null)
        {
            StopCoroutine(presentWordCoroutine);
        }
        presentWordCoroutine = StartCoroutine(PresentWordRoutine());
    }

    // ExperimentManager가 블록 종료/휴식 시 호출
    public void StopBlock()
    {
        if (presentWordCoroutine != null)
        {
            StopCoroutine(presentWordCoroutine);
            presentWordCoroutine = null;
        }
        isWaitingForInput = false;
    }

    // 가상 터치패널의 버튼이 눌렸을 때 호출될 함수
    public void OnPanelButtonPressed(int number)
    {
        if (!isWaitingForInput) return; // 기다리는 중이 아니면 무시

        float responseTime = Time.time - taskStartTime;
        bool isCorrect = (number == currentTask.correctNumber);

        Debug.Log($"부가 과제 응답: {number}, 정답: {currentTask.correctNumber}, 정답여부: {isCorrect}, 반응시간: {responseTime:F2}s");
        if (dataManager != null)
        {
            dataManager.RecordCognitiveTask(isCorrect, responseTime);
        }
        isWaitingForInput = false;
    }

    // 확률적으로 단어를 출제하는 코루틴
    private IEnumerator PresentWordRoutine()
    {
        // 블록이 실행되는 동안 계속 반복
        while (true)
        {
            // 아직 이전 문제에 대한 답을 기다리는 중이면 출제하지 않음
            if (!isWaitingForInput)
            {
                // 매초 정해진 확률을 체크
                if (Random.value < currentChance)
                {
                    // 문제 출제
                    isWaitingForInput = true;
                    currentTask = currentRules[Random.Range(0, currentRules.Count)];
                    taskStartTime = Time.time;
                    audioSource.PlayOneShot(currentTask.wordClip);
                    Debug.Log($"부가 과제 출제: '{currentTask.wordName}', 정답: {currentTask.correctNumber}");
                }
            }
            // 1초마다 확률을 체크
            yield return new WaitForSeconds(1f);
        }
    }
}
