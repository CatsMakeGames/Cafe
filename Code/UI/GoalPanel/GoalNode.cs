using Godot;
using System;

public class GoalNode : ColorRect
{
	public void Init(Goal goal)
	{
		float size = 50 + 50;
		GetNode<Label>("VBoxContainer/Name").Text = goal.DisplayName;
		GetNode<Label>("VBoxContainer/Description").Text = goal.Description;
		foreach (GoalRequirement req in goal.Requirements)
		{
			var container = new HBoxContainer();
			Label name = new Label();
			Label description = new Label();
			container.AddChild(name);
			container.AddChild(description);
			name.Text = req.ObjectName;
			description.Text = ": 0/" + req.Amount.ToString();
			size += 100;
		}
		RectMinSize = new Vector2(RectMinSize.x, size);
	}
}
