
using Godot;
using System;

public class MainMenu : Control
{

    [Export]
    public int MaxSavesCount = 3;

    protected Cafe cafe;

    public Cafe CurrentCafe => cafe;

    protected Popup exitConfirmation;

    protected Popup saveConfirmation;

    protected Panel saveSlotPopup;

	 public Panel SaveSlotPopup => saveSlotPopup;


    protected VBoxContainer saveSlotContainer;

    protected bool newSaveFileMode = false;

    public bool NewSaveFileMode => newSaveFileMode;

    public override void _Ready()
    {
        base._Ready();
        cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");
        exitConfirmation = GetNode<Popup>("ConfirmationDialog");

        saveConfirmation = GetNode<Popup>("SaveConfirmation");

        saveSlotPopup = GetNode<Panel>("SaveSlotPopup");
        saveSlotContainer = GetNode<VBoxContainer>("SaveSlotPopup/SaveSlotContainer");
    }

    private void _on_Exit_pressed()
    {
        exitConfirmation?.PopupCentered();
    }


    private void _on_Save_pressed()
    {
        saveConfirmation?.PopupCentered();
    }


    private void _updateSaveSlotList()
    {
        for (int i = saveSlotContainer.GetChildCount() - 1; i >= 0; i--)
        {
            saveSlotContainer.GetChild(i).QueueFree();
            saveSlotContainer.RemoveChild(saveSlotContainer.GetChild(i));
        }
        //first update the list
        Directory dir = new Directory();
        if (!dir.DirExists("user://Cafe/"))
        {
            dir.MakeDir("user://Cafe/");
        }
        for (int i = 0; i < 3; i++)
        {
            SaveSlotButton but = new SaveSlotButton(i, this);
            if (dir.FileExists($"user://Cafe/game{i}.sav"))
            {
                File saveFile = new File();
                //flex on your players by loading save files each time they open menu
                Error err = saveFile.Open($"user://Cafe/game{i}.sav", File.ModeFlags.Read);
                saveFile.Seek(1u);

                but.Text = saveFile.GetLine();

            }
            else
            {
                but.Text = "Empty cafe";
            }

            saveSlotContainer.AddChild(but);
            but.SizeFlagsHorizontal = (int)(SizeFlags.Fill | SizeFlags.Expand);
            but.SizeFlagsVertical = (int)(SizeFlags.Fill | SizeFlags.Expand);
        }

    }

    private void _on_NewCafe_pressed()
    {
        newSaveFileMode = true;
		_updateSaveSlotList();
        saveSlotPopup.Show();
    }

    private void _on_LoadCafe_pressed()
    {
         newSaveFileMode = false;
		_updateSaveSlotList();
        saveSlotPopup.Show();
    }

    private void _on_ConfirmationDialog_confirmed()
    {
        cafe?.Save();
        GetTree().Quit();
    }

    private void _on_SaveConfirmation_confirmed()
    {
        cafe?.Save();
    }
}
