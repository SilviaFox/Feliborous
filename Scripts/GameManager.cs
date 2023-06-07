using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
	public enum GameState {
		Menu,
		AwaitingInput,
		CharacterSelect,
		Playing
	}

	public enum GameMode {
		Adventure,
		Versus
	}

	public static GameManager instance;

	[Export] PackedScene titleScreen, menuScene, playerScene, charSelectScene, endGraphic, postRoundMenu, cutscenePrefab, readyTextPrefab, creditsPrefab;
	[Export] public Color wellColor, borderColor;
	// [Export] public float gravity = 4, fastFallSpeed = 0.1f;
	[Export] float stageMusicFade = 1f;
	[Export] int gapBetweenWells = 12;
	// [Export] AudioStream[] stageMusic;
	[Export] public Character[] characters;
	public static bool gameOver = true;

	// [Export] public int GarbageReduction = 2;
	// [Export] int amountOfPlayers = 4;
	// [Export] public Texture2D wellSprite;
	// public static int width = 8, height = 13;
	// [Export] public int Colors = 4;
	[Export] public Vector2I borderSize = new Vector2I(40, 20);

	public static int cellSize = 12;
	public static int maxPlayers = 4;

	public static SpriteFrames feliSprite = (SpriteFrames)GD.Load("res://sprites/Feli/FeliFrames.tres");
	public static SpriteFrames feliEatSprite = (SpriteFrames)GD.Load("res://sprites/Feli/FeliEatFrames.tres");
	public static SpriteFrames nuisanceSprite = (SpriteFrames)GD.Load("res://sprites/Feli/GarbageFrames.tres");


	int missionIndex;
	Mission mission;


	Camera2D cam = new Camera2D();
	public Sprite2D background = new Sprite2D();
	public static Ruleset rules = new Ruleset();
	public List<Player> players = new List<Player>();
	public GameState state = GameState.Menu;
	public GameMode mode = GameMode.Versus;


	public static Menu menu;
	AudioStreamPlayer music;
	public static AudioStream stageMusic;

	ColorRect crtEffect;

	public static AnimatedSprite2D transition;
	public static SoundManager globalSounds;

	[Signal] public delegate void MusicChangedEventHandler();
	[Signal] public delegate void GameReadyEventHandler();
	// X = Left
	// Y = Up
	// Z = Right
	// W = Down

	public static CharacterSelect charSelect;
	public static StateData[] stateData = {
		new StateData(new Vector4I(1,1,1,1), "_cross", 0),
		new StateData(new Vector4I(1,1,0,0), "_corner", 0),
		new StateData(new Vector4I(0,1,1,0), "_corner", 90),
		new StateData(new Vector4I(0,0,1,1), "_corner", 180),
		new StateData(new Vector4I(1,0,0,1), "_corner", 270),
		new StateData(new Vector4I(0,0,0,1), "end", 0),
		new StateData(new Vector4I(1,0,0,0), "end", 90),
		new StateData(new Vector4I(0,1,0,0), "end", 180),
		new StateData(new Vector4I(0,0,1,0), "end", 270),
		new StateData(new Vector4I(0,1,0,1), "_thin", 0),
		new StateData(new Vector4I(1,0,1,0), "_thin", 90),
		new StateData(new Vector4I(0,1,1,1), "_split", 0),
		new StateData(new Vector4I(1,0,1,1), "_split", 90),
		new StateData(new Vector4I(1,1,0,1), "_split", 180),
		new StateData(new Vector4I(1,1,1,0), "_split", 270),
	};
	

	public override void _EnterTree()
	{
		instance = this;

		LoadSoundSettings();

		AddChild(cam);
		cam.Enabled = false;
		music = GetNode<AudioStreamPlayer>("Music");
		globalSounds = GetNode<SoundManager>("GlobalSounds");
		transition = GetNode<AnimatedSprite2D>("Transitions");
		crtEffect = GetNode<ColorRect>("CRTEffect");
		
	}

	public void LoadSoundSettings()
	{
		void UpdateBus(string bus)
		{
			var dir = "user://"+ bus +".txt";
			if (!FileAccess.FileExists(dir))
			{
				return;
			}

			using var file = FileAccess.Open("user://"+ bus +".txt", FileAccess.ModeFlags.Read);
			var value = file.GetFloat();

			AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(bus),Mathf.LinearToDb(value));
		}

		UpdateBus("Sounds");
		UpdateBus("Music");
	}


    public override void _Ready()
    {
		AddChild(background);

		AddChild(titleScreen.Instantiate());
        // LoadMenu();
    }

	public void ToggleCRTEffect()
	{
		crtEffect.Visible = !crtEffect.Visible;
	}


	public async void DoTransition(int exit = 3)
	{
		transition.Play("opening");
		await ToSignal(transition, "animation_finished");
		transition.Play("exit" + exit);
	}

	public void LoadMenu()
	{
		transition.Centered = false;
		transition.Position = Vector2.Zero;
		crtEffect.Position = Vector2.Zero;

		if (players.Count > 0)
		{
			cam.Enabled = false;

			foreach (var player in players)
			{
				player.QueueFree();
			}

			players.Clear();

			foreach (var action in InputMap.GetActions())
			{
				if (action.ToString().ToCharArray()[0] != 'u')
				{
					InputMap.EraseAction(action);
				}
			}
		}

		background.Texture = null;
		menu = (Menu)menuScene.Instantiate();
		AddChild(menu);
	}

    public override void _Input(InputEvent @event)
    {
		switch (state)
		{
			case GameState.AwaitingInput:
				if (!(@event is InputEventJoypadButton || @event is InputEventKey))
					return;

				if (players.Count == maxPlayers)
					return;
					
				// Detect if device has been assigned to a player
				foreach(var player in players)
				{
					if (@event is InputEventKey && player.usingKeyboard)
					{
						GD.Print("Keyboard Already Active");
						return;
					}
					else if (@event is InputEventJoypadButton && !player.usingKeyboard && player.inputDevice == @event.Device)
					{
						GD.Print("Gamepad already Active");
						return;
					}
				}

				// Add player if no assigned device is found
				AddPlayer(@event.Device, @event is InputEventKey);
				menu.ShowPlayerActive();
			break;
		}

    }

	public async void ChangeMusic(AudioStream track, float fadeDuration)
	{
		// GD.Print(fadeDuration);
		if (fadeDuration != 0)
		{
			var musicTween = CreateTween().SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Sine).SetParallel(false);
			musicTween.TweenProperty(music, "volume_db", -80f, fadeDuration);

			await ToSignal(musicTween, "finished");
		}

		EmitSignal("MusicChanged");

		music.VolumeDb = 0;
		music.Stream = track;
		music.Play();
	}

	void AddPlayer(int inputDevice, bool isKeyboard, bool singlePlayer = false)
	{
		var newPlayer = (Player)playerScene.Instantiate();
		newPlayer.inputDevice = inputDevice;
		newPlayer.playerID = players.Count + 1;
		newPlayer.Name = "Player " + (newPlayer.playerID);
		newPlayer.usingKeyboard = isKeyboard;

		AddChild(newPlayer);
		players.Add(newPlayer);
		AddPlayerInputs(newPlayer.playerID, inputDevice, isKeyboard);

		// Move player position
		newPlayer.Position = new Vector2(((newPlayer.playerID - 1) * rules.WellSize.X * cellSize) + ((newPlayer.playerID - 1) * gapBetweenWells), 0);
	}

	void AddCPUPlayer()
	{
		var newPlayer = (Player)playerScene.Instantiate();
		newPlayer.playerID = players.Count + 1;
		newPlayer.Name = "CPUPlayer " + (newPlayer.playerID);
		newPlayer.isCPU = true;

		var CPU = new CPU();
		newPlayer.AddChild(CPU);
		CPU.Name = "CPU";

		AddChild(newPlayer);
		players.Add(newPlayer);

		newPlayer.Position = new Vector2(((newPlayer.playerID - 1) * rules.WellSize.X * cellSize) + ((newPlayer.playerID - 1) * gapBetweenWells), 0);
	}

	void AddPlayerInputs(int playerID, int device, bool isKeyboard, bool singlePlayer = false)
	{
		void AddControllerAction(JoyButton button, string actionString)
		{
			var newInput = new InputEventJoypadButton();
			if (!singlePlayer)
				newInput.Device = device;
			newInput.ButtonIndex = button;
			InputMap.AddAction(actionString);
			InputMap.ActionAddEvent(actionString, newInput);
		}

		void AddKeyBoardAction(Key key, string actionString)
		{
			var newInput = new InputEventKey();
			if (!singlePlayer)
				newInput.Device = device;
			newInput.Keycode = key;
			InputMap.AddAction(actionString);
			InputMap.ActionAddEvent(actionString, newInput);
		}

		if (!isKeyboard)
		{
			AddControllerAction(JoyButton.DpadLeft, "play" + playerID + "left");
			AddControllerAction(JoyButton.DpadRight, "play" + playerID + "right");
			AddControllerAction(JoyButton.DpadUp, "play" + playerID + "up");
			AddControllerAction(JoyButton.DpadDown, "play" + playerID + "down");
			AddControllerAction(JoyButton.A, "play" + playerID + "rotateR");
			AddControllerAction(JoyButton.X, "play" + playerID + "rotateL");
			AddControllerAction(JoyButton.Start, "play" + playerID + "start");

		}
		else
		{
			AddKeyBoardAction(Key.Left, "play" + playerID + "left");
			AddKeyBoardAction(Key.Right, "play" + playerID + "right");
			AddKeyBoardAction(Key.Up, "play" + playerID + "up");
			AddKeyBoardAction(Key.Down, "play" + playerID + "down");
			AddKeyBoardAction(Key.X, "play" + playerID + "rotateR");
			AddKeyBoardAction(Key.Z, "play" + playerID + "rotateL");
			AddKeyBoardAction(Key.Enter, "play" + playerID + "start");
		}
	}

	public void OpenCPUMenu()
	{
		
		state = GameState.Menu;

		// if (players.Count == maxPlayers)
		// 	CloseMenu();
		menu.OpenCPUMenu(maxPlayers - players.Count);
	}

	public void CloseMenu(int cpus = 0)
	{
		menu.QueueFree();
		menu = null;

		for (int i = 0; i < cpus; i++)
		{
			AddCPUPlayer();
		}

		OpenCharacterSelect();
	}

	public void OpenCharacterSelect()
	{
		background.Centered = false;
		background.Position = Vector2.Zero;
		
		var newSelect = (CharacterSelect)charSelectScene.Instantiate();
		charSelect = newSelect;
		AddChild(newSelect);

		state = GameState.CharacterSelect;
	}

	public async void AllCharactersSelected()
	{
		DoTransition(1);
		await ToSignal(GameManager.transition, "animation_finished");

		charSelect.QueueFree();
		charSelect = null;

		StartGame();
	}

	public async void StartAdventure()
	{
		state = GameState.Menu;

		missionIndex = 1;
		mission = ResourceLoader.Load<Mission>("res://Missions/Mission" + missionIndex +".tres");

		DoTransition(1);
		await ToSignal(GameManager.transition, "animation_finished");

		SetMissionSettings();

		AddCPUPlayer();
		StartCutscene();
	}

	public async void NextMission()
	{
		missionIndex ++;
		var path = "res://Missions/Mission" + missionIndex + ".tres";

		if (!ResourceLoader.Exists(path))
		{
			DoTransition(3);
			await ToSignal(transition, "animation_finished");
			cam.Enabled = false;
			AddChild(creditsPrefab.Instantiate());
			crtEffect.Position = Vector2.Zero;
			return;
		}

		mission = ResourceLoader.Load<Mission>(path);

		DoTransition(1);
		await ToSignal(GameManager.transition, "animation_finished");

		SetMissionSettings();
		StartCutscene();
	}

	void SetMissionSettings()
	{
		state = GameState.Menu;

		stageMusic = mission.Music;
		background.Texture = mission.Background;
		rules = mission.Rules;
	}

	public void StartCutscene()
	{
		transition.Centered = false;
		transition.Position = Vector2.Zero;

		

		if (menu != null)
		{
			menu.QueueFree();
			menu = null;
		}
		else
		{
			foreach (var player in players)
			{
				player.Visible = false;
			}

			cam.Enabled = false;
		}

		background.Position = Vector2.Zero;
		background.Centered = false;
		var cutscene = cutscenePrefab.Instantiate<Cutscene>();
		AddChild(cutscene);


		cutscene.InitializeCutscene(missionIndex, mission.PlayerChar.TalkSprite, mission.EnemyChar.TalkSprite, mission.PlayerChar.Name, mission.EnemyChar.Name);
	}

	public void StartSinglePlayerMatch()
	{

		players[0].SetCharacter(mission.PlayerChar);
		players[1].SetCharacter(mission.EnemyChar);

		var cpu = players[1].GetNode<CPU>("CPU");
		cpu.shiftDelay = mission.CPUShiftTime;
		cpu.intellegence = mission.CPUIntellegence;

		StartGame();
	}

	public async void StartGame()
	{
		ChangeMusic(null, stageMusicFade);
		if (transition.Animation.ToString() != "opening")
		{
			DoTransition(1);
			await ToSignal(GameManager.transition, "animation_finished");
		}

		gameOver = false;
		state = GameState.Playing;

		cam.Enabled = true;	

		var averageMiddle = 0;
		foreach (var player in players)
		{
			if (player.isCPU)
			{
				var CPU = player.GetNode<CPU>("CPU");
				CPU.InitializeStrats();
			}

			averageMiddle += Mathf.RoundToInt(player.border.GlobalPosition.X);
		}

		averageMiddle /= players.Count;
		cam.GlobalPosition = new Vector2(averageMiddle, rules.WellSize.Y * cellSize / 2);
		background.Centered = true;
		background.Position = cam.Position;

		transition.Centered = true;
		transition.Position = cam.Position;

		crtEffect.Position = cam.Position - (new Vector2(544,306) / 2);

		uint seed = new RandomNumberGenerator().Randi();
		
		foreach (var player in players)
		{
			player.Visible = true;
			player.StartGameLogic(seed);
		}
		ReadyGo();

		await ToSignal(this, "GameReady");

		ChangeMusic(stageMusic, 0);
	}

	public async void ReadyGo()
	{
		var readyTextParent = readyTextPrefab.Instantiate<Node2D>();
		AddChild(readyTextParent);

		var readyText = readyTextParent.GetNode<Label>("ReadyText");

		readyText.LabelSettings = new LabelSettings();
		readyText.LabelSettings.FontSize = 46;
		readyText.LabelSettings.FontColor = new Color(1,1,1,1);

		readyTextParent.Position = cam.Position;


		await ToSignal(GetTree().CreateTimer(1), "timeout");

		readyTextParent.Visible = true;
		globalSounds.PlaySound("Ready");


		var textTween = GetTree().CreateTween().SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
		textTween.TweenProperty(readyText.LabelSettings, "font_size", 16, 1);
		await ToSignal(textTween, "finished");


		await ToSignal(GetTree().CreateTimer(1), "timeout");

		EmitSignal("GameReady");

		globalSounds.PlaySound("Go");
		readyText.Text = "GO!";
		readyText.LabelSettings.FontSize = 30;

		textTween = GetTree().CreateTween().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out).SetParallel(true);
		textTween.TweenProperty(readyText.LabelSettings, "font_size", 80, 0.5);
		textTween.TweenProperty(readyText.LabelSettings, "font_color", new Color(1,1,1,0), 0.35);

		await ToSignal(textTween, "finished");
		readyTextParent.QueueFree();
	}

	public void ResetPlayers()
	{
		foreach (var player in players)
		{
			player.Reset();
		}
		// StartGame();
	}
	
	public void SendGarbage(int playerID, int garbage)
	{
		foreach (var player in players)
		{
			if (playerID != player.playerID)
			{
				player.QueueGarbage(playerID, garbage / rules.GarbageReduction);
			}
		}
	}

	public void ChainEnded(int playerID)
	{
		foreach (var player in players)
		{
			if (playerID != player.playerID)
			{
				player.ActivateGarbage(playerID);
			}
		}
	}
	
	public async void PlayerDied()
	{
		var deathCount = 0;

		foreach (var player in players)
		{
			if (player.isDead)
			{
				deathCount++;
			}
		}

		// END GAME
		if (deathCount >= players.Count - 1)
		{
			gameOver = true;

			foreach (var player in players)
			{
				player.StopFunctioning();
				state = GameState.Menu;
				if (!player.isDead)
				{
					player.ShowEndSprite();

				}
			}

			ChangeMusic(null, 4f);
			await ToSignal(this, "MusicChanged");

			var pMenu = postRoundMenu.Instantiate<Control>();
			AddChild(pMenu);
			pMenu.Position = cam.Position;

			GD.Print("End Game");
		}
	}
}

public struct StateData {
	public string state;
	public Vector4I spaces;
	public int rotation;

	public StateData(Vector4I nSpaces, string nState, int nRotation)
	{
		state = nState;
		spaces = nSpaces;
		rotation = nRotation;
	}
}


