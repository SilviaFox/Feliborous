using Godot;
using System;

public partial class EndingCutscene : Node2D
{
	[Export] float endTimer = 4, creditsLength = 20f;
	[Export] float creditsEndHeight = -700;
	[Export] AudioStream music;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		var animation = GetNode<AnimatedSprite2D>("Animation");
		var sounds = GetNode<SoundManager>("Sounds");

		animation.Play("main");

		await ToSignal(animation, "frame_changed");
		
		sounds.PlaySound("Explosion");

		await ToSignal(animation, "animation_finished");

		sounds.PlaySound("Epic Win");
		animation.Play("ending");

		await ToSignal(GetTree().CreateTimer(6), "timeout");

		GameManager.instance.ChangeMusic(music, 0);
		animation.Visible = false;

		var creditsTween = GetTree().CreateTween();
		var credits = GetNode<Control>("CreditsParent/Credits");

		credits.Visible = true;

		var scrollText = credits.GetNode<Label>("ScrollingText");

		creditsTween.TweenProperty(scrollText, "position", new Vector2(scrollText.Position.X, creditsEndHeight), creditsLength);

		await ToSignal(creditsTween, "finished");

		credits.GetNode<Label>("Created").Visible = true;

		await ToSignal(GetTree().CreateTimer(10), "timeout");

		GameManager.instance.DoTransition(3);
		await ToSignal(GameManager.transition, "animation_finished");

		GameManager.instance.LoadMenu();
		QueueFree();

	}

}
