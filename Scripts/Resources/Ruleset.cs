using Godot;
using System;

public partial class Ruleset : Resource {
	[Export] public Vector2I WellSize {get; set;}
	[Export] public bool Bodies {get; set;}
	[Export] public int MinGroupSize {get; set;}
    [Export] public int MaxGroupSize {get; set;}
	[Export] public int Colors {get; set;}
	[Export] public int GarbageReduction {get; set;}
	[Export] public float Gravity {get; set;}
	[Export] public float FastFallMultiplier {get; set;}

    public Ruleset() : this(new Vector2I(8,13), false, 2, 2, 4, 3, 0.4f, 4f) {}

	public Ruleset(Vector2I wellSize, bool bodies, int minGroup, int maxGroup, int colors, int garbageReduction, float gravity, float fastFallMultiplier)
	{
		WellSize = wellSize;
		Bodies = bodies;
		MinGroupSize = minGroup;
		MaxGroupSize = maxGroup;
		Colors = colors;
		GarbageReduction = garbageReduction;
		Gravity = gravity;
		FastFallMultiplier = fastFallMultiplier;
	}
}