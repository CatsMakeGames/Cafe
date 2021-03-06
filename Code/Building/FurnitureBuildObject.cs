using Godot;
using System;
using System.Linq;

public class FurnitureBuildObject : CafeObject
{
    private int _gridCellSize = 32;
//TODO: update to use collision rect from item data
    private Rect2 _objectRect;

    private StoreItemData _currentItemData;
    public FurnitureBuildObject(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder)
        : base(0/*this object exists outside of the lists, so we just set id to 0*/,texture, size, textureSize, cafe, pos, (int)ZOrderValues.Preview)
    {

    }

    public FurnitureBuildObject(StoreItemData data, Texture tex, Vector2 sizeInWorld, Vector2 frameSize, int gridCellSize, Cafe cafe)
        : base(cafe)
    {
        _gridCellSize = gridCellSize;
        _currentItemData = data;
        size = sizeInWorld;
        //by default preview item shows variation 0 of the item hence pos of frame rect is (0,0)
        GenerateRIDBasedOnTexture(tex, ZOrderValues.Preview, new Rect2(new Vector2(0, 0), frameSize));
        _objectRect  = new Rect2(Position + data.CollisionRectOffset, data.CollisionRectSize);
    }

    public void SetTexture(Texture tex, Vector2 sizeInWorld, Vector2 frameSize)
    {
        size = sizeInWorld;
        //by default preview item shows variation 0 of the item hence pos of frame rect is (0,0)
        GenerateRIDBasedOnTexture(tex, ZOrderValues.Preview, new Rect2(new Vector2(0, 0), frameSize));
        _objectRect = new Rect2(Position, size);
    }

    public bool CanBePlaced
    {
        get => !cafe.Furnitures.Where(p => p.Value.CollisionOverlaps(_objectRect)).Any() && cafe.IsInPlayableArea(_objectRect);
        set
        {
            VisualServer.CanvasItemSetModulate(textureRID, value ? new Color(0, 255, 0) : new Color(255, 0, 0));
        }
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        _objectRect.Position = new Vector2(
            ((int)cafe.GetLocalMousePosition().x / _gridCellSize) * _gridCellSize,
            ((int)cafe.GetLocalMousePosition().y / _gridCellSize) * _gridCellSize
        );
        Position = _objectRect.Position - _currentItemData.CollisionRectOffset;
        CanBePlaced = !cafe.Furnitures.Where(p => p.Value.CollisionOverlaps(_objectRect)).Any() && cafe.IsInPlayableArea(_objectRect);
    }

    public void PlaceNewFurniture()
    {
        if (CanBePlaced)
        {
            Furniture.FurnitureType type;
            Enum.TryParse<Furniture.FurnitureType>(_currentItemData.ClassName, out type);
            cafe.Money -= _currentItemData.Price;
            cafe.AddNewFurniture(new Furniture
            (
                cafe.CurrentFurnitureId,
                type,
                cafe.TextureManager[_currentItemData.TextureName],
                _currentItemData.Size,
                _currentItemData.FrameSize,
                cafe,
                Position,
                _currentItemData.Level,
                _currentItemData.CollisionRectSize,
                _currentItemData.CollisionRectOffset,
                _currentItemData.DecorType,
                _currentItemData.FurnitureCategory
            ));
            cafe.Furnitures[cafe.CurrentFurnitureId - 1/*because it was already updated*/].Price = _currentItemData.Price;
        }
    }

    public override void OnInput(InputEvent @event)
    {
        base.OnInput(@event);
        if (Input.IsActionJustPressed("left_mouse") && cafe.CurrentState == Cafe.State.Building)
        {
            if (cafe.IsInPlayableArea(_objectRect) && cafe.CanAfford(_currentItemData.Price))
            {
                PlaceNewFurniture();
            }
        }
    }
}
