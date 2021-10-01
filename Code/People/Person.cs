using Godot;
using System;

public class Person : CafeObject
{
    protected float movementSpeed = 20;

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

    protected bool shouldUpdate = true;

    public virtual bool ShouldUpdate => shouldUpdate;

    /**<summary>How close does object need to be to the target for that to count</summary>*/
    public float Eps = 10f;

    /**<summary>ID of the current location from pathToTheTarget</summary>*/
    protected int pathId = 0;

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
