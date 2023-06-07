using Godot;
using System;
using System.Collections.Generic;

public partial class Player : Node2D
{

	[Export] PackedScene crossPrefab;
	[Export] float gravityAnimationTime = 0.15f, eatAnimTime = 0.3f, flashAnimTime = 0.2f;
	[Export] int flickerFrames = 8;
	[Export] Vector2 nextOffset, postNextOffset;
	[Export] float dasStartTime = 0.4f, dasEndTime = 0.1f;
	[Export] int dasRampTicks = 3;
	[Export] float deathAnimTime = 1;
	[Export] float baseCharacterScale = 1.5f, baseCharacterOffset = 3f;

	// INPUT
	public int inputDevice;
	public int playerID;
	public bool usingKeyboard;
	public bool isCPU, isDead;
	bool inputEnabled;

	// Game
	public Feli[,] space;
	public Vector2I[] deathPos;
	
	public Node2D currentFeliGroup;
	Queue<Node2D> nextFeliGroup = new Queue<Node2D>();
	Node2D board, nuisanceQueue;
	Sprite2D well;

	Timer gravTimer;
	float spawnPos;

	RandomNumberGenerator rng = new RandomNumberGenerator();
	int typeIncrement;
	int currentDAS;

	Timer flipTimer, DAStimer, animTimer;

	// Garbage
	RandomNumberGenerator garbageRNG = new RandomNumberGenerator();
	Dictionary<int, int> incomingGarbage = new Dictionary<int, int>();

	[Export] float garbageFallTime = 0.02f;
	int currentChain, activeGarbage;

	// Character
	public Character character;
	Sprite2D characterSprite;

	// Audio
	SoundManager sound;

	// End
	[Export] Texture2D winSprite, lossSprite;
	Sprite2D endSprite;
	public Sprite2D border;
	public float borderWidth;


	// Offsets
	public Vector2[] kickPos = {new Vector2(0,0), new Vector2(0, -12), new Vector2(-12, 0), new Vector2(12, 0), new Vector2(0,12)};
	Vector2[] spawnOffsets = {new Vector2(0,0), new Vector2(0,-1), new Vector2(1,0), new Vector2(1,-1)};
	RotationPos[] rotations = {
	new RotationPos(new Vector2I(1,0), new Vector2I(0,1), new Vector2I(0,-1)),
	new RotationPos(new Vector2I(-1,0), new Vector2I(0,-1), new Vector2I(0,1)),
	new RotationPos(new Vector2I(0,1), new Vector2I(-1,0), new Vector2I(1,0)),
	new RotationPos(new Vector2I(0,-1), new Vector2I(1,0), new Vector2I(-1,0)),
	new RotationPos(new Vector2I(1,-1), new Vector2I(1,1), new Vector2I(-1,-1)),
	new RotationPos(new Vector2I(1,1), new Vector2I(-1,1), new Vector2I(1,-1)),
	new RotationPos(new Vector2I(-1,1), new Vector2I(-1,-1), new Vector2I(1,1)),
	new RotationPos(new Vector2I(-1,-1), new Vector2I(1,-1), new Vector2I(-1,1))
	};

	[Signal] public delegate void NewFeliEventHandler();
	[Signal] public delegate void MoveCompleteEventHandler();
	[Signal] public delegate void InvalidShiftEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sound = GetNode<SoundManager>("Sounds");
		flipTimer = GetNode<Timer>("FlipTimer");

		animTimer = new Timer();
		animTimer.OneShot = true;
		AddChild(animTimer);

		DAStimer = new Timer();
		DAStimer.OneShot = true;
		AddChild(DAStimer);

		board = new Node2D();
		board.ZIndex = 1;
		AddChild(board);

		nuisanceQueue = new Node2D();
		nuisanceQueue.ZIndex = 2;
		nuisanceQueue.Position = new Vector2(0, -10);
		AddChild(nuisanceQueue);

		space = new Feli[GameManager.rules.WellSize.X, GameManager.rules.WellSize.Y];
		spawnPos = (GameManager.rules.WellSize.X / 2) - 1;

		CreateWell();

