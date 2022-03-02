using Godot;
using System;

namespace UI
{
	public class StoreMenuBaseButton : Button
	{
		
		/**<summary>Menu which is responsible for handling interaction<para>Used instead of signals because it's faster this way(i think)</para></summary>*/
		public StoreMenu ParentMenu;

		private TextureRect _texture;

		private Label _priceLabel;
        public string Label;
        public Texture Texture;

		public override void _Ready()
		{
			base._Ready();
			//hide the button part of button
			//Flat = true;

			_texture = GetNode<TextureRect>("TextureRect");
			_priceLabel = GetNode<Label>("Price");
            _texture.Texture = Texture;
            _priceLabel.Text = Label;
		}

		protected virtual void onPressed()
		{
			//take away players money and give them floor

		}
	}
}
