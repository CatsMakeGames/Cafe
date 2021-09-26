using System;
using Godot;

namespace Staff
{
    /**<summary>Staff member that takes and devivers orders</summary>*/
    public class Waiter : Person
    {
        public Waiter(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder) : base(texture, size, textureSize, cafe, pos, zorder)
        {
        }
    }
}
