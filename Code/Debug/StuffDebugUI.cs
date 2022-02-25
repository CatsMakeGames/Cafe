using Godot;
using System;
using System.Linq;

public class StuffDebugUI : VBoxContainer
{
	Cafe cafe;

	public override void _Ready()
	{
		base._Ready();
		cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");
	}

	public override void _Process(float delta)
	{
        base._Process(delta);
        GetChild<RichTextLabel>(3).Text = "";
        GetChild<Label>(0).Text = "Customers: ";
        GetChild<Label>(1).Text = "Meals: ";
        GetChild<Label>(2).Text = $"Customers: Total = {cafe.People.OfType<Customer>().Count()}; Waiting: {cafe.People.OfType<Customer>().Where(p => p.orderTaken).Count()}";

        cafe.AvailableTables.ToList().ForEach(p => GetChild<RichTextLabel>(3).Text += $"{p}");
        cafe.CustomersToTakeOrderFrom.ToList().ForEach(p => GetChild<Label>(0).Text += $"{p}");
        cafe.Orders.ToList().ForEach(p => GetChild<Label>(1).Text += $"{p}");
    }
}
