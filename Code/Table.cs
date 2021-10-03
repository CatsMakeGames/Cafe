using Godot;
using System;

/**<summary>Stores data about what table it is as well as manages what table image to draw</summary>*/
public class Table : CafeObject
{
    public enum State
    {
        Free,
        InUse,
        NeedsCleaning
    }
    public State CurrentState = State.Free;

    protected Vector2 tableTextureSize;

    public Customer CurrentCustomer;

    /**<summary>Current table level<para/>Used for calculations and display</summary>*/
    protected int level = 0;

    public int Level
    {
        get => level;
        set
        {
            VisualServer.CanvasItemSetCustomRect(textureRID, true, new Rect2(value * tableTextureSize.x, 0, tableTextureSize));
            level = value;
        }
    }

    /**<summary>
     * Spawns table object into the world
     * </summary>
     * <param name="texture">Table texture altas</param>
     * <param name="tableTextureSize">Size of the table image on the texture atlas</param>
     * <param name="cafe">Cafe object</param>
     * */
    public Table(Texture texture, Vector2 tableTextureSize, Vector2 position, Cafe cafe):base(texture,new Vector2(96,96),tableTextureSize,cafe,position,(int)ZOrderValues.Furniture)
    {
        cafe.OnNewTableIsAvailable(this);
    }
}
