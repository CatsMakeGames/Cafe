using Godot;
using System;
using System.Linq;
using Staff;
using Kitchen;

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
	public Texture CookTexture;

	[Export]
	public Texture WaiterTexture;

	[Export]
	public Texture TableTexture;

	[Export]
	public Texture FloorTexture;

	[Export]
	public Texture FridgeTexture;

	[Export]
	public Texture StoveTexture;

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

	/**<summary>Array of node names that correspond to a specific location node</summary>*/
	[Export]
	public Godot.Collections.Dictionary<string, string> Locations = new Godot.Collections.Dictionary<string, string>();

	/**<summary>Nodes used for naviagiton</summary>*/
	protected Godot.Collections.Dictionary<string, Node2D> LocationNodes = new Godot.Collections.Dictionary<string, Node2D>();

	protected TileMap navigationTilemap;

	protected Navigation2D navigation;

	#region LocationNodes
	[Obsolete("Use LocationNodes instead")]
	protected Node2D customerEntranceLocationNode;

	[Obsolete("Use LocationNodes instead")]
	protected Node2D kitchenLocationNode;
	#endregion

	protected Godot.Collections.Array<Person> people = new Godot.Collections.Array<Person>();

	#region Staff
	protected Godot.Collections.Array<Staff.Waiter> waiters = new Godot.Collections.Array<Staff.Waiter>();

	protected Godot.Collections.Array<Cook> cooks = new Godot.Collections.Array<Cook>();
	#endregion

	public Godot.Collections.Array<Person> People => people;

	#region Furniture
	protected Godot.Collections.Array<Table> tables = new Godot.Collections.Array<Table>();
	public Godot.Collections.Array<Table> Tables => tables;

	protected Godot.Collections.Array<Fridge> fridges = new Godot.Collections.Array<Fridge>();
	public Godot.Collections.Array<Fridge> Fridges => fridges;

	protected Godot.Collections.Array<Appliance> appliances = new Godot.Collections.Array<Appliance>();
	#endregion

	protected Floor floor;

	protected bool pressed = false;

	protected AudioStreamPlayer PaymentSoundPlayer;

	#region WaiterToDoList
	/**<summary>List of tables where customer is sitting and waiting to have their order taken</summary>*/
	protected Godot.Collections.Array<int> tablesToTakeOrdersFrom = new Godot.Collections.Array<int>();

	/**<summary>Orders that have been completed by cooks<para/>Note about how is this used: Waiters search thought the customer list and find those who want this food and who are sitted</summary>*/
	protected Godot.Collections.Array<int> completedOrders = new Godot.Collections.Array<int>();
	#endregion

	#region CookToDoList
	/**<summary>List of order IDs that need to be cooked</summary>*/
	protected Godot.Collections.Array<int> orders = new Godot.Collections.Array<int>();
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

		foreach (var loc in Locations)
		{
			LocationNodes.Add(loc.Key, GetNodeOrNull<Node2D>(loc.Value));
		}

		for (int i = 0; i < 1; i++)
		{
			Waiter waiter = new Waiter(WaiterTexture, this, (new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize))) * GridSize);
			//waiter.Connect(nameof(Waiter.OnWaiterIsFree), this, nameof(OnWaiterIsFree));
			people.Add(waiter);
			waiters.Add(waiter);

			Cook cook = new Cook(CookTexture, this, (new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize))) * GridSize);
			people.Add(cook);
			cooks.Add(cook);
		}
	}

	public Vector2[] FindPathTo(Vector2 locStart, Vector2 locEnd)
	{
		return navigation?.GetSimplePath(locStart, locEnd) ?? null;
	}


	public Appliance FindClosestApplience(Vector2 pos, Type type, out Vector2[] path)
	{
		var apps = appliances.Where(p => p.GetType() == type);
		if (apps.Any())
		{
			float distSq = apps.ElementAt(0).Position.DistanceSquaredTo(pos);
			float dist = 0;
			int smallestId = 0;
			for (int i = 1; i < apps.Count(); i++)
			{
				dist = apps.ElementAt(i).Position.DistanceSquaredTo(pos);
				if (distSq >= dist)
				{
					distSq = dist;
					smallestId = i;
				}
			}
			path = navigation?.GetSimplePath(pos, apps.ElementAt(smallestId).Position) ?? null;
			return apps.ElementAt(smallestId);
		}
		path = null;
		return null;
	}

	public Vector2[] FindClosestFridge(Vector2 pos)
	{
		//not the pretties way but it does the job done
		//maybe sheer of fridges that are too far
		if (fridges.Any())
		{
			float distSq = fridges[0].Position.DistanceSquaredTo(pos);
			float dist = 0;
			int smallestId = 0;
			for (int i = 1; i < fridges.Count; i++)
			{
				dist = fridges[i].Position.DistanceSquaredTo(pos);
				if (distSq >= dist)
				{
					distSq = dist;
					smallestId = i;
				}
			}
			return navigation?.GetSimplePath(pos, fridges[smallestId].Position) ?? null;
		}
		return null;
	}

	/**<summary>Find table that customer can use and can get to</summary>
	 * <param name="path">Path to the table</param>
	 *<returns>Table that customer was assigned to</returns>*/
	public Table FindTable(out Vector2[] path, Vector2 customerLocation, out int tableId)
	{
		tableId = -1;
		foreach (Table table in tables)
		{
			tableId++;
			if (table.CurrentState == Table.State.Free)
			{
				path = navigation.GetSimplePath(customerLocation, table.Position);
				if (path.Length > 0)
				{
					return table;
				}
			}
		}
		//no tables were found
		path = null;
		return null;
	}

	/**<summary>Finds path to location defined as Node2D.<para/>Does not work for finding paths to appliencies</summary>*/
	public Vector2[] FindLocation(string locationName, Vector2 location)
	{
		return navigation?.GetSimplePath(location, LocationNodes[locationName]?.GlobalPosition ?? Vector2.Zero) ?? null;
	}

	[Obsolete]
	public Vector2[] FindExit(Vector2 customerLocation)
	{
		return navigation?.GetSimplePath(customerLocation, customerEntranceLocationNode.GlobalPosition);
	}

	[Obsolete]
	public Vector2[] FindKitchen(Vector2 staffLocation)
	{
		return navigation?.GetSimplePath(staffLocation, customerEntranceLocationNode.GlobalPosition); ;
	}

	public void _onCustomerLeft(Customer customer)
	{
		if (people.Contains(customer))
		{
			
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
					tables.Add(new Table(TableTexture ?? ResourceLoader.Load<Texture>("res://icon.png"), new Vector2(64, 64), resultLocation * GridSize, this));
					navigationTilemap.SetCell((int)resultLocation.x, (int)resultLocation.y, -1);
				}

				else if (mouseEvent.ButtonIndex == (int)ButtonList.Right)
				{
					fridges.Add(
						new Fridge(
							FridgeTexture ?? ResourceLoader.Load<Texture>("res://icon.png"),
							new Vector2(64, 64),
							new Vector2(128, 128),
							this,
							new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize)) * GridSize)
						);
				}
				else if (mouseEvent.ButtonIndex == (int)ButtonList.Middle)
				{
					appliances.Add(
						new Stove(
							StoveTexture ?? ResourceLoader.Load<Texture>("res://icon.png"),
							new Vector2(64, 64),
							new Vector2(128, 128),
							this,
							new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize)) * GridSize)
						);
				}
				pressed = true;
			}
			else if (!mouseEvent.Pressed)
			{
				pressed = false;
			}
		}
	}

	/**<summary>This function creates new customer object<para/>Frequency of customer spawn is based on cafe</summary>*/
	public void SpawnCustomer()
	{
		Customer customer = new Customer(CustomerTexture, this, LocationNodes["Entrance"].GlobalPosition);
		customer.Connect(nameof(Customer.FinishEating), this, nameof(_onCustomerFinishedEating));
		customer.Connect(nameof(Customer.ArivedToTheTable), this, nameof(_onCustomerArrivedAtTheTable));
		people.Add(customer);
	}

	public void OnWaiterIsFree(Waiter waiter)
	{
        if (completedOrders.Any())
        {
			waiter.PathToTheTarget = navigation.GetSimplePath(waiter.Position, tables[completedOrders[0]].Position) ?? throw new NullReferenceException("Failed to find path to the table!");
			waiter.CurrentGoal = Waiter.Goal.AcquireOrder;
			waiter.currentCustomer = tables[completedOrders[0]].CurrentCustomer;
			completedOrders.RemoveAt(0);
		}
			
		//search through the list and find tasks that can be completed
		if (tablesToTakeOrdersFrom.Any())
		{
			waiter.PathToTheTarget = navigation.GetSimplePath(waiter.Position, tables[tablesToTakeOrdersFrom[0]].Position) ?? throw new NullReferenceException("Failed to find path to the table!");
			waiter.CurrentGoal = Waiter.Goal.TakeOrder;
			waiter.currentCustomer = tables[tablesToTakeOrdersFrom[0]].CurrentCustomer;
			tablesToTakeOrdersFrom.RemoveAt(0);
		}
	}

	public void OnCookIsFree(Cook cook)
	{
		if(orders.Any())
        {
			cook.currentGoal = Cook.Goal.TakeFood;
			cook.goalOrderId = orders[0];
			cook.PathToTheTarget = FindClosestFridge(Position);
			orders.RemoveAt(0);
		}
	}

	public void OnOrderComplete(int orderId)
	{
		//make waiter come and pick this up or add this to the pile of tasks
		var freeWaiters = waiters.Where(p => p.CurrentGoal == Staff.Waiter.Goal.None);
		if (!freeWaiters.Any())
		{
			completedOrders.Add(orderId);
		}
		else
		{
			//find customer target
			var targets = people.Where
				(
					p =>
					{
						if (p is Customer customer)
						{
							return customer.OrderId == orderId && customer.IsAtTheTable;
						}
						return false;
					}
				);
			if (targets.Any())
			{
				var waiter = freeWaiters.First();
				waiter.CurrentGoal = Waiter.Goal.AcquireOrder;
				waiter.PathToTheTarget = FindLocation("Kitchen", waiter.Position);
				waiter.currentCustomer = targets.First() as Customer;
			}
		}
	}

	/**<summary>Finds a free cook or puts it into the list of orders</summary>*/
	public void OnNewOrder(int orderId)
	{
		if (orderId != -1)
		{
			var freeCooks = cooks.Where(p => p.currentGoal == 0/*because "None" is the first in the enum anyway*/);
			if (!freeCooks.Any())
			{
				orders.Add(orderId);
			}
			else
			{
				//cach the refernce to avoid iteration
				var cook = freeCooks.ElementAt(0);
				cook.currentGoal = Cook.Goal.TakeFood;
				cook.goalOrderId = orderId;
				cook.PathToTheTarget = FindClosestFridge(Position);
				GD.Print("It's cooking time!");
			}
		}
	}

	public void _onCustomerArrivedAtTheTable(Customer customer)
	{
		if (customer.CurrentTableId != -1)
		{
			tables[customer.CurrentTableId].CurrentCustomer = customer;
			//make waiter go to the table
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
				waiter.currentCustomer = tables[customer.CurrentTableId].CurrentCustomer;
			}
		}
	}

	private void _onCustomerFinishedEating(int payment)
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
		if (people.Any())
		{
			foreach (Person person in people)
			{
				if (IsInstanceValid(person))
				{
					if (person.ShouldUpdate)
						person.Update(delta);
				}
			}
			for (int i = people.Count - 1; i >= 0; i--)
			{
				if (IsInstanceValid(people[i]) && !people[i].Valid)
				{
					people.RemoveAt(i);
				}
			}
		}
	}
	private void _on_CustomerSpawnTimer_timeout()
	{
		SpawnCustomer();
	}
}
