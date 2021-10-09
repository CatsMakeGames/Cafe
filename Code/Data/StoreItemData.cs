
public class StoreItemData : Godot.Object
{
    public string ClassName = nameof(Furniture);

    public string Name = nameof(Furniture);

    public string TextureName = "res://icon.png";

    public int Price = 100;

    public Furniture.Category FurnitureCategory = Furniture.Category.None;

    public StoreItemData(string className, string name, string textureName, int price, Furniture.Category furnitureCategory)
    {
        ClassName = className;
        Name = name;
        TextureName = textureName;
        Price = 100;
        FurnitureCategory = furnitureCategory;
    }
    void fillData(string[] subData)
    {
        Name = subData[1];
        TextureName = subData[2];
        Price = System.Convert.ToInt32(subData[4], System.Globalization.CultureInfo.InvariantCulture);
        ClassName = subData[5];
        foreach (string cat in subData[3].Split('|'))
        {
            FurnitureCategory |= (Furniture.Category)(1 << System.Convert.ToInt32(cat, System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /**<summary>Creates Store item data from csv string</summary>*/
    public StoreItemData(string data)
    {
        string[] subData = data.Split(';');
        fillData(subData);
    }

    public StoreItemData(string[] subData)
    {
        fillData(subData);
    }
}