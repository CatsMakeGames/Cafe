using Godot;
using System;

public class Person : CafeObject
{
    protected float movementSpeed = 2;

    protected float actionSpeed = 1;

    protected Vector2[] pathToTheTarget = null;

    public Vector2[] PathToTheTarget
    {
        get => pathToTheTarget;
        set
        {
            pathToTheTarget = value;
            pathId = 0;
        }
    }

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

    public Person(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder) : base(texture, size, textureSize, cafe, pos, zorder)
    {
        //load speed from the table
    }

    /**<summary>Executed when staff member arrives to their goal</summary>*/
    protected virtual void onArrivedToTheTarget()
    {

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
