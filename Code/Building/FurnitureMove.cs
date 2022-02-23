using Godot;
using System;
using System.Linq;


/**<summary>This class is responsible for handling furniture movement<summary>*/
public class FurnitureMover
{
    Furniture _currentlyMovedFurniture;

    Vector2 _startLocation;

    Cafe cafe;

    public FurnitureMover(Cafe cafe)
    {
        this.cafe = cafe;
    }

    /**<summary>Player has decided to not move furniture</summary>*/
    public void Drop()
    {
        if (_currentlyMovedFurniture != null)
        {
            //reset item position cause nothing happened
            _currentlyMovedFurniture.Position = _startLocation;
            _currentlyMovedFurniture = null;
        }
    }

    public void Sell()
    {
        if (_currentlyMovedFurniture != null)
        {
            _currentlyMovedFurniture.MarkToKIll();
            _currentlyMovedFurniture.Position = _startLocation;
            cafe.Money += _currentlyMovedFurniture.Price;
            cafe.Furnitures.Remove((uint)cafe.GetFurnitureIndex(_currentlyMovedFurniture));
            _currentlyMovedFurniture.ResetUserPaths();
            _currentlyMovedFurniture.Destroy();
            _currentlyMovedFurniture = null;
            //TODO: maybe change state to idle, like it did before?
        }
    }

    public void OnInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("left_mouse") &&
            cafe.CurrentState == Cafe.State.Moving &&
            cafe.IsInPlayableArea(cafe.GetLocalMousePosition()))
        {
            //first we allow player to select furniture to move
            if (_currentlyMovedFurniture != null)
            {
                if (!cafe.Furnitures.Where(p => p.Value.CollistionContains(cafe.GetLocalMousePosition()) && p.Value != _currentlyMovedFurniture).Any())
                {
                    //make this be new place
                    var loc = _currentlyMovedFurniture.Position;
                    _currentlyMovedFurniture.Position = _startLocation;
                    //clear old place
                    _currentlyMovedFurniture.UpdateNavigation(false);
                    _currentlyMovedFurniture.Position = loc;
                    _currentlyMovedFurniture.UpdateNavigation(true);
                    // Reset any person trying to get to this item						 
                    _currentlyMovedFurniture = null;
                }
                return;
            }
            else
            {
                _currentlyMovedFurniture = cafe.Furnitures.FirstOrDefault(p => p.Value.CollistionContains(cafe.GetLocalMousePosition())).Value;
                if (_currentlyMovedFurniture != null)
                {
                    _startLocation = _currentlyMovedFurniture.Position;
                }
            }
        }

        if (@event is InputEventMouseMotion motionEvent &&
            cafe.CurrentState == Cafe.State.Moving &&
            cafe.IsInPlayableArea(cafe.GetLocalMousePosition()))
        {
            if (_currentlyMovedFurniture != null)
            {
                _currentlyMovedFurniture.Position = new Vector2
                (
                    ((int)cafe.GetLocalMousePosition().x / cafe.GridSize) * cafe.GridSize,
                    ((int)cafe.GetLocalMousePosition().y / cafe.GridSize) * cafe.GridSize
                );

                if (cafe.Furnitures.Where(p => p.Value.CollisionOverlaps(_currentlyMovedFurniture.CollisionRect)).Any() &&
                    cafe.IsInPlayableArea(_currentlyMovedFurniture.CollisionRect))
                {
                    _currentlyMovedFurniture.TextureColor = new Color(1, 1, 1);
                }
                else
                {
                    _currentlyMovedFurniture.TextureColor = new Color(1, 0, 0);
                }
            }
        }
    }
}