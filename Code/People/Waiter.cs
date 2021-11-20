using System;
using Godot;
using System.Linq;
using Godot.Collections;

namespace Staff
{
    /**<summary>Staff member that takes and devivers orders</summary>*/
    public class Waiter : Person
    {
        public enum Goal
        {
            /**<summary>Waiter is just idling</summary>*/
            None,
            /**<summary>Take order from customer</summary>*/
            TakeOrder,
            /**<summary>Give order to the kitchen</summary>*/
            PassOrder,
            /**<summary>Take cooked food from kitchen and give to the customer</summary>*/
            AcquireOrder,
            /**<summary>Give order to the customer</summary>*/
            DeliverOrder,
            /**<summary>Leave to the staff room(or kitchen)<para/>This goal can be overriten at any time</summary>*/
            Leave
        }
        /**<summary>Current order that waiter is carrying.<para/>Used for when order is canceled</summary>*/
        public int currentOrder = -1;

        public Goal CurrentGoal = Goal.None;

        public override Class Type => Class.Waiter; 

        public override bool ShouldUpdate => base.ShouldUpdate && CurrentGoal != Goal.None;

        [Signal]
        public delegate void OnWaiterIsFree(Waiter waiter);

        /**<summary>Customer from whom to take or deliver to the order<para/>Note that this value is reset each time action is finished</summary>*/
        public Customer currentCustomer = null;

        public Waiter(Texture texture, Cafe cafe, Vector2 pos) : base(texture, new Vector2(128, 128), texture.GetSize(), cafe, pos, (int)ZOrderValues.Customer)
        {
            EmitSignal(nameof(OnWaiterIsFree), this);
        }


        public Waiter(Cafe cafe,uint[] saveData) : base(cafe,saveData)
        {
            textureSize = cafe.Textures["Waiter"].GetSize();
            size = new Vector2(128, 128);
            GenerateRIDBasedOnTexture(cafe.Textures["Waiter"], ZOrderValues.Customer);
        }

        public override Array<uint> GetSaveData()
        {

            return base.GetSaveData();
            
        }

        public override void ResetOrCancelGoal(bool forceCancel = false)
        {
            base.ResetOrCancelGoal(forceCancel);
            switch (CurrentGoal)
            {
                case Goal.DeliverOrder:
                    //this means that person who ordered their food has been moved or lost their table

                    //customer has been left table-less
                    if (currentCustomer.CurrentTableId == -1 || forceCancel)
                    {
                        cafe.completedOrders.Add(currentOrder);
                        BeFree();
                    }
                    else
                    {
                        PathToTheTarget = cafe.FindPathTo(Position, cafe.Furnitures[currentCustomer.CurrentTableId].Position);
                    }
                    break;
                case Goal.TakeOrder:
                    if (currentCustomer != null)
                    {
                        //table was moved and no other were found
                        if (currentCustomer.CurrentTableId == -1 || forceCancel)
                        {
                            currentCustomer = null;
                            BeFree();
                        }
                        else
                        {
                            PathToTheTarget = cafe.FindPathTo(Position, cafe.Furnitures[currentCustomer.CurrentTableId].Position);
                        }
                    }
                    break;
                    //those two tasks are for going towards kitchen
                case Goal.AcquireOrder:
                case Goal.PassOrder:
                    if (forceCancel)
                    {
                        //this goal is not meant to be reset in this version of the game  and as such this does nothing
                        GD.PrintErr("Warning: Attempted to cancel task when going towards kitchen. This should not happen and will be ignored");
                    }
                    else
                    {
                        PathToTheTarget = cafe.FindLocation("Kitchen", Position);
                    }
                    break;
                default:
                    //it's not like we were doing anything anyway
                    break;
            }
        }

        private void BeFree()
        {
            //waiter is now free
            CurrentGoal = Goal.None;
            //forget about this customer
            currentCustomer = null;
            //since cafe is refenced for using node functions anyway, no need to use signals
            if (cafe.completedOrders.Any())
            {
                cafe.Furnitures[cafe.completedOrders[0]].CurrentUser = this;
                changeTask(cafe.Furnitures[cafe.completedOrders[0]].Position, Waiter.Goal.AcquireOrder, (cafe.Furnitures[cafe.completedOrders[0]] as Table).CurrentCustomer);
                cafe.completedOrders.RemoveAt(0);
            }

            //search through the list and find tasks that can be completed
            else if (cafe.tablesToTakeOrdersFrom.Any())
            {
                cafe.Furnitures[cafe.completedOrders[0]].CurrentUser = this;
                changeTask(cafe.Furnitures[cafe.tablesToTakeOrdersFrom[0]].Position, Waiter.Goal.TakeOrder, (cafe.Furnitures[cafe.tablesToTakeOrdersFrom[0]] as Table).CurrentCustomer);
                cafe.tablesToTakeOrdersFrom.RemoveAt(0);
            }

            else
            {
                //move waiter to "staff location"
                PathToTheTarget = cafe.FindLocation("Kitchen", Position);
                CurrentGoal = Goal.Leave;
            }
        }

        void changeTask(Vector2 target, Waiter.Goal goal, Customer customer)
        {
            PathToTheTarget = cafe.navigation.GetSimplePath(Position, target) ?? throw new NullReferenceException("Failed to find path to the task!");
            CurrentGoal = goal;
            currentCustomer = customer;
        }



        protected override async void onArrivedToTheTarget()
        {
            base.onArrivedToTheTarget();
            switch (CurrentGoal)
            {
                case Goal.TakeOrder:
                    //goal changes to new one
                    CurrentGoal = Goal.PassOrder;

                    //this way we don't hold the execution
                    await ToSignal(cafe.GetTree().CreateTimer(currentCustomer.OrderTime), "timeout");

                    //don't reset current customer because we still need to know the order
                    //find path to the kitchen
                    PathToTheTarget = cafe.FindLocation("Kitchen", Position);
                    break;
                case Goal.PassOrder:
                    //kitchen is now making the order                           
                    cafe.OnNewOrder(currentCustomer.OrderId);
                    BeFree();
                    break;
                case Goal.AcquireOrder:
                    //make way towards customer now
                    PathToTheTarget = cafe.FindPathTo(Position, currentCustomer.Position);
                    CurrentGoal = Goal.DeliverOrder;
                    break;
                case Goal.DeliverOrder:
                    var cust = currentCustomer;
                    BeFree();
                    if (cust.IsAtTheTable && !cust.Eating)
                    {
                        cust.Eat();
                        GD.Print($"Feeding: {cust.ToString()}");
                    }
                    
                    break;

                case Goal.Leave:
                    CurrentGoal = Goal.None;
                    break;
            }

        }
    }
}
