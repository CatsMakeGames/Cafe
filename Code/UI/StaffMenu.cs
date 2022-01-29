using Godot;
using System;
using System.Linq;
using Staff;

/**<summary>Menu that is used to display staff members
 * <para/> Does not update if staff gets fired/hired! (FIX THAT!)</summary>*/
sealed public class StaffMenu  : Control 
{
    [Export(PropertyHint.ResourceType, "Font")]
    Font categoryFont;

    [Export]
    int maxItemsOnLine = 6;

    protected VBoxContainer itemContainer;

    /**<summary>Cafe where data will be taken from</summary>*/
    public Cafe cafe;

    [Export(PropertyHint.File,"*.tscn")]
    public string ButtonSceneName;

    private PackedScene buttonScene;

    public override void _Ready()
    {
        base._Ready();
        if (ResourceLoader.Exists(ButtonSceneName))
        {
            buttonScene = ResourceLoader.Load<PackedScene>(ButtonSceneName);
        }
        itemContainer = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer") ?? throw new NullReferenceException("Missing container for staff management ui buttons!");
    }

    private void CreateForType<StaffType>(System.Collections.Generic.IEnumerable<StaffType> staffMembers, string jobName) where StaffType : Person
    {
        //make label and container
        Label name = new Label();
        name.Text = jobName;
        if (categoryFont != null)
            name.AddFontOverride("font", categoryFont);

        //add label
        itemContainer.AddChild(name);

        //make container for current line of items
        HBoxContainer container = new HBoxContainer();
        container.RectMinSize = new Vector2(192, 192);
        itemContainer.AddChild(container);
        HBoxContainer currentContainer = container;
       
        int elemCount = 0;

        foreach (Person p in staffMembers)
        {
            StaffButton button = buttonScene.InstanceOrNull<StaffButton>();
            if (button != null)
            {
                currentContainer.AddChild(button);
                button.RectMinSize = new Vector2(128, 128);
                button.Staff = p;
            }
           
            //if we got overfill -> switch to the next line
            elemCount++;
            if (elemCount >= maxItemsOnLine)
            {
                currentContainer = new HBoxContainer();
                currentContainer.RectMinSize = new Vector2(128, 192);
                itemContainer.AddChild(currentContainer);
                elemCount = 0;
            }
        }
    }

    /**<summary>Generates ui</summary>*/
    public void Create()
    {

        //menu categorises staff by their jobs
        //all stuff members sadly have to either be hardcoded or put in order of their appearance

        CreateForType(cafe.People.OfType<Waiter>(), "Waiters");
        CreateForType(cafe.People.OfType<Cook>(), "Cook");
    }

}
