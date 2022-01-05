using Godot;
using System;
using System.Linq;

/**<summary>This menu is quickly thrown together placeholder that acts as replacement for unfittingly complex staff menu</summary>*/
public class StaffManagmentSimple : HBoxContainer
{
	public enum StaffType
	{
		Waiter,
		Cook
	};

	protected Cafe cafe;

	protected Label staffCount;

	[Export]
	protected StaffType staffType = StaffType.Waiter;

	public override void _Ready()
	{
		base._Ready();
		cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");
		staffCount = GetNode<Label>("Count");

		switch (staffType)
		{
			case StaffType.Waiter:
				staffCount.Text = cafe.People.OfType<Staff.Waiter>().Count().ToString();
				break;
			case StaffType.Cook:
				staffCount.Text = cafe.People.OfType<Staff.Cook>().Count().ToString();
				break;
		}
	}

	private void _on_Hire_pressed()
	{
		switch (staffType)
		{
			case StaffType.Waiter:
				cafe.SpawnStaff<Staff.Waiter>("Waiter");
				staffCount.Text = cafe.People.OfType<Staff.Waiter>().Count().ToString();
				break;
			case StaffType.Cook:
				cafe.SpawnStaff<Staff.Cook>("Cook");
				staffCount.Text = cafe.People.OfType<Staff.Cook>().Count().ToString();
				break;
		}
		
	}

	private void _on_Fire_pressed()
	{
		switch (staffType)
		{
			case StaffType.Waiter:
				cafe.People.OfType<Staff.Waiter>().FirstOrDefault(p => !p.Fired)?.GetFired();
				staffCount.Text = cafe.People.OfType<Staff.Waiter>().Count().ToString();
				break;
			case StaffType.Cook:
				cafe.People.OfType<Staff.Cook>().FirstOrDefault(p => !p.Fired)?.GetFired();
				staffCount.Text = cafe.People.OfType<Staff.Cook>().Count().ToString();
				break;
		}
	}

}
