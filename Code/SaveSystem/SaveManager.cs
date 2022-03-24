using System;
using Godot;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;

public class SaveManager
{

	/**<summary>Where does the actual save data begin<para/>
	Should be based on all of the  static/fixed sized data
	<para/> Update this based on current system</summary>*/
	public long DataBegining = 1;

	public byte CurrentSaveSystemVersion = 6;

	private int _currentSaveId;

	public int CurrentSaveId
	{
		get => _currentSaveId;
		set => _currentSaveId = value;
	}
	public void StorePerson<T>(File saveFile, Cafe cafe) where T : Person
	{
		saveFile.StoreLine($"{typeof(T).Name}_begin");
		var people = cafe.People.Values.OfType<T>();
		foreach (T person in people)
		{
			if (!person.Fired && person.Valid)
			{
				Godot.Collections.Array<uint> data = person.GetSaveData();
				foreach (uint dat in data)
				{
					saveFile.Store32(dat);
				}
			}
		}
		saveFile.StoreLine($"{typeof(T).Name}_end");
	}

	private void _storeCafe(File saveFile, Cafe cafe)
	{
		saveFile.StoreLine("cafe_begin");
		foreach (float dat in cafe.GetSaveData())
		{
			saveFile.StoreFloat(dat);
		}
		saveFile.StoreLine("cafe_end");

		saveFile.StoreLine("cafe_arrays_begin");
		foreach (int dat in cafe.CustomersToTakeOrderFrom)
		{
			saveFile.Store32((uint)dat);
		}
		saveFile.StoreLine("array_break");
		foreach (int dat in cafe.CompletedOrders)
		{
			saveFile.Store32((uint)dat);
		}
		saveFile.StoreLine("array_break");

		foreach (int dat in cafe.HalfFinishedOrders)
		{
			saveFile.Store32((uint)dat);
		}
		saveFile.StoreLine("array_break");
		foreach (int dat in cafe.AvailableTables)
		{
			saveFile.Store32((uint)dat);
		}
		saveFile.StoreLine("array_break");

		foreach (int dat in cafe.Orders)
		{
			saveFile.Store32((uint)dat);
		}
		saveFile.StoreLine("array_break");

		foreach (int dat in cafe.ShopData)
		{
			saveFile.Store32((uint)dat);
		}
		saveFile.StoreLine("array_break");
		saveFile.StoreLine("cafe_arrays_end");
	}

	private List<int> _loadArray(Cafe cafe, File saveFile)
	{
		List<int> res = new List<int>();
		while (!saveFile.EofReached())
		{
			ulong pos = saveFile.GetPosition();
			var debug = saveFile.GetLine();
			if (debug.Contains("array_break"))
			{
				return res;
			}
			else
			{
				saveFile.Seek((long)pos);
				res.Add((int)saveFile.Get32());
			}
		}
		return res;
	}

	private List<int> _loadArray(Cafe cafe, File saveFile,string endName = "array_break")
	{
		List<int> res = new List<int>();
		while (!saveFile.EofReached())
		{
			ulong pos = saveFile.GetPosition();
			var debug = saveFile.GetLine();
			if (debug.Contains(endName))
			{
				return res;
			}
			else
			{
				saveFile.Seek((long)pos);
				res.Add((int)saveFile.Get32());
			}
		}
		return res;
	}

	private List<uint> _loadArrayUint(Cafe cafe, File saveFile)
	{
		List<uint> res = new List<uint>();
		while (!saveFile.EofReached())
		{
			ulong pos = saveFile.GetPosition();
			var debug = saveFile.GetLine();
			if (debug.Contains("array_break"))
			{
				return res;
			}
			else
			{
				saveFile.Seek((long)pos);
				res.Add(saveFile.Get32());
			}
		}
		return res;
	}

	private void _loadAttractionSystem(Cafe cafe, File saveFile)
	{
		string line = saveFile.GetLine();
		if ( line == "attr_begin")
		{
			List<uint> res = new List<uint>();
			while (!saveFile.EofReached())
			{
				ulong pos = saveFile.GetPosition();
				var debug = saveFile.GetLine();
				if (debug.Contains("attr_end"))
				{
					cafe.Attraction.Load(res);
					return;
				}
				else
				{
					saveFile.Seek((long)pos);
					res.Add(saveFile.Get32());
				}
			}
		}
	}

