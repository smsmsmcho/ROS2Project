using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AI 추천 탭의 채팅 UI 뷰 컴포넌트.
/// 사용자 말풍선(좌측 정렬)과 AI 말풍선(우측 정렬)을 동적 생성하고,
/// 상위 CarConfiguratorUIController와 이벤트 및 메서드로 통신합니다.
/// </summary>
[DisallowMultipleComponent]
public class AiChatUIController : MonoBehaviour
{
    [Header("채팅 UI 참조")]
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private TMP_InputField promptInputField;
    [SerializeField] private Button sendButton;

    [Header("말풍선 프리팹 (또는 비활성화된 템플릿 오브젝트)")]
    [SerializeField] private GameObject userBubblePrefab;
    [SerializeField] private GameObject aiBubblePrefab;

    [Header("말풍선 및 안내 설정")]
    [Tooltip("말풍선 텍스트의 최대 가로폭(px). 이 폭을 넘으면 자동 줄바꿈됩니다.")]
    [SerializeField, Min(80f)] private float maxBubbleWidth = 210f;
    [SerializeField] private Color errorTextColor = new Color(1f, 0.55f, 0.5f, 1f);
    [SerializeField, TextArea(2, 4)] private string welcomeMessage =
        "안녕하세요! 원하시는 분위기나 용도를 입력하시면 어울리는 색상·휠·액세서리 조합을 추천해 드립니다.";

    /// <summary>
    /// 사용자가 프롬프트를 입력하고 전송했을 때 상위 UIController에 알리는 이벤트
    /// </summary>
    public event Action<string> OnPromptSubmitted;

    private RectTransform contentRoot;
    private TMP_Text pendingAiBubbleText;
    private bool isWaitingForResponse;

    private void Awake()
    {
        if (chatScrollRect != null)
        {
            contentRoot = chatScrollRect.content;
        }

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendClicked);
        }

        if (promptInputField != null)
        {
            promptInputField.onSubmit.AddListener(OnInputSubmitted);
        }
    }

    private void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendClicked);
        }

        if (promptInputField != null)
        {
            promptInputField.onSubmit.RemoveListener(OnInputSubmitted);
        }
    }

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(welcomeMessage) && aiBubblePrefab != null)
        {
            CreateBubble(aiBubblePrefab, welcomeMessage, isAi: true);
        }
    }

    private void OnInputSubmitted(string _)
    {
        OnSendClicked();
    }

    public void OnSendClicked()
    {
        if (isWaitingForResponse || promptInputField == null) return;

        string prompt = promptInputField.text?.Trim();
        if (string.IsNullOrEmpty(prompt)) return;

        promptInputField.text = string.Empty;
        CreateBubble(userBubblePrefab, prompt, isAi: false);

        if (OnPromptSubmitted == null)
        {
            ShowErrorMessage("CarConfiguratorUIController가 연결되어 있지 않습니다.");
            return;
        }

        SetInteractable(false);
        pendingAiBubbleText = CreateBubble(aiBubblePrefab, "추천 조합을 분석하고 있습니다...", isAi: true);
        OnPromptSubmitted.Invoke(prompt);
    }

    /// <summary>
    /// 상위 UIController로부터 정상 AI 추천 응답 메시지를 전달받아 말풍선에 표시합니다.
    /// </summary>
    public void ShowAiResponse(string message)
    {
        if (pendingAiBubbleText != null)
        {
            UpdateBubbleText(pendingAiBubbleText, message);
            pendingAiBubbleText = null;
        }
        else
        {
            CreateBubble(aiBubblePrefab, message, isAi: true);
        }

        SetInteractable(true);
        if (promptInputField != null)
        {
            promptInputField.ActivateInputField();
        }
    }

    /// <summary>
    /// 상위 UIController로부터 오류 메시지를 전달받아 강조 색상으로 말풍선에 표시합니다.
    /// </summary>
    public void ShowErrorMessage(string errorMessage)
    {
        TMP_Text targetText = pendingAiBubbleText;
        pendingAiBubbleText = null;

        if (targetText != null)
        {
            UpdateBubbleText(targetText, errorMessage);
        }
        else
        {
            targetText = CreateBubble(aiBubblePrefab, errorMessage, isAi: true);
        }

        if (targetText != null)
        {
            targetText.color = errorTextColor;
        }

        SetInteractable(true);
        if (promptInputField != null)
        {
            promptInputField.ActivateInputField();
        }
    }

    private TMP_Text CreateBubble(GameObject prefab, string message, bool isAi)
    {
        if (prefab == null || contentRoot == null) return null;

        GameObject bubbleRow = Instantiate(prefab, contentRoot);
        bubbleRow.SetActive(true);

        if (bubbleRow.TryGetComponent(out HorizontalLayoutGroup rowLayout))
        {
            rowLayout.childAlignment = isAi ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
        }

        TMP_Text messageText = bubbleRow.GetComponentInChildren<TMP_Text>(true);
        if (messageText != null)
        {
            UpdateBubbleText(messageText, message);
        }

        return messageText;
    }

    private void UpdateBubbleText(TMP_Text messageText, string message)
    {
        messageText.textWrappingMode = TextWrappingModes.Normal;
        messageText.text = message;

        if (!messageText.TryGetComponent(out LayoutElement layoutElement))
        {
            layoutElement = messageText.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = -1f;
        Vector2 unconstrainedValues = messageText.GetPreferredValues(message);

        if (unconstrainedValues.x > maxBubbleWidth)
        {
            // 최대 가로폭에서 줄바꿈한 뒤 실제 가장 긴 줄의 렌더링 폭(renderedWidth)으로 맞춰 우측 빈 여백 제거
            messageText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxBubbleWidth);
            messageText.ForceMeshUpdate();
            float tightWrappedWidth = Mathf.Ceil(messageText.renderedWidth);
            layoutElement.preferredWidth = Mathf.Clamp(tightWrappedWidth, 40f, maxBubbleWidth);
        }

        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (chatScrollRect == null) return;

        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }

    private void SetInteractable(bool interactable)
    {
        isWaitingForResponse = !interactable;
        if (sendButton != null) sendButton.interactable = interactable;
        if (promptInputField != null) promptInputField.interactable = interactable;
    }
}
