using Godot;
using System;

public partial class Dialogue : Resource
{
    [Export(PropertyHint.MultilineText)] public string Text {get; set;}
	[Export] public bool RightChar {get; set;}

    public Dialogue() : this("",false) {}

	public Dialogue(string text, bool rightChar)
	{
		Text = text;
		RightChar = rightChar;
	}
}
