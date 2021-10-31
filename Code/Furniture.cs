using System;
using Godot;

public class Furniture : CafeObject
{
    /**<summary>Used to separate objects in the store and to allow/restrict where items can be placed</summary>*/
    [Flags]
    public enum Category
    {
        None = 0,
        /**<summary>This item can be placed anywhere</summary>*/
        Any = 1 << 1,
        EatingArea = 1 << 2,
        Kitchen = 1 << 3,
        Toilet = 1 << 4
    }

    /**<summary>Price of the funriture in store<para/>Mostly meant for when it's being sold</summary>*/
    public int Price =0;

    /**<summary>Space that is taken up by this furniture</summary>*/
    public Rect2 CollisionRect => new Rect2(Position, size);

    protected bool isInUse = false;

    public virtual bool IsInUse => false;

    protected int level;

    /**<summary>Person who is actively using this furniture<para/>If this is an applience this is meant for recording staff</summary>*/
    public Person CurrentUser = null;

    public int Level
    {
        get => level;
        set
        {
            level = value;
            VisualServer.CanvasItemSetCustomRect(textureRID, true, new Rect2(value * textureSize.x, 0, textureSize));
            //throw new NotImplementedException("Level system is not yet supported");
        }
    }

    /**<summary>If false this furntiure will not be considered in FindClosestFurniture search</summary>*/
    public virtual bool CanBeUsed => true;

    protected Category category = Category.Any;

    public Category ItemCategory => category;
    public Furniture(Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos,Category _category = Category.Any) : base(texture, size, textureSize, cafe, pos, (int)ZOrderValues.Furniture)
    {
        category = _category;
    }

    /**<summary>Forces any ai user to find a new furniture of the same type</summary>*/
    public virtual void ResetUserPaths()
    {
        if(CurrentUser != null)
        {
            CurrentUser.ResetOrCancelGoal();
        }
    }

    /**<summary>Updates cafe navigation tiles
     * </summary><param name="clear">If true navigation tiles are removed</param>
     */
    public void UpdateNavigation(bool clear)
    {
        int tile = clear ? -1 : 0;
        //restore the tilemap
        //calculate before hand to avoid recalculating each iteration
        int width = ((int)(size.x + position.x)) >> cafe.gridSizeP2;
        int height = ((int)(size.y + position.y)) >> cafe.gridSizeP2;
        for (int x = ((int)(position.x)) >> cafe.gridSizeP2/*convert location to tilemap location*/; x < width; x++)
        {
            for (int y = ((int)(position.y)) >> cafe.gridSizeP2; y < height; y++)
            {
                cafe.NavigationTilemap.SetCell(x, y, tile);
            }
        }
        //force any using ai to get reset
        ResetUserPaths();
    }

    public override void Destroy()
    {
        UpdateNavigation(true);
        base.Destroy();
    }
}

