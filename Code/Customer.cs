using Godot;
using System;

public class Customer : Godot.Object
{
    RID textureRID;

    public Customer(Texture texture,Node2D cafe)
    {
        //spawn image in the world
        if (texture != null && cafe != null)
        {
            textureRID = VisualServer.CanvasItemCreate();
            VisualServer.CanvasItemAddTextureRect(textureRID, new Rect2(0, 0, 64, 64), texture.GetRid(), false, null, false, texture.GetRid());
            VisualServer.CanvasItemSetParent(textureRID, cafe.GetCanvasItem());
            VisualServer.CanvasItemSetZIndex(textureRID, (int)ZOrderValues.Customer);
        }
    }
}
