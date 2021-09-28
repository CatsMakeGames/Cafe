using Godot;
using System;

public class Customer : Person
{
    /**<summary>Because all orders are stored as a list we can just pass around id of the order</summary>*/
    protected int orderId = 0;

    public int OrderId => orderId;

    protected bool isAtTheTable = false;

    protected bool ate = false;

    /**<summary>Id of the table where customer sits</summary>*/
    public int CurrentTableId = -1;

    protected float defaultOrderTime = 1;

    public float OrderTime => defaultOrderTime;

    public override bool ShouldUpdate => base.ShouldUpdate && !isAtTheTable;

    [Signal]
    public delegate void FinishEating(int payment);

    [Signal]
    public delegate void OnLeft(Customer customer);

    [Signal]
    public delegate void ArivedToTheTable(Customer customer);

    void move()
    {
        //find table to move to
        var table = cafe.FindTable(out pathToTheTarget, Position, out CurrentTableId);

        if (table != null)
        {
            CurrentTableId = cafe.Tables.IndexOf(table);
            table.CurrentState = Table.State.InUse;
            Line2D pathLine = new Line2D();
            System.Collections.Generic.List<Vector2> path = new System.Collections.Generic.List<Vector2>(pathToTheTarget);
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
        pathId = 0;
        ate = true;
        //leave the cafe
        pathToTheTarget = cafe.FindExit(Position);
        if (pathToTheTarget.Length == 0)
        {
            Destroy();
        }
    }

    public Customer(Texture texture, Cafe cafe, Vector2 pos) : base(texture,new Vector2(64,64),texture.GetSize(), cafe, pos,(int)ZOrderValues.Customer)
    {
        move();
    }

    protected override void onArrivedToTheTarget()
    {
        base.onArrivedToTheTarget();
        if (!ate)
        {
            isAtTheTable = true;
            //tell cafe that they are ready to order
            EmitSignal(nameof(ArivedToTheTable), this);
        }
        else
        {
            Destroy();
        }
    }

    public override void Destroy()
    {
        EmitSignal(nameof(OnLeft), this);
        base.Destroy();
    }
}
