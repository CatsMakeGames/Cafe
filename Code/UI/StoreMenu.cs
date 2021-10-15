using Godot;
using System;

public class StoreMenu : Control
{
	Godot.Collections.Array<StoreItem> storeItems = new Godot.Collections.Array<StoreItem>();

	public Cafe cafe;

	[Export]
	int width;

	[Export]
	int GridSize = 256;

	VScrollBar scrollBar;

	public override void _Ready()
	{
		base._Ready();
		Load();
		GD.Print("Loaded");
		//show on screen
		//cafe = GetTree().Root.FindNode("Cafe") as Cafe ?? throw new NullReferenceException("Failed to find cafe");

		scrollBar = GetNodeOrNull<VScrollBar>("VScrollBar") ?? throw new NullReferenceException("Failed to find scroll bar");
	}

	public void Create()
	{
		int id = 0;
		foreach (StoreItem item in storeItems)
		{
			item.Create(new Vector2((id - ((int)(id / width) * width) + 0.25f), ((int)(id / width)) + 0.25f) * GridSize, this, GridSize * 0.75f);
			id++;
		}
	}

	/**<summary>Flushes and reloads </summary>*/
	void Load()
	{
		File file = new File();
		if(!file.FileExists("res://Data/storeData.dat"))
		{
			GD.PrintErr("Failed to find store items table");
			return;
		}
		file.Open("res://Data/storeData.dat", File.ModeFlags.Read);
		if(file.IsOpen())
		{
			string[] line;
			while(!file.EofReached())
			{
				try
				{
					line = file.GetCsvLine(";");
					storeItems.Add(new StoreItem(line));
				}
				catch (Exception e)
				{
					//ignore cause first line is column names and that causes errors
				}           
			}
		}
	}

	private void _onGUIInput(object @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
        {
			if (mouseEvent.ButtonIndex == (int)ButtonList.Left)
            {
				//find which square has been clicked
				Vector2 resultLocation = new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize));
				int id = (int)(resultLocation.y * width + resultLocation.x);
				if(storeItems.Count > id && id >= 0)
                {
					Vector2 loc = new Vector2(((int)GetLocalMousePosition().x / (GridSize / 9f)), ((int)GetLocalMousePosition().y / (GridSize / 9f)));
					loc = new Vector2(loc.x - 9 * (int)(loc.x / 9), loc.y - 9 * (int)(loc.y / 9));
					if(loc.x <= 9 && loc.x >= 2 && loc.y <= 9 && loc.y >=2)
                    {
						//TODO: Add setting which class will be placed via the data
						cafe.currentPlacingStoreItem = storeItems[id];
						cafe.CurrentState = Cafe.State.Building;
						GD.Print($"{loc} : {storeItems[id].ClassName}");
					}
					
				}
			}
		}
		else if(@event is InputEventScreenTouch touchEvent)
        {
			//handle touch
        }
	}

	private void _on_VScrollBar_scrolling()
	{
		GetTree().SetInputAsHandled();

	}
}