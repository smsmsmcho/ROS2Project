using System;

[Serializable]
public enum PartCategory
{
    Trim,
    Wheel,
    Color
}

[Serializable]
public class CarPart
{
    public string id;
    public string name;
    public int price;
    public PartCategory category;
    public string description;
    public string colorHex;

    public override string ToString()
    {
        return $"[{category}] {name} ({id}) - {price:N0}원";
    }
}
