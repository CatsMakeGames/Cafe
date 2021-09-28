using System;
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

        protected Goal currentGoal = Goal.None;

        public Cook(Texture texture, Cafe cafe, Vector2 pos) : base(texture, new Vector2(64, 64), texture.GetSize(), cafe, pos, (int)ZOrderValues.Customer)
        {
        }

        protected override async void onArrivedToTheTarget()
        {
            base.onArrivedToTheTarget();
            //possible feature -> if all capable cooks are busy lower level cooks do preparations(first two steps) and leave food there
            switch (currentGoal)
            {
                case Goal.TakeFood:
                    currentGoal = Goal.PrepareFood;
                    //move to the cutting table
                    break;
                case Goal.PrepareFood:
                    await ToSignal(cafe.GetTree().CreateTimer(5), "timeout");
                    //move to the applience
                    break;
                case Goal.CookFood:
                    await ToSignal(cafe.GetTree().CreateTimer(5), "timeout");
                    currentGoal = Goal.GiveFood;
                    //move to the finish table
                    break;
                case Goal.GiveFood:
                    currentGoal = Goal.None;
                    //seek next task
                    //cook is idling
                    break;
            }
        }
    }
}
