using Godot;
using System;

public class Customer : CafeObject
{
    async void move()
    {
        //find table to move to
        var table = cafe.FindTable();
        if (table != null)
        {
            await ToSignal(cafe.GetTree().CreateTimer(1), "timeout");
            Position = table.Position;
            table.CurrentState = Table.State.InUse;
        }
        else
        {
            GD.PrintErr("No table available!");
        }
    }
    public Customer(Texture texture, Cafe cafe, Vector2 pos) : base(texture,new Vector2(64,64),texture.GetSize(), cafe, pos,(int)ZOrderValues.Customer)
    {
        move();
    }
}
