using Godot;
using System;

public class Customer : CafeObject
{
    protected Vector2[] pathToTheTable;

    protected int pathId = 0;

    protected float Speed = 10;

    protected bool isAtTheTable = false;

    protected bool ate = false;

    public bool IsAtTheTable => isAtTheTable;

    [Signal]
    public delegate void FinishEating(int payment);

    [Signal]
    public delegate void OnLeft(Customer customer);

    void move()
    {
        //find table to move to
        var table = cafe.FindTable(out pathToTheTable,Position);
        
        if (table != null)
        {
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

    protected virtual async void Eat()
    {
        await ToSignal(cafe.GetTree().CreateTimer(1), "timeout");
        //TODO: make it so it would read payment from the table of values
        EmitSignal(nameof(FinishEating), 100);
        isAtTheTable = false;
        //leave the cafe
        pathToTheTable = cafe.FindExit(Position);
        if(pathToTheTable.Length == 0)
        {
            Destroy();
            
        }
        await ToSignal(cafe.GetTree().CreateTimer(1), "timeout");
        Destroy();
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
            if (pathId >= pathToTheTable.Length)
            {
                if (!ate)
                {
                    isAtTheTable = true;
                    Eat();
                }
                else
                {
                    Destroy();
                }
            }
        }
    }

    public override void Destroy()
    {
        EmitSignal(nameof(OnLeft), this);
        base.Destroy();
    }
}
