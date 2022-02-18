using Godot;
using System;
using System.Collections.Generic;

public class CafeNavigation : Navigation2D
{
	[Export]
	private Dictionary<string,string> _locationNodes = new Dictionary<string, string>();

	private Dictionary<string,Node2D> _locations = new Dictionary<string, Node2D>();

	private TileMap _navigationTilemap;



	Cafe _cafe;

	public override void _Ready()
	{
		base._Ready();
		_cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");
		_navigationTilemap = GetNode<TileMap>("TileMap") ?? throw new NullReferenceException("Failed to find navigation tilemap");
		foreach(var namePathPair in _locationNodes)
		{
			_locations[namePathPair.Key] = _cafe.GetNode<Node2D>(namePathPair.Value);
		}
	}

	/**<summary>Finds path to location defined as Node2D.<para/>Does not work for finding paths to appliances</summary>*/
	public Vector2[] FindLocation(string locationName, Vector2 location)
	{
		return GetSimplePath(location, _locations[locationName]?.GlobalPosition ?? Vector2.Zero) ?? null;
	}

	public Vector2[] FindPathTo(Vector2 locStart, Vector2 locEnd)
	{
		return GetSimplePath(locStart, locEnd) ?? null;
	}

	public void SetNavigationTile(int x,int y,bool hasNavigation)
	{
		_navigationTilemap.SetCell(x,y,hasNavigation ? 0 : -1);
	}

	/**<summary>returns global position of the cafe location</summary>*/
	public Vector2 this[string name] => _locations[name].GlobalPosition;
}
