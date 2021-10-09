using Godot;
using System;

public class StoreMenu : Control
{
    Godot.Collections.Array<StoreItemData> storeItems = new Godot.Collections.Array<StoreItemData>();

    public override void _Ready()
    {
        base._Ready();
        Load();
        GD.Print("Loaded");
    }

    /**<summary>Flushes and reloads </summary>*/
    void Load()
    {
        File file = new File();
        if(!file.FileExists("res://Data/storeData.dat"))
        {
            GD.PrintErr("Failed to find store items table");
            return;
        }
        file.Open("res://Data/storeData.dat", File.ModeFlags.Read);
        if(file.IsOpen())
        {
            string[] line;
            while(!file.EofReached())
            {
                try
                {
                    line = file.GetCsvLine(";");
                    storeItems.Add(new StoreItemData(line));
                }
                catch (Exception e)
                {
                    //ignore
                }           
            }
        }
    }
}
