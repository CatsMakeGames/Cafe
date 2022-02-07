using Godot;
using System.Collections.Generic;

public struct Food
{
    public int Price;
    public Food(int price)
    {
        Price = price;
    }
}

public class FoodData
{
    Dictionary<int,Food> _food = new Dictionary<int, Food>();

    public Food this[int id]
    {
        get => _food[id];
        set => _food[id] = value;
    }

    /**<summary>Loads data from the table</summary>*/
    public FoodData()
    {
        File file = new File();
        if (!file.FileExists("res://Data/food.dat"))
        {
            GD.PrintErr("Failed to find food table");
            return;
        }
        file.Open("res://Data/food.dat", File.ModeFlags.Read);
        if (file.IsOpen())
        {
            while (!file.EofReached())
            {
                var line = file.GetCsvLine(";");
                this[System.Convert.ToInt32(line[0])] = new Food(System.Convert.ToInt32(line[1]));
            }
        }
    }
}