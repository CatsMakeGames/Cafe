using Godot;
using System;

public class CafeObject : Godot.Object
{
    /**<summary>Object id in the cafe</summary>*/
    public uint Id = 0;

    protected Cafe cafe;

    protected Vector2 position;

    protected RID textureRID;

    protected Vector2 textureSize;

    protected bool pendingKill = false;

    /**<summary>How big the object is in the game world</summary>*/
    protected Vector2 size;

    /**<summary>How big the object is in the game world</summary>*/
    public Vector2 Size => size;

    public bool Valid => !pendingKill;

    public virtual Class Type => Class.Default;

    
    public virtual Vector2 Position
    {
        get => position;
        set
        {
            position = value;
            if (textureRID != null)
            {
                VisualServer.CanvasItemSetTransform(textureRID, new Transform2D(0, value));
            }
        }
    }

    public Color TextureColor { set => VisualServer.CanvasItemSetModulate(textureRID, value); }

    public CafeObject(Texture texture, Vector2 size,Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder)
    {
        this.cafe = cafe;
        this.textureSize = textureSize;
        this.size = size;
        this.Position = pos;

        GenerateRIDBasedOnTexture(texture, (ZOrderValues)zorder);
    }

    /**<summary>Constructor used for loading from save data<para/>
     * Note that if using this constructor you need to hard code texture name in every class to avoid usin wrong textures</summary>*/
    public CafeObject(Cafe cafe, uint[] saveData)
    {
        this.cafe = cafe;
        Id = saveData[0];
        this.size = new Vector2(128, 128);
        LoadData(saveData);
    }

    /**<summary>Creates and Initialises RID based on provided texture</summary>*/
    protected void GenerateRIDBasedOnTexture(Texture texture,ZOrderValues zOrder)
    {
        //spawn image in the world
        if (texture != null && cafe != null)
        {
            textureRID = VisualServer.CanvasItemCreate();
            VisualServer.CanvasItemAddTextureRectRegion(textureRID, new Rect2(0, 0, size.x, size.y), texture.GetRid(), new Rect2(0, 0, textureSize), null, false, texture.GetRid());
 
            VisualServer.CanvasItemSetParent(textureRID, cafe.GetCanvasItem());
            VisualServer.CanvasItemSetZIndex(textureRID, (int)zOrder);
        }
        Position = position;
    }

    /**<summary>Gets save data for this object as array of bytes</summary>*/
    public virtual Godot.Collections.Array<uint> GetSaveData()
    {
        //save structure for this item
        //all of the positonal values are stores as int as they are supposed to be dividable by 2, whole numbers(potentional save -> divide everything by the size of the grid to fit bigger numbers
        //first 4 bytes -> id
        //next 8 bytes location.x & location.y 
        //next 8 bytes TextureSize.x &  TextureSize.y
        Godot.Collections.Array<uint> result = new Godot.Collections.Array<uint>();

        //negative id would imply id not being set for a specific reason
        result.Add(Id);
        result.Add((uint)Type);
        
        //unlike position texture size is not allowed to have freedom of float
        result.Add((uint)textureSize.x);
        result.Add((uint)textureSize.y);
        return result;
    }

    /**<summary>Gets save data for the object that needs to be stored as float instead of int. This is used for data like position</summary>*/
    public virtual Godot.Collections.Array<float> GetSaveDataFloat()
    {
        Godot.Collections.Array<float> result = new Godot.Collections.Array<float>();

        //position and location are always > 0
        result.Add(position.x);
        result.Add(position.y);

        return result;
    }

    public virtual void LoadData(uint[] data)
    {
        position = new Vector2(data[1], data[2]);
        textureSize = new Vector2(data[3], data[4]);
    }

    /**<summary>Goes over all saved ids and tries to find objects with given ids</summary>*/
    public virtual void UpdatedLoadedIds()
    {

    }

    /**<summary>Custom alternative to _Ready.<para/>Meant to be called by whatever class spawns the object to notify that that class has finished spawning it</summary>*/
    public virtual void Init()
    {
        
    }

    public virtual void Destroy()
    {
        VisualServer.FreeRid(textureRID);
        Free();
    }
}

