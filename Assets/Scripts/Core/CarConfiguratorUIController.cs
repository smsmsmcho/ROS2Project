
using System.Collections.Generic;
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

    private System.Collections.IEnumerator Start()
    {
        if (manager == null)
        {
            Debug.LogError(
                "CarConfiguratorManager가 연결되지 않았습니다."
            );
            yield break;
        }

        // 매니저의 데이터 초기화가 끝날 때까지 대기
        yield return new WaitUntil(() => manager.IsInitialized);

        // 초기화가 끝난 후 색상 옵션 표시
        OnCategorySelected(PartCategory.Color);
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

        manager.ApplyPart(part.id);
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
            return;

        AiRecommendationData data =
            await manager.RequestRecommendationAsync(prompt);

        ShowAiRecommendation(data);
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

        // 실제 추천 적용은 별도 UI에서 사용자가 결정하도록 둡니다.
    }

    public void OnConfigUpdated(EstimateData data)
    {
        if (data == null)
            return;

        Debug.Log($"현재 견적: {data.totalPrice:N0}원");
    }

    private void OnEnable()
    {
        if (manager != null)
            manager.OnConfigUpdated += OnConfigUpdated;
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.OnConfigUpdated -= OnConfigUpdated;
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
}
