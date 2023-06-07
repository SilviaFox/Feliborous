using Godot;
using System;

public partial class Background : Resource
{
    [Export] public string Name {get; set;}
    [Export] public Texture2D Sprite {get; set;}

    public Background() : this("",null) {}

    public Background(string name, Texture2D sprite)
    {
        Name = name;
        Sprite = sprite;
    }
}
