using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CarConfiguratorUIController : MonoBehaviour
{
    [Header("카테고리 패널")]
    [SerializeField] private GameObject colorPanel;
    [SerializeField] private GameObject wheelPanel;
    [SerializeField] private GameObject accessoryPanel;
    [SerializeField] private GameObject aiPanel;

    [Header("Manager")]
    [SerializeField]
    private CarConfiguratorManager manager;

    [Header("견적 및 AI 채팅 UI (비워두면 자동 탐색)")]
    [SerializeField]
    private TMP_Text priceTotalText;

    [SerializeField]
    private AiChatUIController aiChatUI;

    [Header("액세서리 옵션 버튼")]
    [SerializeField]
    private List<PartOptionItem> accessoryOptionItems
        = new List<PartOptionItem>();

    [Header("휠 옵션 버튼")]
    [SerializeField]
    private List<PartOptionItem> wheelOptionItems
        = new List<PartOptionItem>();

    [Header("색상 옵션 버튼")]
    [SerializeField]
    private List<PartOptionItem> colorOptionItems
        = new List<PartOptionItem>();

    private PartCategory currentCategory;

    // 현재 UI에서 선택된 부품 ID
    private readonly HashSet<string> selectedPartIds
        = new HashSet<string>();


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureControllerInitializedOnPlay()
    {
        CarConfiguratorUIController controller =
            FindAnyObjectByType<CarConfiguratorUIController>(FindObjectsInactive.Include);

        if (controller != null)
        {
            if (!controller.enabled)
            {
                controller.enabled = true;
            }

            controller.BindEvents();
        }
    }

    private void Awake()
    {
        ResolveReferences();

        if (!enabled)
        {
            enabled = true;
        }
    }

    private void ResolveReferences()
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<CarConfiguratorManager>(FindObjectsInactive.Include);
        }

        if (aiPanel != null)
        {
            AiChatUIController panelChatUI = aiPanel.GetComponentInChildren<AiChatUIController>(true);
            if (panelChatUI != null)
            {
                aiChatUI = panelChatUI;
            }
        }

        if (aiChatUI == null)
        {
            aiChatUI = FindAnyObjectByType<AiChatUIController>(FindObjectsInactive.Include);
        }

        if (aiPanel == null && aiChatUI != null)
        {
            aiPanel = aiChatUI.gameObject;
        }

        if (priceTotalText == null)
        {
            foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            {
                if (text.name == "PriceTotal")
                {
                    priceTotalText = text;
                    break;
                }
            }
        }
    }

    private void BindEvents()
    {
        ResolveReferences();

        if (manager != null)
        {
            manager.OnConfigUpdated -= OnConfigUpdated;
            manager.OnConfigUpdated += OnConfigUpdated;
            manager.OnRecommendationFailed -= OnRecommendationFailed;
            manager.OnRecommendationFailed += OnRecommendationFailed;
        }

        foreach (AiChatUIController chatUI in FindObjectsByType<AiChatUIController>(FindObjectsInactive.Include))
        {
            if (chatUI.gameObject.activeInHierarchy || aiChatUI == null)
            {
                aiChatUI = chatUI;
            }

            chatUI.OnPromptSubmitted -= OnPromptSubmitted;
            chatUI.OnPromptSubmitted += OnPromptSubmitted;
        }
    }

    private System.Collections.IEnumerator Start()
    {
        BindEvents();

        if (manager == null)
        {
            Debug.LogError(
                "CarConfiguratorManager가 연결되지 않았습니다."
            );
            yield break;
        }

        // 매니저의 데이터 초기화가 끝날 때까지 대기
        yield return new WaitUntil(() => manager.IsInitialized);

        // 초기화가 끝난 후 기본 색상 패널 활성화, 옵션 바인딩 및 초기 견적 표시
        ShowPanel(colorPanel);
        OnCategorySelected(PartCategory.Color);
        OnConfigUpdated(manager.CalculateEstimate());
    }

    public void OnCategorySelected(PartCategory category)
    {
        if (manager == null)
            return;

        currentCategory = category;
        ShowOptions(manager.GetOptions(category));
    }

    private void ShowPanel(GameObject targetPanel)
    {
        if (colorPanel != null)
            colorPanel.SetActive(targetPanel == colorPanel);

        if (wheelPanel != null)
            wheelPanel.SetActive(targetPanel == wheelPanel);

        if (accessoryPanel != null)
            accessoryPanel.SetActive(targetPanel == accessoryPanel);

        if (aiPanel != null)
            aiPanel.SetActive(targetPanel == aiPanel);
    }


    public void OnOptionSelected(CarPart part)
    {
        if (manager == null || part == null)
            return;

        List<PartOptionItem> items = GetOptionItems(part.category);

        if (part.category == PartCategory.Accessory)
        {
            // 액세서리는 각각 독립적으로 선택/해제
            if (!selectedPartIds.Add(part.id))
                selectedPartIds.Remove(part.id);
        }
        else
        {
            // 색상과 휠은 같은 카테고리에서 하나만 선택
            foreach (PartOptionItem item in items)
            {
                if (item == null || item.Part == null)
                    continue;

                selectedPartIds.Remove(item.Part.id);
            }

            selectedPartIds.Add(part.id);
        }

        // 기존 차량 부품 적용 기능 유지
        manager.ApplyPart(part.id);

        // UI 하이라이트 갱신
        RefreshHighlights(part.category);
    }


    private void RefreshHighlights(PartCategory category)
    {
        List<PartOptionItem> items = GetOptionItems(category);

        foreach (PartOptionItem item in items)
        {
            if (item == null || item.Part == null)
                continue;

            bool isSelected = selectedPartIds.Contains(item.Part.id);

            item.SetSelected(isSelected);
        }
    }



    public void ShowOptions(List<CarPart> options)
    {

        List<PartOptionItem> items = GetOptionItems(
            currentCategory
        );
        if (options == null)
            options = new List<CarPart>();

        Debug.Log($"현재 카테고리: {currentCategory}");
        Debug.Log($"버튼 수: {items.Count}, 옵션 데이터 수: {options.Count}");
        for (int i = 0; i < items.Count; i++)
        {
            bool hasOption = i < options.Count;

            items[i].gameObject.SetActive(hasOption);

            if (hasOption)
            {
                Debug.Log(items[i].gameObject.name);

                    items[i].Bind(options[i], this);
            }

        }


        // 옵션 바인딩 완료 후 현재 선택 상태 복원
        RefreshHighlights(currentCategory);

        if (options.Count > items.Count)
        {
            Debug.LogWarning(
                $"{currentCategory}: 데이터 옵션 수({options.Count})가 " +
                $"버튼 수({items.Count})보다 많습니다."
            );
        }


        if (options.Count > items.Count)
        {
            Debug.LogWarning(
                $"{currentCategory}: 데이터 옵션 수({options.Count})가 " +
                $"버튼 수({items.Count})보다 많습니다."
            );
        }
    }

    private List<PartOptionItem> GetOptionItems(
        PartCategory category)
    {
        switch (category)
        {
            case PartCategory.Accessory:
                return accessoryOptionItems;

            case PartCategory.Wheel:
                return wheelOptionItems;

            case PartCategory.Color:
                return colorOptionItems;

            default:
                return new List<PartOptionItem>();
        }
    }

    public void OnPromptSubmitted(string prompt)
    {
        RequestRecommendation(prompt);
    }

    private async void RequestRecommendation(string prompt)
    {
        if (manager == null)
        {
            OnRecommendationFailed("CarConfiguratorManager가 연결되지 않았습니다.");
            return;
        }

        AiRecommendationData data =
            await manager.RequestRecommendationAsync(prompt);

        if (data != null)
        {
            ShowAiRecommendation(data);
        }
    }

    public void ShowAiRecommendation(AiRecommendationData data)
    {
        if (data == null)
        {
            Debug.LogWarning("AI 추천 결과가 없습니다.");
            return;
        }

        Debug.Log(
            $"AI 추천: {data.conceptTitle}\n{data.reasoning}"
        );

        string reply = !string.IsNullOrWhiteSpace(data.reasoning)
            ? (!string.IsNullOrWhiteSpace(data.conceptTitle)
                ? $"[{data.conceptTitle}]\n{data.reasoning}"
                : data.reasoning)
            : "추천 구성이 차량에 적용되었습니다.";

        bool notified = false;
        foreach (AiChatUIController chatUI in FindObjectsByType<AiChatUIController>(FindObjectsInactive.Exclude))
        {
            chatUI.ShowAiResponse(reply);
            notified = true;
        }

        if (!notified)
        {
            aiChatUI?.ShowAiResponse(reply);
        }

        if (manager != null && data.partIds != null && data.partIds.Count > 0)
        {
            manager.ApplyAiRecommendation(data);
        }
    }

    private void OnRecommendationFailed(string errorMessage)
    {
        bool notified = false;
        foreach (AiChatUIController chatUI in FindObjectsByType<AiChatUIController>(FindObjectsInactive.Exclude))
        {
            chatUI.ShowErrorMessage(errorMessage);
            notified = true;
        }

        if (!notified)
        {
            aiChatUI?.ShowErrorMessage(errorMessage);
        }
    }

    public void OnConfigUpdated(EstimateData data)
    {
        if (data == null)
            return;

        if (priceTotalText != null)
        {
            priceTotalText.text = $"Total: {data.totalPrice:N0}원";
        }

        Debug.Log($"현재 견적: {data.totalPrice:N0}원");
    }

    private void OnEnable()
    {
        BindEvents();
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.OnConfigUpdated -= OnConfigUpdated;
            manager.OnRecommendationFailed -= OnRecommendationFailed;
        }

        if (aiChatUI != null)
        {
            aiChatUI.OnPromptSubmitted -= OnPromptSubmitted;
        }
    }

    // Unity Button의 OnClick 이벤트에 연결하기 위한 함수들
    public void OnAccessoryCategoryClicked()
    {
        ShowPanel(accessoryPanel);
        OnCategorySelected(PartCategory.Accessory);
    }

    public void OnWheelCategoryClicked()
    {
        ShowPanel(wheelPanel);
        OnCategorySelected(PartCategory.Wheel);
    }

    public void OnColorCategoryClicked()
    {
        ShowPanel(colorPanel);
        OnCategorySelected(PartCategory.Color);
    }

    public void OnAiCategoryClicked()
    {
        if (!enabled)
        {
            enabled = true;
        }

        ShowPanel(aiPanel);
        BindEvents();
    }
}
