using System;
using Godot;

public static class SaveManager
{

    /**<summary>Where does the actual save data begin<para/> Update this based on current system</summary>*/
    public static long DataBegining = 1;
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

		Error err = saveFile.Open("user://Cafe/game.sav", File.ModeFlags.Write);
		if (err == Error.Ok)
		{
            //current save version is 2
            saveFile.Store8(2);
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

            //TODO: add other types saving

             saveFile.Close();
        }
        //TODO: Add file reading error handling
    }


    /**<summary>Loads save data from file game.sav<para/>
    cafe should be prepared(cleaned) before calling this function to avoid issues</summary>*/
    public static bool Load(Cafe cafe)
    {
        File saveFile = new File();
        Error err = saveFile.Open("user://Cafe/game.sav", File.ModeFlags.Read);
        if(err == Error.Ok)
        {
            saveFile.Seek(DataBegining + "furniture_begin".Length + 1u);
            
            uint currentDataReadingId = 0;
            uint[] loadedData = new uint[Furniture.SaveDataSize];
            while(!saveFile.EofReached())
            {
                loadedData[currentDataReadingId] = saveFile.Get32();
                currentDataReadingId++;
                if(currentDataReadingId >= Furniture.SaveDataSize)
                {
                    cafe.Furnitures.Add(new Furniture(cafe,loadedData));
                    currentDataReadingId = 0;
                }

                ulong pos = saveFile.GetPosition();
                if(saveFile.GetLine() == "furniture_end")
                {
                    break;
                }
                else
                {
                    saveFile.Seek((long)pos);
                }
            }

            //TODO: make cafe init all objects once they are loaded
            return true;
        }
        //TODO: Add file reading error handling
        return false;
    }
}