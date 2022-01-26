using Godot;
using System;

/**<summary>The whole point of this class is to create simplier methods of managing menu</summary>*/
public class ModeSelectionButton : Button
{
	/**<summary>Menu toggled by this button</summary>*/
	public Control Menu;

	/**<summary> Path to controlled menu node relative to the cafe node </summary>*/
	[Export]
	public string MenuNodePath;

	public Cafe cafe;

	public override void _Ready()
	{
		base._Ready();
        cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");
		Menu = cafe.GetNode<Control>(MenuNodePath);
	}

	public override void _Toggled(bool buttonPressed)
	{
		base._Toggled(buttonPressed);
		//tell cafe to update menu
		cafe?.ToggleMenu(Menu,buttonPressed);
		//quick hack that allows us to reset button
		SetPressedNoSignal(buttonPressed);
	}
}
