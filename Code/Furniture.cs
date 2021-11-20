﻿using System;
using Godot;
using Godot.Collections;

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
    /**<summary>Type of the class used purely to allow compact way of stroing what type is this object</summary>*/
    public static new Class Type = Class.Default;

    /**<summary>Price of the funriture in store<para/>Mostly meant for when it's being sold</summary>*/
    public int Price =0;

    /**<summary>Space that is taken up by this furniture</summary>*/
    public Rect2 CollisionRect => new Rect2(Position, size);

    protected bool isInUse = false;

    public virtual bool IsInUse => false;

    /**<summary>Current level of the furniture.<para/>Used to store same types of furniture under same class<para/>
     * Only affects timing,texture and sometimes price<para/>
     * Only 1 byte of data because there should not be >255 ovens in this damn game </summary>*/
    protected byte level;

    public override Array<uint> GetSaveData()
    {
        var baseSaveData = base.GetSaveData();
        //save structure for this:
        //base
        // Price(to avoid making it reread the price table at spawn)
        //category(a bit wasteful but okay)
        //a weird mix of data saving by store bool values as last byte, furniture type as third category as second byte and store level as first byte
        //note that for now this class dicataes how much data is stored but in future there should be predefined value
        baseSaveData.Add((uint)Price);
        baseSaveData.Add((uint)((isInUse ? 0x01 : 0x00) << 24 | (byte)category << 8 | level));

        //next thing to save is id of the current user
        //this might be a strange solution but if it's 0 then no id was recored if it's anything but 0 then it's a proper id
        //upon loading we will just subtract or ignore this value(storing signed it would waste a lot of possible space)
        //note that id of 0 is possible in the game itself
        baseSaveData.Add(CurrentUser?.Id + 1 ?? 0x0);

        return baseSaveData;
    }

    public override void LoadData(Array<uint> data)
    {
        base.LoadData(data);
        Price = (int)data[5];
        level = (byte)(data[6] & 0x000000ff);
        category = (Furniture.Category)((data[6] & 0x0000ff00) >> 8);
        isInUse = ((data[6] & 0x0000ff00) >> 24) == 1;
    }

    /**<summary>Person who is actively using this furniture<para/>If this is an applience this is meant for recording staff</summary>*/
    public Person CurrentUser = null;

    public byte Level
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
        UpdateNavigation(false);
        base.Destroy();
    }
}

