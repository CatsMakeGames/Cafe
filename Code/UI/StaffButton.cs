using Godot;
using System;

public class StaffButton : Control
{

	public Sprite StaffPicture = null;

	public Sprite FireImage = null;

	/**<summary>Staff member who this button will fire</summary>*/
	public Person Staff; 

	public override void _Ready()
	{
		base._Ready();
		StaffPicture = GetNode<Sprite>("StaffPicture") ?? throw new NullReferenceException("Unable to find sprite for staff image");

	}

	private void _on_Button_pressed()
	{
		Staff?.GetFired();
	}

	private void _on_StaffButton_mouse_entered()
	{
		FireImage?.SetVisible(true);
	}

	public override void _Draw()
	{
		base._Draw();
		//DrawRect(GetRect(), new Color(255, 0, 0));
	}


	private void _on_StaffButton_mouse_exited()
	{
		FireImage?.SetVisible(false);
	}
}
