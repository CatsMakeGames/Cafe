using Godot;
using System;

public class StoreMenu : Control
{
    Godot.Collections.Array<StoreItem> storeItems = new Godot.Collections.Array<StoreItem>();

    public Cafe cafe;

    [Export]
    int width;

    public override void _Ready()
    {
        base._Ready();
        Load();
        GD.Print("Loaded");
        //show on screen
        //cafe = GetTree().Root.FindNode("Cafe") as Cafe ?? throw new NullReferenceException("Failed to find cafe");
    }

    public void Create()
    {
        int id = 0;
        foreach (StoreItem item in storeItems)
        {
            item.Create(new Vector2(id - ((int)(id / width) * width), (int)(id / width)) * 256, this, 256);
            id++;
        }
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
                    storeItems.Add(new StoreItem(line));
                }
                catch (Exception e)
                {
                    //ignore cause first line is column names and that causes errors
                }           
            }
        }
    }
}
