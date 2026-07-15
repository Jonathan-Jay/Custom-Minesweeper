using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MSVisuals : MonoBehaviour
{
	public HoverHandler hover;
	public MSMover mover;
	public Minesweeper game { get; private set; }
	public RectTransform boardParent;
	public Color[] hintColours = { Color.grey };
	public Color mysteryColour = Color.mediumPurple;

	public int mistakes {get; private set;} = 0;
	public int maxMistakes {get; private set;} = 999;
	public bool noOverheal {get; private set;} = false;
	public void SetOverheal(bool value) => noOverheal = !value;
	public bool noHintsOverBombs {get; private set;} = true;
	public bool useFlags {get; private set;} = false;
	public bool playing {get; private set;} = false;
	[NonSerialized] public bool tileFlagged = true;
	public int defaultFlag { get; private set;} = 0;
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
	List<int> activeFlagList;
	RawImage boardBG = null;

	void Awake()
	{
		game = GetComponent<Minesweeper>();
		game.visibilityChanged += Reveal;
		game.flagChanged += UpdateFlag;
		game.mistakeMade += CheckMistake;
		game.firstBreak += (Vector2Int pos) => firstClickText.text = (pos + Vector2Int.one).ToString();
		game.winEvent += () => {
			if (hover.enabled)
				winRect.gameObject.SetActive(true);
			hover.enabled = false;
			playing = false;
		};
	}

	void Start()
	{
		activeFlagList = new List<int>(game.bombOptions.Count);

		boardBG = boardParent.GetComponent<RawImage>();
		tileSize = tileTemplate.GetComponent<RectTransform>().sizeDelta;
		
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
			foreach (var tile in board.linearGrid)
				Destroy(tile.gameObject);
		}
		board = new NodeGrid<Tile>(game.size);

		boardParent.sizeDelta = tileSize * game.size;
		mover.currentHeight = Mathf.Max(boardParent.sizeDelta.x, boardParent.sizeDelta.y);
		mover.movementBounds = boardParent.sizeDelta * 0.5f - Vector2.one * 50f;
		mover.zoomBounds.x = Mathf.Min(mover.referenceHeight / mover.currentHeight, mover.minimumZoom);

		boardBG.uvRect = new Rect(Vector2.zero, game.size);
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
		foreach (Minesweeper.BombPair bombPair in game.bombOptions)
		{
			if (bombPair?.bomb == null || bombPair?.bomb.damage <= 0) continue;

			max += bombPair.count * bombPair.bomb.damage;
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
			hover.enabled = false;
			playing = false;
			game.Lose();
		}
		DoText();
	}

	public void SetupGameSameBreak()
	{
		if (game.waitingForClick || game.animating)	return;

		Vector2Int pos = game.firstClick;

		SetupGame(false);

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
		activeFlagList.Add(0);
		for (int i = 0; i < game.bombOptions.Count; ++i)
		{
			if (game.bombOptions[i]?.count > 0)
				activeFlagList.Add(i);
		}
		SetDefaultFlag(0);

		mistakes = 0;
		SetMaxMistakes(maxMistakes);
		hover.enabled = true;
		mover.ResetCamera();
		playing = true;
		DoText();
		//Random.state = heldState;
	}

	public void SetDefaultFlag(int val)
	{
		defaultFlag = activeFlagList[Mathf.Clamp(val, 0, activeFlagList.Count - 1)];
		defaultFlagImage.sprite = game.bombOptions[defaultFlag]?.bomb?.sprite;
		defaultFlagImage.gameObject.SetActive(defaultFlag != 0);
	}

	public void SetFlag(Vector2Int pos)
	{
		int value = defaultFlag;
		Tile tile = board.GetCell(pos);
		if (tileFlagged || value == 0 || value == tile.hint.flagValue)
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
			if (tile.hint.flagValue != 0)
				tile.FlagText(true);
		}
	}

	public void ToggleFlags(bool option)
	{
		if (useFlags == option)	return;
		
		useFlags = option;
		foreach (Tile tile in board.linearGrid)
		{
			if (!noHintsOverBombs && tile.hint.flagValue != 0)
				tile.FlagText();
			else if (tile.hasValue)
				Reveal(tile.pos);
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
		Bomb bomb = game.bombOptions[defaultFlag]?.bomb;
		if (bomb)
			text.text += " ><sprite=\"" + bomb.sprite.name + "\" index=0><";
		//*/

		foreach (int index in activeFlagList)
		{
			var bombPair = game.bombOptions[index];
			if (bombPair?.bomb == null || bombPair?.count == 0)	continue;
			
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
