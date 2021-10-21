using Godot;
using System;

/**<summary>Stores data about what table it is as well as manages what table image to draw</summary>*/
public class Table : Furniture
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

    public override bool CanBeUsed => CurrentState == State.Free;

    /**<summary>
     * Spawns table object into the world
     * </summary>
     * <param name="texture">Table texture altas</param>
     * <param name="tableTextureSize">Size of the table image on the texture atlas</param>
     * <param name="cafe">Cafe object</param>
     * */
    public Table(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, Category category) :base(texture,size,textureSize,cafe,pos,category)
    {
        cafe.OnNewTableIsAvailable(this);
    }
}
