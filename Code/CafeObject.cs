using Godot;
using System;

public class CafeObject : Godot.Object
{
    /**<summary>Object id in the cafe</summary>*/
    private uint _id = 0;

    public uint Id { get => _id; set => _id = value; }

    protected Cafe cafe;

    protected Vector2 position;

    protected RID textureRID;

    protected Vector2 textureSize;
    
    /**<summary>Should this object be deleted on the next frame?<para/>
    Note that not every object will actually be deleted on the next frame</summary>*/
    protected bool pendingKill = false;

    /**<summary>How big the object is in the game world</summary>*/
    protected Vector2 size;

    /**<summary>How big the object is in the game world</summary>*/
    public Vector2 Size => size;

    public bool Valid => !pendingKill;

    public static uint SaveDataSize = 5u;

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

    /**<summary>Version of the constructor that skips all of the construction</summary>*/
    public CafeObject(Cafe cafe)
    {
        this.cafe = cafe;
    }

    public CafeObject(uint id,Texture texture, Vector2 size,Vector2 textureSize, Cafe cafe, Vector2 pos, int zorder)
    {
        this.cafe = cafe;
        this.textureSize = textureSize;
        this.size = size;
        this.Position = pos;
        _id = id;

        GenerateRIDBasedOnTexture(texture, (ZOrderValues)zorder);
    }

    /**<summary>Constructor used for loading from save data<para/>
     * Note that if using this constructor you need to hard code texture name in every class to avoid using wrong textures</summary>*/
    public CafeObject(Cafe cafe, uint[] saveData)
    {
        this.cafe = cafe;
        _id = saveData[0];
        LoadData(saveData);
    }

    /**<summary>Creates and Initialises RID based on provided texture</summary>*/
    protected void GenerateRIDBasedOnTexture(Texture texture,ZOrderValues zOrder)
    {
        if(textureRID != null)
        {
            VisualServer.FreeRid(textureRID);
        }
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

    /**<summary>Creates and Initialises RID based on provided texture.<para/> This version allows to pass texture param</summary>
    <param name = "renderingFrame"> Rectangle defining which part of the image should be drawn</param>
    */
    protected void GenerateRIDBasedOnTexture(Texture texture,ZOrderValues zOrder,Rect2 renderingFrame)
    {
        if(textureRID != null)
        {
            VisualServer.FreeRid(textureRID);
        }
        //spawn image in the world
        if (texture != null && cafe != null)
        {
            textureRID = VisualServer.CanvasItemCreate();
            VisualServer.CanvasItemAddTextureRectRegion(textureRID, new Rect2(0, 0, size.x, size.y), texture.GetRid(), renderingFrame, null, false, texture.GetRid());
 
            VisualServer.CanvasItemSetParent(textureRID, cafe.GetCanvasItem());
            VisualServer.CanvasItemSetZIndex(textureRID, (int)zOrder);
        }
        Position = position;
    }

    /**<summary>Gets save data for this object as array of bytes</summary>*/
    public virtual Godot.Collections.Array<uint> GetSaveData()
    {
        //save structure for this item
        //all of the positional values are stores as int as they are supposed to be dividable by 2, whole numbers(potential save -> divide everything by the size of the grid to fit bigger numbers
        //first 4 bytes -> id
        //next 8 bytes location.x & location.y 
        //next 8 bytes TextureSize.x &  TextureSize.y
        Godot.Collections.Array<uint> result = new Godot.Collections.Array<uint>();

        //negative id would imply id not being set for a specific reason
        result.Add(Id);//[0]
       
        result.Add((uint)textureSize.x);//[1]
        result.Add((uint)textureSize.y);//[2]

        result.Add((uint)size.x);//[3]
        result.Add((uint)size.y);//[4]

        //position is stored as integer because that's simpler and because it does not change result that much
        result.Add((uint)position.x);//[5]
        result.Add((uint)position.y);//[6]
        return result;
    }

    public virtual void LoadData(uint[] data)
    {
        position = new Vector2(data[5], data[6]);
        textureSize = new Vector2(data[1], data[2]);
        size = new Vector2(data[3], data[4]);
    }

    /**<summary>Init object based on data loaded from save file</summary>*/
    public virtual void SaveInit()
    {

    }

    /**<summary>Goes over all saved ids and tries to find objects with given ids</summary>*/
    public virtual void UpdatedLoadedIds()
    {

    }

    /**<summary>Custom alternative to _Ready.<para/>Meant to be called by whatever class spawns the object to notify that that class has finished spawning it</summary>*/
    public virtual void Init()
    {
        
    }

    /**<summary>Called when object is going to be deleted and it's resources need to be freed</summary>
    <param name = "cleanUp">Is it called during clean up? <para/>Allows to create custom behavior that would prevent calling unnecessary actions</param>*/
    public virtual void Destroy(bool cleanUp = false)
    {
        VisualServer.FreeRid(textureRID);
        Free();
    }

    public virtual void Update(float deltaTime){}

    public virtual void OnInput(InputEvent @event){}

    /**<summary> Sets pending kill to true, but does not actually remove anything</summary>*/
    public void MarkToKIll()
    {
        pendingKill = true;
    }
}

