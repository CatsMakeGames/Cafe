using System;
using Godot;
using System.Linq;

namespace Staff
{
    /**<summary>Cooks works similar to waiter but instead of taking orders from customers that use machines<para/>
     * A note on how this whole thing works: <para/>
     * Cook doesn't actually do actions they just stand near the object and wait</summary>*/
    public class Cook : Person
    {
        public enum Goal
        {
            None,
            /**<summary>Cook needs to take food from the fridge</summary>*/
            TakeFood,
            /**<summary>Cook needs to perform actions with food</summary>*/
            PrepareFood,
            /**<summary>Cook needs to use the machine</summary>*/
            CookFood,
            /**<summary>Cook needs to put food on the table where waiter could take it from</summary>*/
            GiveFood
        }

        public Goal currentGoal = Goal.None;

        public int goalOrderId = -1;

        protected int currentApplienceId = -1;

        //prepare and cook are basically wait tasks done using timers so no need for update function
        public override bool ShouldUpdate => base.ShouldUpdate && currentGoal != Goal.None && goalOrderId > -1;


        public Cook(Texture texture, Cafe cafe, Vector2 pos) : base(texture, new Vector2(128, 128), texture.GetSize(), cafe, pos, (int)ZOrderValues.Customer)
        {
        }

        public void TakeNewOrder(int orderId)
        {
            
            currentGoal = Cook.Goal.TakeFood;
            goalOrderId = orderId;
            Vector2[] temp;
            var fridge = cafe.FindClosestFurniture<Kitchen.Fridge>(Position, out temp);
            //mark this fridge as used by this cook for movement mode 
            fridge.CurrentUser = this;
            PathToTheTarget = temp;
           
        }


        public override void GetFired()
        {
            base.GetFired();
            if (currentApplienceId > -1)
            {
                cafe.Furnitures[currentApplienceId].CurrentUser = null;
            }
            /**
             * Raw food that needs to be cut just dissapears as it would be no different from raw food that was not touched
             * Cut food that was not finished is added as separate incase other chef would want to finish it
             */

            if (currentGoal == Goal.CookFood)
            {
                cafe.halfFinishedOrders.Add(goalOrderId);
            }
            switch (currentGoal)
            {         
                case Goal.CookFood:
                case Goal.PrepareFood:
                case Goal.TakeFood:
                    //notify cafe about order needing to be finished
                    cafe.OnNewOrder(goalOrderId);
                    break;
            }
            GD.PrintErr("Cooks are not complete!");
        }
        void BeFree()
        {
            currentApplienceId = -1;
            goalOrderId = -1;
            currentGoal = Goal.None;
            if (cafe.orders.Any())
            {
                TakeNewOrder(cafe.orders[0]);
                cafe.orders.RemoveAt(0);
            }
        }

        public override void ResetOrCancelGoal(bool forceCancel = false)
        {
            base.ResetOrCancelGoal(forceCancel);
            Vector2[] temp = null;
            bool found = false;
            switch (currentGoal)
            {
                //find new applience and move food there
                case Goal.CookFood:
                    //true we found new applience suitable to continue work
                    var stove = cafe.FindClosestFurniture<Kitchen.Stove>(Position, out temp);
                    if (forceCancel || stove == null)
                    {
                        cafe.orders.Add(goalOrderId);
                        BeFree();
                    }
                    else
                    {
                        stove.CurrentUser = this;
                        PathToTheTarget = temp;
                    }
                    break;
                //uninteraptable event(output table is unmoveable because it's not an entity)
                case Goal.GiveFood:
                    break;
                    //unused goal
                case Goal.PrepareFood:
                    break;
                    //the only solution is to find new fridge
                case Goal.TakeFood:
                    //true we found new applience suitable to continue work
                    var fridge = cafe.FindClosestFurniture<Kitchen.Fridge>(Position, out temp);
                    if (forceCancel || fridge == null)
                    {
                        cafe.orders.Add(goalOrderId);
                        BeFree();
                    }
                    else
                    {
                        fridge.CurrentUser = this;
                        PathToTheTarget = temp;
                    }
                    break;
            }
        }

        protected override async void onArrivedToTheTarget()
        {
            base.onArrivedToTheTarget();
            if (!Fired)
            {
                //possible feature -> if all capable cooks are busy lower level cooks do preparations(first two steps) and leave food there
                switch (currentGoal)
                {
                    case Goal.TakeFood:
                        await System.Threading.Tasks.Task.Delay(1000);
                        currentGoal = Goal.CookFood;
                        var stove = cafe.FindClosestFurniture<Kitchen.Stove>(Position, out pathToTheTarget);
                        stove.CurrentUser = this;
                        currentApplienceId = (int)stove.Id;
                        pathId = 0;
                        //move to the cutting table
                        break;
                    //this step is currently missing
                    case Goal.PrepareFood:
                        await System.Threading.Tasks.Task.Delay(1000);
                        //move to the applience
                        break;
                    case Goal.CookFood:
                        await System.Threading.Tasks.Task.Delay(1000);
                        currentGoal = Goal.GiveFood;
                        PathToTheTarget = cafe.FindLocation("Kitchen", Position);
                        //move to the finish table
                        break;
                    case Goal.GiveFood:
                        cafe.OnOrderComplete(goalOrderId);

                        BeFree();
                        //seek next task
                        //cook is idling
                        break;
                }
            }
        }
    }
}
