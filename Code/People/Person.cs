using Godot;
using Godot.Collections;
using System;

public class Person : CafeObject
{
	protected float movementSpeed = 10;

	protected float actionSpeed = 1;

	protected Vector2[] pathToTheTarget = null;

	private Vector2 _loadedDestination;

	protected bool fired = false;

	/**<summary> If true no tasks can be assigned</summary>*/
	public bool Fired => fired;

	public virtual bool IsFree => !fired;

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

	public virtual bool ShouldUpdate => shouldUpdate || Fired || TaskIsActive/*because we want ai still be able to leave*/;

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

		PathToTheTarget = cafe.Navigation.FindLocation("Exit", Position);
	}

//This is version of wait system, that can actually be saved
//set timer, activate it and use callback
#region Timers
	/**<summary>How long is current task going to take</summary>*/
	private float TaskNeededTime = 0;

	/**<summary>How long has this task been going for</summary>*/
	private float TaskCurrentTime = 0;

	/**<summary>Should timer be counted</summary>*/
	protected bool TaskIsActive = false;

	public new static uint SaveDataSize = 12u;

	/**<summary>Sets new time for timer.<para/>This will reset timer if it's active</summary>*/
	protected void SetTaskTimer(float time)
	{
		TaskNeededTime = time;
		TaskCurrentTime = 0;
		TaskIsActive = true;
	}

	protected void FinishTaskTimer()
	{
		TaskIsActive = false;
		OnTaskTimerRunOut();
	}
	protected virtual void OnTaskTimerRunOut(){}
#endregion
	public Person(uint id,Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder) : base(id,texture, size, textureSize, cafe, pos, zorder)
	{
		//load speed from the table
	}

	/**<summary>Default constructor that assumes size of the humans</summary>*/
	public Person(uint id,Texture texture, Cafe cafe, Vector2 pos) : base(id,texture, new Vector2(128, 128), texture.GetSize(), cafe, pos, (int)ZOrderValues.Customer)
	{
		//load speed from the table
	}

	public Person(Cafe cafe, uint[] saveData) : base(cafe, saveData)
	{
        _loadedDestination = new Vector2(
            (float)saveData[7],
            (float)saveData[8]
        );
		TaskIsActive 	=  	Convert.ToBoolean(saveData[9]);
		TaskNeededTime 	= 	Convert.ToSingle(saveData[10]);
		TaskCurrentTime	= 	Convert.ToSingle(saveData[11]);
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
		Array<uint> data = base.GetSaveData();
		if (pathToTheTarget != null)
		{
			//if ai has goal save that
			data.Add((uint)pathToTheTarget[pathToTheTarget.Length - 1].x);//[7]
			data.Add((uint)pathToTheTarget[pathToTheTarget.Length - 1].y);//[8]
		}
		else
		{
			data.Add((uint)position.x);//[7]
			data.Add((uint)position.y);//[5]
		}

		data.Add((uint)TaskNeededTime);//[9]
		data.Add((uint)TaskCurrentTime);//[10]
		data.Add(TaskIsActive ? 1u : 0u);//[11]
		return data;
	}

	public override void SaveInit()
	{
		base.SaveInit();
        if (_loadedDestination != Position)
        {
            PathToTheTarget = cafe.Navigation.FindPathTo(position, _loadedDestination);
		}
	}

	public override void Update(float deltaTime)
	{
		if(TaskIsActive)
		{
			TaskCurrentTime += deltaTime;
			if(TaskCurrentTime >= TaskNeededTime)
			{
				FinishTaskTimer();
			}
		}	
		
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
