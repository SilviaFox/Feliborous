using Godot;
using System;
using System.Collections.Generic;

public partial class Cutscene : Control
{
	[Export] AudioStream music;

	[Signal] public delegate void ProceedEventHandler();

	float dialogueRevealTime = 0.02f;

	public async void InitializeCutscene(int missionID, Texture2D leftChar, Texture2D rightChar, string leftName, string rightName)
	{
		GameManager.instance.ChangeMusic(music, 1);

		// load dialogue
		var path = "res://Missions/Mission" + missionID;

		if (!DirAccess.DirExistsAbsolute(path))
			return;

		var dir = DirAccess.Open(path);
		var files = dir.GetFiles();

		var dialogue = new List<Dialogue>();

		int d = 1;

		foreach (var file in files)
		{
			dialogue.Add(ResourceLoader.Load<Dialogue>(path + "/" + d.ToString() + ".tres"));
			d++;
		}

		var dialogueTimer = new Timer();
		AddChild(dialogueTimer);

		dialogueTimer.WaitTime = dialogueRevealTime;
		dialogueTimer.OneShot = false;

		GetNode<TextureRect>("LeftChar").Texture = leftChar;
		GetNode<Label>("LeftChar/Name").Text = leftName;
		GetNode<TextureRect>("RightChar").Texture = rightChar;
		GetNode<Label>("RightChar/Name").Text = rightName;

		var textBox = GetNode<TextureRect>("TextBox");
		var label = GetNode<Label>("TextBox/Dialogue");

		foreach (var text in dialogue)
		{
			label.Text = text.Text;
			textBox.FlipH = text.RightChar;
			dialogueTimer.Start();

			for (int i = 0; i <= text.Text.ToCharArray().Length; i++)
			{
				label.VisibleCharacters = i;
				await ToSignal(dialogueTimer, "timeout");
			}
			dialogueTimer.Stop();
			await ToSignal(this, "Proceed");
		}

		GameManager.instance.StartSinglePlayerMatch();
		QueueFree();

	}

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept"))
		{
			EmitSignal("Proceed");
		}
    }

}



