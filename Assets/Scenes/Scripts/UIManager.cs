using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI; // Button을 사용하기 위해 이 줄을 추가해야 합니다.

public class UIManager : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public GameObject instructionPanel;
    public TextMeshProUGUI instructionText;

    // --- [추가] 버튼 패널 연결 ---
    [Header("버튼 패널 연결")]
    [Tooltip("1, 2, 3번 버튼들의 부모 오브젝트")]
    public GameObject numberButtonsPanel;
    [Tooltip("튜토리얼용 확인 버튼 오브젝트")]
    public GameObject confirmButton;

    void Start()
    {
        // 시작 시 모든 UI를 숨김
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
        // --- [추가] ---
        HideAllButtons();
    }

    // --- [추가] 버튼 제어 메서드들 ---

    /// <summary>
    /// 튜토리얼 상태에 맞는 버튼들(확인 버튼)만 표시합니다.
    /// </summary>
    public void ShowTutorialButtons()
    {
        if (numberButtonsPanel != null) numberButtonsPanel.SetActive(false);
        if (confirmButton != null) confirmButton.SetActive(true);
    }

    /// <summary>
    /// 메인 게임 상태에 맞는 버튼들(숫자 버튼)만 표시합니다.
    /// </summary>
    public void ShowMainGameButtons()
    {
        if (numberButtonsPanel != null) numberButtonsPanel.SetActive(true);
        if (confirmButton != null) confirmButton.SetActive(false);
    }

    /// <summary>
    /// 모든 인터랙션 버튼을 숨깁니다.
    /// </summary>
    public void HideAllButtons()
    {
        if (numberButtonsPanel != null) numberButtonsPanel.SetActive(false);
        if (confirmButton != null) confirmButton.SetActive(false);
    }

    // 튜토리얼 전용 UI 표시 함수
    public void ShowTutorialScreen(string message)
    {
        if (instructionPanel == null) return;
        instructionPanel.SetActive(true);
        instructionText.text = message;
        if (confirmButton != null) confirmButton.gameObject.SetActive(true);
    }

    public void ShowInstruction(string message)
    {
        if (instructionPanel == null) return;
        instructionPanel.SetActive(true);
        instructionText.text = message;
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
    }

    public void HideInstruction()
    {
        if (instructionPanel == null) return;
        instructionPanel.SetActive(false);
    }

    public void ShowBreakScreen(float duration)
    {
        if (instructionPanel == null) return;
        StartCoroutine(BreakRoutine(duration));
    }

    private IEnumerator BreakRoutine(float duration)
    {
        instructionPanel.SetActive(true);
        float timer = duration;
        while (timer > 0)
        {
            instructionText.text = $"휴식 시간입니다.\n{Mathf.CeilToInt(timer)}초 후 다음 블록이 시작됩니다.";
            timer -= Time.deltaTime;
            yield return null;
        }
        instructionPanel.SetActive(false);
    }

    public void ShowEndOfExperimentScreen()
    {
        if (instructionPanel == null) return;
        instructionPanel.SetActive(true);
        instructionText.text = "실험이 모두 종료되었습니다.\n수고하셨습니다.";
    }
}
