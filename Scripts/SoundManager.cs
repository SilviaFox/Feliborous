using Godot;
using System;
using System.Collections.Generic;

public partial class SoundManager : Node
{
	Dictionary<string,AudioStreamPlayer> sounds = new Dictionary<string, AudioStreamPlayer>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach (AudioStreamPlayer item in GetChildren())
		{
			sounds.Add(item.Name, item);
			item.Bus = "Sounds";
		}
	}

	public void PlaySound(string sound, float pitch = 1)
	{
		sounds[sound].PitchScale = pitch;
		sounds[sound].Play();
	}
}
