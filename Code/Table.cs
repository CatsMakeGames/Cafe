using Godot;
using System;

/**<summary>Stores data about what table it is as well as manages what table image to draw</summary>*/
public class Table : Godot.Object
{
    protected Vector2 position;

    protected Vector2 tableTextureSize;

    public Vector2 Position => position;

    protected RID textureRID;

    /**<summary>Current table level<para/>Used for calculations and display</summary>*/
    protected int level = 0;

    public int Level
    {
        get => level;
        set
        {
            VisualServer.CanvasItemSetCustomRect(textureRID, true, new Rect2(0, 0, tableTextureSize));
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
    public Table(Texture texture, Vector2 tableTextureSize, Vector2 position, Node2D cafe)
    {
        this.tableTextureSize = tableTextureSize;
        this.position = position;
        //spawn image in the world
        if (texture != null && cafe != null)
        {
            textureRID = VisualServer.CanvasItemCreate();
            VisualServer.CanvasItemSetParent(textureRID, cafe.GetCanvasItem());
            VisualServer.CanvasItemAddTextureRectRegion(textureRID, new Rect2(position.x, position.y, 128, 128), texture.GetRid(), new Rect2(0, 0, tableTextureSize), null, false, texture.GetRid());
            VisualServer.CanvasItemSetZIndex(textureRID,1);
        }
    }
}
