using Godot;
using System;

public class Customer : CafeObject
{
    protected Vector2[] pathToTheTable;

    protected int pathId = 1;

    protected float Speed = 10;

    async void move()
    {
        //find table to move to
        var table = cafe.FindTable(out pathToTheTable);
        
        if (table != null)
        {
            GD.PrintErr(new Godot.Collections.Array<Vector2>(pathToTheTable));
            /* await ToSignal(cafe.GetTree().CreateTimer(1), "timeout");
             Position = table.Position;*/
            table.CurrentState = Table.State.InUse;
            Line2D pathLine = new Line2D();
            System.Collections.Generic.List<Vector2> path = new System.Collections.Generic.List<Vector2>(pathToTheTable);
            path.RemoveAt(0);
            pathLine.Points = path.ToArray();
            pathLine.ShowOnTop = true;
            cafe.AddChild(pathLine);
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

    //should make this function be async in some way
    public void Update(float deltaTime)
    {
        if (pathToTheTable == null || pathId >= pathToTheTable.Length) { return; }
        //move customer along the path

        Position += (pathToTheTable[pathId] - position).Normalized() * Speed;
        if (position.DistanceTo(pathToTheTable[pathId]) <= 3f)
        {
            pathId++;
            GD.Print(pathId);
        }
    }
}
