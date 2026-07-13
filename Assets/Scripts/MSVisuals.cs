using System;
using UnityEngine;

public class MSVisuals : MonoBehaviour
{
	[SerializeField] Tile tileTemplate;
	public RectTransform boardParent;
	[SerializeField] TMPro.TMP_Text text;
	[SerializeField] TMPro.TMP_Text timeText;
	[SerializeField] RectTransform winRect;
	[SerializeField] RectTransform loseRect;
	NodeGrid<Tile> board;
	public Action<int> seedUpdated;
	public HoverHandler hover;
	public Minesweeper game { get; private set; }

	public Color[] hintColours = { Color.grey };
	public Color mysteryColour = Color.mediumPurple;
	public int mistakes {get; private set;} = 0;
	public int maxMistakes {get; private set;} = 1;
	public bool NoHintsOverBombs {get; private set;} = true;
	public bool useFlags {get; private set;} = false;
	public Vector2 tileSize {get; private set;} = Vector2.zero;

	[NonSerialized] public bool tileFlagged = true;
	public int defaultFlag { get; private set;}= 0;

	void Awake()
	{
		game = GetComponent<Minesweeper>();
		game.visibilityChanged += Reveal;
		game.flagChanged += UpdateFlag;
		game.winEvent += () => {
			winRect.gameObject.SetActive(true);
			hover.enabled = false;
		};
		game.mistakeMade += CheckMistake;

		board = new NodeGrid<Tile>(game.size);
	}

	void Start()
	{
		tileSize = tileTemplate.GetComponent<RectTransform>().sizeDelta;
		Vector2 offset = new Vector2((game.size.x * -0.5f + 0.5f) * tileSize.x, (game.size.y * -0.5f + 0.5f) * tileSize.y);
		
		boardParent.sizeDelta = tileSize * game.size;

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

		hover.callbackL = game.Click;
		hover.callbackR = SetFlag;
		hover.callbackM = game.ClearFlag;

		maxMistakes = 3;

		SetupGame(false);
	}

	void Update()
	{
		timeText.text = "Time: " + game.time.ToString("0.00");
	}

	public void SetMaxMistakes(int value)
	{
		int max = 0;
		foreach (Minesweeper.BombPair bombPair in game.bombOptions)
		{
			if (bombPair?.bomb == null) continue;
			max += bombPair.count * bombPair.bomb.damage;
		}

		maxMistakes = Mathf.Min(Mathf.Max(value, 1), Mathf.Max(max, 1));
		DoText();
	}

	void CheckMistake(Bomb bomb)
	{
		mistakes += bomb.damage;
		if (mistakes >= maxMistakes)
		{
			loseRect.gameObject.SetActive(true);
			hover.enabled = false;
			game.time = -game.time;
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
			foreach (Tile tile in board.linearGrid)
				tile.Hide();
		}

		if (newSeed)
			game.seed = 0;
		game.Generate();
		seedUpdated?.Invoke(game.seed);
		
		mistakes = 0;
		hover.enabled = true;
		DoText();

		//Random.state = heldState;
	}

	public void SetDefaultFlag(int val)
	{
		defaultFlag = Mathf.Clamp(val, 0, game.bombOptions.Count - 1);
		DoText();
	}

	public void SetFlag(Vector2Int pos)
	{
		int value = defaultFlag;
		Tile tile = board.GetCell(pos);
		if (tileFlagged || value == 0 || value == tile.hint.flagValue)
			value = (tile.hint.flagValue + 1) % game.bombOptions.Count;
		tileFlagged = true;
		game.SetFlag(pos, value);
	}

	public void ToggleHintOverBombs(bool option)
	{
		if (NoHintsOverBombs == !option)	return;

		NoHintsOverBombs = !option;
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
			if (!NoHintsOverBombs && tile.hint.flagValue != 0)
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
		text.text = "Tiles remaining: " + game.tileCount;
		
		Bomb bomb = game.bombOptions[defaultFlag]?.bomb;
		if (bomb)
			text.text += " - Flag with ><sprite=\"" + bomb.sprite.name + "\" index=0><";

		text.text += " - HP: " + (maxMistakes - mistakes) + "/" + maxMistakes;

		foreach (var bombPair in game.bombOptions)
		{
			if (bombPair?.bomb == null)	continue;
			
			text.text += " - <sprite=\"" + bombPair.bomb.sprite.name + "\" index=0>: " + bombPair.flagCount + "/" + bombPair.count;
		}
	}
}
