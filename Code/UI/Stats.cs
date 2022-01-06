using Godot;
using System;

public sealed class Stats : Control
{

	private Cafe cafe;

	private Label label;

	private Label pausedLabel;

	public override void _Ready()
	{
		cafe = GetNode<Cafe>("/root/Cafe") ?? throw new NullReferenceException("Failed to find cafe node at /root/Cafe");

		label = GetNode<Label>("HBoxContainer/MoneyCount");

		pausedLabel = GetNode<Label>("HBoxContainer/Paused");

		label.Text = cafe.Money.ToString();

		cafe.Connect("MoneyUpdated", this, nameof(OnCafeMoneyUpdated));

		cafe.Connect("ChangedPlayerPause", this, nameof(PlayerPaused));
	}

	private void PlayerPaused(bool paused)
	{
		pausedLabel.Visible = paused;
	}

	private void OnCafeMoneyUpdated(int money)
	{
		label.Text = money.ToString();
	}
}
