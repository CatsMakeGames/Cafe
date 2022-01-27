using System;
using Godot;

public class ModeChangeButton : ModeSelectionButton
{
    public override void _Toggled(bool buttonPressed)
    {
        base._Toggled(buttonPressed);
		cafe.CurrentState = buttonPressed ? Cafe.State.Moving : Cafe.State.Idle;
    }
}