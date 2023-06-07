using Godot;
using Godot.Collections;
using System;

public partial class Character : Resource
{
    [Export] public Texture2D Sprite {get; set;}
    [Export] public Texture2D SelectSprite {get; set;}
    [Export] public Texture2D TalkSprite {get; set;}
    [Export] public string Name {get; set;}

    [Export] public Strategy OpeningStrat {get; set;}
    [Export] public Strategy PrimaryStrat {get; set;}
    [Export] public Strategy SecondaryStrat {get; set;}
    [Export] public int StratSwitchHeight {get; set;}

    public Character() : this(null, null, null, null, Strategy.Random, Strategy.MatchColor, Strategy.MatchEnds, 0) {}

    public Character(string name, Texture2D sprite, Texture2D selectSprite, Texture2D talkSprite, Strategy openingStrat, Strategy primaryStrat, Strategy secondaryStrat, int stratSwitchHeight)
    {
        Sprite = sprite;
        Name = name;
        SelectSprite = selectSprite;
        TalkSprite = talkSprite;
        OpeningStrat = openingStrat;
        PrimaryStrat = primaryStrat;
        SecondaryStrat = secondaryStrat;
        StratSwitchHeight = stratSwitchHeight;
    }

}
