using Godot;
using System;

public partial class MusicTrack : Resource
{
	[Export] public AudioStream Audio {get; set;}
	[Export] public string Name {get; set;}

	public MusicTrack() : this(null, "") {}

	public MusicTrack(AudioStream audio, string name)
	{
		Audio = audio;
		Name = name;
	}
}
