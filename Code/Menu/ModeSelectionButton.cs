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
	protected CafeControl _cafeControlMenu;

	public CafeControl CafeControlMenu { set => _cafeControlMenu = value; }
	public Cafe cafe;

	public void Init()
	{
		cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");
		Menu = _cafeControlMenu?.GetNodeOrNull<Control>(MenuNodePath);
	}

	public override void _Toggled(bool buttonPressed)
	{
		base._Toggled(buttonPressed);
		if (buttonPressed)
		{
			_cafeControlMenu.ShowMenu(Menu, this);
		}
		else
		{
			 _cafeControlMenu.CloseAllMenus();
		}
	}
}
