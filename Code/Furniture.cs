﻿using System;
using Godot;

public class Furniture : CafeObject
{
    /**<summary>Used to separate objects in the store and to allow/restrict where items can be placed</summary>*/
    [Flags]
    public enum Category
    {
        None = 0,
        /**<summary>This item can be placed anywhere</summary>*/
        Any = 1 << 1,
        EatingArea = 1 << 2,
        Kitchen = 1 << 3,
        Toilet = 1 << 4
    }

    protected int level;

    public int Level
    {
        get => level;
        set
        {
            level = value;
            VisualServer.CanvasItemSetCustomRect(textureRID, true, new Rect2(value * textureSize.x, 0, textureSize));
            //throw new NotImplementedException("Level system is not yet supported");
        }
    }

    protected Category category = Category.Any;

    public Category ItemCategory => category;
    public Furniture(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos,Category _category = Category.Any) : base(texture, size, textureSize, cafe, pos, (int)ZOrderValues.Furniture)
    {
        category = _category;
    }

}
