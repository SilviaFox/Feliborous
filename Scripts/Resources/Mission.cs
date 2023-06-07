using Godot;
using System;

public partial class Mission : Resource
{
    // [Export] public Dialogue[] Dialogue {get; set;}
    [Export] public Character PlayerChar {get; set;}
    [Export] public Character EnemyChar {get; set;}
    [Export] public Ruleset Rules {get; set;}
    [Export] public AudioStream Music {get; set;}
    [Export] public Texture2D Background {get; set;}
    [Export] public int CPUIntellegence {get; set;}
    [Export] public float CPUShiftTime {get; set;}

    public Mission() : this(null, null, null, null, null, 0, 0) {}

    public Mission(Character playerChar, Character enemyChar, Ruleset rules, AudioStream music, Texture2D background, int cpuIntellegence, float cpuShiftTime)
    {
        // Dialogue = dialogue;
        PlayerChar = playerChar;
        EnemyChar = enemyChar;
        Rules = rules;
        Music = music;
        Background = background;
        CPUIntellegence = cpuIntellegence;
        CPUShiftTime = cpuShiftTime;
    }
}
