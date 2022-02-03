using Godot;
using System;
using System.Linq;

public class FurnitureBuildObject: CafeObject
{
    private int _gridCellSize = 32;

    private Rect2 _objectRect;
    public FurnitureBuildObject(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder)
        : base(texture, size, textureSize, cafe, pos, (int)ZOrderValues.Preview)
    {

    }

    public FurnitureBuildObject(Texture tex,Vector2 sizeInWorld,Vector2 frameSize,int gridCellSize,Cafe cafe)
        : base(cafe)
    {
        _gridCellSize = gridCellSize;
        size  = sizeInWorld;
        //by default preview item shows variation 0 of the item hence pos of frame rect is (0,0)
        GenerateRIDBasedOnTexture(tex,ZOrderValues.Preview,new Rect2(new Vector2(0,0),frameSize));
        _objectRect = new Rect2(Position,size);
    }

    public void SetTexture(Texture tex,Vector2 sizeInWorld,Vector2 frameSize)
    {
        size  = sizeInWorld;
        //by default preview item shows variation 0 of the item hence pos of frame rect is (0,0)
        GenerateRIDBasedOnTexture(tex,ZOrderValues.Preview,new Rect2(new Vector2(0,0),frameSize));
        _objectRect = new Rect2(Position,size);
    }

    public bool CanBePlaced
    {
        set => VisualServer.CanvasItemSetModulate(textureRID, value ? new Color(0, 255, 0) : new Color(255, 0, 0));
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        _objectRect.Position = new Vector2(
            ((int)cafe.GetLocalMousePosition().x / _gridCellSize) * _gridCellSize,
            ((int)cafe.GetLocalMousePosition().y / _gridCellSize) * _gridCellSize
        );
        Position = _objectRect.Position;
        CanBePlaced = !cafe.Furnitures.Where(p=>p.CollisionOverlaps(_objectRect)).Any() && cafe.IsInPlayableArea(_objectRect);
    }
}
