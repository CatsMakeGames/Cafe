using System;
using Godot;
using Godot.Collections;
using System.Linq;

/**<summary>Class that houses all of the furniture logic
Because of the game's design furniture does not actually have any logic inside instead serving more of a map marker purpose</summary>*/
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

    public enum State
    {
        Free,
        InUse,
        NeedsCleaning
    }


    /**<summary></summary*/
    public enum FurnitureType
    {
        /**<summary>There was an issue with creation</summary>*/
        Invalid,
        Table,
        Stove,
        Fridge
    }

    /**<summary>Texture used for this object</summary>*/
    private Texture _texture;
    private uint _loadedUserId = 0;

    private uint _loadedCustomerId = 0;

    protected int variation = 0;
    public State CurrentState = State.Free;

    public FurnitureType CurrentType = FurnitureType.Invalid;

    /**<summary>Price of the furniture in store<para/>Mostly meant for when it's being sold</summary>*/
    public int Price = 0;

    /**<summary>Space that is taken up by this furniture</summary>*/
    public Rect2 CollisionRect => new Rect2(Position, size);

    [Obsolete("No longer in use",true)]
    protected bool isInUse = false;

    public virtual bool IsInUse => CurrentState == State.InUse;

    /**<summary>Current level of the furniture.<para/>Used to store same types of furniture under same class<para/>
     * Only affects timing,texture and sometimes price<para/>
     * Only 1 byte of data because there should not be >255 ovens in this damn game </summary>*/
    protected byte level;

    /**<summary>Amount of bytes used by CafeObject + amount of bytes used by this object</summary>*/
    public new static uint SaveDataSize = 14u;

    public override Array<uint> GetSaveData()
    {
        var baseSaveData = base.GetSaveData();
        //save structure for this:
        //base
        // Price(to avoid making it reread the price table at spawn)
        //category(a bit wasteful but okay)
        //a weird mix of data saving by store bool values as last byte, furniture type as third category as second byte and store level as first byte
        //note that for now this class dictates how much data is stored but in future there should be predefined value
        baseSaveData.Add((uint)Price);//[5]

        //next thing to save is id of the current user
        //this might be a strange solution but if it's 0 then no id was recorded if it's anything but 0 then it's a proper id
        //upon loading we will just subtract or ignore this value(storing signed it would waste a lot of possible space)
        //note that id of 0 is possible in the game itself
        baseSaveData.Add(CurrentUser?.Id + 1 ?? 0x0);//[6]
        baseSaveData.Add(CurrentCustomer?.Id + 1 ?? 0x0);//[7]

        baseSaveData.Add((uint)CurrentType);//[8]
        baseSaveData.Add((uint)CurrentState);//[9]
        baseSaveData.Add((uint)Level);//[10]

        baseSaveData.Add((uint)size.x);//[11]
        baseSaveData.Add((uint)size.y);//[12]

        baseSaveData.Add((uint)variation);//[13]
        return baseSaveData;
    }

    public override void SaveInit()
    {
        base.SaveInit();
        if(_loadedUserId > 0)
        {
           CurrentUser = cafe.People.FirstOrDefault(p=>p.Id == _loadedUserId - 1u);
        }
        if(_loadedCustomerId > 0)
        {
            CurrentCustomer = cafe.People.OfType<Customer>().FirstOrDefault(p=>p.Id == _loadedCustomerId - 1u);
        }
    }

    public override void LoadData(uint[] data)
    {
        base.LoadData(data);
        Price = (int)data[5];
        level = (byte)(data[6] & 0x000000ff);
    }

    /**<summary>Person who is actively using this furniture<para/>If this is an appliance this is meant for recording staff</summary>*/
    public Person CurrentUser = null;

    /**<summary>If this is machine that needs customer and staff operation<para/>
    most left as compatibility thing(to avoid casting and possible issues)</summary>*/
    public Customer CurrentCustomer = null;

    public byte Level
    {
        get => level;
        set
        {
            level = value;
            //clean current textures
            VisualServer.FreeRid(textureRID);
            //generate new texture
            GenerateRIDBasedOnTexture(_texture,ZOrderValues.Furniture,new Rect2(value * textureSize.x, variation * textureSize.y, textureSize));
        }
    }

    /**<summary>If false this furntiure will not be considered in FindClosestFurniture search</summary>*/
    public virtual bool CanBeUsed => CurrentState == State.Free;

    public Furniture(Furniture.FurnitureType type, Texture texture, Vector2 size, Vector2 textureSize, Cafe cafe, Vector2 pos,byte lvl, Category _category = Category.Any) : base(texture, size, textureSize, cafe, pos, (int)ZOrderValues.Furniture)
    {
        CurrentType = type;
        Random rand = new Random();
        variation = rand.Next(0,2);
        _texture = texture;
        Level = lvl;  
    }

    public Furniture(Cafe cafe,uint[] data) : base(cafe,data)
    {
        CurrentType = (FurnitureType)data[8];
        CurrentState = (State)data[9];
       

        _loadedUserId = data[6];
        _loadedCustomerId = data[7];
        size = new Vector2
                        (
                            (float)data[11],
                            (float)data[12]
                        );
        //level = (byte)data[10];
        variation = (int)data[13];
        _texture = cafe.Textures[CurrentType.ToString()];
        level = (byte)data[10];
        GenerateRIDBasedOnTexture(cafe.Textures[CurrentType.ToString()], ZOrderValues.Furniture,new Rect2(level * textureSize.x, variation * textureSize.y, textureSize));
        //because level directly affects texture 
    }

    public override void Init()
    {
        base.Init();
         if(CurrentType == FurnitureType.Table)
        {
            cafe.OnNewTableIsAvailable(this);
        }
    }

    /**<summary>Forces any ai user to find a new furniture of the same type</summary>*/
    public virtual void ResetUserPaths()
    {
        switch (CurrentType)
        {
            case FurnitureType.Table:
                if (CurrentState == State.InUse)
                {
                    CurrentState = State.Free;
                }
                if (CurrentCustomer != null)
                {
                    //temporary value so we could still call the customer funcions 
                    var tempCustomer = CurrentCustomer;
                    //reset table id for better context
                    tempCustomer.CurrentTableId = -1;
                    //clear it out to avoid stuck references and if this table is selected again customer will update this value itself
                    CurrentCustomer = null;
                    //make customer look for new table and go back to queue if none are found
                    if (!tempCustomer.FindAndMoveToTheTable())
                    {
                        //If customer has to wait back in the queue then waiter has to be reset
                        CurrentUser?.ResetOrCancelGoal(true);
                        //throw new NotImplementedException("Function for moving customer back to queue is not yet implemented");
                        GD.PrintErr("Function for moving customer back to queue is not yet implemented");
                    }
                }
                break;
            case FurnitureType.Stove:
            case FurnitureType.Fridge:
                var temp = CurrentUser;
			    CurrentUser = null;
			    temp?.ResetOrCancelGoal();   
                break;
            default:
                throw new NotImplementedException("Attempted to use unfinished furniture type");
        }
        if (CurrentUser != null)
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

    public override void Destroy(bool cleanUp = false)
    {
        //TODO: fix issue caused by unloading during clean up
        //game attempts to find replacement but clean up is already removing object
        if(!cleanUp)
        {
            UpdateNavigation(false);
        }
        base.Destroy();
    }
}

