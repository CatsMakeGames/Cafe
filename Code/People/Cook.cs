using System;
using Godot;
using System.Linq;
using Godot.Collections;

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

        public override bool IsFree => base.IsFree && currentGoal == Goal.None;

        protected int currentApplianceId = -1;

        /**<summary>Amount of bytes used by CafeObject + amount of bytes used by this object</summary>*/
        public new static uint SaveDataSize = 10u;

        //prepare and cook are basically wait tasks done using timers so no need for update function
        public override bool ShouldUpdate => (base.ShouldUpdate && currentGoal != Goal.None && goalOrderId > -1) || Fired;


        public Cook(Texture texture, Cafe cafe, Vector2 pos) : base(texture, new Vector2(128, 128), texture.GetSize(), cafe, pos, (int)ZOrderValues.Customer)
        {
            Salary = 100;
            cafe.Connect(nameof(Cafe.OnNewOrderAdded),this,nameof(OnNewOrder));
        }

        public Cook(Cafe cafe,uint[] saveData) : base(cafe,saveData)
        {
            Salary = 100;
            textureSize = cafe.Textures["Cook"].GetSize();
			size = new Vector2(128, 128);
            currentGoal = (Goal)saveData[7];
            goalOrderId = (int)saveData[8];
            currentApplianceId = saveData[9] == 0 ? -1 : (int)saveData[9] - 1;
			GenerateRIDBasedOnTexture(cafe.Textures["Cook"], ZOrderValues.Customer);
        }

        public void TakeNewOrder(int orderId)
        {
            Vector2[] temp;
            //first try to find the fridge and if succeeded do the thing
            Furniture fridge = cafe.FindClosestFurniture(Furniture.FurnitureType.Fridge, Position, out temp);
            if (fridge != null)
            {
                currentGoal = Cook.Goal.TakeFood;
                goalOrderId = orderId;
                //mark this fridge as used by this cook for movement mode 
                fridge.CurrentUser = this;
                currentApplianceId = cafe.GetFurnitureIndex(fridge);
                PathToTheTarget = temp;
            }
        }

        public void OnNewOrder()
        {
            if (cafe.orders.Any() && IsFree)
            {
                Vector2[] temp;
                //first try to find the fridge and if succeeded do the thing
                //if there are no tools we can not perform this task
                Furniture fridge = cafe.FindClosestFurniture(Furniture.FurnitureType.Fridge, Position, out temp);
                if (fridge != null)
                {
                    currentGoal = Cook.Goal.TakeFood;
                    goalOrderId = cafe.orders.First();
                    //mark this fridge as used by this cook for movement mode 
                    fridge.CurrentUser = this;
                    currentApplianceId = cafe.GetFurnitureIndex(fridge);
                    PathToTheTarget = temp;
                    cafe.orders.RemoveAt(0);
                }
            }
        }


        public override void GetFired()
        {
            base.GetFired();
            if (currentApplianceId > -1)
            {
                cafe.GetFurniture(currentApplianceId).CurrentUser = null;
            }
            /**
             * Raw food that needs to be cut just dissapears as it would be no different from raw food that was not touched
             * Cut food that was not finished is added as separate incase other chef would want to finish it
             */

            if (currentGoal == Goal.CookFood)
            {
                cafe.halfFinishedOrders.Push(goalOrderId);
            }
            switch (currentGoal)
            {         
                case Goal.CookFood:
                case Goal.PrepareFood:
                case Goal.TakeFood:
                    //notify cafe about order needing to be finished
                    cafe.AddNewOrder(goalOrderId);
                    break;
            }
            GD.PrintErr("Cooks are not complete!");
        }
        void BeFree()
        {
            currentApplianceId = -1;
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
            switch (currentGoal)
            {
                //find new applience and move food there
                case Goal.CookFood:
                    //true we found new appliance suitable to continue work
                    var stove = cafe.FindClosestFurniture(Furniture.FurnitureType.Stove,Position, out temp);
                    if (forceCancel || stove == null)
                    {
                        cafe.orders.Add(goalOrderId);
                        BeFree();
                    }
                    else
                    {
                        stove.CurrentUser = this;
                        currentApplianceId = cafe.GetFurnitureIndex(stove);
                        PathToTheTarget = temp;
                    }
                    break;
                //uninterruptible event(output table is immoveable because it's not an entity)
                case Goal.GiveFood:
                    break;
                    //unused goal
                case Goal.PrepareFood:
                    break;
                    //the only solution is to find new fridge
                case Goal.TakeFood:
                    //true we found new appliance suitable to continue work
                    var fridge = cafe.FindClosestFurniture(Furniture.FurnitureType.Fridge,Position, out temp);
                    if (forceCancel || fridge == null)
                    {
                        cafe.orders.Add(goalOrderId);
                        BeFree();
                    }
                    else
                    {
                        fridge.CurrentUser = this;
                        currentApplianceId = cafe.GetFurnitureIndex(fridge);
                        PathToTheTarget = temp;
                    }
                    break;
            }
        }

        public override Array<uint> GetSaveData()
        {
            //total count of CafeObject is 5; total count of Person is 2
            Array<uint> data = base.GetSaveData();
            data.Add((uint)currentGoal);//[7]
            data.Add((uint)goalOrderId);//[8]
            data.Add((uint)currentApplianceId + 1u);//[9]
            return data;
        }

        public override void SaveInit()
        {
            base.SaveInit();
            //TODO: check if internal id matches new id
            Furniture fur  = cafe.GetFurniture(currentApplianceId);
            if(fur != null)
            {
                fur.CurrentUser = this;
            }
        }

        protected override void OnTaskTimerRunOut()
        {
            base.OnTaskTimerRunOut();
            switch (currentGoal)
            {
                case Goal.TakeFood:
                    currentGoal = Goal.CookFood;
                    var stove = cafe.FindClosestFurniture(Furniture.FurnitureType.Stove, Position, out pathToTheTarget);
                    currentApplianceId = cafe.GetFurnitureIndex(stove);
                    stove.CurrentUser = this;
                    currentApplianceId = (int)stove.Id;
                    pathId = 0;
                    break;
                case Goal.PrepareFood:
                    SetTaskTimer(1);
                    break;
                case Goal.CookFood:
                    currentApplianceId = -1;
                    currentGoal = Goal.GiveFood;
                    PathToTheTarget = cafe.FindLocation("Kitchen", Position);
                    break;
            }
        }

        protected override void onArrivedToTheTarget()
        {
            base.onArrivedToTheTarget();
            if (!Fired)
            {
                //possible feature -> if all capable cooks are busy lower level cooks do preparations(first two steps) and leave food there
                switch (currentGoal)
                {
                    case Goal.TakeFood:
                        SetTaskTimer(1);
                        //move to the cutting table
                        break;
                    case Goal.CookFood:
                         SetTaskTimer(1);           
                        //move to the finish table
                        break;
                    case Goal.GiveFood:
                        cafe.AddCompletedOrder(goalOrderId);
                        BeFree();
                        //seek next task
                        //cook is idling
                        break;
                }
            }
        }
    }
}
