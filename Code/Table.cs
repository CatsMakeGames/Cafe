using Godot;
using System;

/**<summary>Stores data about what table it is as well as manages what table image to draw</summary>*/
public class Table : Furniture
{
    public enum State
    {
        Free,
        InUse,
        NeedsCleaning
    }
    public State CurrentState = State.Free;

    public override bool IsInUse => CurrentState == State.InUse;

    protected Vector2 tableTextureSize;

    public Customer CurrentCustomer;

    public override void ResetUserPaths()
    {
        base.ResetUserPaths();
        if(CurrentState == State.InUse)
        {
            CurrentState = State.Free;
        }
        if (CurrentCustomer != null)
        {
            //temporary value so we could still call the customer fucns
            var temp = CurrentCustomer;
            //clear it out to avoid stuck references and if this table is selected again customer will update this value itself
            CurrentCustomer = null;
            //make customer look for new table and go back to queue if none are found
            if (!temp.FindAndMoveToTheTable())
            {
                //If customer has to wait back in the queue then waiter has to be reset
                CurrentUser?.ResetOrCancelGoal(true);
                //throw new NotImplementedException("Function for moving customer back to queue is not yet implemented");
                GD.PrintErr("Function for moving customer back to queue is not yet implemented");
            }
        }
    }

    public override bool CanBeUsed => CurrentState == State.Free && base.CanBeUsed;

    public override void Init()
    {
        base.Init();
        cafe.OnNewTableIsAvailable(this);
    }

    /**<summary>
     * Spawns table object into the world
     * </summary>
     * <param name="texture">Table texture altas</param>
     * <param name="tableTextureSize">Size of the table image on the texture atlas</param>
     * <param name="cafe">Cafe object</param>
     * */
    public Table(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, Category category) :base(texture,size,textureSize,cafe,pos,category)
    {
       
    }
}
