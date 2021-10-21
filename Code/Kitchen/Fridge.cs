using System;
using Godot;

namespace Kitchen
{
    /**<summary>Fridge is more of a decorative item with no logic attached. <para/>Only used by cooks as location and by player as decorative itemto take food from</summary>*/
    public class Fridge : Furniture
    {
        /**<summary>
         * Spawns table object into the world
        * </summary>
        * <param name="texture">Fridge texture atlas</param>
        * <param name="tableTextureSize">Size of the fridge image on the texture atlas</param>
        * <param name="cafe">Cafe object</param>
        * */
        public Fridge(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, Category category) : base(texture, size, textureSize, cafe, pos, category)
        {
        }
    }
}