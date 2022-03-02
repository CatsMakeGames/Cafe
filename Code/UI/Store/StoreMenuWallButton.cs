using Godot;
using System;

namespace UI
{
	public class StoreMenuWallButton : StoreMenuBaseButton
	{
		public int TextureId = -1;

		protected override void onPressed()
		{
			if(ParentMenu.cafe.Money >= 100)
			{
				ParentMenu.cafe.Money -= 100;
				ParentMenu.cafe.GetNode<TileMap>("TileMap").TileSet = ParentMenu.cafe.TextureManager.WallTilesets[TextureId];
                ParentMenu.cafe.TextureManager.CurrentWallTextureId = TextureId;
			}
		}
	}
}
