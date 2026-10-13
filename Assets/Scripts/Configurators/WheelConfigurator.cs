using UnityEngine;

public class WheelConfigurator : MonoBehaviour
{
    [Header("타이어 모델 부모")]
    [SerializeField] private Transform tireParent;

    public void SelectTire(int index)
    {
        if (tireParent == null)
        {
            Debug.LogWarning("Lab_Tires_MD가 연결되지 않았습니다.");
            return;
        }

        if (index < 0 || index >= tireParent.childCount)
        {
            Debug.LogWarning("잘못된 타이어 인덱스입니다.");
            return;
        }

        for (int i = 0; i < tireParent.childCount; i++)
        {
            tireParent.GetChild(i).gameObject.SetActive(i == index);
        }

        Debug.Log($"타이어 모델 변경: {tireParent.GetChild(index).name}");
    }
}