
using UnityEngine;
using UnityEngine.UI;

public class PartOptionItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private GameObject activeHighlight;

    private CarPart part;
    private CarConfiguratorUIController controller;

    public CarPart Part => part;

    public void Bind(
        CarPart part,
        CarConfiguratorUIController controller)
    {
        this.part = part;
        this.controller = controller;

        if (button == null)
            button = GetComponent<Button>();

        // ActiveHighlight 자동 탐색
        if (activeHighlight == null)
        {
            Transform highlightTransform =
                transform.Find("ActiveHighlight");

            if (highlightTransform != null)
                activeHighlight = highlightTransform.gameObject;
        }

        if (button == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Button 컴포넌트를 찾을 수 없습니다."
            );
            return;
        }

        button.onClick.RemoveListener(OnClicked);
        button.onClick.AddListener(OnClicked);

        SetSelected(false);
    }

    public void OnClicked()
    {
        Debug.Log($"버튼 클릭됨: {gameObject.name}");

        if (part == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 연결된 CarPart가 없습니다."
            );
            return;
        }

        if (controller != null)
            controller.OnOptionSelected(part);
    }

    public void SetSelected(bool selected)
    {
        if (activeHighlight != null)
        {
            activeHighlight.SetActive(selected);
        }
    }
}
