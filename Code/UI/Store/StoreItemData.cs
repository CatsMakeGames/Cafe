using Godot;
using System;


    /**<summary>Class that stores item data loaded from datatable</summary>*/
    public class StoreItemData : Godot.Object
    {
        /**<summart>Id of the item in the original item table<para/>This way id stays consitstent no matter what</summart>*/
        public ushort tableId = 0x0;

        public string ClassName = nameof(Furniture);

        public string Name = nameof(Furniture);

        public string TextureName = "res://icon.png";

        public int Price = 100;

        public Furniture.Category FurnitureCategory = Furniture.Category.None;

        /**<summary>This category will be used to display in StoreMenu</summary>*/
        public Furniture.Category DisplayCategory = Furniture.Category.None;


        /**<summary>Creates Store item data from csv string</summary>*/
        public StoreItemData(string data)
        {
            string[] subData = data.Split(';');
            fillData(subData);
        }

        public StoreItemData(string[] subData)
        {
            fillData(subData);
        }

        void fillData(string[] subData)
        {
            //because id is 16bit integer that is always bigger then 0
            tableId = System.Convert.ToUInt16(subData[0]);
            Name = subData[1];
            TextureName = subData[2];
            Price = System.Convert.ToInt32(subData[4], System.Globalization.CultureInfo.InvariantCulture);
            ClassName = subData[5];
            foreach (string cat in subData[3].Split('|'))
            {
                FurnitureCategory |= (Furniture.Category)(1 << System.Convert.ToInt32(cat, System.Globalization.CultureInfo.InvariantCulture));
            }
            DisplayCategory = (Furniture.Category)(1 << System.Convert.ToInt32(subData[6], System.Globalization.CultureInfo.InvariantCulture));
        }
    }
