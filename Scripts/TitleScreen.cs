using Godot;
using System;

public partial class TitleScreen : Control
{
	[Export] AudioStream music;
	[Export] float flashDuration = 0.5f;

	bool ready;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{

		var splash = GetNode<Control>("SplashIntro");
		var splashTimer = GetNode<Timer>("SplashTimer");

		splashTimer.Start();

		await ToSignal(splashTimer, "timeout");
		
		GameManager.instance.ChangeMusic(music, 0);

		foreach (Control item in splash.GetChildren())
		{
			item.Visible = true;
			await ToSignal(splashTimer, "timeout");
			item.Visible = false;
		}

		splashTimer.Stop();
		splash.Visible = false;

		var flash = GetNode<ColorRect>("Flash");
		var flashTween = GetTree().CreateTween();
		var title = GetNode<Control>("Title");

		flash.Visible = true;
		title.Visible = true;

		flashTween.TweenProperty(flash, "color", new Color(1,1,1,0), flashDuration);

		ready = true;
	}


	public override void _Input(InputEvent @event)
	{
		if (!ready)
			return;

		if (@event is InputEventJoypadButton)
		{
			var joyEvent = (InputEventJoypadButton)@event;
			if (joyEvent.ButtonIndex == JoyButton.Start)
			{
				StartGame();
			}
		}
		else if (@event is InputEventKey)
		{
			var keyEvent = (InputEventKey)@event;
			if (keyEvent.Keycode == Key.Enter)
			{
				StartGame();		
			}
		}
	}
	

	async void StartGame()
	{
		ready = false;
		GameManager.instance.DoTransition();
		await ToSignal(GameManager.transition, "animation_finished");
		GameManager.instance.LoadMenu();
		QueueFree();
	}
}
