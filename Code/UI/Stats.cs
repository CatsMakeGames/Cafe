using Godot;
using System;

public sealed class Stats : Control
{

	private Cafe cafe;

	private Label label;

	public override void _Ready()
	{
		cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");

		label = GetNode<Label>("HBoxContainer/MoneyCount");

		label.Text = cafe.Money.ToString();

		cafe.Connect("MoneyUpdated",this,nameof(OnCafeMoneyUpdated));
	}

	private void OnCafeMoneyUpdated(int money)
	{
		label.Text = money.ToString();
	}
}
