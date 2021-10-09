using Godot;
public class StoreItem : Godot.Object
{
    protected RID rid;
    public string ClassName = nameof(Furniture);

    public string Name = nameof(Furniture);

    public string TextureName = "res://icon.png";

    public int Price = 100;

    public Furniture.Category FurnitureCategory = Furniture.Category.None;

    public Cafe cafe;

    public StoreItem(string className, string name, string textureName, int price, Furniture.Category furnitureCategory)
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

    /**<summary>Loads textures and creates item</summary>*/
    public void Create(Vector2 position,StoreMenu menu,int size = 256)
    {
        rid = VisualServer.CanvasItemCreate();
        VisualServer.CanvasItemAddTextureRect(rid, new Rect2(position, new Vector2(size, size)), menu.cafe.Textures[TextureName].GetRid(), false, null, false, menu.cafe.Textures[TextureName].GetRid());
        VisualServer.CanvasItemSetParent(rid, menu.GetCanvasItem());
    }

    /**<summary>Creates Store item data from csv string</summary>*/
    public StoreItem(string data)
    {
        string[] subData = data.Split(';');
        fillData(subData);
    }

    public StoreItem(string[] subData)
    {
        fillData(subData);
    }
}