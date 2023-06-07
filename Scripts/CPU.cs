using Godot;
using System;
public enum Strategy {
	SideStacking,
	MatchColor,
	FillSpace,
	Random,
	MatchEnds
}

public enum CPUState {
	Shift,
	Drop,
	Awaiting
}

public partial class CPU : Node
{


	Player parent;
	RandomNumberGenerator cpuRandom = new RandomNumberGenerator();

	Strategy openingStrat = Strategy.Random, primaryStrat = Strategy.MatchColor, secondaryStrat = Strategy.MatchEnds; 
	int stratSwitchHeight = 4;
	Strategy strategy;
	CPUState state = CPUState.Awaiting;

	public int intellegence = 5;
	public float shiftDelay = 0.5f, rotateDelay = 1f;
	Timer delayTimer, rotateTimer;

	int moveID;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
		strategy = openingStrat;

		delayTimer = new Timer();
		delayTimer.OneShot = true;
		AddChild(delayTimer);

		rotateTimer = new Timer();
		rotateTimer.OneShot = true;
		rotateTimer.WaitTime = rotateDelay;
		AddChild(rotateTimer);

		parent = GetParent<Player>();

		parent.NewFeli += CPULoop;
		parent.InvalidShift += InvalidShift;
		parent.MoveComplete += MoveComplete;
	}

	public void InitializeStrats()
	{
		openingStrat = parent.character.OpeningStrat;
		primaryStrat = parent.character.PrimaryStrat;
		secondaryStrat = parent.character.SecondaryStrat;

		strategy = openingStrat;
	}

	public void InvalidShift() => state = CPUState.Drop;

	public void MoveComplete()
	{
		state = CPUState.Awaiting;

		strategy = GetStrategy();
	}
	
	Strategy GetStrategy()
	{
		for (int x = 0; x < GameManager.rules.WellSize.X; x++)
		{
			if (parent.space[x,GameManager.rules.WellSize.Y - stratSwitchHeight] != null)
			{
				return secondaryStrat;
			}
		}

		return strategy == openingStrat? openingStrat : primaryStrat;
	}

	public async void RotatePiece()
	{
		rotateTimer.Start();
		await ToSignal(rotateTimer, "timeout");

		parent.RotateCurrent(GD.RandRange(0,1) == 1);
	}

	public async void CPULoop()
	{
		moveID ++;
		var thisMove = moveID;

		var toPos = GetToPos();

		if (intellegence > 1)
		{
			foreach (var pos in parent.deathPos)
			{
				if (parent.space[pos.X, 1] != null && toPos == pos.X)
				{
					strategy = Strategy.FillSpace;
					toPos = GetToPos();
					break;
				}
			}
		}

		delayTimer.WaitTime = shiftDelay;
		state = CPUState.Shift;

		if (intellegence > 2)
			RotatePiece();

		while (state == CPUState.Shift && thisMove == moveID && parent.currentFeliGroup.Position.X / 12 != toPos)
		{
			var direction = parent.currentFeliGroup.Position.X / 12 < toPos? 1 : -1;
			parent.Shift(direction);

			delayTimer.Start();
			await ToSignal(delayTimer, "timeout");
		}

		state = CPUState.Drop;

		if (thisMove != moveID)
			return;

		if (intellegence > 3 && state == CPUState.Drop)
		{
			parent.FastFall();
			await ToSignal(parent, "MoveComplete");
			parent.ReleaseFastFall();
		}

		
		
	}

	int GetToPos()
	{
		// Get Position to move to
		switch (strategy)
		{
			case Strategy.SideStacking:
				// move to shorter side

				int CheckPos(int point)
				{
					for (int i = 0; i < GameManager.rules.WellSize.Y; i++)
					{
						if (parent.space[point, i] != null)
						{
							return i;
						}
					}

					return GameManager.rules.WellSize.Y;
				}

				// check left
				var leftPos = 0;

				while (parent.space[leftPos, 0] != null)
					leftPos ++;

				var leftHeight = CheckPos(leftPos);

				// check right
				var rightPos = GameManager.rules.WellSize.X - 1;

				while (parent.space[rightPos, 0] != null)
					rightPos --;

				var rightHeight = CheckPos(rightPos);

				// pick the lower of the two
			return (leftHeight > rightHeight? leftPos : rightPos);


			case Strategy.FillSpace:

				// for each y pos, check x pos, if x pos is free, thats the space

				for (int y = GameManager.rules.WellSize.Y -1; y >= 0; y--)
				{
					for (int x = 0; x < GameManager.rules.WellSize.X; x++)
					{
						if (parent.space[x, y] == null)
						{
							return x;
						}
						
					}
				}

				// pick the lower of the two
			return 0;

			case Strategy.MatchColor:

				foreach (Feli feli in parent.currentFeliGroup.GetChildren())
				{
					parent.RotateCurrent(false);
					for (int x = 0; x < GameManager.rules.WellSize.X; x++)
					{
						for (int y = 0; y < GameManager.rules.WellSize.Y; y++)
						{
							if (parent.space[x,y] != null && parent.space[x,y].color == feli.color)
							{
								return x;
							}
						}
					}
				}

			return cpuRandom.RandiRange(0, GameManager.rules.WellSize.X -1);

			case Strategy.Random:
				var rotation = cpuRandom.RandiRange(-1,1);
				if (rotation == -1) parent.RotateCurrent(true);
				if (rotation == 1) parent.RotateCurrent(false); 

			return cpuRandom.RandiRange(0,GameManager.rules.WellSize.X-1);

			case Strategy.MatchEnds:

				foreach (Feli feli in parent.currentFeliGroup.GetChildren())
				{
					if (feli.type != Feli.Type.Body)
					{
						var targetType = feli.type == Feli.Type.Head? Feli.Type.Tail : Feli.Type.Head;

						parent.RotateCurrent(false);
						for (int x = 0; x < GameManager.rules.WellSize.X; x++)
						{
							for (int y = 0; y < GameManager.rules.WellSize.Y; y++)
							{
								if (parent.space[x,y] != null && parent.space[x,y].color == feli.color && feli.type == targetType)
								{
									return x;
								}
							}
						}
					}
				}

			return cpuRandom.RandiRange(0, GameManager.rules.WellSize.X -1);

		}


		return 0;
	}

}


