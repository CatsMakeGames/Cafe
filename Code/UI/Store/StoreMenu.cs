using Godot;
using System;
using System.Linq;

namespace UI
{
	/**<summary>Store ui based on nodes rather then using visual server</summary>*/
	public class StoreMenu : Control
	{
		[Export(PropertyHint.ResourceType, "Font")]
		Font categoryFont;

		protected VBoxContainer itemContainer;

		public Cafe cafe;

		/**<summary>Key is id of the item in the table and value is true if item is bought</summary>*/
		Godot.Collections.Array<ushort> purchasedItems = new Godot.Collections.Array<ushort>();

		Godot.Collections.Array<StoreItemData> data = new Godot.Collections.Array<StoreItemData>();

		/**<summary>saves purchasedItems to the file<para/>Save data is simply continous line of 16bit unsigned integerts(2 byte numbers always bigger then 0) each representing an id</summary>*/
		bool savePurchaseData()
		{
			var save = new File();
			Directory dir = new Directory();
			//file fails to create file if directory does not exist
			if (!dir.DirExists("user://Cafe/"))
				dir.MakeDir("user://Cafe/");

			var err = save.Open("user://Cafe/store.sav", File.ModeFlags.Write);
			if (err == Error.Ok)
			{
				foreach (var item in purchasedItems)
				{
					save.Store16(item);
				}
				save.Close();
				return true;
			}
			else
			{
				GD.PrintErr(err.ToString());
			}
			return false;
		}

		bool loadPurchaseData()
		{
			var save = new File();
			var err = save.Open("user://Cafe/store.sav", File.ModeFlags.Read);
			if (err == Error.Ok)
			{
				while (!save.EofReached())
				{
					purchasedItems.Add(save.Get16());
					GD.Print(purchasedItems);
				}
				save.Close();

				return true;
			}
			else
			{
				GD.PrintErr(err.ToString());
			}
			return false;
		}

		/**<summary>Spawn child nodes based on data</summary>*/
		protected void Create()
		{
			
			foreach (var category in Enum.GetValues(typeof(Furniture.Category)))
			{
				var arr = data.Where(p => p.DisplayCategory == (Furniture.Category)category);
				if(arr.Any())
                {
					//make label and container
					Label name = new Label();
					name.Text = ((Furniture.Category)category).ToString();
					if(categoryFont != null)
						name.AddFontOverride("font", categoryFont);

					itemContainer.AddChild(name);
					//make horizontal container
					HBoxContainer container = new HBoxContainer();
					itemContainer.AddChild(container);
					foreach (var item in arr)
                    {
						//make this a custom class that can store additional data
						UI.StoreMenuItemButton button = new UI.StoreMenuItemButton();
						button.TextureNormal = cafe.Textures[item.TextureName] ?? cafe.TableTexture;
						button.ItemData = item;
						button.ParentMenu = this;
						if(!purchasedItems.Contains(item.tableId))
							button.Modulate = Color.Color8(125,125,125);
						container.AddChild(button);
						//price tag
						Label price = new Label();
						price.Text = item.Price.ToString();
						if (categoryFont != null)
							price.AddFontOverride("font", categoryFont);
						price.Align = Label.AlignEnum.Center;
						price.Valign = Label.VAlign.Center;
						button.AddChild(price);

					}
                }
			}
			
		}

		/**<summary>Process child button being pressed<para/>Logic is placed here to avoid having refenced to cafe in buttons and because menu loads/saves data</summary>*/
		public void OnButtonPressed(StoreItemData data, StoreMenuItemButton button)
		{
			if (cafe.Money >= data.Price)
			{
				cafe.Money -= data.Price;
				//check if was purchased
				if (purchasedItems.Contains(data.tableId))
				{
					//select the item
					cafe.currentPlacingItem = data;
					cafe.CurrentState = Cafe.State.Building;
				}
				else
				{

					purchasedItems.Add(data.tableId);
					button.Modulate = Color.Color8(255, 255, 255);
					savePurchaseData();
					//play some noise
					//we don't jump to placing to let player know that it was purchased rather then selected

				}
			}
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
			itemContainer = 
				GetNodeOrNull<VBoxContainer>("ScrollContainer/VBoxContainer")
				?? throw new NullReferenceException("Failed to find container for store items.\n There must be scroll box with child vbox attached to menu node");
			loadPurchaseData();
			Load();
			Create();
		}
	}

}