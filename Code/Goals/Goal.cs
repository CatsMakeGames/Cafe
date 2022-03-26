using System;
using System.Collections.Generic;

/**<summary>Goal type determines when will the goal check be performed</summary>*/
public enum GoalType
{
    Staff,
    Money,
    Furniture,
    Attraction,
    /**<summary>Total amount of goal types</summary>*/
    MAX
}
public enum GoalOperation
{
   Equals,
   LessThen,
   MoreThen
}

public readonly struct GoalRequirement
{
    /**<summary>Values of that needs to be checked</summary>*/
    public readonly string ObjectName;
    public readonly GoalOperation Operation;
    public readonly int Amount;

    public GoalRequirement(string objectName, GoalOperation operation, int amount)
    {
        ObjectName = objectName;
        Operation = operation;
        Amount = amount;
    }
}

public class Goal
{
    /**<summary>What goals need to be completed before this can be completed</summary>*/
    public readonly List<string> NeededGoals;
    public readonly string Name;
    public readonly string DisplayName;
    public readonly string Description;
    public readonly GoalType Type;
    public readonly List<GoalRequirement> Requirements;

    public Goal(Godot.Collections.Dictionary data)
    {
        Name = data["name"].ToString();
        DisplayName = data["display"].ToString();
        Description = data["description"].ToString();
        Type = (GoalType)Convert.ToInt32(data["type"]);

        NeededGoals = new List<string>();
        Requirements = new List<GoalRequirement>();

        if (data["prereqs"] is Godot.Collections.Array prereqs)
        {
            foreach (object prereq in prereqs)
            {
                NeededGoals.Add(prereq.ToString());
            }
        }
        if(data["requirements"] is Godot.Collections.Array reqs)
        {
            foreach(Godot.Collections.Array req in reqs)
            {
                GoalOperation op;
                switch (req[1].ToString())
                {
                    case ">":
                        op = GoalOperation.MoreThen;
                        break;
                    case "<":
                        op = GoalOperation.LessThen;
                        break;
                    case "=":
                        op = GoalOperation.Equals;
                        break;
                    default:
                        op = GoalOperation.Equals;
                        break;
                }

                Requirements.Add(
                    new GoalRequirement(req[0].ToString(),
                    op,
                    Convert.ToInt32(req[2])));
            }
        }
    }
}