using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MSVisuals : MonoBehaviour
{
	public HoverHandler hover;
	public Minesweeper game { get; private set; }
	public RectTransform boardParent;
	public RawImage boardCover;
	public Image clickPoint;
	public Color[] hintColours = { Color.grey };
	public Color mysteryColour = Color.mediumPurple;

	public int mistakes {get; private set;} = 0;
	public int maxMistakes {get; private set;} = 999;
	public bool noOverheal {get; private set;} = false;
	public void SetOverheal(bool value) => noOverheal = !value;
	public bool noHintsOverBombs {get; private set;} = true;
	public bool useFlags {get; private set;} = false;
	//public bool playing {get; private set;} = false;
	[NonSerialized] public bool tileFlagged = true;
	public Vector2Int defaultFlag { get; private set;} = Vector2Int.left;
	public event Action<int> seedUpdated;
	public event Action bombListUpdated;
	public Vector2 tileSize {get; private set;} = Vector2.zero;
	public Vector2 offset {get; private set;}

	[SerializeField] Tile tileTemplate;
	[SerializeField] Image defaultFlagImage;
	[SerializeField] TMPro.TMP_Text text;
	[SerializeField] TMPro.TMP_Text timeText;
	[SerializeField] TMPro.TMP_Text firstClickText;
	[SerializeField] TMPro.TMP_Text currentPosText;
	[SerializeField] RectTransform winRect;
	[SerializeField] RectTransform loseRect;

	NodeGrid<Tile> board = null;
	List<Vector2Int> activeFlagList;

	void Awake()
	{
		game = GetComponent<Minesweeper>();
		game.visibilityChanged += Reveal;
		game.flagChanged += UpdateFlag;
		game.mistakeMade += CheckMistake;
		game.firstBreak += (Vector2Int pos) => firstClickText.text = (pos + Vector2Int.one) + " r" + game.initialIslandRadius;
		game.winEvent += () => {
			if (!hover.noClicks)
				winRect.gameObject.SetActive(true);
			hover.SetNoClicks(true);
			//playing = false;
		};
	}

	void Start()
	{
		int count = 0;
		foreach (BombCategory bombCategory in game.bombCategories)
			count += bombCategory.Count;
		activeFlagList = new List<Vector2Int>(count);
		tileSize = tileTemplate.GetComponent<RectTransform>().sizeDelta;
		clickPoint.GetComponent<RectTransform>().sizeDelta = tileSize;

		boardCover.gameObject.SetActive(false);
		clickPoint.gameObject.SetActive(false);
		
		hover.callbackL = game.Click;
		hover.callbackR = SetFlag;
		hover.callbackM = game.ClearFlag;

		Resize();

		SetupGame(false);
	}

	void Update()
	{
		timeText.text = game.time.ToString("0.00");
	}

	void Resize()
	{
		if (game.size == board?.size)	return;
		
		if (board?.linearGrid != null)
		{
			foreach (Tile tile in board.linearGrid)
				Destroy(tile.gameObject);
		}
		board = new NodeGrid<Tile>(game.size);

		boardCover.uvRect = new Rect(Vector2.zero, game.size);
		boardParent.sizeDelta = tileSize * game.size;
		boardParent.GetComponent<RawImage>().uvRect = boardCover.uvRect;

		hover.mover.currentHeight = Mathf.Max(boardParent.sizeDelta.x, boardParent.sizeDelta.y);
		hover.mover.movementBounds = boardParent.sizeDelta * 0.5f - Vector2.one * 50f;
		hover.mover.zoomBounds.x = Mathf.Min(hover.mover.referenceHeight / hover.mover.currentHeight, hover.mover.minimumZoom);

		offset = new Vector2((game.size.x * -0.5f + 0.5f) * tileSize.x, (game.size.y * -0.5f + 0.5f) * tileSize.y);

		Vector2Int pos = Vector2Int.zero;
		for (pos.x = 0; pos.x < game.size.x; ++pos.x)
		{
			for (pos.y = 0; pos.y < game.size.y; ++pos.y)
			{
				Tile tile = Instantiate(tileTemplate, boardParent);
				tile.GetComponent<RectTransform>().anchoredPosition = offset + new Vector2(pos.x * tileSize.x, pos.y * tileSize.y);
				tile.pos = pos;
				tile.hint = game.GetHint(pos);
				tile.visuals = this;

				board.SetCell(pos, tile);
			}
		}

		boardCover.transform.SetAsLastSibling();
		clickPoint.transform.SetAsLastSibling();
	}

	public void ValidateChanges(Vector2Int newSize, bool forceUpdate)
	{
		int bombCount = game.bombCount;
		SetMaxMistakes(maxMistakes);

		Vector2 size = game.size;
		game.SetSize(newSize);
		
		if (forceUpdate || size != newSize)
		{
			Resize();
			SetupGame(false);
			return;
		}

		if (game.waitingForClick)
		{
			int tileCount = game.totalTileCount - game.bombCount;

			int radius = game.initialIslandRadius + game.initialIslandRadius + 1;
			radius *= radius;

			// reset if too many bombs or bomb count changed (previously was too many bombs)
			if ((tileCount < radius) != (bombCount != game.bombCount))
				SetupGame(false);
		}
	}

	public void SetMaxMistakes(int value)
	{
		int max = 0;
		foreach (BombCategory bombCategory in game.bombCategories)
		{
			foreach (BombPair bombPair in bombCategory.bombOptions)
			{
				if (bombPair.bomb.damage <= 0) continue;

				max += bombPair.count * bombPair.bomb.damage;
			}
		}

		maxMistakes = Mathf.Min(Mathf.Max(value, 1), Mathf.Max(max, 1));
		DoText();
	}

	void CheckMistake(Bomb bomb)
	{
		mistakes += bomb.damage;

		if (noOverheal)
			mistakes = Mathf.Max(mistakes, 0);

		if (mistakes >= maxMistakes)
		{
			loseRect.gameObject.SetActive(true);
			hover.SetNoClicks(true);
			//playing = false;
			game.Lose();
		}
		DoText();
	}

	public void SetupGameSameBreak()
	{
		if (game.waitingForClick)	return;

		SetupGameSpecificBreak(game.firstClick);
	}

	public void SetupGameSpecificBreak(Vector2Int pos)
	{
		if (game.animating)	return;

		SetupGame(false);

		//makes it require a player click instead
		if (clickPoint)
		{
			clickPoint.gameObject.SetActive(true);
			boardCover.gameObject.SetActive(true);
			clickPoint.GetComponent<RectTransform>().anchoredPosition = tileSize * (pos + Vector2.one * 0.5f);
			boardCover.uvRect = new Rect(-pos, game.size);
			
			StartCoroutine(RainbowClickPoint());
		}
		else
			game.Click(pos);
	}

	public void SetupGame(bool newSeed)
	{
		if (game.animating) return;

		//Random.State heldState = Random.state;

		winRect.gameObject.SetActive(false);
		loseRect.gameObject.SetActive(false);

		if (!game.waitingForClick)
		{
			firstClickText.text = "";
			foreach (Tile tile in board.linearGrid)
				tile.Hide();
		}

		if (newSeed)
			game.seed = 0;
		game.Generate();

		seedUpdated?.Invoke(game.seed);
		bombListUpdated?.Invoke();
		activeFlagList.Clear();
		activeFlagList.Add(Vector2Int.left);
		for (int c = 0; c < game.bombCategories.Count; ++c)
		{
			for (int i = 0; i < game.bombCategories[c].Count; ++i)
			{
				if (game.bombCategories[c][i].count > 0)
					activeFlagList.Add(new Vector2Int(c, i));
			}
		}
		SetDefaultFlag(0);

		mistakes = 0;
		SetMaxMistakes(maxMistakes);
		hover.SetNoClicks(false);
		hover.mover.ResetCamera();
		//playing = true;
		DoText();
		//Random.state = heldState;
	}

	public void ClickPoint()
	{
		clickPoint.gameObject.SetActive(false);
		boardCover.gameObject.SetActive(false);
		game.Click(new Vector2Int(-Mathf.RoundToInt(boardCover.uvRect.position.x), -Mathf.RoundToInt(boardCover.uvRect.position.y)));
	}

	IEnumerator RainbowClickPoint()
	{
		float t = 0;
		while (clickPoint.gameObject.activeInHierarchy)
		{
			clickPoint.color = Color.Lerp(Color.white, Color.limeGreen, Mathf.Abs(t - 1f));
			yield return null;
			t = Mathf.Repeat(t + Time.unscaledDeltaTime, 2f);
		}
	}

	public void SetDefaultFlag(int val)
	{
		defaultFlag = activeFlagList[Mathf.Clamp(val, 0, activeFlagList.Count - 1)];
		if (defaultFlag == Vector2Int.left)	defaultFlagImage.sprite = null;
		else	defaultFlagImage.sprite = game.bombCategories[defaultFlag.x][defaultFlag.y].bomb.sprite;
		defaultFlagImage.gameObject.SetActive(defaultFlag != Vector2Int.left);
	}

	public void SetFlag(Vector2Int pos)
	{
		Vector2Int value = defaultFlag;
		Tile tile = board.GetCell(pos);
		if (tileFlagged || value == Vector2Int.left || value == tile.hint.flagValue)
			value = activeFlagList[(activeFlagList.IndexOf(tile.hint.flagValue) + 1) % activeFlagList.Count];
		tileFlagged = true;
		game.SetFlag(pos, value);
	}

	public void ToggleHintOverBombs(bool option)
	{
		if (noHintsOverBombs == !option)	return;

		noHintsOverBombs = !option;
		foreach (Tile tile in board.linearGrid)
		{
			//for any flags in general
			if (tile.hint.flagValue != Vector2Int.left)
				tile.FlagText(true);
		}
	}

	public void ToggleFlags(bool option)
	{
		if (useFlags == option)	return;
		
		useFlags = option;
		foreach (Tile tile in board.linearGrid)
		{
			if (!noHintsOverBombs && tile.hint.flagValue != Vector2Int.left)
				tile.FlagText();
			else if (tile.hasValue)
				tile.UpdateText();
		}
		DoText();
	}

	void Reveal(Vector2Int pos)
	{
		Tile tile = board.GetCell(pos);
		tile.Reveal();
		DoText();
	}

	void UpdateFlag(Vector2Int pos)
	{
		board.GetCell(pos).SetFlag(game.GetFlag(pos));
		DoText();
	}

	void DoText()
	{
		text.text = "HP: " + (maxMistakes - mistakes) + "/" + maxMistakes + " <sprite=\"BGTile\" index=0>: " + game.tileCount;

		/* Render default flag in the hud
		Bomb bomb = defaultFlag game.bombCategory[defaultFlag.x][defaultFlag.y].bomb;
		if (bomb)
			text.text += " ><sprite=\"" + bomb.sprite.name + "\" index=0><";
		//*/

		foreach (Vector2Int index in activeFlagList)
		{
			if (index == Vector2Int.left)	continue;
			
			BombPair bombPair = game.bombCategories[index.x][index.y];
			if (bombPair.count == 0)	continue;
			
			text.text += " - <sprite=\"" + bombPair.bomb.sprite.name + "\" index=0>: <color=";
			switch (bombPair.count - bombPair.flagCount)
			{
				case > 0:
					text.text += "white";
					break;
				case 0:
					text.text += "green";
					break;
				case < 0:
					text.text += "red";
					break;
			}
			text.text += ">" + bombPair.flagCount + "/" + bombPair.count;
		}
	}

	public void SetCursorText(Vector2Int pos)
	{
		currentPosText.text = pos.x < 0 ? "" : (pos + Vector2Int.one).ToString();
	}
}
