using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class Cafe : Node2D
{
	/**<summary>Which state is player currently in<para/>
	 * Depending on these states input will be handled differently</summary>*/
	public enum State
	{
		/**<summary>Player is not in any special state</summary>*/
		Idle,
		/**<summary>Placing furniture</summary>*/
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
	public uint CurrentFurnitureId => _currentFurnitureId;
	public uint CurrentPersonId => _currentPersonId;

	/**<summary>This is used for save file naming</summary>*/
	protected int currentSaveId = 0;

	/**<Summary>This is used in save file management to help players identify their saves</summary>*/
	public string cafeName = "Get Psyched!";

	private SaveManager _saveManager;
	public SaveManager SaveManager => _saveManager;

	[Export]
	protected Rect2 playableArea;
	
	///below are furniture type definitions used by various calculations
	[Export]
	public readonly Godot.Collections.Array<Furniture.FurnitureType> DecorFurnitureTypes;
	[Export]
	public readonly Godot.Collections.Array<Furniture.FurnitureType> FoodFurnitureTypes;

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
			EmitSignal("MoneyUpdated", money);
		}
	}

	[Export]
	public readonly int GridSize = 32;

	[Export(PropertyHint.Layers2dPhysics)]
	public int ClickTaken = 0;

	private FurnitureMover _furnitureMover;

	public bool ShouldProcessMouse => ClickTaken == 0;

	[Export(PropertyHint.Enum)]
	protected State currentState;

	private Timer _customerSpawnTimer;

	private float _staffPayment = 1f;
	public float StaffPaymentMultiplier
	{
		get => _staffPayment;
		set => _staffPayment = value > 0.1f ? value : 0.1f;
	}

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
					OnPaused(Paused);
				break;
				case State.Moving:
					OnPaused(Paused);
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
				OnPaused(true);
			}
			if(currentState == State.Idle)
			{
				OnPaused(Paused);
			}
		}
	}

	/**<summary>How many customers are actually going to spawned even if there are no tables available</summary>*/
	[Export]
	public readonly int MinSpawnedCustomersInQueue = 1;

	public int MaxSpawnedCustomersInQueue => MinSpawnedCustomersInQueue + Furnitures.Count(p=>p.Value.CurrentType == Furniture.FurnitureType.Table);

	Attraction 	_attraction = new Attraction();
	public Attraction Attraction => _attraction;

	FoodData _foodData 	= new FoodData();

	/**Replace with loading from data table to allow more control over texture size or maybe use default texture size*/
	[Export]
	public Godot.Collections.Dictionary<string, Texture> Textures = new Godot.Collections.Dictionary<string, Texture>();
	/**<summary>Array of node names that correspond to a specific location node</summary>*/

	/**<summary>How much money was spent during last pay check time</summary>*/
	protected int lastSpending = 0;
	/**<summary>How much money was earned between updates</summary>*/
	protected int lastEarning = 0;

	protected Label CustomerCountLabel;
	private CafeNavigation _navigation;

	public CafeNavigation Navigation => _navigation;

	[Export(PropertyHint.File,"*.tscn")]
	private PackedScene _navigationSystemPrefab;

	protected Dictionary<uint,Person> people = new Dictionary<uint,Person>();
	/**<summary>Collection of all people in the cafe.<para/> Used for global updates or any function that applies to any human<para/>
	 * for working with specific staff members use dedicated arrays</summary>*/
	public Dictionary<uint,Person> People => people;

	/**<summary>Array containing every furniture object</summary>*/
	private Dictionary<uint,Furniture> _furnitures = new Dictionary<uint,Furniture>();

	public void StartBuildingItem(StoreItemData data)
	{
		_furnitureBuildPreview = new FurnitureBuildObject(
					data,Textures[data.TextureName],data.Size,
					new Vector2(32, 32),//TODO: read from table
					GridSize,this);
		CurrentState = State.Building;
	}

	public Furniture GetFurniture(uint id)
	{
		return _furnitures.ContainsKey(id) ? _furnitures[id] : null;
	}

	public int GetFurnitureIndex(Furniture fur)
	{
		KeyValuePair<uint, Furniture> pair = _furnitures.FirstOrDefault(p => p.Value == fur);
		return pair.Value == null ? -1 : (int)pair.Key;
	}
	public Dictionary<uint,Furniture>  Furnitures => _furnitures;

	/**<summary>This function adds furniture without notifying objects. Useful for loading</summary>*/
	public void AddFurniture(Furniture fur)
	{
		if (!_furnitures.ContainsValue(fur))
		{
			_furnitures[_currentFurnitureId++] = fur;
		}
	}

	/**<summary>Adds new furniture to the world as well as updates ids</summary>*/
	public void AddNewFurniture(Furniture fur)
	{
		
		if (!_furnitures.ContainsValue(fur))
		{
			_furnitures[_currentFurnitureId] = fur;
			GD.Print($"added{_currentFurnitureId}");
			fur.Id = _currentFurnitureId++;//because we don't know if it was set already
			fur.UpdateNavigation(true);
			fur.Init();
			UpdateAttraction();
		}
	}

	public void UpdateAttraction()
	{
		_attraction.Update(this);
	}

	private Floor _floor;
	public Floor Floor => _floor;
	protected bool pressed = false;
	protected AudioStreamPlayer PaymentSoundPlayer;

	#region WaiterToDoList
	/**<summary>List of tables where customer is sitting and waiting to have their order taken</summary>*/
	public List<uint> CustomersToTakeOrderFrom = new List<uint>();
	/**<summary>Orders that have been completed by cooks<para/>Note about how is this used: Waiters search thought the customer list and find those who want this food and who are sitted</summary>*/
	public List<int> CompletedOrders = new List<int>();
	public Stack<int> HalfFinishedOrders = new Stack<int>();
	public Stack<uint> AvailableTables = new Stack<uint>();
	/**<summary>List of order IDs that need to be cooked</summary>*/
	public List<int> Orders = new List<int>();
	#endregion
	public List<int> ShopData = new List<int>();
	private CafeControl _cafeControlMenu;
	public CafeControl CafeControlMenu => _cafeControlMenu;
	protected MainMenu mainMenu;

	protected Godot.Collections.Array<MouseBlockArea> mouseBlockAreas = new Godot.Collections.Array<MouseBlockArea>();

	private FurnitureBuildObject _furnitureBuildPreview;

	private TextureManager _textureManager;
	public TextureManager TextureManager => _textureManager;

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

	public List<float> GetSaveData()
	{
		List<float> result = new List<float>();
		result.Add((float)_currentFurnitureId);//[0]
		result.Add((float)_currentPersonId);//[1]
		result.Add((float)money);//[2]
		result.Add(_staffPayment);//[3]
		//rest of the save file will be dedicated to listing all of the unsaved orders
		return result;
	}

	public void Load(List<int> data)
	{
		_currentFurnitureId = (uint)data[0];
		_currentPersonId = (uint)data[1];
		money = data[2];
		_staffPayment = data[3];
	}
	 
	public void SpawnStaff<T>(string textureName) where T : Person
	{
		//this is simplest system of spawning that accounts for every future type
		//a simple switch could work but that means more things to keep in mind when adding new staff types
		people[_currentPersonId] =(System.Activator.CreateInstance
			(	
				typeof(T),
				_currentPersonId++,
				_textureManager[textureName],
				this,
				(new Vector2(((int)GetLocalMousePosition().x / GridSize), ((int)GetLocalMousePosition().y / GridSize))) * GridSize
			) as Person);
	}

	public override void _Ready()
	{
		_textureManager = GetNode<TextureManager>("TextureManager");
		base._Ready();	
		
		_furnitureMover = new FurnitureMover(this);
		_cafeControlMenu  = GetNode<CafeControl>("UI") ?? throw new NullReferenceException("Missing menu!");
		_cafeControlMenu.Cafe = this;
		_floor = new Floor(_textureManager["Floor"], new Vector2(1000, 1000), this);

		_navigation = GetNode<CafeNavigation>("CafeNavigation") ?? throw new NullReferenceException("Failed to find navigation system");
		CustomerCountLabel = GetNodeOrNull<Label>("UILayer/UI/CustomerCountLabel");
		PaymentSoundPlayer = GetNode<AudioStreamPlayer>("PaymentSound");

		mainMenu = GetNode<MainMenu>("UI/MainMenu");
		_customerSpawnTimer = GetNode<Timer>("CustomerSpawnTimer");
		foreach (var node in GetTree().GetNodesInGroup("MouseBlock"))
		{
			mouseBlockAreas.Add(node as MouseBlockArea);
		}	
		_cafeControlMenu.Init();
		_saveManager = new SaveManager();
	}

	public void RemoveCustomerFromWaitingList(Customer customer)
	{
		//not the prettiest way but it gets the job done
		uint ind = People.FirstOrDefault(p=>p.Value == customer).Key;
		if(ind >= 0 && ind < CustomersToTakeOrderFrom.Count)
		{
			CustomersToTakeOrderFrom.Remove(ind);
		}
	}

	public Furniture FindClosestFurniture(Furniture.FurnitureType type, Vector2 pos, out Vector2[] path)
	{
		Furniture closest = _furnitures.Where(p => p.Value.CurrentType == type && p.Value.CanBeUsed).OrderBy(
				p => p.Value.Position.DistanceSquaredTo(pos)
			).FirstOrDefault<KeyValuePair<uint,Furniture>>().Value;
		if (closest != null)
		{
			path = _navigation?.GetSimplePath(pos, closest.Position) ?? null;
			return closest;
		}
		else
		{
			path = null;
			return null;
		}
	}

	public void Save()
	{
		_saveManager.Save(this);	
	}

	/**<summary>Clears the world from current objects and spawns new ones</summary>*/
	public bool Load()
	{
		Clean();
		return _saveManager.Load(this);
	}

	public void Clean()
	{
		//clear the world because we will fill it with new data
		foreach (var person in People)
		{
			person.Value.Destroy();
		}
		people.Clear();

		foreach (var fur in _furnitures)
		{
			fur.Value.Destroy(true);
		}
		CustomersToTakeOrderFrom.Clear();
		Orders.Clear();
		HalfFinishedOrders.Clear();
		AvailableTables.Clear();
		CompletedOrders.Clear();
		_furnitures.Clear();
		_navigation.QueueFree();
		_navigation = _navigationSystemPrefab.Instance<CafeNavigation>();
		AddChild(_navigation);
	}

	public bool CanAfford(int amount) => amount <= money;

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
		//TODO: replace with proper calculation
		if (_furnitures.Values.Any(p => p.CurrentType == Furniture.FurnitureType.Table))
		{
			Random rand = new Random();
			people[_currentPersonId] = new Customer(_currentPersonId++, _textureManager["Customer"], this, _navigation["Entrance"],
				rand.Next(_attraction.CustomerLowestQuality, _attraction.CustomerHighestQuality));
			//init function needs customer to be in cafe
			people[_currentPersonId - 1].Init();
			return people[_currentPersonId - 1] as Customer;
		}
		else
		{
			return null;//we should not spawn customers if there are no tables
		}
	}

	public void AddCompletedOrder(int orderId)
	{
		CompletedOrders.Add(orderId);
		EmitSignal(nameof(OnOderFinished));
	}

	/**<summary>Finds a free cook or puts it into the list of orders</summary>*/
	public void AddNewOrder(int orderId)
	{
		Orders.Add(orderId);
		EmitSignal(nameof(OnNewOrderAdded));
	}

	public void AddNewArrivedCustomer(Customer customer)
	{
		CustomersToTakeOrderFrom.Add(people.First(p => p.Value == customer).Key);
		//now it's up to waiters to find if they want to serve this table
		EmitSignal(nameof(OnCustomerArrivedAtTheTable));
	}

	public void OnCustomerFinishedMeal(Customer customer)
	{
		//TODO: Add cleaning service >:(
		//we don't have cleaning service yet
		(_furnitures[(uint)customer.CurrentTableId]).CurrentState = Furniture.State.Free;
		AddNewAvailableTable((_furnitures[(uint)customer.CurrentTableId]));
		PaymentSoundPlayer?.Play();
	}

	/**<summary>Notifies customers that new table is available</summary>*/
	public void AddNewAvailableTable(Furniture table)
	{
		uint tableId = (uint)GetFurnitureIndex(table);
		if (!AvailableTables.Contains(tableId))
		{
			AvailableTables.Push(tableId);
			EmitSignal(nameof(OnNewTableIsAvailable));
		}
	}

	public void OnCustomerServed(Customer customer)
	{
		Money += (int)((_foodData[customer.OrderId].Price * (_attraction.CustomerSatisfaction / 100) + _foodData[customer.OrderId].Price) * _attraction.PriceMultiplier);
	}

	public bool IsInPlayableArea(Rect2 rect) =>  playableArea.Encloses(rect);
	public bool IsInPlayableArea(Vector2 point) => playableArea.HasPoint(point);

	public override void _Process(float delta)
	{
		base._Process(delta);
		if (!Paused)
		{
			if (people.Any())
			{
				people.ToList().ForEach(p =>
				{
					if ((IsInstanceValid(p.Value) && p.Value.ShouldUpdate))
					{
						p.Value.Update(delta);
					}
				});
			}
			List<uint> idsToRemove = new List<uint>();
			//if this doesn't work just write bad indices and remove them afterwards
			foreach(var person in People)
			{
				if(IsInstanceValid(person.Value) && !person.Value.Valid)
				{
					person.Value.Destroy();
					idsToRemove.Add(person.Key);
				}
			}
			foreach(uint key in idsToRemove)
			{
				people.Remove(key);
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
		//perform timer update
		//this number was chosen randomly, but it looked nice in spreadsheets :D
		_customerSpawnTimer.WaitTime = _attraction.CustomerAttraction > 0 ? 20 / _attraction.CustomerAttraction : 1f;
	}

	public void SellCurrentHoldingFurniture() => _furnitureMover.Sell();

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
		people.Where(p => !p.Value.Fired).ToList().ForEach(p => { payment += p.Value.Salary; });
		Money -= (int)(payment * StaffPaymentMultiplier);
	}
}
