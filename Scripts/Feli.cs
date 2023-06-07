using Godot;
using System;

public partial class Feli : AnimatedSprite2D
{
	public enum FeliColor {
		Blue,
		Green,
		Yellow,
		Red,
		Pink,
		Garbage
	}
	
	public enum Type {
		Head,
		Tail,
		Body
	}

	public FeliColor color = FeliColor.Blue;
	public Type startType = Type.Head;
	public Type type = Type.Head;

	public Vector4I occupiedSpaces;
	// public bool markedForClear;



	public Feli(int newColor, Type newType)
	{
		SpriteFrames = GameManager.feliSprite;
		ZIndex = 3;

		color = (FeliColor)newColor;
		startType = newType;
		type = newType;
		Play(GetColorAsString() + "_" + GetTypeAsString());
	}

	public void UpdateState()
	{

		foreach (var state in GameManager.stateData)
		{
			if (state.spaces == occupiedSpaces)
			{
				RotationDegrees = state.rotation;
				var stateString = state.state;

				if (state.state == "end")
				{
					switch (startType)
					{
						case Type.Head:
							stateString = "_conhead";
						break;

						case Type.Body:
							stateString = "_conbody";
						break;

						case Type.Tail:  
							stateString = "_contail";
						break;
					}
				}
				else
				{
					type = Type.Body;
				}

				Play(GetColorAsString() + stateString);
				return;
			}

			// switch to non-connective versions when not connected to something
			string anim = "_head";


			switch (startType)
			{
				case Type.Head:
					anim = "_head";
				break;

				case Type.Body:
					anim = "_body";
				break;

				case Type.Tail:  
					anim = "_contail";
				break;
			}

			Play(GetColorAsString() + anim);
		}
		

	}

	public string GetColorAsString()
	{
		switch(color)
		{
			case FeliColor.Blue:
				return "blue";
			case FeliColor.Green:
				return "green";
			case FeliColor.Pink:
				return "pink";
			case FeliColor.Yellow:
				return "yellow";
			case FeliColor.Red:
				return "red";
			default:
				return "garb";
		}
	}

	public string GetTypeAsString()
	{
		switch(type)
		{
			case Type.Head:
			return "head";

			case Type.Body:
			return "body";

			case Type.Tail:
			return "contail";

			default:
			return "contail";
		}
	}
}

