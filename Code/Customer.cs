using Godot;
using System;

public class Customer : Person
{
    /**<summary>Because all orders are stored as a list we can just pass around id of the order</summary>*/
    protected int orderId = 0;

    public int OrderId => orderId;

    protected bool isAtTheTable = false;

    public bool IsAtTheTable => isAtTheTable;

    protected bool movingToTheTable = false;

    public bool MovingToTheTable => movingToTheTable;

    protected bool ate = false;

    protected bool eating = false;

    public bool Eating => eating;

    /**<summary>Id of the table where customer sits</summary>*/
    public int CurrentTableId = -1;

    protected float defaultOrderTime = 1;

    public float OrderTime => defaultOrderTime;

    public override bool ShouldUpdate => base.ShouldUpdate && !isAtTheTable;

    [Signal]
    public delegate void FinishEating(int payment);

    [Signal]
    public delegate void ArivedToTheTable(Customer customer);

    public bool FindAndMoveToTheTable()
    {
        //find table to move to
        var table = cafe.FindClosestFurniture<Table>(position, out pathToTheTarget);
        pathId = 0;
        if (table != null)
        {
            CurrentTableId = cafe.Furnitures.IndexOf(table);
            table.CurrentState = Table.State.InUse;

            movingToTheTable = true;
            table.CurrentCustomer = this;
            //handle table being moved via build mode
            if (isAtTheTable && table.Position.DistanceTo(position) > 5f)
            {
                isAtTheTable = false;
            }
            else if(isAtTheTable)
            {
                GD.PrintErr($"Distance: {table.Position.DistanceTo(position)}. To {table}, from {this}");
            }
           
            return true;
        }
        return false;
    }

    private async void eat()
    {
        GD.Print($"{ToString()}: Eating");
        eating = true;
        await ToSignal(cafe.GetTree().CreateTimer(5), "timeout");
        GD.Print($"{ToString()}: Ate");
        //TODO: make it so it would read payment from the table of values
        cafe._onCustomerFinishedEating(this, 100);
        isAtTheTable = false;
        pathId = 0;
        ate = true;
        pathToTheTarget = cafe.FindLocation("Exit",Position);
        //leave the cafe
        /*pathToTheTarget = cafe.FindExit(Position);
        if (pathToTheTarget.Length == 0)
        {
            Destroy();
        }*/
    }

    public void Eat()
    {
        eat(); 
    }

    public Customer(Texture texture, Cafe cafe, Vector2 pos) : base(texture,new Vector2(128,128),texture.GetSize(), cafe, pos,(int)ZOrderValues.Customer)
    {
        FindAndMoveToTheTable();
    }

    protected override void onArrivedToTheTarget()
    {
        base.onArrivedToTheTarget();
        if (!ate)
        {
            isAtTheTable = true;
            //check to make sure no waiters are already serving this table
            if (cafe.Furnitures[CurrentTableId].CurrentUser == null)
            {
                //tell cafe that they are ready to order
                cafe._onCustomerArrivedAtTheTable(this);
            }
        }
        else
        {

            pendingKill = true;
            cafe._onCustomerLeft(this);
        }
    }
}
