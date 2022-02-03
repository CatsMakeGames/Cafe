using System;
using Godot;

public class ModeChangeButton : ModeSelectionButton
{
    [Export]
    Cafe.State StateToChangeTo = Cafe.State.Moving;

    [Export]
    Cafe.State StateToExitTo = Cafe.State.Idle;

    public override void _Toggled(bool buttonPressed)
    {
        base._Toggled(buttonPressed);
        if (buttonPressed)
        {
            _cafeControlMenu.ShowMenu(Menu, this);
        }
        cafe.CurrentState = buttonPressed ? StateToChangeTo : StateToExitTo;
    }
}