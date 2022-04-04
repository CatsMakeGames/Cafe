using System;
using System.Collections.Generic;
using Godot;
using System.Linq;
/**<summary>Goal manager catches events and marks goals as completed if conditions are met</summary>*/
public class GoalManager : Godot.Object
{
    [Signal]
    public delegate void OnGoalCompleted(string name);
    /**<summary>This value needs to be saved during save process</summary>*/
    private List<string> _completedGoals = new List<string>();
    private Dictionary<GoalType,List<Goal>> _goals = new Dictionary<GoalType, List<Goal>>();
    private Cafe _cafe;

    public Dictionary<GoalType, List<Goal>> Goals => _goals;

    public bool IsCompleted(string name) => _completedGoals.Contains(name);

    public GoalManager(Cafe cafe)
    {
        _cafe = cafe;
        for (GoalType i = 0; i < GoalType.MAX; i++)
        {
            _goals[i] = new List<Goal>();
        }
        //load goal file
        File file = new File();
        Error err = file.Open("res://Data/goals.json", File.ModeFlags.Read);
        if (err == Error.Ok)
        { 
            JSONParseResult res =  JSON.Parse(file.GetAsText());
            file.Close();
            if (res.Result is Godot.Collections.Dictionary dict)
            {
                var array = dict["goals"] as Godot.Collections.Array;
                foreach (Godot.Collections.Dictionary goal in array)
                {
                    GoalType type = (GoalType)Convert.ToInt32(goal["type"]);
                    _goals[type].Add(new Goal(goal));
                }
            }
        }
        else
        {
            GD.PrintErr(err);
        }
    }

    public void PerformGoalCheck(GoalType type)
    {
        _checkConditions(_goals[type]);
    }

    /**<summary>Returns value from the main checked object</summary>*/
    private int _getValue(string name)
    {
        //TODO: maybe instead have string-int dictionary that is updated only when related variables are updated? 


        //i hate this
        switch (name)
        {
            case "Waiter":
                return _cafe.People.OfType<Staff.Waiter>().Count();
            case "Cook":
                return _cafe.People.OfType<Staff.Cook>().Count();
            case "Table":
                return _cafe.Furnitures.Count(p => p.Value.CurrentType == Furniture.FurnitureType.Table);
            case "Fridge":
                return _cafe.Furnitures.Count(p => p.Value.CurrentType == Furniture.FurnitureType.Fridge);
            case "Stove":
                return _cafe.Furnitures.Count(p => p.Value.CurrentType == Furniture.FurnitureType.Stove);
            default:
                GD.PrintErr($"Goal requested variable {name}, but no such variable is present");
                return -1;
        }   
    }

    private bool _checkRequirements(Goal goal)
    {
        int req_count = goal.Requirements.Count;
        for (int j = 0; j < req_count; j++)
        {
            var requirement = goal.Requirements[j];
            switch (requirement.Operation)
            {
                case GoalOperation.Equals:
                    if (_getValue(requirement.ObjectName) != requirement.Amount) { return false; }
                    break;
                case GoalOperation.LessThen:
                    if (!(_getValue(requirement.ObjectName) < requirement.Amount)) { return false; }
                    break;
                case GoalOperation.MoreThen:
                    if (!(_getValue(requirement.ObjectName) > requirement.Amount)) { return false; }
                    break;
                default:
                    break;
            }
        }
        return true;
    }

    private void _checkConditions(List<Goal> goals)
    {
        int size = goals.Count;
        for (int i = 0; i < size; i++)
        {
            if (_completedGoals.Contains(goals[i].Name)){ continue; }
            if (_checkRequirements(goals[i]))
            {
                _completedGoals.Add(goals[i].Name);
                _completedGoals.Sort();
                EmitSignal(nameof(OnGoalCompleted), goals[i].DisplayName);
                GD.Print($"Complted goal: {goals[i].DisplayName}");
            }
        }
    }
}