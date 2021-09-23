using Godot;
using System;

/* All items drawing order
 * 0 - floor
 * 1- furniture
 * 2- customers(so they'd appear in front of the furniture while moving)(maybe have it be based on row(will make customers have to do layer jumping tho)
 */
public class Cafe : Node2D
{
	/**
	* <summary>How much money player has</summary>
	*/
	[Export]
	public int Money = 1000;

	[Export]
	public Texture CustomerTexture;

	[Export]
	public Texture TableTexture;

	[Export]
	public Texture FloorTexture;

	[Export]
	public int GridSize = 32;

	/**
	 * <summary>Overall rating of the front part of the establishment</summary>
	 */
	[Export]
	public float CashierRating = 1;

	[Export]
	public float ServerRating = 1;

	[Export]
	public float DecorRating = 1;

	protected TileMap navigationTilemap;

	Godot.Collections.Array<Customer> customers = new Godot.Collections.Array<Customer>();

	Godot.Collections.Array<Table> tables = new Godot.Collections.Array<Table>();

	protected Floor floor;

	public override void _Ready()
	{
		base._Ready();
		SpawnCustomer();

		navigationTilemap = GetNode<TileMap>("Navigation2D/TileMap") ?? throw new NullReferenceException("Failed to find navigation grid");

		floor = new Floor(FloorTexture, new Vector2(1000, 1000), this); 
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		if (@event is InputEventMouseButton mouseEvent)
		{
			Vector2 resultLocation = new Vector2(((int)GetLocalMousePosition().x / GridSize) , ((int)GetLocalMousePosition().y / GridSize));
			tables.Add(new Table(TableTexture, new Vector2(64,64), resultLocation * GridSize, this));
			navigationTilemap.SetCell((int)resultLocation.x , (int)resultLocation.y, -1);
		}
	}

	/**<summary>This function creates new customer object<para/>Frequency of customer spawn is based on cafe</summary>*/
	public void SpawnCustomer()
	{
		customers.Add(new Customer(CustomerTexture,this));
	}

	public void OnCustomerServed(Customer customer)
	{
		//money update logic is fairly simple
		/*
		 each time customer is served player gets some money
		money is calculated as rating based system and on customer type
		all of which is based on current rating of the staff
		which is calculated based on what player has and who was serving them

		*/
		Money += (int)(CashierRating * ServerRating * DecorRating * 100);
	}
}
