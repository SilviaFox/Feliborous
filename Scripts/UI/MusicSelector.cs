using Godot;
using System;

public partial class MusicSelector : OptionButton
{
	[Export] public MusicTrack[] music = new MusicTrack[] {};

    public override void _Ready()
    {
		int i = 0;
		foreach (var track in music)
		{
			
			AddItem(track.Name, i);
			i++;
		}
    }
}
