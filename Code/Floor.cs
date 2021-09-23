using Godot;
using System;

public class Floor : Godot.Object
{
    RID textureRID;
    
    public Floor(Texture texture,Vector2 roomSize, Cafe cafe)
    {
        //spawn image in the world
        if (texture != null && cafe != null)
        {
            textureRID = VisualServer.CanvasItemCreate();
            VisualServer.CanvasItemSetParent(textureRID, cafe.GetCanvasItem());
            VisualServer.CanvasItemAddTextureRect(textureRID, new Rect2(0, 0, roomSize.x, roomSize.y), texture.GetRid(), true, null, false, texture.GetRid());
        }
    }
}
