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

	[Export]
	public  int Salary =  0;

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

	public virtual bool ShouldUpdate => shouldUpdate || Fired/*because we want ai still be able to leave*/;

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

		PathToTheTarget = cafe.FindLocation("Exit", Position);
	   
	}

	public Person(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder) : base(texture, size, textureSize, cafe, pos, zorder)
	{
		//load speed from the table
	}

	/**<summary>Default constructor that assumes size of the humans</summary>*/
	public Person(Texture texture, Cafe cafe, Vector2 pos) : base(texture, new Vector2(128, 128), texture.GetSize(), cafe, pos, (int)ZOrderValues.Customer)
	{
		//load speed from the table
	}

	public Person(Cafe cafe, uint[] saveData) : base(cafe, saveData)
	{

	}

	/**<summary>Executed when staff member arrives to their goal</summary>*/
	protected virtual void onArrivedToTheTarget()
	{
		if (fired)
		{
			pendingKill = true;
		}
	}

	public override Array<uint> GetSaveData()
	{
		Array<uint> data = new Array<uint>();
		if (pathToTheTarget != null)
		{
			//if ai has goal save that
			data.Add((uint)pathToTheTarget[pathToTheTarget.Length - 1].x);
			data.Add((uint)pathToTheTarget[pathToTheTarget.Length - 1].y);
		}
		else
		{
			data.Add((uint)position.x);
			data.Add((uint)position.y);
		}
		return base.GetSaveData();
	}

	public void Update(float deltaTime)
	{
		//we have nowhere to go so we stop
		if (pathToTheTarget == null || pathId >= pathToTheTarget.Length)
		{
			if (Fired)
			{
				pendingKill = true;
				GD.PrintErr("Destroying fired person unexpectedly. Reason: no path to the exit");
			}
			return;
		}

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
