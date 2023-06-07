using Godot;
using System;

public partial class CharacterSelect : Control
{
	[Export] AudioStream music;
	Texture2D selectBorder = GD.Load<Texture2D>("res://ui/charselectborder.png");
	HBoxContainer container;
	Label playerLabel;
	int currentSelection, currentPlayer = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _EnterTree()
	{
		GameManager.instance.ChangeMusic(music, 0);

		playerLabel = GetNode<Label>("PlayerLabel");
		container = GetNode<HBoxContainer>("Container");

		foreach (var chara in GameManager.instance.characters)
		{
			var borderRect = new TextureRect();
			var selectRect = new CharacterSelectButton(chara, borderRect);

			selectRect.CustomMinimumSize = new Vector2(80,80);
			selectRect.Texture = chara.SelectSprite;

			container.AddChild(selectRect);

			borderRect.Texture = selectBorder;

			selectRect.AddChild(borderRect);
			selectRect.Border = borderRect;
		}

		ChangeSelected(0, currentPlayer);
		UpdatePlayerLabel();
	}

	void UpdatePlayerLabel()
	{
		playerLabel.Text = (GameManager.instance.players[currentPlayer-1].isCPU? "CPU " : "Player ") + currentPlayer;

	} 

	public void ChangeSelected(int direction, int playerID)
	{
		if (playerID == currentPlayer || (GameManager.instance.players[currentPlayer-1].isCPU && playerID == 1))
		{
			currentSelection += direction;

			if (currentSelection < 0)
				currentSelection = container.GetChildCount() - 1;
			else if (currentSelection > container.GetChildCount() - 1)
				currentSelection = 0;

			foreach (CharacterSelectButton button in container.GetChildren())
			{
				button.Border.Modulate = new Color(1,1,1,1);
			}

			container.GetChild<CharacterSelectButton>(currentSelection).Border.Modulate = new Color(0,1,0,1);
			GameManager.globalSounds.PlaySound("MenuSwitch");
		}
	}

	public void SelectCharacter(int playerID)
	{
		if (playerID == currentPlayer || (GameManager.instance.players[currentPlayer-1].isCPU && playerID == 1))
		{
			GameManager.globalSounds.PlaySound("MenuSelect");
			GameManager.instance.players[currentPlayer - 1].SetCharacter(container.GetChild<CharacterSelectButton>(currentSelection).Character);
			currentPlayer ++;

			if (currentPlayer > GameManager.instance.players.Count)
			{
				GameManager.instance.state = GameManager.GameState.Menu;
				GameManager.instance.AllCharactersSelected();
			}
			else
			{
				currentSelection = 0;
				ChangeSelected(0, currentPlayer);
				UpdatePlayerLabel();
			}
		}
	}
}
