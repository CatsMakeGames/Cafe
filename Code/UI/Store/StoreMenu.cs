using Godot;
using System;
using System.Linq;
using Godot.Collections;

namespace UI
{
    /**<summary>Store ui based on nodes rather then using visual server</summary>*/
    public class StoreMenu : Control
    {
        [Export(PropertyHint.ResourceType, "Font")]
        Font categoryFont;

        protected VBoxContainer itemContainer;

        public Cafe cafe;

        Godot.Collections.Array<StoreItemData> data = new Godot.Collections.Array<StoreItemData>();

        [Export()]
        private Godot.Collections.Array<Texture> _iconTextures = new Array<Texture>();

        [Export(PropertyHint.File, "*.tscn")]
        public string ButtonScenePath;

        [Export]
        int _itemsPerLine = 4;

        [Export]
        int _itemSize = 128;

        protected PackedScene buttonScene;

        protected Label nameLabel;

        protected Label descriptionLabel;

        /**<summary>Spawn child nodes based on data</summary>*/
        protected void Create()
        {
            foreach (var category in Enum.GetValues(typeof(Furniture.Category)))
            {
                var arr = data.Where(p => p.DisplayCategory == (Furniture.Category)category);
                if (arr.Any())
                {
                    //make label and container
                    Label name = new Label();
                    name.Text = ((Furniture.Category)category).ToString();
                    if (categoryFont != null)
                        name.AddFontOverride("font", categoryFont);

                    itemContainer.AddChild(name);
                    //make horizontal container
                    HBoxContainer container = new HBoxContainer();
                    container.RectMinSize = new Vector2(_itemSize, _itemSize);
                    itemContainer.AddChild(container);
                    HBoxContainer currentContainer = container;
                    int elemCount = 0;
                    foreach (var item in arr)
                    {

                        StoreMenuItemButton button = buttonScene.InstanceOrNull<StoreMenuItemButton>() ?? throw new NullReferenceException("Unable to create isntance from buttom template. Maybe template is incorrect?");
                        currentContainer.AddChild(button);
                        button.Texture.Texture = cafe.TextureManager[item.TextureName];
                        button.ItemData = item;
                        button.ParentMenu = this;
                        button.PriceLabel.Text = item.Price.ToString();

                        if (!cafe.ShopData.Contains(item.tableId))
                        {
                            button.Modulate = Color.Color8(125, 0, 0);
                        }
                        elemCount++;
                        if (elemCount >= _itemsPerLine)
                        {
                            currentContainer = new HBoxContainer();
                            currentContainer.RectMinSize = new Vector2(_itemSize, _itemSize);
                            itemContainer.AddChild(currentContainer);
                            elemCount = 0;
                        }
                    }

                    container.RectMinSize = new Vector2(_itemSize * elemCount, _itemSize);
                }
            }

        }

        /**<summary>Process child button being pressed<para/>Logic is placed here to avoid having referenced to cafe in buttons and because menu loads/saves data</summary>*/
        public void OnButtonPressed(StoreItemData data, StoreMenuItemButton button)
        {
            if (cafe.Money >= data.Price)
            {
                //check if was purchased
                if ( cafe.ShopData.Contains(data.tableId))
                {
                   cafe.StartBuildingItem(data);
                }
                else
                {
                    cafe.Money -= data.Price;
                    cafe.ShopData.Add(data.tableId);
                    button.Modulate = Color.Color8(255, 255, 255);
                    //play some noise
                    //we don't jump to placing to let player know that it was purchased rather then selected
                }
            }
        }

        public void DisplayItemInfo(StoreItemData info)
        {
			nameLabel.Text = info.Name;
			descriptionLabel.Text = info.Description;
        }

		public void HideItemInfo()
		{
			nameLabel.Text = "Nothing";
			descriptionLabel.Text = "Select any item, to learn about it";
		}

        protected void Load()
        {
            File file = new File();
            if (!file.FileExists("res://Data/storeData.dat"))
            {
                GD.PrintErr("Failed to find store items table");
                return;
            }
            file.Open("res://Data/storeData.dat", File.ModeFlags.Read);
            if (file.IsOpen())
            {
                string[] line;
                while (!file.EofReached())
                {
                    try
                    {
                        line = file.GetCsvLine(";");
                        data.Add(new StoreItemData(line));
                    }
                    catch (Exception e)
                    {
                        //ignore cause first line is column names and that causes errors
                    }
                }
            }
        }

        public override void _Ready()
        {
            base._Ready();
            cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");
            if (!ResourceLoader.Exists(ButtonScenePath))
            {
                throw new NullReferenceException($"Unable to find button template for store menu! Given path: {ButtonScenePath}");
            }
            buttonScene = ResourceLoader.Load<PackedScene>(ButtonScenePath);
            itemContainer = GetNodeOrNull<VBoxContainer>("ScrollContainer/VBoxContainer")
                ?? throw new NullReferenceException("Failed to find container for store items.\n There must be scroll box with child vbox attached to menu node");
            nameLabel = GetNode<Label>("itemInfoContainer/ItemName");
            descriptionLabel = GetNode<Label>("itemInfoContainer/Description");        
        }

        /**<summary>Initialises the menu<para/>
        There is an error caused by godot calling _Ready on this object before cafe, so this must be called ONLY after cafe updated values for this object</summary>*/
        public void Init()
        {
            Load();
            Create();
			HideItemInfo();
        }
    }

}