	private void _storeAttractionSystem(File saveFile, Cafe cafe)
	{
		saveFile.StoreLine("attr_begin");
		foreach (uint dat in cafe.Attraction.GetSaveData())
		{
			saveFile.Store32(dat);
		}
		saveFile.StoreLine("attr_end");
	}

	/**<summary>Saves all of the data in cafe into a file</summary>*/
	public void Save(Cafe cafe,ref ProgressBar progress)
	{
		//save file is structured like this -> 
		/*version name
		basic stats    
		AdditionalBlock
		{
			StoreData,
			VariousPriceValues
		}
		furniture block 
		[
			{
				furniture data
			},
			...
			{
				furniture data
			}
		}
		PeopleBlock
		{
			(for each class)
			ClassName : 
			[
				(repeat for every instance)
				class data
			]      
		}
		*/

		File saveFile = new File();
		Directory dir = new Directory();
		//file fails to create file if directory does not exist
		if (!dir.DirExists("user://Cafe/"))
			dir.MakeDir("user://Cafe/");

		Error err = saveFile.Open($"user://Cafe/game{_currentSaveId}.sav", File.ModeFlags.Write);
		if (err == Error.Ok)
		{
			progress.Value = 0;
			//we also need to store cafe name
			saveFile.Store8(CurrentSaveSystemVersion);
			progress.Value += progress.MaxValue / 10.0;//10 here is amount data objects being saved(arrays are counted as one data object)
			saveFile.StoreLine(cafe.cafeName);
			progress.Value += progress.MaxValue / 10.0;
			saveFile.Store32((uint)cafe.TextureManager.CurrentFloorTextureId);
			progress.Value += progress.MaxValue / 10.0;
			saveFile.Store32((uint)cafe.TextureManager.CurrentWallTextureId);
			progress.Value += progress.MaxValue / 10.0;
			_storeCafe(saveFile, cafe);
			progress.Value += progress.MaxValue / 10.0;
			_storeAttractionSystem(saveFile, cafe);
			progress.Value += progress.MaxValue / 10.0;
			//mainly for easier debugging
			saveFile.StoreLine("furniture_begin");
			foreach (var fur in cafe.Furnitures)
			{
                if (fur.Value.Valid)
                {
                    Godot.Collections.Array<uint> data = fur.Value?.GetSaveData();
                    foreach (uint dat in data)
                    {
                        saveFile.Store32(dat);
                    }
                }
			}
			saveFile.StoreLine("furniture_end");
			progress.Value += progress.MaxValue / 10.0;
			StorePerson<Staff.Waiter>(saveFile, cafe);
			progress.Value += progress.MaxValue / 10.0;
			StorePerson<Staff.Cook>(saveFile, cafe);
			progress.Value += progress.MaxValue / 10.0;
			StorePerson<Customer>(saveFile, cafe);
			progress.Value += progress.MaxValue / 10.0;
			saveFile.Close();
		}
		//TODO: Add file reading error handling
	}

	public void LoadPerson(Cafe cafe, File saveFile)
	{
		//compiled lambda constructor
		//using this instead of activator because it's faster compared to it when there are a lot of entities
		//because function is fast and the only slow part is compilaton
		Func<Cafe, uint[], Person> ctor;
		Type[] argTypes = new Type[2] { typeof(Cafe), typeof(uint[]) };

		//get type based on name
		string blockName = saveFile.GetLine();
		if (blockName == "") { return; }
		blockName = blockName.Substr(0, blockName.Find("_begin"));
		Type type = TypeSearch.GetTypeByName(blockName);
		//get data
		uint size = (uint)(type.GetField("SaveDataSize").GetValue(null));
		//make constructor

		ConstructorInfo info = type.GetConstructor(argTypes);
		ParameterExpression[] args = new ParameterExpression[2];
		args[0] = System.Linq.Expressions.Expression.Parameter(typeof(Cafe), "cafe");
		args[1] = System.Linq.Expressions.Expression.Parameter(typeof(uint[]), "saveData");

		ctor = System.Linq.Expressions.Expression.Lambda<Func<Cafe, uint[], Person>>(System.Linq.Expressions.Expression.New(info, args), args).Compile();
		while (!saveFile.EofReached())
		{
			ulong pos = saveFile.GetPosition();
			var debug = saveFile.GetLine();
			if (debug.Contains("_end"))
			{
				return;
			}
			else
			{
				saveFile.Seek((long)pos);
			}
			uint[] loadedData = new uint[size];
			for (uint i = 0; i < size; i++)
			{
				loadedData[i] = saveFile.Get32();
			}
			//objects save id as first element
			//adding directly to avoid cafe events
			cafe.People.Add(loadedData[0], ctor(cafe, loadedData));
		}
	}

