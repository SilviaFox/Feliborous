using Godot;
using System;

public partial class PostRoundMenu : Control
{
	public override void _Ready()
	{
		var rematchButton = GetNode<Button>("Container/Rematch");
		rematchButton.GrabFocus();

		if (GameManager.instance.mode == GameManager.GameMode.Adventure)
		{
			rematchButton.Text = GameManager.instance.players[0].isDead? "Retry" : "Continue";
		}
	}

	public async void Menu()
	{
		Visible = false;
		GameManager.instance.DoTransition(3);
		await ToSignal(GameManager.transition, "animation_finished");

		GameManager.instance.LoadMenu();
		QueueFree();
	}

	public void Rematch()
	{
		Visible = false;
		if (GameManager.instance.mode == GameManager.GameMode.Adventure && !GameManager.instance.players[0].isDead)
		{
			GameManager.instance.ResetPlayers();
			GameManager.instance.NextMission();
		}
		else
		{
			GameManager.instance.ResetPlayers();
			GameManager.instance.StartGame();
		}
		QueueFree();
	}
}
