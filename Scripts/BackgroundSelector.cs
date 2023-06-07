using Godot;
using System;

public partial class BackgroundSelector : OptionButton
{
	[Export] public Background[] bgs = new Background[] {};

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		int i = 0;
		foreach (var bg in bgs)
		{
			
			AddItem(bg.Name, i);
			i++;
		}
	}

}
