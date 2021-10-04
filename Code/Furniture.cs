using System;
using Godot;

public class Furniture : CafeObject
{
    
    public Furniture(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder = (int)ZOrderValues.Furniture) : base(texture, size, textureSize, cafe, pos, zorder)
    {

    }

}

