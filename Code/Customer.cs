using Godot;
using System;
using System.Linq;

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

    protected float defaultOrderTime = 5;

    public float OrderTime => defaultOrderTime;

    public override bool ShouldUpdate => base.ShouldUpdate && !isAtTheTable;

    [Signal]
    public delegate void FinishEating(int payment);

    [Signal]
    public delegate void ArivedToTheTable(Customer customer);

    public static new Class Type = Class.Customer;

    /**<summary>Amount of bytes used by CafeObject + amount of bytes used by this object</summary>*/
    public new static uint SaveDataSize = 12u;

    public bool Available => !IsAtTheTable && !Eating && !MovingToTheTable;
    public bool FindAndMoveToTheTable()
    {

        Vector2[] path = null;

        //find table to move to
        var table = cafe.FindClosestFurniture(Furniture.FurnitureType.Table, position, out path);
        //we don't want to reset paths accidentally
        if (path != null)
        {
            PathToTheTarget = path;
        }
        pathId = 0;
        if (table != null)
        {
            CurrentTableId = cafe.Furnitures.IndexOf(table);
            table.CurrentState = Furniture.State.InUse;

            movingToTheTable = true;
            table.CurrentCustomer = this;
            //handle table being moved via build mode
            if (isAtTheTable && table.Position.DistanceTo(position) > 5f)
            {
                isAtTheTable = false;
            }
            else if (isAtTheTable)
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

        cafe.Connect(nameof(Cafe.OnNewTableIsAvailable),this,nameof(OnNewTableIsAvailable));
    }

    public Customer(Cafe cafe,uint[] data) : base(cafe,data)
    {
        cafe.Connect(nameof(Cafe.OnNewTableIsAvailable),this,nameof(OnNewTableIsAvailable));
        orderId = (int)data[7];
        isAtTheTable = data[8] == 1u ? true : false;
        movingToTheTable = data[9] == 1u ? true : false;
        eating = data[10] == 1u ? true : false;
        CurrentTableId = (int)data[11];

        GenerateRIDBasedOnTexture(cafe.Textures["Customer"], ZOrderValues.Customer);
    }

    public void OnNewTableIsAvailable()
    {
        if(!IsAtTheTable && !Eating && !MovingToTheTable)
        {
            //check if path to new table exists
            //if it does move to it
            Vector2[] path = cafe.FindPathTo(position, cafe.Furnitures[cafe.AvailableTables.Last()].Position);
            if (path != null)
            {
                CurrentTableId = cafe.AvailableTables.Last();
                cafe.Furnitures[CurrentTableId].SetNewCustomer(this);
                movingToTheTable = true;
                cafe.AvailableTables.RemoveAt(cafe.AvailableTables.Count - 1);
                PathToTheTarget = path;
            }
        }
    }

    public override Godot.Collections.Array<uint> GetSaveData()
    {
        Godot.Collections.Array<uint> data = base.GetSaveData();
        data.Add((uint)orderId);//[7]
        data.Add(isAtTheTable ? 1u: 0u);//[8]
        data.Add(movingToTheTable ? 1u :0u);//[9]
        data.Add(eating ? 1u :0u);//[10]
        data.Add((uint)CurrentTableId);//[11]
        return data;
    }

    public override void SaveInit()
    {
        base.SaveInit();
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
