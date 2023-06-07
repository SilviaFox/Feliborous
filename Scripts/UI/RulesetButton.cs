using Godot;
using System;

public partial class RulesetButton : MenuButton
{
	[Export] Ruleset ruleset = new Ruleset();

    public override void _EnterTree()
    {
        Pressed += () => StartGameWithRuleset();
    }

	public void StartGameWithRuleset()
	{
		GetNode<Menu>("../../../").StartGame(ruleset);
	}
}
