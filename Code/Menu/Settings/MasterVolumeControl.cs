using Godot;
using System;

public class MasterVolumeControl : HSlider
{
    public override void _Ready()
    {
        base._Ready();
        Step = 0.0001;
    }

    private void _onValueChanged(float value)
    {
       AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"),GD.Linear2Db(value));
    }
}
