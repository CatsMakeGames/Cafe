using Godot;
using System;

namespace UI
{
    public class StoreMenuItemButton : Button
    {
        /**<summary>Menu which is responsible for handling interaction<para>Used instead of signals because it's faster this way(i think)</para></summary>*/
        public StoreMenu ParentMenu;

        public StoreItemData ItemData;

        public TextureRect Texture;

        public Label PriceLabel;

        public override void _Ready()
        {
            base._Ready();
            //hide the button part of button
            //Flat = true;

            Texture = GetNode<TextureRect>("TextureRect");
            PriceLabel = GetNode<Label>("Price");
        }

        protected void onPressed()
        {
            //while this check might seem useless and rather stupid, godot so far has an issue of if button is not
            //set to expand it shrinks weirdly, so the best idea i had is to just hide the button and do additional check
            //128 instead of 192 because of weird offset issue

            //x is relative to the x of viewport
            //y is relative to the parent contrainer
            //why? your guess is as good as mine, but it could do with how scrollbox thing works
            if ((new Rect2(/*RectPosition.x + */170, 0, 128, 128)).HasPoint(GetLocalMousePosition()))
            {
                // ParentMenu?.OnButtonPressed(ItemData, this);
            }
            //ignoring that "fix" because ui looks bad either way
            ParentMenu?.OnButtonPressed(ItemData, this);
            GD.PrintErr
            (
                $"Mouse: {GetLocalMousePosition()}, Rect: {RectPosition}, Item: {Name}"
            );


        }

        protected void onMouseOver()
        {
            ParentMenu.DisplayItemInfo(ItemData);
        }

        private void onMouseOut()
        {
            ParentMenu.HideItemInfo();
        }
    }
}







