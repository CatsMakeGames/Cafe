using Godot;
using System;
using System.Linq;

public class StaffManagmentSimple : HBoxContainer
{
	public enum StaffType
	{
		Waiter,
		Cook
	};

	protected Cafe cafe;

	[Export]
	protected StaffType staffType = StaffType.Waiter;

	public override void _Ready()
	{
		base._Ready();
		cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");
	}

	private void _on_Hire_pressed()
	{
		switch (staffType)
		{
			case StaffType.Waiter:
				cafe.SpawnStaff<Staff.Waiter>("Waiter");
				break;
			case StaffType.Cook:
				cafe.SpawnStaff<Staff.Cook>("Cook");
				break;
		}
	}

	private void _on_Fire_pressed()
	{
		switch (staffType)
		{
			case StaffType.Waiter:
				cafe.People.OfType<Staff.Waiter>().FirstOrDefault(p => !p.Fired)?.GetFired();
				break;
			case StaffType.Cook:
				cafe.People.OfType<Staff.Cook>().FirstOrDefault(p => !p.Fired)?.GetFired();
				break;
		}
	}

}


