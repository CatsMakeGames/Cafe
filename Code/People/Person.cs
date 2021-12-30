using Godot;
using Godot.Collections;
using System;

public class Person : CafeObject
{
    protected float movementSpeed = 10;

    protected float actionSpeed = 1;

    protected Vector2[] pathToTheTarget = null;

    protected bool fired = false;

    /**<summary> If true no tasks can be assigned</summary>*/
    public bool Fired => fired;

    public Vector2[] PathToTheTarget
    {
        get => pathToTheTarget;
        set
        {
            pathToTheTarget = value;
            pathId = 0;
        }
    }

    static HumanTypes Type;

    /**<summary>Applience currently used by this person or null if none are used<para/>Mostly meant for staff that works in the kitchen</summary>*/
    protected Kitchen.Appliance appliance;

    protected bool shouldUpdate = true;

    public virtual bool ShouldUpdate => shouldUpdate;

    /**<summary>How close does object need to be to the target for that to count</summary>*/
    public float Eps = 10f;

    /**<summary>ID of the current location from pathToTheTarget</summary>*/
    protected int pathId = 0;

    /**<summary>Forces ai to either find a new furniture for the goal or to cancel task if none were found</summary>*/
    public virtual void ResetOrCancelGoal(bool forceCancel = false)
    {
    }

    /**<summary>Put current task in the the task array and leave through the main door</summary>*/
    public virtual void GetFired()
    {
        fired = true;

        pathToTheTarget =  cafe.FindLocation("Exit", Position);
        pathId = 0;
    }

    public Person(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder) : base(texture, size, textureSize, cafe, pos, zorder)
    {
        //load speed from the table
    }

    public Person(Cafe cafe,uint[] saveData):base(cafe,saveData)
    {
        
    }

    /**<summary>Executed when staff member arrives to their goal</summary>*/
    protected virtual void onArrivedToTheTarget()
    {
        if (fired)
        {
            Destroy();
        }
    }

    public override Array<uint> GetSaveData()
    {   Array<uint> data = new Array<uint>();
        if(pathToTheTarget != null)
        {

        }
        //if there is no target or player is at the target -> just set
        else
        {

        }
        return base.GetSaveData();
    }

    public void Update(float deltaTime)
    {
        //we have nowhere to go so we stop
        if (pathToTheTarget == null || pathId >= pathToTheTarget.Length) { return; }

        //move customer along the path
        Position += (pathToTheTarget[pathId] - position).Normalized() * movementSpeed;
        if (position.DistanceTo(pathToTheTarget[pathId]) <= 10f)
        {
            pathId++;
            if (pathId >= pathToTheTarget.Length)
            {
                onArrivedToTheTarget();
            }
        }
    }
}
