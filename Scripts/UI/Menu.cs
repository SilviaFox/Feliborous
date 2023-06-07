using Godot;
using System;

public partial class Menu : Control
{
	[Export] AudioStream music;
	[Export] Color addedPlayerColor;
	bool allowBack = true;
	Control currentlyOpen;
	OptionButton cpuButton;
	bool opened = false;

	public Label description;

	public override void _Ready()
	{
		description = GetNode<Label>("%Description");

		OpenMain();
		opened = true;
		GameManager.instance.ChangeMusic(music, 0.5f);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			GameManager.globalSounds.PlaySound("MenuBack");
			GoBack();
		}
		else if (@event.IsActionPressed("ui_up") || @event.IsActionPressed("ui_down") || @event.IsActionPressed("ui_left") || @event.IsActionPressed("ui_right"))
		{
			GameManager.globalSounds.PlaySound("MenuSwitch");
		}
		else if (@event.IsActionPressed("ui_accept"))
		{
			GameManager.globalSounds.PlaySound("MenuSelect");
		}

	}

	void GoBack()
	{
		if (allowBack)
		{
			OpenMain();
		}
	}

	public async void OpenMain()
	{
		SwitchActive("Main", true);
		await ToSignal(GameManager.transition, "animation_finished");

		AllowBack(false);
		GetNode<Button>("Main/Container/Adventure").GrabFocus();
	}

	public async void OpenVersus()
	{
		SwitchActive("Versus", true);
		await ToSignal(GameManager.transition, "animation_finished");

		AllowBack(true);
		GetNode<Button>("Versus/Container/Competitive").GrabFocus();
	}

	public void StartParty()
	{
		var rules = new Ruleset(new Vector2I(8,13), false, 2, 4, 3, 2, 0.4f, 4f);
		GameManager.rules = rules;
		OpenVersusLobby();
	}

	public void StartGame (Ruleset ruleset)
	{
		GameManager.rules = ruleset;
		OpenVersusLobby();
	}
	
	public void StartComp()
	{
		var rules = new Ruleset();
		GameManager.rules = rules;
		OpenVersusLobby();
	}

	public async void OpenVersusLobby()
	{
		AllowBack(false);
		SwitchActive("VersusLobby");
		GameManager.instance.mode = GameManager.GameMode.Versus;
		await ToSignal(GameManager.transition, "animation_finished");

		GameManager.maxPlayers = 4;
		GameManager.instance.state = GameManager.GameState.AwaitingInput;
	}

	public async void OpenAdventureLobby()
	{
		AllowBack(false);
		GameManager.instance.mode = GameManager.GameMode.Adventure;
		SwitchActive("AdventureLobby");
		await ToSignal(GameManager.transition, "animation_finished");


		GameManager.maxPlayers = 1;
		GameManager.instance.state = GameManager.GameState.AwaitingInput;
	}

	public void ShowPlayerActive()
	{
		GetNode<ColorRect>("%Player" + GameManager.instance.players.Count).Color = addedPlayerColor;
	}

	public async void OpenCPUMenu(int availableslots)
	{
		SwitchActive("VersusCPUSetup");
		await ToSignal(GameManager.transition, "animation_finished");

		cpuButton = GetNode<OptionButton>("VersusCPUSetup/CPUs");
		cpuButton.GrabFocus();

		for (int i = 0; i <= availableslots; i++)
		{
			cpuButton.AddItem(i + " CPU", i);
		}
	}

	public async void CloseMenu()
	{
		if (GameManager.transition.IsPlaying())
			return;

		int cpuCount = cpuButton.GetSelectedId();

		var music = GetNode<MusicSelector>("VersusCPUSetup/Music");
		var bg = GetNode<BackgroundSelector>("VersusCPUSetup/Background");
		GameManager.stageMusic = music.music[music.Selected].Audio;
		GameManager.instance.background.Texture = bg.bgs[bg.Selected].Sprite;

	 	GameManager.instance.DoTransition(2);
		await ToSignal(GameManager.transition, "animation_finished");

		GameManager.instance.CloseMenu(cpuCount);
	}

	public async void OpenSettings()
	{

		
		SwitchActive("Settings");
		await ToSignal(GameManager.transition, "animation_finished");

		AllowBack(true);
		GetNode<Button>("Settings/Fullscreen").GrabFocus();
	}

	async void SwitchActive(string tab, bool showDescription = false)
	{

		Control previouslyOpen = null;
		if (currentlyOpen != null)
			previouslyOpen = currentlyOpen;

		if (!GameManager.transition.IsPlaying() && opened)
		{
			GameManager.instance.DoTransition();
			await ToSignal(GameManager.transition, "animation_finished");
		}
		
		if (previouslyOpen != null)
			previouslyOpen.Visible = false;

		currentlyOpen = GetNode<Control>(tab);
		currentlyOpen.Visible = true;

		description.GetParent<Control>().Visible = showDescription;
	}

	void AllowBack(bool toggle)
	{
		allowBack = toggle;
		GetNode<Control>("InputLabel").Visible = toggle;
	}

	void ExitGame()
	{
		GetTree().Quit();
	}
}
