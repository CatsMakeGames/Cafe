using System;
using Godot;

namespace Kitchen
{
    /**<summary>Advanced version of object allowing to store data like use time</summary>*/
    public class Appliance : CafeObject
    {
        public float UsageTime = 10;
        public Appliance(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos) : base(texture, size, textureSize, cafe, pos, (int)ZOrderValues.Furniture)
        {

        }
    }
}