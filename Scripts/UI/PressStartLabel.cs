using Godot;
using System;

public partial class PressStartLabel : Label
{
	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		while (true)
		{
			Visible = !Visible;
			await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
		}
	}

	
}
