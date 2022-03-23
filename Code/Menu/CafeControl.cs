using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class CafeControl : Node
{
	private List<Button> _buttons = new List<Button>();

	private List<Control> _menus = new List<Control>();

	Cafe _cafe;

	public Cafe Cafe{set => _cafe = value;}

	Button _exitToIdleButton;

	public override void _Ready()
	{
		base._Ready();
		//record references to needed objects
		_buttons = GetNode<Container>("Menu").GetChildren().OfType<Button>().ToList();
		foreach(Button but in _buttons)
		{
			if(but is ModeSelectionButton button)
			{
				button.cafe = _cafe;
				button.CafeControlMenu = this;
				button.Init();
			}
		}
		
		_menus = GetTree().GetNodesInGroup("Menu").OfType<Control>().ToList();

		_exitToIdleButton = GetNode<Button>("ExitToIdleModeButton");
	}

	public void Init()
	{
		foreach (Control menu in _menus)
		{	
			if(menu is IMenuInferface m)
			{
				m.Init();
			}
		}
	}

	public void CloseAllMenus()
	{
		foreach(Control menu in _menus)
		{
			menu.Hide();
		}

		foreach(Button but in _buttons)
		{
			but.SetPressedNoSignal(false);
		}

		 _exitToIdleButton?.Hide();
	}

	/**<summary>Makes selected menu visible and hides all other</summary>
	<param name="menu">Menu to show</param>
	<param name="button">button that called this function</param>*/
	public void ShowMenu(Control menu, Button button)
	{
		foreach (Control _menu in _menus)
		{
			if (_menu != menu)
			{
				_menu.Hide();
			}
		}

		foreach (Button but in _buttons)
		{
			if(but != button)
			{   
				but.SetPressedNoSignal(false);
			}
		}

		menu.Show();
	}

	private void _on_ExitToIdleModeButton_pressed()
	{
		//close all menus
		CloseAllMenus();
		//hide the button
		_exitToIdleButton?.Hide();

		_cafe.CurrentState = Cafe.State.Idle;
	}

	public void ChangeModeExitButtonVisibility(bool vis)
	{
		_exitToIdleButton.Visible = vis;
	}

	private void _on_SellButton_pressed()
	{
		_cafe?.SellCurrentHoldingFurniture();
	}

	private void _onStaffPaymentSliderChanged(float value)
	{
	   _cafe.StaffPaymentMultiplier = value;
	   GetNode<Label>("StaffManagmentMenuSimple/Label2").Text = $"Current: {_cafe.StaffPaymentMultiplier.ToString()}";
	}

	private void _on_PriceRate_value_changed(float value)
	{
		_cafe.Attraction.PriceMultiplier = value;
		GetNode<Label>("CafeStats/VBOX/HBoxContainer/label3").Text = _cafe.Attraction.PriceMultiplier.ToString();
	}
}
