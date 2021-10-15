using Godot;
using System;

public class MouseBlockArea : Control
{
    [Export(PropertyHint.Range,"0,32")]
    private int areaId;

    protected Cafe cafe;

    public override void _Ready()
    {
        base._Ready();
        if (areaId > 32 || areaId < 0)
            throw new ArgumentOutOfRangeException("AreaId must be bigger then 0 but less then 32");
        areaId = 1 << areaId;

        cafe = GetNodeOrNull<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find main cafe node");
        Connect("mouse_entered", this, nameof(onMouseOver));
        Connect("mouse_exited", this, nameof(onMouseLeave));
    }

    private void onMouseOver()
    {
        if (Visible)
        {
            cafe.ClickTaken |= areaId;
            GD.Print(cafe.ClickTaken);
        }
    }

    private void onMouseLeave()
    {
        //unsure about this check
        if ((cafe.ClickTaken & (areaId)) > 0)
        {
            cafe.ClickTaken ^= areaId;
            GD.Print(cafe.ClickTaken);
        }
    }
    
}
