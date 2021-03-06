using Godot;
using System;
using System.Collections.Generic;

public class TextureManager : Node
{
	/**<summary>Texture for table. Also is used as fallback texture</summary>*/
	[Export]
	private Texture _fallbackTexture;

	[Export]
	private int _textureFrameSize = 32;

	/**<summary>Collection of all floor textures in the game<para/>
	unlike object textrues floor textures don't have a name only index and as such don't share</summary>*/
	[Export]
	private List<Texture> _floorTextures = new List<Texture>();
	public List<Texture> FloorTextures => _floorTextures;

	[Export]
	private List<Texture> _wallTilesetIcons = new List<Texture>();
	public List<Texture> WallTilesetIcons => _wallTilesetIcons;

	/**Replace with loading from data table to allow more control over texture size or maybe use default texture size*/
	[Export]
	private Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();

	[Export]
	private List<TileSet> _wallTilesets = new List<TileSet>();
	public List<TileSet> WallTilesets => _wallTilesets;
	
	/**<summary>Id of the current texture, used only for save system and does not have any effect on how the game works</summary>*/
	public int CurrentWallTextureId = 0;
	/**<summary>Id of the current texture, used only for save system and does not have any effect on how the game works</summary>*/
	public int CurrentFloorTextureId = 0;

	public bool HasTexture(string name)
	{
		return _textures.ContainsKey(name);
	}

	/**<summary>Returns texture with the same name or fallback texture</summary>*/

	public Texture GetTexture(string name)
	{
		Texture res;
		return _textures.TryGetValue(name,out res) ? res : _fallbackTexture;
	}

	/**<summary>Returns texture with the same name or fallback texture</summary>*/
	public Texture this[string name]
	{
		get => GetTexture(name);
		
	}
}
