using Godot;
using System;

public partial class CharacterSelectButton : TextureRect
{
	public Character Character;
	public TextureRect Border;

	public CharacterSelectButton(Character character, TextureRect border)
	{
		Character = character;
		Border = border;
	}
}
