#define USE_SIMPLE_STAFF_MENU

using Godot;
using System;
using System.Linq;
using Staff;
using System.Collections.Generic;

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

	[Signal]
	public delegate void OnNewOrderAdded();

	[Signal]
	public delegate void OnCustomerArrivedAtTheTable();

	[Signal]
	public delegate void OnNewTableIsAvailable();

	[Signal]
	public delegate void OnOderFinished();
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
	public readonly int GridSize = 32;

	[Export(PropertyHint.Layers2dPhysics)]
	public int ClickTaken = 0;

	private FurnitureMover _furnitureMover;

	public bool ShouldProcessMouse => ClickTaken == 0;

	[Export(PropertyHint.Enum)]
	protected State currentState;

	public State CurrentState
	{
		get => currentState;
		set
		{
			State oldState = currentState;
			currentState = value;
			if(oldState  == value)
			{
				return;
			}
			//process old state
			switch (oldState )
			{
				case State.Idle:case State.UsingMenu:
					_cafeControlMenu.CloseAllMenus();
				break;
				case State.Building:
					_furnitureBuildPreview.Destroy();
				break;
				case State.Moving:
					_furnitureMover.Drop();
				break;
				default:
				break;
			}

			//process new state

			if (currentState == State.Building || currentState == State.Idle)
			{
				_cafeControlMenu.CloseAllMenus();
			}
			if(currentState == State.Moving || currentState == State.Building)
			{
				_cafeControlMenu.ChangeModeExitButtonVisibility(true);
			}
			if(currentState == State.Idle)
			{
				OnPaused(Paused);
			}
		}
	}

	/**<summary>How many customers are actually going to spawned even if there are no tables available</summary>*/
	[Export]
	public int MinSpawnedCustomersInQueue = 1;

	public int MaxSpawnedCustomersInQueue => MinSpawnedCustomersInQueue + Furnitures.Where(p=>p.CurrentType == Furniture.FurnitureType.Table).Count();

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

	/**<summary>Nodes used for navigation</summary>*/
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
	private List<Furniture> _furnitures = new List<Furniture>();

	public void StartBuildingItem(StoreItemData data)
	{
		_furnitureBuildPreview = new FurnitureBuildObject(
					data,
					Textures[data.TextureName],
					data.Size,
					new Vector2(32, 32),//TODO: read from table
					GridSize,
					this);
		CurrentState = State.Building;
	}

	public Furniture GetFurniture(int id)
	{
		return id  >= 0 && id < _furnitures.Count ?  _furnitures[id] : null;
	}

	public int GetFurnitureIndex(Furniture fur)
	{
		return _furnitures.IndexOf(fur);
	}
	public List<Furniture> Furnitures => _furnitures;

	/**<summary>This function adds furniture without notifying objects. Useful for loading</summary>*/
	public void AddFurniture(Furniture fur)
	{
		_furnitures.Add(fur);
	}

	/**<summary>Adds new furniture to the world as well as updates ids</summary>*/
	public void AddNewFurniture(Furniture fur)
	{
		_furnitures.Add(fur);
		fur.Init();
		fur.Id = _currentFurnitureId++;
		fur.UpdateNavigation(true);
	}
	protected Floor floor;

	protected bool pressed = false;

	protected AudioStreamPlayer PaymentSoundPlayer;

	#region WaiterToDoList
	/**<summary>List of tables where customer is sitting and waiting to have their order taken</summary>*/
	public List<int> customersToTakeOrderFrom = new List<int>();

	/**<summary>Orders that have been completed by cooks<para/>Note about how is this used: Waiters search thought the customer list and find those who want this food and who are sitted</summary>*/
	public List<int> completedOrders = new List<int>();

	public Stack<int> halfFinishedOrders = new Stack<int>();

	public Stack<int> AvailableTables = new Stack<int>();
	#endregion

	private CafeControl _cafeControlMenu;

	protected MainMenu mainMenu;

	protected Godot.Collections.Array<MouseBlockArea> mouseBlockAreas = new Godot.Collections.Array<MouseBlockArea>();

	/**<summary>Returns texture with the same name or default texture</summary>*/
	public Texture GetTexture(string name)
	{
		return Textures[name] ?? FallbackTexture;
	}

	/**<summary>List of order IDs that need to be cooked</summary>*/
	public List<int> orders = new List<int>();

	private FurnitureBuildObject _furnitureBuildPreview;

	/**<summary>More touch friendly version of the function that just makes sure that press/touch didn't happen inside of any visible MouseBlocks</summary>*/
	public bool NeedsProcessPress(Vector2 pressLocation)
	{
		return !(mouseBlockAreas.Where(p => (p.Visible) &&
		(
			pressLocation.x >= p.RectPosition.x &&
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
		_furnitureMover = new FurnitureMover(this);
		_cafeControlMenu  = GetNode<CafeControl>("UI") ?? throw new NullReferenceException("Missing menu!");
		_cafeControlMenu.Cafe = this;
		navigationTilemap = GetNode<TileMap>("Navigation2D/TileMap") ?? throw new NullReferenceException("Failed to find navigation grid");

		floor = new Floor(FloorTexture, new Vector2(1000, 1000), this);

		navigation = GetNode<Navigation2D>("Navigation2D") ?? throw new NullReferenceException("Failed to find navigation node");
		CustomerCountLabel = GetNodeOrNull<Label>("UILayer/UI/CustomerCountLabel");
		PaymentSoundPlayer = GetNode<AudioStreamPlayer>("PaymentSound");

		mainMenu = GetNode<MainMenu>("UI/MainMenu");

		foreach (var loc in Locations)
		{
			LocationNodes.Add(loc.Key, GetNodeOrNull<Node2D>(loc.Value));
		}
		foreach (var node in GetTree().GetNodesInGroup("MouseBlock"))
		{
			mouseBlockAreas.Add(node as MouseBlockArea);
		}
	}

	public void RemoveCustomerFromWaitingList(Customer customer)
	{
		int ind = People.IndexOf(customer);
		if(ind >= 0 && ind < customersToTakeOrderFrom.Count)
		{
			customersToTakeOrderFrom.RemoveAt(ind);
		}
	}

	public Vector2[] FindPathTo(Vector2 locStart, Vector2 locEnd)
	{
		return navigation?.GetSimplePath(locStart, locEnd) ?? null;
	}

	public Furniture FindClosestFurniture(Furniture.FurnitureType type, Vector2 pos, out Vector2[] path)
	{
		Furniture closest = _furnitures.Where(p => p.CurrentType == type && p.CanBeUsed).OrderBy(
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

			for (int i = _furnitures.Count - 1; i >= 0; i--)
			{
				_furnitures[i].Destroy(true);
			}
		_furnitures.Clear();
	}

	public bool CanAfford(int amount)
	{
		return amount <= money;
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		if (Input.IsActionJustPressed("movemode"))
		{
			if (CurrentState != State.UsingMenu)
			{
				playerPaused = !playerPaused;
				OnPaused(Paused);
			}
		}
		if (Input.IsActionJustPressed("save"))
		{
			Save();
			return;
		}
		if (Input.IsActionJustPressed("pause"))
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
		_furnitureBuildPreview?.OnInput(@event);
		_furnitureMover?.OnInput(@event);
	}

	/**<summary>This function creates new customer object<para/>Frequency of customer spawn is based on cafe</summary>*/
	public Customer SpawnCustomer()
	{
		Customer customer = new Customer(Textures["Customer"] ?? FallbackTexture, this, LocationNodes["Entrance"].GlobalPosition);
		people.Add(customer);
		customer.Id = _currentPersonId++;
		return customer;
	}

	public void AddCompletedOrder(int orderId)
	{
		completedOrders.Add(orderId);
		EmitSignal(nameof(OnOderFinished));
	}

	/**<summary>Finds a free cook or puts it into the list of orders</summary>*/
	public void AddNewOrder(int orderId)
	{
		orders.Add(orderId);
		EmitSignal(nameof(OnNewOrderAdded));
	}

	public void AddNewArrivedCustomer(Customer customer)
	{
		customersToTakeOrderFrom.Add(people.IndexOf(customer));
		//now it's up to waiters to find if they want to serve this table
		EmitSignal(nameof(OnCustomerArrivedAtTheTable));
	}

	public void OnCustomerFinishedMeal(Customer customer)
	{
		//TODO: Add cleaning service >:(
		//we don't have cleaning service yet
		(_furnitures[customer.CurrentTableId]).CurrentState = Furniture.State.Free;
		AddNewAvailableTable((_furnitures[customer.CurrentTableId]));
		Money += 100/*pay based on meal*/;
		PaymentSoundPlayer?.Play();
	}

	/**<summary>Notifies customers that new table is available</summary>*/
	public void AddNewAvailableTable(Furniture table)
	{
		AvailableTables.Push(_furnitures.IndexOf(table));
		EmitSignal(nameof(OnNewTableIsAvailable));
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

	public bool IsInPlayableArea(Rect2 rect)
	{
		return playableArea.Encloses(rect);
	}

	public bool IsInPlayableArea(Vector2 point)
	{
		return playableArea.HasPoint(point);
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

			//TODO: look into storing people as sets or generating new list with no invalid objects and updating the ref
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
			if(_furnitureBuildPreview != null)
			{
				_furnitureBuildPreview.Update(delta);
			}
		}
	}

	private void _on_CustomerSpawnTimer_timeout()
	{
		int customerCount = people.OfType<Customer>().Count();
		//cafe's waiting area can only hold so many people, if they don't fit -> they leave
		if (customerCount < MaxSpawnedCustomersInQueue)
		{
			SpawnCustomer();
		}
	}

	public void SellCurrentHoldingFurniture()
	{
		_furnitureMover.Sell();
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
