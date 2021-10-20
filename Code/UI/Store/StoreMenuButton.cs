using Godot;
using System;

namespace UI
{
    public class StoreMenuItemButton : TextureButton
    {
        /**<summary>Menu which is responsible for handling interation<para>Used instead of signals because it's faster this way(i think)</para></summary>*/
        public StoreMenu ParentMenu;

        public StoreItemData ItemData;

        public override void _Ready()
        {
            base._Ready();
            Connect("pressed", this, nameof(onPressed));
        }

        protected void onPressed()
        {
            ParentMenu?.OnButtonPressed(ItemData, this);
        }
    }
}