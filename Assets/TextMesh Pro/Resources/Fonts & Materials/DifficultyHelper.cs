using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DifficultyHelper
{


	public static string GetDifficultyString(Difficulty difficulty)
	{
		switch (difficulty)
		{
			case Difficulty.Easy:
				return "easy";
			case Difficulty.Medium:
				return "normal";
			case Difficulty.Hard:
				return "hard";
			default:
				return "easy";
		}
	}
	public static string GetIconSizeByDifficulty(Difficulty difficulty)
	{
		switch (difficulty)
		{
			case Difficulty.Easy:
				return "large";
			case Difficulty.Medium:
				return "normal";
			case Difficulty.Hard:
				return "small";
			default:
				return "large";
		}
	}

	public static Vector2 GetPlayAreaSize(Difficulty difficulty)
	{
		switch (difficulty)
		{
			case Difficulty.Easy:
				return new Vector2(4, 3);
			case Difficulty.Medium:
				return new Vector2(5, 4);
			case Difficulty.Hard:
				return new Vector2(6, 5);
			default:
				return new Vector2(4, 3);
		}
	}
}
