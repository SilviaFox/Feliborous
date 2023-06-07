using Godot;
using System;

public partial class Settings : Control
{

	public void ToggleFullscreen()
	{
		var window = GetWindow();

		if (window.Mode != Window.ModeEnum.Fullscreen)
			window.Mode = Window.ModeEnum.Fullscreen;
		else
			window.Mode = Window.ModeEnum.Windowed;
	}

	public void ToggleCRT()
	{
		GameManager.instance.ToggleCRTEffect();
	}
}
