using Godot;
using System;

public partial class MenuButton : Button
{
	[Export(PropertyHint.MultilineText)] string description;

    public override void _Ready()
    {
        FocusEntered += () => ChangeMenuDescription();
    }

	public void ChangeMenuDescription()
	{
		GameManager.menu.description.Text = description;
	}
}
