using Godot;
using System;

public class Floor : Godot.Object
{
    RID textureRID;
    private readonly RID cafeCanvasRID;

    Vector2 _roomSize;
    public Texture Texture
    {
        set
        {
            //spawn image in the world
            if (value != null)
            {
                textureRID = VisualServer.CanvasItemCreate();
                VisualServer.CanvasItemSetParent(textureRID, cafeCanvasRID);
                VisualServer.CanvasItemAddTextureRect(textureRID, new Rect2(0, 0, _roomSize.x, _roomSize.y), value.GetRid(), true, null, false, value.GetRid());
            }
        }
    }
    
    public Floor(Texture texture, Vector2 roomSize, Cafe cafe)
    {
        _roomSize = roomSize;
        //spawn image in the world
        if (texture != null && cafe != null)
        {
            cafeCanvasRID = cafe.GetCanvasItem();
            textureRID = VisualServer.CanvasItemCreate();
            VisualServer.CanvasItemSetParent(textureRID, cafeCanvasRID);
            VisualServer.CanvasItemAddTextureRect(textureRID, new Rect2(0, 0, roomSize.x, roomSize.y), texture.GetRid(), true, null, false, texture.GetRid());
        }
    }
}
