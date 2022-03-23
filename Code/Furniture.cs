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
        Fridge,
        /**<summary>These items can not be used by anybody but improve cafe rating</summary>*/
        Decor,
        Poster
    }
    
    /**<summary>Subtype used by decor items for getting right texture</summary>*/
    public enum DecorFurnitureType
    {
        None,
        Plant,
        Lamp
    }

    /**<summary>Texture used for this object</summary>*/
    private Texture _texture;
    private uint _loadedUserId = 0;

    private uint _loadedCustomerId = 0;

    protected int variation = 0;

    private int _decorLevel = 1;
    public int DecorLevel => _decorLevel;
    public State CurrentState = State.Free;

    private FurnitureType _currentType = FurnitureType.Invalid;
    private DecorFurnitureType _currentDecorType = DecorFurnitureType.None;

    private Vector2 _collisionRectSize;
    private Vector2 _collisionOffset;

    public FurnitureType CurrentType => _currentType;

    /**<summary>Price of the furniture in store<para/>Mostly meant for when it's being sold</summary>*/
    public int Price = 0;

    /**<summary>Space that is taken up by this furniture</summary>*/
    public Rect2 CollisionRect => new Rect2(Position + _collisionOffset, _collisionRectSize);
    public virtual bool IsInUse => CurrentState == State.InUse;

    /**<summary>Current level of the furniture.<para/>Used to store same types of furniture under same class<para/>
     * Only affects timing,texture and sometimes price<para/>
     * Only 1 byte of data because there should not be >255 ovens in this damn game </summary>*/
    protected byte level;

    /**<summary>Amount of bytes used by CafeObject + amount of bytes used by this object</summary>*/
    public new static uint SaveDataSize = 22u;

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
        baseSaveData.Add(CurrentUser?.Id + 1 ?? 0x0);//[8]
        baseSaveData.Add(CurrentCustomer?.Id + 1 ?? 0x0);//[9]

        baseSaveData.Add((uint)CurrentType);//[10]
        baseSaveData.Add((uint)CurrentState);//[11]
        baseSaveData.Add((uint)Level);//[12]

        baseSaveData.Add((uint)size.x);//[13]
        baseSaveData.Add((uint)size.y);//[14]

        baseSaveData.Add((uint)variation);//[15]
        baseSaveData.Add((uint)_decorLevel);//[16]

        baseSaveData.Add((uint)_collisionRectSize.x);//[17]
        baseSaveData.Add((uint)_collisionRectSize.y);//[18]

        baseSaveData.Add((uint)_collisionOffset.x);//[19]
        baseSaveData.Add((uint)_collisionOffset.y);//[20]

        baseSaveData.Add((uint)_currentDecorType);//[21]
        return baseSaveData;
    }

    public override void SaveInit()
    {
        base.SaveInit();
        if(_loadedUserId > 0)
        {
           CurrentUser = cafe.People[_loadedUserId - 1u];
        }
        if(_loadedCustomerId > 0)
        {
            CurrentCustomer = cafe.People.OfType<Customer>().FirstOrDefault(p=>p.Id == _loadedCustomerId - 1u);
        }
        UpdateNavigation(true);
    }

    /**<summary>Person who is actively using this furniture<para/>If this is an appliance this is meant for recording staff</summary>*/
    private Person currentUser = null;

    public Person CurrentUser
    {
        set => currentUser = value;
        get => currentUser;
    }

    /**<summary>If this is machine that needs customer and staff operation<para/>
    most left as compatibility thing(to avoid casting and possible issues)</summary>*/
    private Customer _currentCustomer = null;

    public Customer CurrentCustomer 
    {
        set 
        {
            CurrentState = Furniture.State.InUse;
            _currentCustomer =value;
        }
        get => _currentCustomer;
    }

    private void _remakeTexture()
    {
        //idk how should texture atlases should be structured so idk move around variation and level if it looks weird
        GenerateRIDBasedOnTexture(_texture,ZOrderValues.Furniture,new Rect2(variation* textureSize.x,level * textureSize.y, textureSize));
    }

    public byte Level
    {
        get => level;
        set
        {
            level = value;
            //clean current textures
            VisualServer.FreeRid(textureRID);
            //generate new texture
            _remakeTexture();
        }
    }

    /**<summary>If false this furniture will not be considered in FindClosestFurniture search</summary>*/
    public virtual bool CanBeUsed => CurrentState == State.Free;

    public Furniture(uint id,
        FurnitureType type,
        Texture texture,
        Vector2 size,
        Vector2 textureSize,
        Cafe cafe,
        Vector2 pos,
        byte lvl,
        Vector2 collisionSize,
        Vector2 collisionOffset,
        DecorFurnitureType _decType = DecorFurnitureType.None,
        Category _category = Category.Any)
        : base(id,texture, size, textureSize, cafe, pos,
               type == FurnitureType.Decor ? (int)ZOrderValues.FurnitureDecor :  (int)ZOrderValues.Furniture)
    {
        _currentType = type;
        Random rand = new Random();
        variation = rand.Next(0,2);
        _texture = texture;
        Level = lvl;  
        _collisionOffset = collisionOffset;
        _collisionRectSize = collisionSize;
        _currentDecorType = _decType;
    }

    public Furniture(Cafe cafe,uint[] data) : base(cafe,data)
    {
        _currentType = (FurnitureType)data[10];
        CurrentState = (State)data[11];
       

        _loadedUserId = data[8];
        _loadedCustomerId = data[9];
        size = new Vector2
                        (
                            (float)data[13],
                            (float)data[14]
                        );
        //level = (byte)data[10];
        variation = (int)data[15];
        _currentDecorType = (DecorFurnitureType)data[21];
        _texture = cafe.TextureManager.GetTexture(
            CurrentType == FurnitureType.Decor? _currentDecorType.ToString() : CurrentType.ToString());
        level = (byte)data[12];
        _decorLevel = (int)data[16];
        _remakeTexture();
        _collisionRectSize = new Vector2(
            (float)data[17],
            (float)data[18]
            );
        _collisionOffset = new Vector2(
            (float)data[19],
            (float)data[20]
            );
      
        //because level directly affects texture 
    }

    public override void Init()
    {
        base.Init();
         if(CurrentType == FurnitureType.Table)
        {
            cafe.AddNewAvailableTable(this);
        }
    }

    public bool CollisionOverlaps(Rect2 rect)
    {
        return CollisionRect.Intersects(rect) || CollisionRect.Encloses(rect);
    }

    public bool CollistionContains(Vector2 point)
    {
        return CollisionRect.HasPoint(point);
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
                    if(cafe.Navigation.FindPathTo(_currentCustomer.Position, position) == null || pendingKill)
                    {
                        //customer has to look for other alternatives
                        CurrentUser?.ResetOrCancelGoal(true);

                        _currentCustomer.OnTableIsUnavailable();
                    }
                    else
                    {
                        _currentCustomer.TableLocationChanged(position);
                    }
                }
                break;
            case FurnitureType.Stove:
            case FurnitureType.Fridge:
                var temp = CurrentUser;
			    CurrentUser = null;
			    temp?.ResetOrCancelGoal();   
                break;
            case FurnitureType.Decor:case FurnitureType.Poster:
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
     * </summary><param name="clear">If true navigation tiles are removed</param>*/
    public void UpdateNavigation(bool clear)
    {
        //restore the tilemap
        //calculate before hand to avoid recalculating each iteration
        int width =  ((int)(CollisionRect.Size.x + CollisionRect.Position.x)) / cafe.GridSize;
        int height = ((int)(CollisionRect.Size.y + CollisionRect.Position.y)) / cafe.GridSize;
        for (int x = ((int)(CollisionRect.Position.x)) / cafe.GridSize/*convert location to tilemap location*/; x < width; x++)
        {
            for (int y = ((int)CollisionRect.Position.y) / cafe.GridSize; y < height; y++)
            {
                cafe.Navigation.SetNavigationTile(x, y, !clear);
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

