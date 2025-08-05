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
    [Tooltip("튜토리얼용 확인 버튼 오브젝트")]
    public GameObject confirmButton;
    [Tooltip("자신감 수준을 선택하는 UI 패널")]
    public GameObject confidencePanel;

    void Start()
    {
        if (instructionPanel != null || confidencePanel != null)
        {
            instructionPanel.SetActive(false);
            confidencePanel.SetActive(false);
        }
        HideAllButtons();
    }

    public void ShowTutorialButtons()
    {
        if (confirmButton != null) confirmButton.SetActive(true);
    }

    public void HideAllButtons()
    {
        if (confirmButton != null) confirmButton.SetActive(false);
    }

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

    public void ShowConfidencePanel()
    {
        if (confidencePanel != null)
        {
            confidencePanel.SetActive(true);
        }
    }

    public void HideConfidencePanel()
    {
        StartCoroutine(HidePanelAfterDelay(0.2f)); 
    }

    private IEnumerator HidePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (confidencePanel != null)
        {
            confidencePanel.SetActive(false);
        }
    }
}
