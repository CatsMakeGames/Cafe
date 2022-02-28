using Godot;
using System;

namespace UI
{
	public class StoreMenuFloorButton : StoreMenuBaseButton
	{
		public int TextureId = -1;

		protected override void onPressed()
		{
			if(ParentMenu.cafe.Money >= 100)
            {
                ParentMenu.cafe.Money -= 100;
                ParentMenu.cafe.Floor.Texture = ParentMenu.cafe.TextureManager.FloorTextures[TextureId];
            }
		}
	}
}
