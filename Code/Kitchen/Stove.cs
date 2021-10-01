using System;
using Godot;

namespace Kitchen
{
    public class Stove : Appliance
    {
        public Stove(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos) : base(texture, size, textureSize, cafe, pos)
        {
            UsageTime = 10;
        }
    }
}