#define USE_SIMPLE_STAFF_MENU

using Godot;
using System;
using System.Linq;
using Staff;
using System.Linq.Expressions;

//TODO: Refactor this code and make it simplier if possible
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

	/**<summary>RID of elements that is used to preview if item can be placed</summary>*/
	private RID _placementPreviewTextureRID;

	/**<summary>Id that will be given to next spawned person</summary>*/
	private uint _currentPersonId = 0;

	/**<summary>Id that will be given to next spawned furniture</summary>*/

	private uint _currentFurnitureId = 0;

	/**<summary>This is used for save file naming</summary>*/
	public int currentSaveId = 0;

	/**<Summary>This is used in save file management to help players identify their saves</summary>*/
	public string cafeName = "Get Psyched!";

	[Export]
	protected Rect2 playableArea;

	/**<summary>Additional flag that would allow player to keep game pause no matter the state<summary>*/
	protected bool playerPaused = false;
	public bool Paused => (currentState == State.Building || currentState == State.Moving)|| playerPaused;

	[Signal]
	public delegate void MoneyUpdated(int amount);

	[Signal]
	public delegate void ChangedPlayerPause(bool paused);

	/**
	* <summary>How much money player has</summary>
	*/
	[Export]
	protected int money = 1000;

	/**
		* <summary>How much money player has</summary>
		*/
	public int Money
	{
		get { return money; }
		set
		{
			money = value;
			//this way cafe manager does not care about specific implementations of
			EmitSignal("MoneyUpdated", money);
		}
	}

	/**<summary>Texture for table. Also is used as fallback texture</summary>*/
	[Export]
	public Texture FallbackTexture;

	[Export]
	public Texture FloorTexture;

	[Export]
	private int _textureFrameSize = 32;

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
				VisualServer.CanvasItemSetVisible(_placementPreviewTextureRID, false);
			}
			else if (currentState == State.Building && currentPlacingItem != null)
			{
				VisualServer.CanvasItemAddTextureRect
			(
				_placementPreviewTextureRID,
				new Rect2(new Vector2(0, 0),
				new Vector2(128, 128)),
				Textures[currentPlacingItem.TextureName].GetRid(),
				true,
				new Color(155, 0, 0),
				false,
				Textures[currentPlacingItem.TextureName].GetRid()
			);
				VisualServer.CanvasItemSetVisible(_placementPreviewTextureRID, true);
			}
			if (currentState == State.Building || currentState == State.Idle)
			{
				foreach (Control cont in menus)
				{
					cont.Visible = false;
				}

				foreach (Button but in menuToggleButtons)
				{
					but.Pressed = false;
				}
			}
			if (currentState != State.Idle && currentState != State.UsingMenu)
			{
				exitToIdleModeButton.Visible = true;
			}

			if(currentState == State.Idle)
			{
				OnPaused(Paused);
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

	/**<summary>How much money was spent during last pay check time</summary>*/
	protected int lastSpending = 0;

	/**<summary>How much money was earned between updates</summary>*/
	protected int lastEarning = 0;

	protected Label CustomerCountLabel;

	/**<summary>Nodes used for navigaiton</summary>*/
	public Godot.Collections.Dictionary<string, Node2D> LocationNodes = new Godot.Collections.Dictionary<string, Node2D>();

	protected TileMap navigationTilemap;

	/**<summary>Navigation tilemap used for the cafe<para/>Set unwalkable areas to -1 and walkable to 0</summary>*/
	public TileMap NavigationTilemap => navigationTilemap;

	public Navigation2D navigation;

	protected Godot.Collections.Array<Person> people = new Godot.Collections.Array<Person>();
	/**<summary>Collection of all people in the cafe.<para/> Used for global updates or any function that applies to any human<para/>
	 * for working with specific staff members use dedicated arrays</summary>*/
	public Godot.Collections.Array<Person> People => people;

	/**<summary>Array containing every furniture object</summary>*/
	public Godot.Collections.Array<Furniture> Furnitures = new Godot.Collections.Array<Furniture>();

	protected Floor floor;

	protected bool pressed = false;

	protected AudioStreamPlayer PaymentSoundPlayer;

	#region WaiterToDoList
	/**<summary>List of tables where customer is sitting and waiting to have their order taken</summary>*/
	public Godot.Collections.Array<int> tablesToTakeOrdersFrom = new Godot.Collections.Array<int>();

	/**<summary>Orders that have been completed by cooks<para/>Note about how is this used: Waiters search thought the customer list and find those who want this food and who are sitted</summary>*/
	public Godot.Collections.Array<int> completedOrders = new Godot.Collections.Array<int>();

	public Godot.Collections.Array<int> halfFinishedOrders = new Godot.Collections.Array<int>();
	#endregion

	protected System.Collections.Generic.List<Control> menus;

	protected System.Collections.Generic.List<ModeSelectionButton> menuToggleButtons;

	protected UI.StoreMenu storeMenu;

#if !USE_SIMPLE_STAFF_MENU
	protected StaffMenu staffMenu;
#else
	protected Control staffMenu;
#endif

	protected Button storeMenuButton;

	protected Button exitToIdleModeButton;

	protected MainMenu mainMenu;

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
		pressLocation.x < (p.RectSize.x + p.RectPosition.x) &&
		pressLocation.y < (p.RectSize.y + p.RectPosition.y))
		)).Any();
	}

	public void SpawnStaff<T>(string textureName) where T : Person
	{
		//this is simplest system of spawning that accounts for every future type
		//a simple switch could work but that means more things to keep in mind when adding new staff types
		people.Add(System.Activator.CreateInstance
			(
				typeof(T),
				Textures[textureName] ?? FallbackTexture,
				this,
				(new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize))) * GridSize
			) as Person);
			//set new id and increment it after wards
			people.Last().Id = _currentPersonId++;
	}

	public override void _Ready()
	{
		base._Ready();
		menus = GetTree().GetNodesInGroup("Menu").OfType<Control>().ToList();
		menuToggleButtons = GetNode("UI/Menu").GetChildren().OfType<ModeSelectionButton>().ToList();

		foreach (ModeSelectionButton but in menuToggleButtons)
		{
			but.cafe = this;
		}
		gridSizeP2 = (int)Math.Log(GridSize, 2);
		navigationTilemap = GetNode<TileMap>("Navigation2D/TileMap") ?? throw new NullReferenceException("Failed to find navigation grid");

		floor = new Floor(FloorTexture, new Vector2(1000, 1000), this);

		navigation = GetNode<Navigation2D>("Navigation2D") ?? throw new NullReferenceException("Failed to find navigation node");
		CustomerCountLabel = GetNodeOrNull<Label>("UILayer/UI/CustomerCountLabel");
		PaymentSoundPlayer = GetNode<AudioStreamPlayer>("PaymentSound");

		storeMenu = GetNodeOrNull<UI.StoreMenu>("UI/StoreMenu") ?? throw new NullReferenceException("Failed to find store menu");
		storeMenu.cafe = this;
		storeMenu.Visible = false;

		exitToIdleModeButton = GetNodeOrNull<Button>("UI/ExitToIdleModeButton") ?? throw new NullReferenceException("Failed to find mode reset button");


		foreach (var loc in Locations)
		{
			LocationNodes.Add(loc.Key, GetNodeOrNull<Node2D>(loc.Value));
		}

		foreach (var node in GetTree().GetNodesInGroup("MouseBlock"))
		{
			mouseBlockAreas.Add(node as MouseBlockArea);
		}

		_placementPreviewTextureRID = VisualServer.CanvasItemCreate();
		VisualServer.CanvasItemAddTextureRect
			(
				_placementPreviewTextureRID,
				new Rect2(new Vector2(0, 0),
				new Vector2(128, 128)),
				(Textures["Table"] ?? FallbackTexture).GetRid(),
				true,
				new Color(155, 0, 0),
				false,
				(Textures["Table"] ?? FallbackTexture).GetRid()
			);
		VisualServer.CanvasItemSetVisible(_placementPreviewTextureRID, false);
		VisualServer.CanvasItemSetParent(_placementPreviewTextureRID, GetCanvasItem());
		VisualServer.CanvasItemSetZIndex(_placementPreviewTextureRID, (int)ZOrderValues.MAX);

#if !USE_SIMPLE_STAFF_MENU
		staffMenu = GetNode<StaffMenu>("UI/StaffMenu");
		staffMenu.cafe = this;
		staffMenu.Create();
#else
		staffMenu = GetNode<Control>("UI/StaffManagmentMenuSimple");
#endif
	mainMenu = GetNode<MainMenu>("UI/MainMenu");
	}

	public Vector2[] FindPathTo(Vector2 locStart, Vector2 locEnd)
	{
		return navigation?.GetSimplePath(locStart, locEnd) ?? null;
	}

	public Furniture FindClosestFurniture(Furniture.FurnitureType type, Vector2 pos, out Vector2[] path)
	{
		Furniture closest = Furnitures.Where(p => p.CurrentType == type && p.CanBeUsed).OrderBy(
				p => p.Position.DistanceSquaredTo(pos)
			).FirstOrDefault<Furniture>();
		if (closest != null)
		{
			path = navigation?.GetSimplePath(pos, closest.Position) ?? null;
			return closest;
		}
		else
		{
			path = null;
			return null;
		}
	}

	/**<summary>Finds path to location defined as Node2D.<para/>Does not work for finding paths to appliencies</summary>*/
	public Vector2[] FindLocation(string locationName, Vector2 location)
	{
		return navigation?.GetSimplePath(location, LocationNodes[locationName]?.GlobalPosition ?? Vector2.Zero) ?? null;
	}

	public void _onCustomerLeft(Customer customer)
	{

	}

	/**<summary>Saves entire save data
	 * <para>The save system uses json to allow simplier addition of additional data,but to avoid having variable names copied over several times</para>
	 * <para>it will store the object data as array of ints(with each character representing the unsigned it value)</para></summary>*/
	public void Save()
	{
		SaveManager.Save(this);	
	}


	/**<summary>Clears the world from current objects and spawns new ones</summary>*/
	public bool Load()
	{
		Clean();
		return SaveManager.Load(this);
	}

	public void Clean()
	{
		//clear the world because we will fill it with new data
			//TODO: Make sure i actually cleaned everything
			for (int i = people.Count - 1; i >= 0; i--)
			{
				people[i].Destroy(true);
			}
			people.Clear();

			for (int i = Furnitures.Count - 1; i >= 0; i--)
			{
				Furnitures[i].Destroy(true);
			}
		Furnitures.Clear();
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
		Vector2 endLoc = new Vector2
			(
				((int)GetLocalMousePosition().x >> gridSizeP2) << gridSizeP2,
				((int)GetLocalMousePosition().y >> gridSizeP2) << gridSizeP2
			);

		Rect2 rect2 = new Rect2(endLoc, currentPlacingItem.Size);
		//to prevent spawning on menu
		if (playableArea.Encloses(rect2))
		{
			var fur = Furnitures.Where(p => p.CollisionRect.Intersects(rect2) || p.CollisionRect.Encloses(rect2));
			if (!fur.Any())
			{
				try
				{
					Furniture.FurnitureType type;
					Enum.TryParse<Furniture.FurnitureType>(currentPlacingItem.ClassName, out type);
					Money -= currentPlacingItem.Price;
					Furnitures.Add(new Furniture
					(
							type,
							Textures[currentPlacingItem.TextureName],
							currentPlacingItem.Size,
							new Vector2(_textureFrameSize,_textureFrameSize),
							this,
							endLoc,
							currentPlacingItem.Level,
							currentPlacingItem.FurnitureCategory
					));
					Furniture lastFur = Furnitures.Last();
					lastFur.Init();
					lastFur.Id = _currentFurnitureId++;
					lastFur.Price = currentPlacingItem.Price;
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
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		if (Input.IsActionJustPressed("movemode"))
		{
			if (CurrentState == State.Idle)
				CurrentState = State.Moving;
			else if (currentState == State.Moving)
				currentState = State.Idle;
			return;
		}
		if (Input.IsActionJustPressed("save"))
		{
			Save();
			return;
		}
		if(Input.IsActionJustPressed("pause"))
		{
			mainMenu.Visible = !mainMenu.Visible;
			playerPaused = !playerPaused;
			OnPaused(Paused);
			GD.Print($"Paused: {playerPaused}");
		}

		if (Input.IsActionJustPressed("load"))
		{
			Load();
			return;
		}
		if (Input.IsActionJustPressed("debug_fire"))
		{
			people.OfType<Cook>().FirstOrDefault()?.GetFired();
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
									//make sure we actually can place this item here
									if (!Furnitures.Where(p =>
										 (
											 p.CollisionRect.Intersects(CurrentlyMovedItem.CollisionRect) || p.CollisionRect.Encloses(CurrentlyMovedItem.CollisionRect))
											 && p != CurrentlyMovedItem
										).Any())
									{
										GD.Print("placing item");
										//make this be new place
										var loc = CurrentlyMovedItem.Position;
										CurrentlyMovedItem.Position = movedItemStartLocation;
										//clear old place
										CurrentlyMovedItem.UpdateNavigation(false);
										CurrentlyMovedItem.Position = loc;
										CurrentlyMovedItem.UpdateNavigation(true);										
										// Reset any person trying to get to this item						 
										CurrentlyMovedItem = null;
									}
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
		Customer customer = new Customer(Textures["Customer"] ?? FallbackTexture, this, LocationNodes["Entrance"].GlobalPosition);
		people.Add(customer);
		customer.FindAndMoveToTheTable();
		customer.Id = _currentPersonId++;
		return customer;
	}

	public void OnOrderComplete(int orderId)
	{
		//make waiter come and pick this up or add this to the pile of tasks
		var freeWaiter = people.OfType<Waiter>().FirstOrDefault(p => (p.CurrentGoal == Staff.Waiter.Goal.None || p.CurrentGoal == Staff.Waiter.Goal.Leave) && !p.Fired);
		if (freeWaiter is null)
		{
			completedOrders.Add(orderId);
		}
		else
		{
			var target = people.OfType<Customer>().FirstOrDefault(p => p.OrderId == orderId && p.IsAtTheTable && !p.Eating);
			if (target != null)
			{
				freeWaiter.CurrentGoal = Waiter.Goal.AcquireOrder;
				freeWaiter.currentOrder = orderId;
				freeWaiter.PathToTheTarget = FindLocation("Kitchen", freeWaiter.Position);
				freeWaiter.currentCustomer = target;
			}
		}
	}

	/**<summary>Finds a free cook or puts it into the list of orders</summary>*/
	public void OnNewOrder(int orderId)
	{
		if (orderId != -1)
		{
			var freeCook = people.OfType<Cook>().FirstOrDefault(p => p.currentGoal == Cook.Goal.None && !p.Fired);
			if (freeCook is null)
			{
				orders.Add(orderId);
			}
			else
			{
				freeCook.TakeNewOrder(orderId);
			}
		}
	}

	public void _onCustomerArrivedAtTheTable(Customer customer)
	{
		if (customer.CurrentTableId != -1)
		{
			(Furnitures[customer.CurrentTableId]).CurrentCustomer = customer;
			//make waiter go to the table
			//if no free waiters are available -> add to the list of waiting people
			//each time waiter is done with the task they will read from the list 
			//lists priority goes in the order opposite of the values in Goal enum
			Waiter freeWaiter = people.OfType<Waiter>().FirstOrDefault(p => (p.CurrentGoal == Waiter.Goal.None || p.CurrentGoal == Waiter.Goal.Leave) && !p.Fired);
			if (freeWaiter != null)
			{
				Furniture table = (Furnitures[customer.CurrentTableId]);
				freeWaiter.PathToTheTarget = navigation.GetSimplePath(freeWaiter.Position, Furnitures[customer.CurrentTableId].Position) ?? throw new NullReferenceException("Failed to find path to the table!");
				freeWaiter.CurrentGoal = Waiter.Goal.TakeOrder;
				freeWaiter.currentCustomer = table.CurrentCustomer;
				table.CurrentUser = freeWaiter;
			}
			else
			{
				tablesToTakeOrdersFrom.Add(customer.CurrentTableId);
			}
		}


	}

	public void _onCustomerFinishedEating(Customer customer, int payment)
	{
		//we don't have cleaning service yet
		(Furnitures[customer.CurrentTableId]).CurrentState = Furniture.State.Free;
		OnNewTableIsAvailable((Furnitures[customer.CurrentTableId]));
		Money += payment;
		PaymentSoundPlayer?.Play();
	}

	/**<summary>Finds customer that was not yet sitted and assignes them a table</summary>*/
	public void OnNewTableIsAvailable(Furniture table)
	{
		Customer unSittedCustomer = people.OfType<Customer>().FirstOrDefault(customer => !customer.IsAtTheTable && !customer.Eating && !customer.MovingToTheTable);
		if (unSittedCustomer != null)
		{
			unSittedCustomer?.FindAndMoveToTheTable();
		}
		else
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
		else if (currentState == State.Building)
		{
			//notexactly happy with this check running nearly every frame
			//but testing shows that is has not much effect on the performance
			Vector2 endLoc = new Vector2
			(
				((int)GetLocalMousePosition().x >> gridSizeP2) << gridSizeP2,
				((int)GetLocalMousePosition().y >> gridSizeP2) << gridSizeP2
			);

			Rect2 rect2 = new Rect2(endLoc, currentPlacingItem.Size);
			var fur = Furnitures.Where(p => p.CollisionRect.Intersects(rect2) || p.CollisionRect.Encloses(rect2));
			if (!fur.Any() && playableArea.Encloses(rect2))
			{
				VisualServer.CanvasItemSetModulate(_placementPreviewTextureRID, new Color(0, 1, 0));
			}
			else
			{
				VisualServer.CanvasItemSetModulate(_placementPreviewTextureRID, new Color(1, 0, 0));
			}
			VisualServer.CanvasItemSetTransform(_placementPreviewTextureRID, new Transform2D
				(
					0,new Vector2
					(
						((int)GetLocalMousePosition().x >> gridSizeP2) << gridSizeP2,
						((int)GetLocalMousePosition().y >> gridSizeP2) << gridSizeP2
					)
				));
		}
		else if (currentState == State.Moving && CurrentlyMovedItem != null)
		{
			if (!Furnitures.Where(p =>
			(
			p.CollisionRect.Intersects(CurrentlyMovedItem.CollisionRect) || p.CollisionRect.Encloses(CurrentlyMovedItem.CollisionRect))
			&& p != CurrentlyMovedItem
			).Any() || !playableArea.Encloses(CurrentlyMovedItem.CollisionRect))
			{
				CurrentlyMovedItem.TextureColor = new Color(1, 1, 1);
			}
			else
			{
				CurrentlyMovedItem.TextureColor = new Color(1, 0, 0);
			}
		}
	}

	private void _on_CustomerSpawnTimer_timeout()
	{
		int customerCount = people.OfType<Customer>().Count(customer => !customer.IsAtTheTable);
		if (customerCount < MaxSpawnedCustomersInQueue)
		{
			SpawnCustomer();
		}
		else
		{
			QueuedNotSpawnedCustomersCount++;
		}
	}

	/**<summary>Changes state of the given menu if current state allows that</summary>*/
	public void ToggleMenu(Control menu,bool button_pressed)
	{
		if (currentState == State.UsingMenu || currentState == State.Idle || currentState == State.Moving)
		{
			foreach(Control _menu in menus)
			{
				_menu.Hide();
			}
			if (menu != null)
			{
				menu.Visible = button_pressed;
				currentState = button_pressed ? State.UsingMenu : State.Idle;
			}

			//reset all buttons
			//pressed button show set it's state by itself
			foreach(Button but in menuToggleButtons)
			{
				but.SetPressedNoSignal(false);
			}		
		}
	}
	private void _on_ExitToIdleModeButton_pressed()
	{
		exitToIdleModeButton.Visible = false;
		currentState = State.Idle;
		if (CurrentlyMovedItem != null)
		{
			CurrentlyMovedItem = null;
		}
	}

	private void _on_SellButton_pressed()
	{
		if (CurrentlyMovedItem != null)
		{
			CurrentlyMovedItem.Position = movedItemStartLocation;
			Money += CurrentlyMovedItem.Price;
			Furnitures.Remove(CurrentlyMovedItem);
			CurrentlyMovedItem.ResetUserPaths();
			CurrentlyMovedItem.Destroy();
			CurrentlyMovedItem = null;
			currentState = State.Idle;
		}
	}

	private void OnPaused(bool paused)
	{
		var timers = GetTree().GetNodesInGroup("Timers");
		foreach(Timer timer in timers)
		{
			timer.Paused = paused;
		}
		EmitSignal(nameof(ChangedPlayerPause),Paused);
	}

	private void _on_PaycheckTimer_timeout()
	{
		//loop over all the staff and count how much money you owe them
		int payment = 0;
		people.Where(p => !p.Fired).ToList().ForEach(p => { payment += p.Salary; });
		Money -= payment;
		GD.Print($"You payed {payment} to your staff");
	}
}
