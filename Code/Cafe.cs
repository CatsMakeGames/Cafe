using Godot;
using System;
using System.Linq;
using Staff;

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

	protected Navigation2D navigation;

	#region LocationNodes
	protected Node2D customerEntranceLocationNode;

	protected Node2D kitchenLocationNode;
	#endregion

	protected Godot.Collections.Array<Person> people = new Godot.Collections.Array<Person>();

	#region Staff
	protected Godot.Collections.Array<Staff.Waiter> waiters = new Godot.Collections.Array<Staff.Waiter>();
	#endregion

	public Godot.Collections.Array<Person> People => people;
	
	protected Godot.Collections.Array<Table> tables = new Godot.Collections.Array<Table>();

	public Godot.Collections.Array<Table> Tables => tables;

	protected Floor floor;

	protected bool pressed = false;

	protected AudioStreamPlayer PaymentSoundPlayer;

	#region WaiterToDoList
	/**<summary>List of tables where customer is sitting and waiting to have their order taken</summary>*/
	protected Godot.Collections.Array<int> tablesToTakeOrdersFrom = new Godot.Collections.Array<int>();
	#endregion

	public override void _Ready()
	{
		base._Ready();
		//SpawnCustomer();

		navigationTilemap = GetNode<TileMap>("Navigation2D/TileMap") ?? throw new NullReferenceException("Failed to find navigation grid");

		floor = new Floor(FloorTexture, new Vector2(1000, 1000), this);

		navigation = GetNode<Navigation2D>("Navigation2D") ?? throw new NullReferenceException("Failed to find navigation node");

		customerEntranceLocationNode = GetNode<Node2D>("Entrance") ?? throw new NullReferenceException("Failed to find cafe entrance");

		kitchenLocationNode = GetNode<Node2D>("Kitchen") ?? throw new NullReferenceException("Failed to find kitchen");

		PaymentSoundPlayer = GetNode<AudioStreamPlayer>("PaymentSound");
	}

	/**<summary>Find table that customer can use and can get to</summary>
	 * <param name="path">Path to the table</param>
	 *<returns>Table that customer was assigned to</returns>*/
	public Table FindTable(out Vector2[] path,Vector2 customerLocation,out int tableId)
	{
		tableId = -1;
		foreach (Table table in tables)
		{
			tableId++;
			if (table.CurrentState == Table.State.Free)
			{
				path = navigation.GetSimplePath(customerLocation, table.Position);
				if(path.Length > 0)
				{
					return table;
				}
			}		
		}
		//no tables were found
		path = null;
		return null;
	}

	public Vector2[] FindExit(Vector2 customerLocation)
	{
		return navigation?.GetSimplePath(customerLocation, customerEntranceLocationNode.GlobalPosition);
	}
	
	public Vector2[] FindKitchen(Vector2 staffLocation)
	{
		return navigation?.GetSimplePath(staffLocation, customerEntranceLocationNode.GlobalPosition); ;
	}

	private void _onCustomerLeft(Customer customer)
	{
		if(people.Contains(customer))
		{
			people.Remove(customer);
			GD.Print("Customer left");
		}
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (!pressed)
			{
				if (mouseEvent.ButtonIndex == (int)ButtonList.Left)
				{
					Vector2 resultLocation = new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize));
					tables.Add(new Table(TableTexture, new Vector2(64, 64), resultLocation * GridSize, this));
					navigationTilemap.SetCell((int)resultLocation.x, (int)resultLocation.y, -1);
				}

				else if (mouseEvent.ButtonIndex == (int)ButtonList.Right)
				{
					Customer customer = new Customer(CustomerTexture, this, (new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize))) * GridSize);
					customer.Connect(nameof(Customer.FinishEating), this, nameof(_onCustomerFinishedEating));
					customer.Connect(nameof(Customer.OnLeft), this, nameof(_onCustomerLeft));
					customer.Connect(nameof(Customer.ArivedToTheTable), this, nameof(_onCustomerArrivedAtTheTable));
					people.Add(customer);
				}
				else if(mouseEvent.ButtonIndex == (int)ButtonList.Middle)
				{
					Waiter waiter = new Waiter(CustomerTexture, this, (new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize))) * GridSize);
					waiter.Connect(nameof(Waiter.OnWaiterIsFree), this, nameof(_onWaiterIsFree));
					people.Add(waiter);
					waiters.Add(waiter);
				}
				pressed = true;
			}
			else if(!mouseEvent.Pressed)
			{
				pressed = false;
			}
		}
	}

	/**<summary>This function creates new customer object<para/>Frequency of customer spawn is based on cafe</summary>*/
	public void SpawnCustomer()
	{
		Customer customer = new Customer(CustomerTexture, this, customerEntranceLocationNode.Position);
		customer.Connect(nameof(Customer.FinishEating), this, nameof(_onCustomerFinishedEating));
		customer.Connect(nameof(Customer.OnLeft), this, nameof(_onCustomerLeft));
		customer.Connect(nameof(Customer.ArivedToTheTable), this, nameof(_onCustomerArrivedAtTheTable));
		people.Add(customer);
	}

	private void _onWaiterIsFree(Waiter waiter)
	{
		//search through the list and find tasks that can be completed
		if(tablesToTakeOrdersFrom.Count > 0)
		{
			waiter.PathToTheTarget = navigation.GetSimplePath(waiter.Position, tables[tablesToTakeOrdersFrom[0]].Position) ?? throw new NullReferenceException("Failed to find path to the table!");
			waiter.CurrentGoal = Staff.Waiter.Goal.TakeOrder;
			tablesToTakeOrdersFrom.RemoveAt(0);
		}
	}

	private void _onCustomerArrivedAtTheTable(Customer customer)
	{
		if (customer.CurrentTableId != -1)
		{   //make waiter go to the table
			//if no free waiters are available -> add to the list of waiting people
			//each time waiter is done with the task they will read from the list 
			//lists priority goes in the order opposite of the values in Goal enum
			var freeWaiters = waiters.Where(p => p.CurrentGoal == Staff.Waiter.Goal.None);
			if (!freeWaiters.Any())
			{
				tablesToTakeOrdersFrom.Add(customer.CurrentTableId);
			}
			else
			{
				var waiter = freeWaiters.ElementAt(0);
				waiter.PathToTheTarget = navigation.GetSimplePath(waiter.Position, tables[customer.CurrentTableId].Position) ?? throw new NullReferenceException("Failed to find path to the table!");
				waiter.CurrentGoal = Waiter.Goal.TakeOrder;
			}
		}
	}

	private void  _onCustomerFinishedEating(int payment)
	{
		Money += payment;
		PaymentSoundPlayer?.Play();
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
	public override void _Process(float delta)
	{
		base._Process(delta);
		foreach(Person person in people)
		{
			if (IsInstanceValid(person))
			{
				if (person.ShouldUpdate)
					person.Update(delta);
			}
		}
	}
}
