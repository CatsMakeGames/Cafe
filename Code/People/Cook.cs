﻿using System;
using Godot;

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

        //prepare and cook are basically wait tasks done using timers so no need for update function
        public override bool ShouldUpdate => base.ShouldUpdate && currentGoal != Goal.None && goalOrderId > -1;


        public Cook(Texture texture, Cafe cafe, Vector2 pos) : base(texture, new Vector2(128, 128), texture.GetSize(), cafe, pos, (int)ZOrderValues.Customer)
        {
        }

        protected override async void onArrivedToTheTarget()
        {
            base.onArrivedToTheTarget();
            //possible feature -> if all capable cooks are busy lower level cooks do preparations(first two steps) and leave food there
            switch (currentGoal)
            {
                case Goal.TakeFood:
                    await ToSignal(cafe.GetTree().CreateTimer(1), "timeout");
                    currentGoal = Goal.CookFood;
                    cafe.FindClosestAppliance(Position, typeof(Kitchen.Stove),out pathToTheTarget);
                    pathId = 0;
                    //move to the cutting table
                    break;
                case Goal.PrepareFood:
                    await ToSignal(cafe.GetTree().CreateTimer(1), "timeout");
                    //move to the applience
                    break;
                case Goal.CookFood:
                    await ToSignal(cafe.GetTree().CreateTimer(1), "timeout");
                    currentGoal = Goal.GiveFood;
                    PathToTheTarget = cafe.FindLocation("Kitchen", Position);
                    //move to the finish table
                    break;
                case Goal.GiveFood:
                    cafe.OnOrderComplete(goalOrderId);
                    goalOrderId = -1;
                    currentGoal = Goal.None;
                    cafe.OnCookIsFree(this);
                    //seek next task
                    //cook is idling
                    break;
            }
        }
    }
}