	/**<summary>Loads save data from file game.sav<para/>
	cafe should be prepared(cleaned) before calling this function to avoid issues</summary>*/
	public bool Load(Cafe cafe,ref ProgressBar progress)
	{
		File saveFile = new File();
		Error err = saveFile.Open($"user://Cafe/game{_currentSaveId}.sav", File.ModeFlags.Read);
		if (err == Error.Ok)
		{
			const double totalDataValueCount = 10f;
			progress.Value = 0;
			byte version = saveFile.Get8();
			progress.Value += progress.MaxValue / totalDataValueCount;
			if (version != CurrentSaveSystemVersion)
			{
				throw new Exception($"Incompatible version of save system are used! Expected v{CurrentSaveSystemVersion} got v{version}");
			}
			
			cafe.cafeName = saveFile.GetLine();
			progress.Value += progress.MaxValue / totalDataValueCount;
			//TODO: add safety check to avoid invalid ids
			cafe.Floor.Texture = cafe.TextureManager.FloorTextures[(int)saveFile.Get32()];
			progress.Value += progress.MaxValue / totalDataValueCount;
			cafe.GetNode<TileMap>("TileMap").TileSet = cafe.TextureManager.WallTilesets[(int)saveFile.Get32()];
			progress.Value += progress.MaxValue / totalDataValueCount;
			//now move past this
			saveFile.Seek((long)saveFile.GetPosition() + "cafe_begin".Length + 1u);
			cafe.Load(_loadArray(cafe, saveFile,"cafe_end"));
			progress.Value += progress.MaxValue / totalDataValueCount;
			if (saveFile.GetLine() == "cafe_arrays_begin")
			{
				cafe.CustomersToTakeOrderFrom = _loadArrayUint(cafe, saveFile);
				cafe.CompletedOrders = _loadArray(cafe, saveFile);
				cafe.HalfFinishedOrders = new Stack<int>(_loadArray(cafe, saveFile));
				cafe.AvailableTables = new Stack<uint>(_loadArrayUint(cafe, saveFile));
				cafe.Orders = _loadArray(cafe, saveFile);
				cafe.ShopData = _loadArray(cafe, saveFile);
				progress.Value += progress.MaxValue / totalDataValueCount;
			}
			else
			{
				throw new Exception("Failed to find cafe arrays in save file!");
			}

			if (saveFile.GetLine() != "cafe_arrays_end")
			{
				 throw new Exception("Missing end of cafe arrays in save file!");
			}
			_loadAttractionSystem(cafe, saveFile);
			progress.Value += progress.MaxValue / totalDataValueCount;
			//cafe name is last single object data so after that we read furniture
			saveFile.Seek((long)saveFile.GetPosition() + "furniture_begin".Length + 1u);

			uint currentDataReadingId = 0;
			uint[] loadedData = new uint[Furniture.SaveDataSize];
			while (!saveFile.EofReached())
			{
				ulong pos = saveFile.GetPosition();
				if (saveFile.GetLine() == "furniture_end")
				{
					break;
				}
				else
				{
					saveFile.Seek((long)pos);
				}

				loadedData[currentDataReadingId] = saveFile.Get32();
				currentDataReadingId++;
				if (currentDataReadingId >= Furniture.SaveDataSize)
				{
					cafe.Furnitures[loadedData[0]] = new Furniture(cafe, loadedData);
					currentDataReadingId = 0;
				}
			}
			progress.Value += progress.MaxValue / totalDataValueCount;
			while (!saveFile.EofReached())
			{
				LoadPerson(cafe, saveFile);
			}
			progress.Value += progress.MaxValue / totalDataValueCount;
			foreach (var person in cafe.People)
			{
				person.Value.SaveInit();
			}
			progress.Value += progress.MaxValue / totalDataValueCount;
			foreach (var fur in cafe.Furnitures)
			{
				fur.Value.SaveInit();
			}
			progress.Value += progress.MaxValue / totalDataValueCount;
			cafe.UpdateAttraction();
			//all objects are loaded -> init them

			return true;
		}
		else if (err == Error.FileNotFound)
		{
			GD.PrintErr($"Failed to find save file \"game{_currentSaveId}.sav\".\n New save will be generated");
		}
		//TODO: Add file reading error handling
		return false;
	}
}
