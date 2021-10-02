using System;
using Godot;

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

        public Goal CurrentGoal = Goal.None;

        public override bool ShouldUpdate => base.ShouldUpdate && CurrentGoal != Goal.None;

        [Signal]
        public delegate void OnWaiterIsFree(Waiter waiter);

        /**<summary>Customer from whom to take or deliver to the order<para/>Note that this value is reset each time action is finished</summary>*/
        public Customer currentCustomer = null;

        public Waiter(Texture texture, Cafe cafe, Vector2 pos) : base(texture, new Vector2(64, 64), texture.GetSize(), cafe, pos, (int)ZOrderValues.Customer)
        {
            EmitSignal(nameof(OnWaiterIsFree), this);
        }

        private void BeFree()
        {
            //waiter is now free
            CurrentGoal = Goal.None;
            //forget about this customer
            currentCustomer = null;
            //since cafe is refenced for using node functions anyway, no need to use signals
            cafe.OnWaiterIsFree(this);
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
                    if (cust.IsAtTheTable)
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
