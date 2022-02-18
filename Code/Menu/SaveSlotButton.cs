using Godot;
using System;

public class SaveSlotButton : Button
{
    public int SaveSlotId = 0;

    public MainMenu Menu;

    public SaveSlotButton(int slotId,MainMenu menu)
    {
        SaveSlotId = slotId;
        Menu = menu;
    }

    public override void _Pressed()
    {
        base._Pressed();
        if(Menu.NewSaveFileMode)
        {
            //TODO: ask if user wants to override file
            //if yes clean the cafe and start from the beggining
            //if no do nothing
            if(SaveSlotId == Menu.CurrentCafe.SaveManager.CurrentSaveId)
            {
                //avoid loosing data -> save cafe in a new slot
                Menu.CurrentCafe.Save();
                Menu.CurrentCafe.SaveManager.CurrentSaveId = SaveSlotId;
                Menu.CurrentCafe.Clean();
                //we are doing full reset
                Menu.GetTree().ReloadCurrentScene();
                Menu.SaveSlotPopup.Hide();
            }
        }
        else
        {
            //TODO: ask if user wants to load this file
            //call load function
            if (Menu.CurrentCafe != null)
            {
                Menu.CurrentCafe.SaveManager.CurrentSaveId = SaveSlotId;
                Menu.CurrentCafe.Load();

                Menu.SaveSlotPopup.Hide();
            }
        }
        
    }
}
