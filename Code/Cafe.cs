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
	/**<summary>Which state is player currently in<para/>
	 * Depending on these states input will be handled differently</summary>*/
	public enum State
	{
		/**<summary>Player is not in any special state</summary>*/
		Idle,
		/**<summary>Placing funriniture</summary>*/
		Building,
		/**<summary>Moving/deleting/selling furniture</summary>*/
		Moving,
		/**<summary>Player is currently browsing menu</summary>*/
		UsingMenu
	}

	public bool Paused => currentState == State.Building || currentState == State.Moving;
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

	/**<summary>2^n = GridSize</summary>*/
	public int gridSizeP2;

	[Export(PropertyHint.Layers2dPhysics)]
	public int ClickTaken = 0;

	/**<summary>Data for what object is going to be spawned via building system</summary>*/
	public StoreItemData currentPlacingItem = null;

	public bool ShouldProcessMouse => ClickTaken == 0;

	/**<summary>Item currently being moved around by player</summary>*/
	public Furniture CurrentlyMovedItem = null;

	/**<summary>Location where currently moved item was taken from</summary>*/
	protected Vector2 movedItemStartLocation;

	[Export(PropertyHint.Enum)]
	protected State currentState;

	public State CurrentState 
	{
		get => currentState;
		set
		{
			currentState = value;
			if (currentState != State.Moving)
			{
				CurrentlyMovedItem = null;
			}

			if (currentState != State.Building)
			{
				currentPlacingItem = null;
			}
			if (currentState != State.UsingMenu)
			{
				//hide all of the menus
				storeMenu.Visible = false;
				//untoggle all of the buttons
				storeMenuButton.Pressed = false;
			}
			if(currentState != State.Idle && currentState != State.UsingMenu)
			{
				exitToIdleModeButton.Visible = true;
			}
		}
	}

	/**<summary>How many customers are actually going to spawned even if there are no tables available</summary>*/
	[Export]
	public int MaxSpawnedCustomersInQueue = 2;

	/**<summary>How many customers are in the queue but not yet spawned</summary>*/
	public int QueuedNotSpawnedCustomersCount = 0;

	/**
	 * <summary>Overall rating of the front part of the establishment</summary>
	 */
	[Export]
	public float CashierRating = 1;

	[Export]
	public float ServerRating = 1;

	[Export]
	public float DecorRating = 1;

	/**Replace with loading from data table to allow more control over texture size or maybe use default texture size*/
	[Export]
	public Godot.Collections.Dictionary<string, Texture> Textures = new Godot.Collections.Dictionary<string, Texture>();
	/**<summary>Array of node names that correspond to a specific location node</summary>*/
	[Export]
	public Godot.Collections.Dictionary<string, string> Locations = new Godot.Collections.Dictionary<string, string>();

	protected Label CustomerCountLabel;

	/**<summary>Nodes used for naviagiton</summary>*/
	protected Godot.Collections.Dictionary<string, Node2D> LocationNodes = new Godot.Collections.Dictionary<string, Node2D>();

	protected TileMap navigationTilemap;

	/**<summary>Navigation tilemap used for the cafe<para/>Set unwalkable areas to -1 and walkable to 0</summary>*/
	public TileMap NavigationTilemap => navigationTilemap;

	public Navigation2D navigation;

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

	protected Godot.Collections.Array<Fridge> fridges = new Godot.Collections.Array<Fridge>();
	public Godot.Collections.Array<Fridge> Fridges => fridges;

	[Obsolete("Appliances array will be replaced with furniture array in next updates.")]
	protected Godot.Collections.Array<Appliance> appliances = new Godot.Collections.Array<Appliance>();

	/**<summary>Array containing every furniture object</summary>*/
	public Godot.Collections.Array<Furniture> Furnitures = new Godot.Collections.Array<Furniture>();
	#endregion

	protected Floor floor;

	protected bool pressed = false;

	protected AudioStreamPlayer PaymentSoundPlayer;

	#region WaiterToDoList
	/**<summary>List of tables where customer is sitting and waiting to have their order taken</summary>*/
	public Godot.Collections.Array<int> tablesToTakeOrdersFrom = new Godot.Collections.Array<int>();

	/**<summary>Orders that have been completed by cooks<para/>Note about how is this used: Waiters search thought the customer list and find those who want this food and who are sitted</summary>*/
	public Godot.Collections.Array<int> completedOrders = new Godot.Collections.Array<int>();
	#endregion

	protected UI.StoreMenu storeMenu;

	protected Button storeMenuButton;

	protected Button exitToIdleModeButton;

	protected Godot.Collections.Array<MouseBlockArea> mouseBlockAreas = new Godot.Collections.Array<MouseBlockArea>();

	#region CookToDoList
	/**<summary>List of order IDs that need to be cooked</summary>*/
	public Godot.Collections.Array<int> orders = new Godot.Collections.Array<int>();
	#endregion

	/**<summary>More touch friendly version of the function that just makes sure that press/touch didn't happen inside of any visible MouseBlocks</summary>*/
	public bool NeedsProcessPress(Vector2 pressLocation)
	{
		return !(mouseBlockAreas.Where(p => (p.Visible) && 
		(pressLocation.x >= p.RectPosition.x &&
		pressLocation.y >= p.RectPosition.y && 
		pressLocation.x < (p.RectSize.x + p.RectPosition.x )&&
		pressLocation.y < (p.RectSize.y + p.RectPosition.y))
		)).Any();
	}

	public override void _Ready()
	{
		base._Ready();
		//SpawnCustomer();

	
		gridSizeP2 = (int)Math.Log(GridSize, 2);
		GD.Print(gridSizeP2);
		navigationTilemap = GetNode<TileMap>("Navigation2D/TileMap") ?? throw new NullReferenceException("Failed to find navigation grid");

		floor = new Floor(FloorTexture, new Vector2(1000, 1000), this);

		navigation = GetNode<Navigation2D>("Navigation2D") ?? throw new NullReferenceException("Failed to find navigation node");

		CustomerCountLabel = GetNodeOrNull<Label>("UILayer/UI/CustomerCountLabel");

		PaymentSoundPlayer = GetNode<AudioStreamPlayer>("PaymentSound");

		storeMenu = GetNodeOrNull<UI.StoreMenu>("UI/StoreMenu") ?? throw new NullReferenceException("Failed to find store menu");
		storeMenu.cafe = this;
		storeMenu.Visible = false;

		storeMenuButton = GetNodeOrNull<Button>("Menu/StoreButton") ?? throw new NullReferenceException("Failed to find store menu activation button");

		exitToIdleModeButton = GetNodeOrNull<Button>("ExitToIdleModeButton") ?? throw new NullReferenceException("Failed to find mode reset button");


		foreach (var loc in Locations)
		{
			LocationNodes.Add(loc.Key, GetNodeOrNull<Node2D>(loc.Value));
		}

		for (int i = 0; i < 5; i++)
		{
			Waiter waiter = new Waiter(WaiterTexture, this, (new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize))) * GridSize);
			//waiter.Connect(nameof(Waiter.OnWaiterIsFree), this, nameof(OnWaiterIsFree));
			people.Add(waiter);
			waiters.Add(waiter);

			Cook cook = new Cook(CookTexture, this, (new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize))) * GridSize);
			people.Add(cook);
			cooks.Add(cook);
		}

		foreach(var node in GetTree().GetNodesInGroup("MouseBlock"))
		{
			mouseBlockAreas.Add(node as MouseBlockArea);
		}
	}

	public Vector2[] FindPathTo(Vector2 locStart, Vector2 locEnd)
	{
		return navigation?.GetSimplePath(locStart, locEnd) ?? null;
	}

	/**<summary>Finds closest furniture of that type that can be used</summary>*/
	public FurnitureType FindClosestFurniture<FurnitureType>(Vector2 pos, out Vector2[] path) where FurnitureType : Furniture
	{
		var type_fur = typeof(FurnitureType);
		var apps = Furnitures.Where(p => p.GetType() == type_fur && p.CanBeUsed);
		var furnitures = apps as Furniture[] ?? apps.ToArray();
		if (furnitures.Any())
		{
			float distSq = furnitures.ElementAt(0).Position.DistanceSquaredTo(pos);
			float dist = 0;
			int smallestId = 0;
			for (int i = 1; i < furnitures.Count(); i++)
			{
				dist = furnitures.ElementAt(i).Position.DistanceSquaredTo(pos);
				if (distSq >= dist)
				{
					distSq = dist;
					smallestId = i;
				}
			}
			path = navigation?.GetSimplePath(pos, furnitures.ElementAt(smallestId).Position) ?? null;
			return furnitures.ElementAt(smallestId) as FurnitureType;
		}
		path = null;
		return null;
	}

	/**<summary>Finds path to location defined as Node2D.<para/>Does not work for finding paths to appliencies</summary>*/
	public Vector2[] FindLocation(string locationName, Vector2 location)
	{
		return navigation?.GetSimplePath(location, LocationNodes[locationName]?.GlobalPosition ?? Vector2.Zero) ?? null;
	}

	public void _onCustomerLeft(Customer customer)
	{
		if (people.Contains(customer))
		{
			
		}
	}

	public void PlaceNewFurniture()
	{
		if (currentPlacingItem == null || Money < currentPlacingItem.Price || currentState != State.Building)
		{
			return;
		}
		//if we convert to int -> divide by grid size -> multiply by grid size we get location converted to grid
		//for small speed benefit it bitshifts to the right to divide by two( int)GetLocalMousePosition().x  >> gridSizeP2 is same as int)GetLocalMousePosition().x /GridSize)
		//and them bitshifts to the left to multiply by gridSize again
		//2^gridSizeP2 = GridSize
		//it's mostly like that because i wanted to play around with optimizing basic math operations today :D
		Vector2 endLoc = new Vector2(((int)GetLocalMousePosition().x  >> gridSizeP2) << gridSizeP2, ((int)GetLocalMousePosition().y  >> gridSizeP2) << gridSizeP2) ;
		Rect2 rect2 = new Rect2(endLoc, new Vector2(GridSize, GridSize));
		var fur = Furnitures.Where(p => rect2.Intersects(new Rect2(p.Position, p.Size)));

		if (!fur.Any())
		{
			try
			{
				Type type = Type.GetType(currentPlacingItem.ClassName/*must include any namespace used*/, true);
				Money -= currentPlacingItem.Price;
				Furnitures.Add(System.Activator.CreateInstance
								(
									type,
									Textures[currentPlacingItem.TextureName],
									new Vector2(128, 128),//TODO: make this dynamic you fool
									Textures[currentPlacingItem.TextureName].GetSize(),
									this,
									endLoc,
									currentPlacingItem.FurnitureCategory
								) as Furniture);
				Furniture lastFur = Furnitures.Last();
				//clear tilemap underneath
				//tilemap is 32x32
				var size = lastFur.Size;
				var pos = lastFur.Position;
				//calculate before hand to avoid recalculating each iteration
				int width = ((int)(size.x + pos.x)) >> gridSizeP2;
				int height = ((int)(size.y + pos.y)) >> gridSizeP2;
				for (int x = ((int)(pos.x)) >> gridSizeP2/*convert location to tilemap location*/; x < width; x++)
				{
					for (int y = ((int)(pos.y)) >> gridSizeP2; y < height; y++)
					{
						navigationTilemap.SetCell(x, y, -1);
					}
				}

				lastFur.Init();
			}
			catch (Exception e)
			{
				GD.PrintErr($"Unable to find or load type. Error: {e.Message} Type: {currentPlacingItem?.ClassName ?? null}");
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		if(Input.IsActionJustPressed("movemode"))
		{
			if(CurrentState == State.Idle)
				CurrentState = State.Moving;
			else if (currentState == State.Moving)
				currentState = State.Idle;
			return;
		}
		if (!GetTree().IsInputHandled() && NeedsProcessPress(GetLocalMousePosition()))
		{
			if (@event is InputEventMouseButton mouseEvent)
			{
				if (!pressed && mouseEvent.Pressed)
				{
					if (mouseEvent.ButtonIndex == (int)ButtonList.Left)
					{
						GD.Print("button");
						switch (CurrentState)
						{
							case State.Building:
								PlaceNewFurniture();
								break;
							case State.Moving:
								//first we allow player to select furniture to move
								if (CurrentlyMovedItem != null)
								{
									GD.Print("placing item");
									//make this be new place
									var loc = CurrentlyMovedItem.Position;
									CurrentlyMovedItem.Position = movedItemStartLocation;
									//clear old place
									CurrentlyMovedItem.UpdateNavigation(false);
									CurrentlyMovedItem.Position = loc;
									CurrentlyMovedItem.UpdateNavigation(true);

									/*
									 * Reset any person trying to get to this item
									 */
									CurrentlyMovedItem = null;
									return;
								}
								else
								{
									Vector2 mouseLoc = new Vector2
										(
											((int)GetLocalMousePosition().x >> gridSizeP2) << gridSizeP2,
											((int)GetLocalMousePosition().y >> gridSizeP2) << gridSizeP2
										);
									//find based on click
									//because items are not ordered based on the grid by rather based on the placement order
									//we have to use basic iteration search
									var arr = Furnitures.Where(p => (new Rect2(p.Position, p.Size).HasPoint(mouseLoc)));
									if (arr.Any())
									{
										//take first element and work with it
										CurrentlyMovedItem = arr.ElementAt(0);
										movedItemStartLocation = CurrentlyMovedItem.Position;
										GD.Print($"Started to move item: {CurrentlyMovedItem}");
									}
								}
								break;
							default:
								break;

						}
					}
					pressed = true;
				}
				else if (!mouseEvent.Pressed)
				{
					pressed = false;
				}
			}
			if (@event is InputEventMouseMotion motionEvent)
			{
				if (CurrentlyMovedItem != null)
				{
					CurrentlyMovedItem.Position = new Vector2
										(
											((int)GetLocalMousePosition().x >> gridSizeP2) << gridSizeP2,
											((int)GetLocalMousePosition().y >> gridSizeP2) << gridSizeP2
										);

				}
			}
		}
	}

	/**<summary>This function creates new customer object<para/>Frequency of customer spawn is based on cafe</summary>*/
	public Customer SpawnCustomer()
	{
		Customer customer = new Customer(CustomerTexture, this, LocationNodes["Entrance"].GlobalPosition);
		people.Add(customer);
		return customer;
	}

	public void OnOrderComplete(int orderId)
	{
		//make waiter come and pick this up or add this to the pile of tasks
		var freeWaiter = waiters.First(p => p.CurrentGoal == Staff.Waiter.Goal.None);
		if (freeWaiter == null)
		{
			completedOrders.Add(orderId);
		}
		else
		{
			try
			{
				//find customer target
				var target = people.First
				(
					p =>
					{
						if (p is Customer customer)
						{
							return customer.OrderId == orderId && customer.IsAtTheTable && !customer.Eating;
						}

						return false;
					}
				);
				freeWaiter.CurrentGoal = Waiter.Goal.AcquireOrder;
				freeWaiter.currentOrder = orderId;
				freeWaiter.PathToTheTarget = FindLocation("Kitchen", freeWaiter.Position);
				freeWaiter.currentCustomer = target as Customer;

			}
			catch (InvalidOperationException e)
			{
				//no fitting customers were found
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
				//cache the refernce to avoid iteration
				var cook = freeCooks.ElementAt(0);
				cook.TakeNewOrder(orderId);
			}
		}
	}

	public void _onCustomerArrivedAtTheTable(Customer customer)
	{
		if (customer.CurrentTableId != -1)
		{
			(Furnitures[customer.CurrentTableId] as Table).CurrentCustomer = customer;
			//make waiter go to the table
			//if no free waiters are available -> add to the list of waiting people
			//each time waiter is done with the task they will read from the list 
			//lists priority goes in the order opposite of the values in Goal enum
			var freeWaiters = waiters.Where(p => p.CurrentGoal == Staff.Waiter.Goal.None || p.CurrentGoal == Staff.Waiter.Goal.None);
			if (!freeWaiters.Any())
			{
				tablesToTakeOrdersFrom.Add(customer.CurrentTableId);
			}
			else
			{
				var waiter = freeWaiters.ElementAt(0);
				var table = (Furnitures[customer.CurrentTableId] as Table);
				waiter.PathToTheTarget = navigation.GetSimplePath(waiter.Position, Furnitures[customer.CurrentTableId].Position) ?? throw new NullReferenceException("Failed to find path to the table!");
				waiter.CurrentGoal = Waiter.Goal.TakeOrder;
				waiter.currentCustomer = table.CurrentCustomer;
				table.CurrentUser = waiter;
			}
		}
	}

	public void _onCustomerFinishedEating(Customer customer,int payment)
	{
		//we don't have cleaning service yet
		(Furnitures[customer.CurrentTableId] as Table).CurrentState = Table.State.Free;
		OnNewTableIsAvailable((Furnitures[customer.CurrentTableId] as Table));
		Money += payment;
		PaymentSoundPlayer?.Play();
	}

	/**<summary>Finds customer that was not yet sitted and assignes them a table</summary>*/
	public void OnNewTableIsAvailable(Table table)
	{
		try
		{
			var unSittedCustomer = people.First
			(
				p =>
				{
					if (p is Customer customer)
					{
						return !customer.IsAtTheTable && !customer.Eating;
					}

					return false;
				}
			);
			(unSittedCustomer as Customer)?.FindAndMoveToTheTable();
		}
		catch (System.InvalidOperationException e)
		{
			//no unsitted customers and spawned customers were found
			if (QueuedNotSpawnedCustomersCount > 0)
			{
				SpawnCustomer().FindAndMoveToTheTable();
				QueuedNotSpawnedCustomersCount--;
			}
		}
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
		//CustomerCountLabel?.SetText($"Queue: {QueuedNotSpawnedCustomersCount.ToString()} | Tables(free/occupied) : {tables.Where(p=>p.CurrentState == Table.State.Free).Count()}/{tables.Where(p => p.CurrentState == Table.State.InUse).Count()}");
		if (!Paused)
		{
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
			}

			for (int i = people.Count - 1; i >= 0; i--)
			{
				if (IsInstanceValid(people[i]) && !people[i].Valid)
				{
					if (IsInstanceValid(people[i]))
						people[i].Destroy();
					people.RemoveAt(i);
				}
			}
		}

	}

	private void _on_CustomerSpawnTimer_timeout()
	{
		var cust = people.Where
				(
					p =>
					{
						if (p is Customer customer)
						{
							return !customer.IsAtTheTable;
						}
						return false;
					}
				);
		if (cust.Count() < MaxSpawnedCustomersInQueue)
		{
			SpawnCustomer();
		}
		else
		{
			QueuedNotSpawnedCustomersCount++;
		}
	}

	private void _on_StoreButton_toggled(bool button_pressed)
	{
		GD.Print("Menu");
		
		if (currentState == State.UsingMenu || currentState == State.Idle )
		{
			storeMenu.Visible = button_pressed;
			currentState = button_pressed ? State.UsingMenu : State.Idle;
		}
	}

	private void _on_ExitToIdleModeButton_pressed()
	{
		exitToIdleModeButton.Visible = false;
		currentState = State.Idle;
		if(CurrentlyMovedItem != null)
		{
			CurrentlyMovedItem = null;
		}
	}
}
