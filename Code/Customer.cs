using Godot;
using System;
using System.Linq;

public class Customer : Person
{

    public enum Goal
    {
        /**<summary>Customer has no table and is actively looking for one</summary>*/
        WaitForTable,
        /**<summary>Customer is going to sit at the table</summary>*/
        WalkToTable,
        /**<summary>Customer is at the table and waits for waiter to come and take the order</summary>*/
        WaitForWaiter,
        /**<summary>Waiter took the order and now customer is waiting for food</summary>*/
        WaitForFood,
        /**<summary>Customer is eating</summary>*/
        Eat,
        /**<summary>Customer ate food and is now leaving</summary>*/
        Leave,
        /**<summary>Table has been moved and customer has to find it again</summary>*/
        MoveBackToTable
    }

    /**<summary>If there are any temporary changes in environment this value is set to 
    whatever goal is active until environmental changes are adopted to</summary>*/
    private Goal _primaryGoalBackup;

    private Goal _currentGoal;

    /**<summary>Because all orders are stored as a list we can just pass around id of the order</summary>*/
    protected int orderId = 0;

    public int OrderId => orderId;

    /**<summary>Id of the table where customer sits</summary>*/
    private int _currentTableId = -1;

    public int CurrentTableId => _currentTableId;

    protected float defaultOrderTime = 3;

    public float OrderTime => defaultOrderTime;

    public override bool ShouldUpdate => base.ShouldUpdate || (_currentGoal == Goal.Leave || _currentGoal == Goal.MoveBackToTable || _currentGoal == Goal.WalkToTable);

    [Signal]
    public delegate void FinishEating(int payment);

    /**<summary>Amount of bytes used by CafeObject + amount of bytes used by this object</summary>*/
    public new static uint SaveDataSize = 12u;

    /**<summary> Waiter that is currently bringing the cooked meal</summary>*/
    public Staff.Waiter CurrentWaiter;

    public bool orderTaken = false;

    public bool Available => _currentGoal == Goal.WaitForTable;

    public void TableLocationChanged(Vector2 newLocation)
    {
        PathToTheTarget = cafe.FindPathTo(position, newLocation);
        _primaryGoalBackup = _currentGoal;
        _currentGoal = Goal.MoveBackToTable;
        
    }

    protected override void OnTaskTimerRunOut()
    {
        base.OnTaskTimerRunOut();
        if(_currentGoal == Goal.Eat)
        {
            cafe.OnCustomerFinishedMeal(this);
            _currentGoal = Goal.Leave;
            PathToTheTarget = cafe.FindLocation("Exit",Position);
        }
    }

    public void Eat()
    {
        _currentGoal = Goal.Eat;
        SetTaskTimer(1);
    }

    public Customer(Texture texture, Cafe cafe, Vector2 pos) : base(texture,new Vector2(128,128),texture.GetSize(), cafe, pos,(int)ZOrderValues.Customer)
    {
       OnNewTableIsAvailable();

        cafe.Connect(nameof(Cafe.OnNewTableIsAvailable),this,nameof(OnNewTableIsAvailable));
    }

    public Customer(Cafe cafe,uint[] data) : base(cafe,data)
    {
        cafe.Connect(nameof(Cafe.OnNewTableIsAvailable),this,nameof(OnNewTableIsAvailable));
        orderId = (int)data[7];
        //TODO: update save system
        _currentTableId = (int)data[10];


        GenerateRIDBasedOnTexture(cafe.GetTexture("Customer"), ZOrderValues.Customer);
    }

    public void OnNewTableIsAvailable()
    {
        if(cafe.AvailableTables.Any() && _currentGoal == Goal.WaitForTable)
        {
            //check if path to new table exists
            //if it does move to it
            Vector2[] path = cafe.FindPathTo(position, cafe.GetFurniture(cafe.AvailableTables.Peek()).Position);
            if (path != null)
            {
                _currentTableId = cafe.AvailableTables.Peek();
                cafe.GetFurniture(_currentTableId).SetNewCustomer(this);
                cafe.AvailableTables.Pop();
                _currentGoal = Goal.WalkToTable;
                PathToTheTarget = path;
            }
        }
    }

    public void OnTableIsUnavailable()
    {
       _currentTableId = -1;
       _primaryGoalBackup = _currentGoal;
       _currentGoal = Goal.WaitForTable;
    }

    public override Godot.Collections.Array<uint> GetSaveData()
    {
        Godot.Collections.Array<uint> data = base.GetSaveData();
        data.Add((uint)orderId);//[7]
        data.Add((uint)_currentGoal);//[8]
        data.Add((uint)_primaryGoalBackup);//[9]
        data.Add((uint)_currentTableId);//[10]
        return data;
    }

    public bool IsWaitingForOrder(int order)
    {
        return orderId == order && _currentGoal == Goal.WaitForFood && CurrentWaiter == null;
    }



    public override void SaveInit()
    {
        base.SaveInit();
    }

    public void OnOrderTaken()
    {
        _currentGoal = Goal.WaitForFood;
    }

    protected override void onArrivedToTheTarget()
    {
        base.onArrivedToTheTarget();
        switch (_currentGoal)
        {
            case Goal.WalkToTable:
                if (cafe.GetFurniture(_currentTableId).CurrentUser == null)
                {
                    //if customer had to find new table we want them to be reset
                    //but because customer does not need to notify about it we just silently wait
                    if (_primaryGoalBackup != Goal.WaitForTable)
                    {
                        _currentGoal = _primaryGoalBackup;
                    }
                    else
                    {
                        _currentGoal = Goal.WaitForWaiter;
                        cafe.AddNewArrivedCustomer(this);
                    }
                }
                break;
            case Goal.Leave:
                pendingKill = true;
                cafe._onCustomerLeft(this);
                break;
            case Goal.MoveBackToTable:
                _currentGoal = _primaryGoalBackup;
                break;
        }
    }
}
