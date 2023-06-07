using Godot;
using System;


public partial class VolumeSlider : HSlider
{
	[Export(PropertyHint.Enum, "Sounds,Music")] string targetBus {get; set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Value = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex(targetBus)));
	}

	public void ChangeVolume(float value)
	{
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(targetBus),Mathf.LinearToDb(value));

		// Save volume
		using var audioSave = FileAccess.Open("user://"+ targetBus +".txt", FileAccess.ModeFlags.Write);
		audioSave.StoreFloat(value);

	}



}
