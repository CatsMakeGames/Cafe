using System;
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
    protected Category category = Category.Any;

    public Category ItemCategory => category;
    public Furniture(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos,Category _category = Category.Any, int zorder = (int)ZOrderValues.Furniture) : base(texture, size, textureSize, cafe, pos, zorder)
    {
        category = _category;
    }

}