		gravTimer = new Timer();
		AddChild(gravTimer);
		gravTimer.Timeout += ActiveFall;

		
	}

	public void CreateWell()
	{
		// WELL
		well = new Sprite2D();
		AddChild(well);
		well.Name = "Well"; 
		well.ClipChildren = ClipChildrenMode.AndDraw;
		well.ZIndex = 1;

		var wellText = Image.Create(GameManager.rules.WellSize.X * 12, (GameManager.rules.WellSize.Y - 1) * 12, false, Image.Format.Rgbaf);
		wellText.Fill(GameManager.instance.wellColor);

		well.Texture = ImageTexture.CreateFromImage(wellText);
		well.Position = new Vector2((GameManager.rules.WellSize.X - 1) * 12, (GameManager.rules.WellSize.Y) * 12) / 2;

		// BORDER
		border = new Sprite2D();
		AddChild(border);

		var borderRes = new Vector2I((GameManager.rules.WellSize.X * 12) + GameManager.instance.borderSize.X, (GameManager.rules.WellSize.Y * 12) + GameManager.instance.borderSize.Y);
		borderWidth = borderRes.X;

		var borderText = Image.Create(borderRes.X, borderRes.Y, false, Image.Format.Rgbaf);
		borderText.Fill(GameManager.instance.borderColor);

		border.Texture = ImageTexture.CreateFromImage(borderText);
		border.Position = new Vector2I((((GameManager.rules.WellSize.X) /2) * 12) + 8, (borderRes.Y - 24) /2);

		// Set Death position
		var leftCross = (Sprite2D)crossPrefab.Instantiate();
		AddChild(leftCross);
		leftCross.Position = new Vector2(spawnPos * 12, 12);

		var rightCross = (Sprite2D)crossPrefab.Instantiate();
		AddChild(rightCross);
		rightCross.Position = new Vector2((spawnPos * 12) + 12, 12);

		deathPos = new Vector2I[] {RoundToVec2I(rightCross.Position / 12), RoundToVec2I(leftCross.Position / 12)};

		// SetCharacter(GD.Load<Character>("res://Characters/Araneae/Araneae.tres"));
	}

	public void SetCharacter(Character newChar)
	{

		character = newChar;

		if (characterSprite == null)
		{
			characterSprite = new Sprite2D();
			well.AddChild(characterSprite);
		}

		var preferredScale = GameManager.rules.WellSize * 12;
		var textureScale = character.Sprite.GetSize();

		var s = preferredScale / textureScale;

		characterSprite.Scale = (Vector2.One * baseCharacterScale) * s.X;
		characterSprite.Modulate = new Color(1,1,1,0.5f);
		characterSprite.Texture = character.Sprite;
	}

	public void Reset()
	{
		incomingGarbage.Clear();
		isDead = false;
		activeGarbage = 0;

		foreach (var nextFeli in nextFeliGroup)
		{
			nextFeli.QueueFree();
		}

		nextFeliGroup.Clear();

		foreach (var child in nuisanceQueue.GetChildren())
		{
			child.QueueFree();
		}

		foreach (var child in board.GetChildren())
		{
			child.QueueFree();
		}

		endSprite.QueueFree();

		ReleaseFastFall();

		space = new Feli[GameManager.rules.WellSize.X, GameManager.rules.WellSize.Y];
	}

	public async void StartGameLogic(uint seed)
	{
		gravTimer.WaitTime = GameManager.rules.Gravity;
		rng.Seed = seed;
		garbageRNG.Seed = seed;

		OrganizeNextQueue();

		await ToSignal(GameManager.instance, "GameReady");
		NewFeliGroup();
	}

	public int GetGroupSize()
	{
		if (GameManager.rules.MaxGroupSize == 4 && GameManager.rules.MinGroupSize == 2)
		{
			int groupSize = rng.RandiRange(0, 20);
			if (groupSize > 18)
				return 4;
			else if (groupSize > 14)
				return 3;

			return groupSize = 2;
		}
		

		return rng.RandiRange(GameManager.rules.MinGroupSize, GameManager.rules.MaxGroupSize);
	}

	public int GetFeliType()
	{
		if (GameManager.rules.Bodies)
		{
			typeIncrement ++;
			return Mathf.Wrap(typeIncrement, 0, 3);
		}
		
		return rng.RandiRange(0,1);
	}

	public void NewNextFeli()
	{
		var nextFeli = new Node2D();
		board.AddChild(nextFeli);

		var groupSize = GetGroupSize();
		// int groupSize = 4;
		
		for (int i = 0; i < groupSize; i++)
		{
			int newColor = rng.RandiRange(0, GameManager.rules.Colors - 1);
			// int startType = rng.RandiRange(0, 2);
			int startType = GetFeliType();

			var newFeli = new Feli(newColor, (Feli.Type)startType);

			newFeli.Position = spawnOffsets[i] * 12;
			nextFeli.AddChild(newFeli);

		}

		nextFeliGroup.Enqueue(nextFeli);

	}

	public void OrganizeNextQueue()
	{
		while (nextFeliGroup.Count < 2)
		{
			NewNextFeli();
		}
		var xOffset = Vector2.Right * GameManager.rules.WellSize.X * 12;

		nextFeliGroup.ToArray()[0].Position = xOffset + nextOffset;
		nextFeliGroup.ToArray()[1].Position = xOffset + nextOffset + postNextOffset;
	}

	public void NewFeliGroup()
	{
		if (GameManager.gameOver) return;


		currentFeliGroup = nextFeliGroup.Dequeue();

		currentFeliGroup.Position = new Vector2(spawnPos, 1) * GameManager.cellSize;
		gravTimer.Start();	
		inputEnabled = true;

		OrganizeNextQueue();
		EmitSignal("NewFeli");
		
	}

	// Shift
	public async void AutoShift(int direction, string input)
	{
		currentDAS ++;
		int dasID = currentDAS;

		DAStimer.Stop();
		DAStimer.WaitTime = dasStartTime;
		int ticks = 0;

		while (Input.IsActionPressed(input))
		{
			Shift(direction);

			DAStimer.Start();

			await ToSignal(DAStimer, "timeout");

			if (currentDAS != dasID || !inputEnabled) return;

			if (ticks != dasRampTicks)
			{
				ticks ++;
				DAStimer.WaitTime = Mathf.Lerp(dasStartTime, dasEndTime, (float)ticks / dasRampTicks);
			}

		}

	}

	public void Shift(int direction)
	{
		foreach (Feli feli in currentFeliGroup.GetChildren())
		{
			Vector2I nextPos = RoundToVec2I(((feli.Position + currentFeliGroup.Position) / 12) + (Vector2.Right * direction));

			if (!ValidLocation(nextPos))
			{
				DAStimer.Stop();
				EmitSignal("InvalidShift");
				return;
			}
		}
		
		sound.PlaySound("Shift");
		currentFeliGroup.Position += Vector2.Right * direction * 12;
	}

	public void RotateCurrent(bool counter)
	{
		if (!inputEnabled) return;

		Vector2 mainFrom = currentFeliGroup.Position;
		Dictionary<Feli, Vector2> feliFrom = new Dictionary<Feli, Vector2>(); 

		// Do rotation
		foreach (Feli child in currentFeliGroup.GetChildren())
		{
			feliFrom.Add(child, child.Position);

			if (child.Position != Vector2.Zero)
			{
				foreach (var pos in rotations)
				{
					if (pos.from * 12 == child.Position)
					{
						child.Position = (counter? pos.counter : pos.clock) * 12;
						break;
					}
				}

			}
		}


		// Check positions/kick
		bool validPos = true;

		foreach (var pos in kickPos)
		{
			currentFeliGroup.Position = mainFrom + pos;

			foreach(Feli child in currentFeliGroup.GetChildren())
			{
				if (!ValidLocation(RoundToVec2I((currentFeliGroup.Position + child.Position) / 12)))
				{
					validPos = false;
				}
			}

			if (validPos)
			{
				sound.PlaySound("Rotate");
				return;
			}
			else
				validPos = true;

		}

		// If rotation fails
		currentFeliGroup.Position = mainFrom;
		foreach (Feli child in currentFeliGroup.GetChildren())
		{
			child.Position = feliFrom[child];
		}

		if (currentFeliGroup.GetChildCount() == 2)
		{
			if (!flipTimer.IsStopped())
			{
				// try 180 degree rotation
				foreach (Feli feli in currentFeliGroup.GetChildren())
				{
					if (feli.Position != Vector2.Zero)
					{
						var pos = RoundToVec2I(feli.Position * -1);
						if (ValidLocation((RoundToVec2I(currentFeliGroup.Position) + pos) / 12))
						{
							feli.Position = pos;
						}
					}
				}

				flipTimer.Stop();
				sound.PlaySound("Rotate");

			}
			else
				flipTimer.Start();
		}

	}

	// Gravity
	public void ActiveFall()
	{
		foreach (Feli feli in currentFeliGroup.GetChildren())
		{
			Vector2I nextPos = RoundToVec2I(((currentFeliGroup.Position + feli.Position) / 12) + Vector2.Down);

			if (!ValidLocation(nextPos))
			{
				RegisterFelis();
				return;
			}

		}
		currentFeliGroup.Position += Vector2.Down * 12;
	}

	void RegisterFelis()
	{
		EmitSignal("MoveComplete");
		inputEnabled = false;

		var queue = currentFeliGroup.GetChildren();
		foreach (Feli feli in queue)
		{
			var pos = RoundToVec2I((currentFeliGroup.Position + feli.Position) / 12);

			feli.GetParent().RemoveChild(feli);
			board.AddChild(feli);
			
			MoveBoardFeli(pos, feli, true);
		}

		currentFeliGroup.QueueFree();

		sound.PlaySound("Place");

		gravTimer.Stop();
		Gravity();
	}

	async void Gravity()
	{
		float longestDistance = 0;

		for (int y = GameManager.rules.WellSize.Y - 1; y > -1; y--)
		{
			for (int x = 0; x < GameManager.rules.WellSize.X; x++)
			{

				if (space[x,y] != null)
				{
					var startPos = new Vector2I(x,y);
					var endPos = startPos;

					// check for empty space
					while (ValidLocation(endPos + Vector2I.Down))
					{
						endPos += Vector2I.Down;
					}

					var distance = ((Vector2)startPos).DistanceTo(endPos);
					if (distance > longestDistance) longestDistance = distance;

					MoveBoardFeli(endPos, space[x,y], false, gravityAnimationTime);
				}
			}
		}

		if (longestDistance != 0)
		{
			animTimer.WaitTime = longestDistance * gravityAnimationTime;
			animTimer.Start();

			await ToSignal(animTimer, "timeout");
		}



		ClearFelis();
	}

	void MoveBoardFeli(Vector2I pos, Feli feli, bool skipRemoveCheck = false, float tweenTime = 0, bool multiplyByDistance = true)
	{

		var oldPos = RoundToVec2I(feli.Position / 12);
		if (!skipRemoveCheck)
		{
			space[oldPos.X,oldPos.Y] = null;
		}

		space[pos.X,pos.Y] = feli;

		var endPos =  pos * 12;

		if (multiplyByDistance)
		{
			var distance = ((Vector2)oldPos).DistanceTo(endPos / 12);

			if (tweenTime == 0 || distance == 0)
			{
				feli.Position = endPos;
			}
			else {
				var feliTween = GetTree().CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);
				feliTween.TweenProperty(feli, "position", (Vector2)endPos, tweenTime * distance);
				feliTween.Finished += () => sound.PlaySound("Place");
			}
		}
		else
		{
			var feliTween = GetTree().CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);
			feliTween.TweenProperty(feli, "position", (Vector2)endPos, tweenTime);
			feliTween.Finished += () => sound.PlaySound("Place");
		}
	}

	void ConnectFelis(bool postGarbage = false)
	{
		var adjacents = new List<Feli>();

		int CheckSpace(Vector2I pos, Feli.FeliColor color)
		{
			// check if pos is within space
			
			if (pos.X < 0 || pos.X == GameManager.rules.WellSize.X || pos.Y < 0 || pos.Y == GameManager.rules.WellSize.Y)
			{
				return 0;
			}

			if (space[pos.X, pos.Y] != null && color == space[pos.X, pos.Y].color)
			{
				adjacents.Add(space[pos.X,pos.Y]);
				return 1;
			}
			
			return 0;
		}

		for (int x = 0; x < GameManager.rules.WellSize.X; x++)
		{
			for (int y = 0; y < GameManager.rules.WellSize.Y; y++)
			{
				if (space[x,y] != null && space[x,y].color != Feli.FeliColor.Garbage)
				{

					var feli = space[x,y];
					var color = space[x,y].color;
					// Is there a feli above
					feli.occupiedSpaces.Y = CheckSpace(new Vector2I(x, y-1), color);
					feli.occupiedSpaces.W = CheckSpace(new Vector2I(x, y+1), color);
					feli.occupiedSpaces.X = CheckSpace(new Vector2I(x-1, y), color);
					feli.occupiedSpaces.Z = CheckSpace(new Vector2I(x+1, y), color);



					// Extent as body
					if (feli.type == Feli.Type.Body && adjacents.Count == 1)
					{
						feli.startType = adjacents[0].type;
						feli.type = adjacents[0].type;
					}

					feli.UpdateState();
					adjacents.Clear();
				}

				
			}	
		}

		// check for kill
		for (int x = 0; x < GameManager.rules.WellSize.X; x++)
		{
			for (int y = 0; y < GameManager.rules.WellSize.Y; y++)
			{
				foreach (var pos in deathPos)
				{
					if (space[x,y] != null && new Vector2I(x,y) == pos)
					{
						KillPlayer();
						return;
					}
				}
			}
		}

		if (!postGarbage)
			RecieveGarbage();
		else
			NewFeliGroup();
	}

	async void ClearFelis()
	{
		// check spaces for heads connected to tails
		List<Feli> itteratedForClear = new List<Feli>();
		var headPositions = new List<EatAnimInfo>();
		var checkPos = new Vector2I[] {new Vector2I(1,0),new Vector2I(-1,0),new Vector2I(0,1),new Vector2I(0,-1)};

		EatAnimInfo[] CheckAdjacents(Vector2I pos, Feli.FeliColor color)
		{
			List<EatAnimInfo> confirmedChecks = new List<EatAnimInfo>();

			foreach (var adjacent in checkPos)
			{
				var check = pos + adjacent;
				if (InBounds(check))
				{
					var newMark = space[check.X, check.Y];
					var rotation = 0;
					var flip = false;

					if (adjacent.X == -1) flip = true;
					else if (adjacent.Y == -1) rotation = -90;
					else if (adjacent.Y == 1) rotation = 90;

					if (newMark != null && !itteratedForClear.Contains(newMark) && (newMark.color == color || newMark.color == Feli.FeliColor.Garbage))
					{
						itteratedForClear.Add(newMark);
						if (newMark.color != Feli.FeliColor.Garbage)
							confirmedChecks.Add(new EatAnimInfo(check, rotation, flip));
					}

				}

			}

			return confirmedChecks.ToArray();
		}

		// If heads are connected to tails mark both for clear
		for (int x = 0; x < GameManager.rules.WellSize.X; x++)
		{
			for (int y = 0; y < GameManager.rules.WellSize.Y; y++)
			{
				if (space[x,y] != null && space[x,y].type == Feli.Type.Head)
				{
					var start = new Vector2I(x,y);
					var color = space[x,y].color;
					bool found = false;
					

					// check adjacent spaces
					foreach (var pos in checkPos)
					{
						var check = start + pos;
						if (InBounds(check) && space[check.X, check.Y] != null)
						{
							var checkFeli = space[check.X, check.Y];
							if (checkFeli.color == color && checkFeli.type == Feli.Type.Tail)
							{
								found = true;
							}
							
						}

					}

					if (found) {
						headPositions.Add(new EatAnimInfo(new Vector2I(x,y), 0, false));
						itteratedForClear.Add(space[x,y]);
					} 
				}
			}
		}

		// clear heads and tails adjacent to spaces found
		if (itteratedForClear.Count == 0) // Nothing marked for clear
		{
			// GARBAGE end chain
			GameManager.instance.ChainEnded(playerID);
			currentChain = 0;

			ConnectFelis();
			return;
		}
		
		sound.PlaySound("ClearStart");

		var eatSprites = new Node2D();
		AddChild(eatSprites);

		void ItterateAnimation(EatAnimInfo[] animInfos, bool start = false)
		{
			foreach (var sprite in eatSprites.GetChildren())
			{
				sprite.QueueFree();
			}

			foreach (var anim in animInfos)
			{
				var baseFeli = space[anim.pos.X, anim.pos.Y];
				baseFeli.Visible = false;

				var sprite = new AnimatedSprite2D();
				sprite.SpriteFrames = GameManager.feliEatSprite;
				sprite.SpeedScale = 2;

				sprite.Play(baseFeli.GetColorAsString() + (start? "_flash" : "_eat"));

				eatSprites.AddChild(sprite);
				sprite.ZIndex = 5;
				sprite.RotationDegrees = anim.rotation;
				sprite.FlipH = anim.flip;
				sprite.Position = anim.pos * 12;

			}
		}

		var heads = itteratedForClear;
		
		ItterateAnimation(headPositions.ToArray(), true);

		animTimer.WaitTime = flashAnimTime;
		animTimer.Start();
		await ToSignal(animTimer, "timeout");

		int count = itteratedForClear.Count;
		bool moreItterated = true;
		
		animTimer.WaitTime = eatAnimTime;
		List<EatAnimInfo> positionsToAnim = new List<EatAnimInfo>();

		float pitch = 1;
		
		while (moreItterated)
		{
			positionsToAnim.Clear();

			for (int i = 0; i < count; i++)
			{
				var feli = itteratedForClear[i];

				if (feli.color != Feli.FeliColor.Garbage)
				{
					var pos = RoundToVec2I(feli.Position / 12);	
					positionsToAnim.AddRange(CheckAdjacents(pos, feli.color));
				}
				
			}

				

			if (count == itteratedForClear.Count)
			{
				moreItterated = false;
				ItterateAnimation(positionsToAnim.ToArray());
			}
			else
			{
				count = itteratedForClear.Count;

				ItterateAnimation(positionsToAnim.ToArray());

				animTimer.Start();
				await ToSignal(animTimer, "timeout");

			}

			sound.PlaySound("Chomp", pitch);
			pitch += 0.5f;
		}
		
		animTimer.WaitTime = 0.03f;

		for (int i = 0; i < flickerFrames; i++)
		{
			foreach (var feli in itteratedForClear)
			{
				feli.Visible = !feli.Visible;
			}
			
			sound.PlaySound("ClearFlicker", 1f + (0.25f * currentChain));
			animTimer.Start();
			await ToSignal(animTimer, "timeout");
		}

		// GARBAGE: increase chain count
		eatSprites.QueueFree();
		currentChain ++;

		int headMultiplier = 0;
		int garbage = 0;

		// clear marked felis
		foreach(var feli in space)
		{
			if (feli != null && itteratedForClear.Contains(feli))
			{
				if (feli.type == Feli.Type.Head && feli.color != Feli.FeliColor.Garbage)
					headMultiplier ++;
				
				garbage ++;

				var pos = RoundToVec2I(feli.Position / 12);

				feli.QueueFree();
				space[pos.X, pos.Y] = null;
			}
		}

		// sound.PlaySound("Clear");

		garbage = garbage * currentChain * headMultiplier;
		garbage = OffsetGarbage(garbage);
		UpdateNuisanceQueue();

		GameManager.instance.SendGarbage(playerID, garbage);

		Gravity();

	}

	int OffsetGarbage(int outgoing)
	{
		// check active garbage
		if (activeGarbage > 0)
		{
			activeGarbage -= outgoing;
			if (activeGarbage > 0)
				return 0;
			else
			{
				outgoing = -activeGarbage;
				activeGarbage = 0;
			}
			
		}

		foreach (var garb in incomingGarbage.Keys)
		{
			// check active garbage
			if (incomingGarbage[garb] > 0)
			{
				incomingGarbage[garb] -= outgoing;
				

				if (incomingGarbage[garb] > 0)
					return 0;
				else
				{
					outgoing = -incomingGarbage[garb];
					incomingGarbage[garb] = 0;
				}
				
			}
		}

		// PARRY!!
		return outgoing;
	}

	// Put garbage on well
	async void RecieveGarbage()
	{	

		bool CheckKeepGoing(bool[] checks)
		{
			foreach(var check in checks)
			{
				if (check)
				{
					return true;	
				}
			}

			return false;
		}

		if (activeGarbage == 0)
		{
			NewFeliGroup();
			return;
		}

		var ableToRecieve = new List<bool>();
		bool keepGoing = true;
		
		while (activeGarbage != 0 && keepGoing)
		{
			ableToRecieve.Clear();

			var garbPos = GetGarbagePositions();
			for (int x = 0; x < GameManager.rules.WellSize.X; x++)
			{

				if (activeGarbage == 0)
				{
					break;
				}

				var y = GameManager.rules.WellSize.Y - 1;
				var pos = garbPos[x];
				bool foundValid = false;

				while (y != -1 && !foundValid)
				{
					
					if (ValidLocation(new Vector2I(pos,y)))
					{
						activeGarbage --;
						// Add garbage to board
						var garbage = new Feli((int)Feli.FeliColor.Garbage, Feli.Type.Head);
						garbage.color = Feli.FeliColor.Garbage;

						board.AddChild(garbage);
						garbage.Position = (new Vector2I(pos,-1)) * 12;

						MoveBoardFeli(new Vector2I(pos,y), garbage, true, garbageFallTime, false);

						foundValid = true;
						ableToRecieve.Add(true);
					}

					if (!foundValid)
					{
						ableToRecieve.Add(false);
					}

					y--;
				}

			}

			keepGoing = CheckKeepGoing(ableToRecieve.ToArray());

			

		}

		UpdateNuisanceQueue();

		animTimer.WaitTime = garbageFallTime;
		animTimer.Start();

		await ToSignal(animTimer, "timeout");

		ConnectFelis(true);

	}

	// Recieve outgoing garbage
	public void QueueGarbage(int garbID, int amount)
	{
		if (incomingGarbage.ContainsKey(garbID))
		{
			incomingGarbage[garbID] += amount;
		}
		else
			incomingGarbage.Add(garbID, amount);

		UpdateNuisanceQueue();
	}

	// pull down all garbage from the queue
	public void ActivateGarbage(int garbID)
	{
		if (incomingGarbage.ContainsKey(garbID))
		{
			activeGarbage += incomingGarbage[garbID];
			incomingGarbage.Remove(garbID);
		}
	}

	public void UpdateNuisanceQueue()
	{
		foreach (var child in nuisanceQueue.GetChildren())
		{
			child.QueueFree();
		}

		// Read through garbage
		var totalGarbage = activeGarbage;

		foreach (var garbage in incomingGarbage.Values)
		{
			totalGarbage += garbage;
		}

		List<string> garbageAnims = new List<string>();

		// Place sprites above well that represent garbage
		while (totalGarbage != 0)
		{
			if (totalGarbage > GameManager.rules.WellSize.X * (GameManager.rules.WellSize.Y / 2))
			{
				garbageAnims.Add("large");
				totalGarbage -= GameManager.rules.WellSize.X * (GameManager.rules.WellSize.Y / 2);

			}
			else if (totalGarbage > GameManager.rules.WellSize.X)
			{
				garbageAnims.Add("medium");
				totalGarbage -= GameManager.rules.WellSize.X;
			}
			else
			{
				garbageAnims.Add("small");
				totalGarbage --;
			}
		}

		int i = 0;

		foreach (var anim in garbageAnims)
		{
			var sprite = new AnimatedSprite2D();
			sprite.SpriteFrames = GameManager.nuisanceSprite;
			nuisanceQueue.AddChild(sprite);

			sprite.Position = new Vector2(i * 10,0);
			sprite.Play(anim);

			i ++;
		}
	}

	async void KillPlayer()
	{
		if (GameManager.gameOver) return;

		sound.PlaySound("Death");

		var deathTween = GetTree().CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce).SetParallel(true);
		deathTween.SetProcessMode(Tween.TweenProcessMode.Idle);

		for (int y = GameManager.rules.WellSize.Y - 1; y > -1; y--)
		{
			for (int x = 0; x < GameManager.rules.WellSize.X; x++)
			{
				var feli = space[x,y];
				if (feli != null)
				{
					float addedTime = (x + (GameManager.rules.WellSize.Y - y)) * deathAnimTime;
					deathTween.TweenProperty(feli, "position", feli.Position + (Vector2.Down * GameManager.rules.WellSize.Y * 2 * 12), deathAnimTime + addedTime);
				}
			}

		}

		isDead = true;
		GameManager.instance.PlayerDied();

		ShowEndSprite();

		await ToSignal(deathTween, "finished");
	}

	public void ShowEndSprite()
	{
		endSprite = new Sprite2D();
		AddChild(endSprite);

		endSprite.Centered = false;
		endSprite.Texture = isDead? lossSprite : winSprite;
		endSprite.ZIndex = 10;
	}

	bool ValidLocation(Vector2I pos)
	{
		if (pos.X >= GameManager.rules.WellSize.X || pos.X < 0)
			return false;

		if (pos.Y >= GameManager.rules.WellSize.Y || pos.Y < 0)
			return false;

		if (space[pos.X, pos.Y] != null)
			return false;

		return true;
	}

	bool InBounds(Vector2 pos)
	{
		if (pos.X < 0 || pos.X == GameManager.rules.WellSize.X || pos.Y < 0 || pos.Y == GameManager.rules.WellSize.Y)
		{
			return false;
		}
		return true;
	}

	public void FastFall()
	{
		if (!gravTimer.IsStopped())
		{
			gravTimer.Stop();
			gravTimer.WaitTime = GameManager.rules.Gravity / GameManager.rules.FastFallMultiplier;
			gravTimer.Start();
		}
		else
		{
			gravTimer.WaitTime = GameManager.rules.Gravity / GameManager.rules.FastFallMultiplier;
		}
	}

	public void ReleaseFastFall() => gravTimer.WaitTime = GameManager.rules.Gravity;

	public void StopFunctioning()
	{
		gravTimer.Stop();
		inputEnabled = false;
	}

    public override void _Input(InputEvent @event)
    {
		if (isCPU) return;

		switch (GameManager.instance.state)
		{
			case GameManager.GameState.AwaitingInput:
        		if (@event.IsActionPressed("play" + playerID + "start") && GameManager.instance.mode == GameManager.GameMode.Versus)
				{
					GameManager.instance.OpenCPUMenu();
					// GameManager.instance.StartGame();
				}
				else if (GameManager.instance.mode == GameManager.GameMode.Adventure)
				{
					GameManager.instance.StartAdventure();
				}
			break;

			case GameManager.GameState.CharacterSelect:
				if (@event.IsActionPressed("play" + playerID + "left")) GameManager.charSelect.ChangeSelected(-1, playerID);

				if (@event.IsActionPressed("play" + playerID + "right")) GameManager.charSelect.ChangeSelected(1, playerID);

				if (@event.IsActionPressed("play" + playerID + "rotateR")) GameManager.charSelect.SelectCharacter(playerID); 
			break;

			case GameManager.GameState.Playing:
				if (inputEnabled)
				{
					if (@event.IsActionPressed("play" + playerID + "left")) AutoShift(-1, "play" + playerID + "left");

					if (@event.IsActionPressed("play" + playerID + "right")) AutoShift(1, "play" + playerID + "right");

					if (@event.IsActionPressed("play" + playerID + "rotateL")) RotateCurrent(true);

					if (@event.IsActionPressed("play" + playerID + "rotateR")) RotateCurrent(false); 

					if (@event.IsActionPressed("play" + playerID + "down")) {
						FastFall();
					}
				}


				if (@event.IsActionReleased("play" + playerID + "down")) {
					ReleaseFastFall();
				}
			break;
		}
    }

	public int[] GetGarbagePositions()
	{
		List<int> order = new List<int>();
		List<int> shuffled = new List<int>();

		for (int i = 0; i < GameManager.rules.WellSize.X; i++)
		{
			order.Add(i);
		}

		while (order.Count != 0)
		{
			var randPoint = garbageRNG.RandiRange(0, order.Count-1);
			shuffled.Add(order[randPoint]);
			order.Remove(order[randPoint]);
		}


		return shuffled.ToArray();
	}

	public static Vector2I RoundToVec2I(Vector2 from)
	{
		var x = Mathf.RoundToInt(from.X);
		var y = Mathf.RoundToInt(from.Y);

		return new Vector2I(x,y);
	}
}

public class RotationPos
{
	public Vector2I from, clock, counter;

	public RotationPos(Vector2I nFrom, Vector2I nClock, Vector2I nCounter)
	{
		from = nFrom;
		clock = nClock;
		counter = nCounter;
	}
}

public struct EatAnimInfo
{
	public Vector2I pos;
	public int rotation;
	public bool flip;

	public EatAnimInfo(Vector2I nPos, int nRotation, bool nFlip)
	{
		pos = nPos;
		rotation = nRotation;
		flip = nFlip;
	}
}