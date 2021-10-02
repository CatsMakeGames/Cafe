using Godot;
using System;

public class CafeObject : Godot.Object
{
    protected Cafe cafe;

    protected Vector2 position;

    protected RID textureRID;

    protected Vector2 textureSize;

    protected bool pendingKill = false;

    public bool Valid => !pendingKill;

    public virtual Vector2 Position
    {
        get => position;
        set
        {
            position = value;
            VisualServer.CanvasItemSetTransform(textureRID, new Transform2D(0, value));
        }
    }

    public CafeObject(Texture texture, Vector2 size,Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder)
    {
        this.cafe = cafe;
        this.textureSize = textureSize;
        //spawn image in the world
        if (texture != null && cafe != null)
        {
            textureRID = VisualServer.CanvasItemCreate();
            VisualServer.CanvasItemAddTextureRectRegion(textureRID, new Rect2(0, 0, size.x, size.y), texture.GetRid(), new Rect2(0, 0, textureSize), null, false, texture.GetRid());
            Position = pos;
            VisualServer.CanvasItemSetParent(textureRID, cafe.GetCanvasItem());
            VisualServer.CanvasItemSetZIndex(textureRID, zorder);
        }
    }

    public virtual void Destroy()
    {
        GD.Print($"{ToString()}: Destroyed");
        VisualServer.FreeRid(textureRID);
        Free();
    }
}

