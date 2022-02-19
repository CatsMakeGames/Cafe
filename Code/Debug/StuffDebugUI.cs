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
		GetChild<Label>(0).Text = $"Cooks: Total = {cafe.People.OfType<Staff.Cook>().Count().ToString()}; Idle = {cafe.People.OfType<Staff.Cook>().Where(p => p.IsFree).Count()}";
		GetChild<Label>(1).Text = $"Waiters: Total = {cafe.People.OfType<Staff.Waiter>().Count().ToString()}; Idle = {cafe.People.OfType<Staff.Waiter>().Where(p => p.IsFree).Count()}";
		GetChild<Label>(2).Text = $"Customers: Total = {cafe.People.OfType<Customer>().Count()}; Waiting: {cafe.People.OfType<Customer>().Where(p => p.orderTaken ).Count()}";

		cafe.AvailableTables.ToList().ForEach(p=>GetChild<RichTextLabel>(3).Text += $"{p}");
	}
}
