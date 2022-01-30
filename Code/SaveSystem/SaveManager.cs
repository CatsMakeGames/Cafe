using System;
using Godot;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public static class SaveManager
{

    /**<summary>Where does the actual save data begin<para/>
    Should be based on all of the static/fixed sized data
    <para/> Update this based on current system</summary>*/
    public static long DataBegining = 1;

    public static byte CurrentSaveSystemVersion = 4;
    public static void StorePerson<T>(File saveFile,Cafe cafe) where T: Person
    {
        saveFile.StoreLine($"{typeof(T).Name}_begin");
        var people = cafe.People.OfType<T>();
        foreach (T person in people)
        {
            if (!person.Fired)
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

    public static void Save(Cafe cafe)
    {
        //save file is structured like this -> 
        /*version name
        basic stats
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

		Error err = saveFile.Open($"user://Cafe/game{cafe.currentSaveId}.sav", File.ModeFlags.Write);
		if (err == Error.Ok)
		{
            //we also need to store cafe name
            //current save version is 2
            saveFile.Store8(CurrentSaveSystemVersion);
            saveFile.StoreLine(cafe.cafeName);
            //mainly for easier debugging
            saveFile.StoreLine("furniture_begin");
            foreach(Furniture fur in cafe.Furnitures)
            {
                Godot.Collections.Array<uint> data = fur.GetSaveData();
                foreach(uint dat in data)
                {
                    saveFile.Store32(dat);
                }
            }
            saveFile.StoreLine("furniture_end");

            StorePerson<Staff.Waiter>(saveFile,cafe);
            StorePerson<Staff.Cook>(saveFile,cafe);
            StorePerson<Customer>(saveFile,cafe);
            saveFile.Close();
        }
        //TODO: Add file reading error handling
    }

    public static void LoadPerson(Cafe cafe,File saveFile)
    {
        //compiled lambda constructor
        //using this instead of activator because it's faster compared to it when there are a lot of entities
        //because function is fast and the only slow part is compilaton
        Func<Cafe, uint[], Person> ctor;
        Type[] argTypes = new Type[2] { typeof(Cafe), typeof(uint[]) };

        //get type based on name
        string blockName = saveFile.GetLine();
        if(blockName == ""){return;}
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
            cafe.People.Add(ctor(cafe,loadedData));            
        }
 
    }

    /**<summary>Loads save data from file game.sav<para/>
    cafe should be prepared(cleaned) before calling this function to avoid issues</summary>*/
    public static bool Load(Cafe cafe)
    {
        File saveFile = new File();
        Error err = saveFile.Open($"user://Cafe/game{cafe.currentSaveId}.sav", File.ModeFlags.Read);
        if(err == Error.Ok)
        {
            byte version = saveFile.Get8();
            if(version != CurrentSaveSystemVersion)
            {
                throw new Exception($"Incompatible version of save system are used! Expected v{CurrentSaveSystemVersion} got v{version}");
            }
            cafe.cafeName = saveFile.GetLine();
            //now move past this
           
           //cafe name is last single object data so after that we read furniture
            saveFile.Seek((long)saveFile.GetPosition() + "furniture_begin".Length + 1u);
            
            uint currentDataReadingId = 0;
            uint[] loadedData = new uint[Furniture.SaveDataSize];
            while(!saveFile.EofReached())
            {
                 ulong pos = saveFile.GetPosition();
                if(saveFile.GetLine() == "furniture_end")
                {
                    break;
                }
                else
                {
                    saveFile.Seek((long)pos);
                }

                loadedData[currentDataReadingId] = saveFile.Get32();
                currentDataReadingId++;
                if(currentDataReadingId >= Furniture.SaveDataSize)
                {
                    cafe.AddFurniture(new Furniture(cafe,loadedData));
                    currentDataReadingId = 0;
                }  
            }

            while (!saveFile.EofReached())
            {
                LoadPerson(cafe, saveFile);
            }
            foreach(Person person in cafe.People)
            {
                person.SaveInit();
            }

            foreach (Furniture fur in cafe.Furnitures)
            {
                fur.SaveInit();
            }

            //all objects are loaded -> init them

            return true;
        }
        else if(err == Error.FileNotFound)
        {
            GD.PrintErr($"Failed to find save file \"game{cafe.currentSaveId}.sav\".\n New save will be generated");
        }
        //TODO: Add file reading error handling
        return false;
    }
}