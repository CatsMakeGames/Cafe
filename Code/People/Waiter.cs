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
            DeliverOrder
        }

        public Goal CurrentGoal = Goal.None;

        public override bool ShouldUpdate => base.ShouldUpdate && CurrentGoal != Goal.None;

        /**<summary>Customer from whom to take or deliver to the order<para/>Note that this value is reset each time action is finished</summary>*/
        public Customer currentCustomer = null;

        public Waiter(Texture texture, Cafe cafe, Vector2 pos) : base(texture,new Vector2(64,64),texture.GetSize(), cafe, pos,(int)ZOrderValues.Customer)
        {
        }

        protected override void onArrivedToTheTarget()
        {
            base.onArrivedToTheTarget();
            switch (CurrentGoal)
            {
                case Goal.TakeOrder:
                    //goal changes to new one
                    CurrentGoal = Goal.PassOrder;
                    //don't reset current customer because we still need to know the order
                    //find path to the kitchen
                    PathToTheTarget = cafe.FindKitchen(Position);
                    break;
                case Goal.PassOrder:
                    //kitchen is now making the order
                    //waiter is now free
                    CurrentGoal = Goal.None;
                    GD.Print("Free!");
                    break;
                case Goal.AcquireOrder:
                    break;
                case Goal.DeliverOrder:
                    break;
            }

        }
    }
}
