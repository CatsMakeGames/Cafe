using Godot;
using System;
using System.Collections.Generic;
public class GoalPanel : ScrollContainer
{
	[Export(PropertyHint.File, "*.tscn")]
	private string _goalNodeScenePath;
	private VBoxContainer _container;
	public override void _Ready()
	{
		base._Ready();
		Cafe cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");
		cafe.Connect(nameof(Cafe.OnInitFinished), this, nameof(Init));
		_container = GetNode<VBoxContainer>("box"); 
	}

	public void Init()
	{
		Cafe cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");
		PackedScene _goalNodeScene = ResourceLoader.Load<PackedScene>(_goalNodeScenePath);
		foreach (List<Goal> goals in cafe.GoalManager.Goals.Values)
		{
			foreach (Goal goal in goals)
			{
				GoalNode node = _goalNodeScene.Instance<GoalNode>();
				node.Init(goal);
				_container.AddChild(node);
			}
		}
	}
}
