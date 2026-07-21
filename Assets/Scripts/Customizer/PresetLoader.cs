using UnityEngine;
using UnityEngine.UI;

public class PresetLoader : MonoBehaviour
{
	[SerializeField] MSCustomizer customizer;
	[SerializeField] TMPro.TMP_InputField input;
	[SerializeField] Toggle firstClickToggle;

	const char sizeChar = 't';
	const char mistakesChar = 'm';
	const char radiusChar = 'r';
	const char seedChar = 's';
	const char firstClickChar = 'f';
	const char bombCategoryChar = 'c';
	const char bombPairChar = 'b';
	bool exportSize = true;
	bool exportMistakes = true;
	bool exportRadius = true;
	bool exportSeed = false;
	bool exportFirstClick = false;

	public void ToggleSize(bool value) => exportSize = value;

	public void ToggleMistakes(bool value) => exportMistakes = value;

	public void ToggleRadius(bool value) => exportRadius = value;

	public void ToggleSeed(bool value)
	{
		exportSeed = value;
		exportFirstClick = exportFirstClick && value;
		firstClickToggle.isOn = exportFirstClick;
		firstClickToggle.interactable = value;
	}

	public void ToggleFirstClick(bool value) => exportFirstClick = value;

	public void LoadPreset()
	{
		if (input.text == "" || customizer.visuals.game.animating) return;

		string[] codes = input.text.Split(".", System.StringSplitOptions.RemoveEmptyEntries);
		int currentCategory = -1;

		Vector2Int pos = -Vector2Int.one;
		foreach (string code in codes)
		{
			switch (code[0])
			{
				case sizeChar:
					string[] xCoords = code.Substring(1).Split(",");
					if (xCoords.Length != 2) break;
					customizer.SetXSize(xCoords[0]);
					customizer.SetYSize(xCoords[1]);
					break;
				case mistakesChar:
					customizer.SetMaxHealth(code.Substring(1));
					break;
				case radiusChar:
					customizer.SetInitialRadius(code.Substring(1));
					break;
				case seedChar:
					customizer.SetSeed(code.Substring(1));
					break;
				case firstClickChar:
					string[] fCoords = code.Substring(1).Split(",");
					if (fCoords.Length != 2)	break;
					if (int.TryParse(fCoords[0], out int x))	pos.x = x;
					if (int.TryParse(fCoords[1], out int y))	pos.y = y;
					break;
				case bombCategoryChar:
					if (int.TryParse(code.Substring(1), out int res))
					{
						for (int c = currentCategory + 1; c <= res; ++c)
						{
							foreach(BombPair bombPair in customizer.visuals.game.bombCategories[c].bombOptions)
								bombPair.realCount = 0;
						}
						currentCategory = res;
					}
					break;
				case bombPairChar:
					if (currentCategory < 0)	break;
					string[] bCoords = code.Substring(1).Split(",");
					if (bCoords.Length != 3) break;
					if (int.TryParse(bCoords[0], out int idx) && int.TryParse(bCoords[1], out int dmg) && int.TryParse(bCoords[2], out int cnt))
					{
						customizer.visuals.game.bombCategories[currentCategory][idx].bomb.damage = dmg;
						customizer.visuals.game.bombCategories[currentCategory][idx].realCount = cnt;
					}
					break;
			}
		}

		//Force UI to update
		if (currentCategory != -1)
		{
			for (int c = currentCategory + 1; c < customizer.visuals.game.bombCategories.Count; ++c)
			{
				foreach (BombPair bombPair in customizer.visuals.game.bombCategories[c].bombOptions)
					bombPair.realCount = 0;
			}
			customizer.UpdateBombList();
			customizer.ForceUpdate();
		}

		// If forcing a start, close the editor and start a game. user can always sub out if they want
		if (pos.x >= 0 && pos.y >= 0)
		{
			customizer.Toggle();
			customizer.visuals.SetupGameSpecificBreak(pos);
		}
	}

	public void GeneratePreset()
	{
		string preset = "";

		if (exportSize)		preset += sizeChar + customizer.tempSize.x.ToString() + "," + customizer.tempSize.y + ".";
		if (exportMistakes)	preset += mistakesChar + customizer.visuals.maxMistakes.ToString() + ".";
		if (exportRadius)	preset += radiusChar + customizer.visuals.game.initialIslandRadius.ToString() + ".";
		if (exportSeed)		preset += seedChar + customizer.visuals.game.seed.ToString() + ".";
		if (exportFirstClick && !customizer.visuals.game.waitingForClick)
			preset += firstClickChar + customizer.visuals.game.firstClick.x.ToString() + "," + customizer.visuals.game.firstClick.y + ".";

		int c = 0;
		foreach (BombCategory bombCategory in customizer.visuals.game.bombCategories)
		{
			int i = 0;
			bool noCategoryTag = true;
			foreach (BombPair bombPair in bombCategory.bombOptions)
			{
				if (bombPair.realCount > 0)
				{
					if (noCategoryTag)
					{
						noCategoryTag = false;
						preset += bombCategoryChar + c.ToString() + ".";
					}
					preset += bombPairChar + i.ToString() + "," + bombPair.bomb.damage + "," + bombPair.realCount + ".";
				}
				++i;
			}
			++c;
		}
		if (preset.Length > 1)
			input.text = preset.Substring(0, preset.Length - 1);
		else
			input.text = "";
	}
}
