using Godot;
using System;


/**<summary>Data class that stores item data that is loaded from datatable</summary>*/
public class StoreItemData : Godot.Object
{
    /**<summart>Id of the item in the original item table<para/>This way id stays consitstent no matter what</summart>*/
    public ushort tableId = 0x0;

    public string ClassName = nameof(Furniture);

    public string Name = nameof(Furniture);

    public string TextureName = "res://icon.png";

    public string Description = "Get psyched!";

    public byte Level = 0;

    public byte DecorLevel = 1;

    public int Price = 100;

    public Vector2 Size = new Vector2(128, 128);

    public Vector2 FrameSize = new Vector2(32,32);

    public Vector2 CollisionRectSize = new Vector2(128, 128);

    public Vector2 CollisionRectOffset = new Vector2(0, 0);

    public Furniture.Category FurnitureCategory = Furniture.Category.None;

    /**<summary>This category will be used to display in StoreMenu</summary>*/
    public Furniture.Category DisplayCategory = Furniture.Category.None;
    public Furniture.DecorFurnitureType DecorType = Furniture.DecorFurnitureType.None;


    /**<summary>Creates Store item data from csv string</summary>*/
    public StoreItemData(string data)
    {
        string[] subData = data.Split(';');
        _fillData(subData);
    }

    public StoreItemData(string[] subData)
    {
        _fillData(subData);
    }

    /**<summary>Converts raw byte data into item data</summary>*/
    private void _fillData(string[] subData)
    {
        //because id is 16bit integer that is always bigger then 0
        tableId = System.Convert.ToUInt16(subData[0]);
        Name = subData[1];
        TextureName = subData[2];
        Price = System.Convert.ToInt32(subData[4], System.Globalization.CultureInfo.InvariantCulture);
        ClassName = subData[5];
        foreach (string cat in subData[3].Split('|'))
        {
            FurnitureCategory |= (Furniture.Category)(1 << System.Convert.ToInt32(cat, System.Globalization.CultureInfo.InvariantCulture));
        }
        DisplayCategory = (Furniture.Category)(1 << System.Convert.ToInt32(subData[6], System.Globalization.CultureInfo.InvariantCulture));
        Size = new Vector2
        (
            System.Convert.ToInt32(subData[7], System.Globalization.CultureInfo.InvariantCulture),
            System.Convert.ToInt32(subData[8], System.Globalization.CultureInfo.InvariantCulture)
        );

        Description = subData[9];
        Level = Convert.ToByte(subData[10]);
        DecorLevel = System.Convert.ToByte(subData[11], System.Globalization.CultureInfo.InvariantCulture);
        FrameSize = new Vector2
        (
            System.Convert.ToInt32(subData[12], System.Globalization.CultureInfo.InvariantCulture),
            System.Convert.ToInt32(subData[13], System.Globalization.CultureInfo.InvariantCulture)
        );
        CollisionRectSize = new Vector2
        (
            System.Convert.ToInt32(subData[14], System.Globalization.CultureInfo.InvariantCulture),
            System.Convert.ToInt32(subData[15], System.Globalization.CultureInfo.InvariantCulture)
        );
        CollisionRectOffset = new Vector2
        (
            System.Convert.ToInt32(subData[16], System.Globalization.CultureInfo.InvariantCulture),
            System.Convert.ToInt32(subData[17], System.Globalization.CultureInfo.InvariantCulture)
        );
        if(!Enum.TryParse<Furniture.DecorFurnitureType>(subData[18], out DecorType))
        {
            GD.PrintErr($"\nFailed to recognise decor type. Name: {subData[18]}");
        }
    }
}
