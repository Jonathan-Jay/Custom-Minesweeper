using System.Collections;
using UnityEngine;

public class MSVisuals : MonoBehaviour
{
	[SerializeField] Tile tileTemplate;
	[SerializeField] RectTransform boardParent;
	[SerializeField] TMPro.TMP_Text text;
	[SerializeField] TMPro.TMP_Text timeText;
	[SerializeField] TMPro.TMP_Text winText;
	[SerializeField] TMPro.TMP_Text seedText;
	NodeGrid<Tile> board;
	Minesweeper game;

	bool useFlags = false;

	void Awake()
	{
		game = GetComponent<Minesweeper>();
		game.visibilityChanged += Reveal;
		game.flagChanged += UpdateFlag;
		game.winEvent += () => winText.gameObject.SetActive(true);

		board = new NodeGrid<Tile>(game.size);
	}

	void Start()
	{
		Vector2 tileSize = tileTemplate.GetComponent<RectTransform>().sizeDelta;
		
		Vector2 offset = new Vector2((game.size.x * -0.5f + 0.5f) * tileSize.x, (game.size.y * -0.5f + 0.5f) * tileSize.y);

		Vector2Int pos = Vector2Int.zero;
		for (pos.x = 0; pos.x < game.size.x; ++pos.x)
		{
			for (pos.y = 0; pos.y < game.size.y; ++pos.y)
			{
				Tile tile = Instantiate(tileTemplate, boardParent);
				tile.GetComponent<RectTransform>().anchoredPosition = offset + new Vector2(pos.x * tileSize.x, pos.y * tileSize.y);
				tile.pos = pos;
				tile.hint = game.GetHint(pos);

				board.SetCell(pos, tile);
				tile.callbackL += game.Click;
				tile.callbackR += game.Flag;
			}
		}

		SetupGame(false);
	}

	void Update()
	{
		timeText.text = "Time: " + game.time.ToString("0.00");
	}

	public void SetupGameSameBreak()
	{
		if (game.waitingForClick)	return;

		Vector2Int pos = game.firstClick;

		SetupGame(false);

		game.Click(pos);
	}

	public void SetupGame(bool newSeed)
	{
		//Random.State heldState = Random.state;

		winText.gameObject.SetActive(false);

		if (!game.waitingForClick)
		{
			foreach (Tile tile in board.linearGrid)
			{
				tile.Hide(useFlags);
			}
		}

		if (newSeed)
			game.seed = 0;
		game.Generate();

		seedText.text = "Seed: " + game.seed;

		DoText();

		//Random.state = heldState;
	}

	public void ToggleFlags()
	{
		useFlags = !useFlags;
		foreach (Tile tile in board.linearGrid)
		{
			if (tile.hasValue)
				Reveal(tile.pos);
		}
		DoText();
	}

	void Reveal(Vector2Int pos)
	{
		Tile tile = board.GetCell(pos);
		tile.Reveal(useFlags);
		DoText();
	}

	void UpdateFlag(Vector2Int pos)
	{
		board.GetCell(pos).SetFlag(game.GetFlag(pos));
		DoText();
	}

	void DoText()
	{
		text.text = (useFlags ? "Flag hints: On - Tiles remaining: " : "Flag hints: Off - Tiles remaining: ") + game.tileCount;
		foreach (var bombPair in game.bombOptions)
		{
			if (bombPair == null)	continue;
			
			text.text += " - " + bombPair.bomb.name + ": " + bombPair.flagCount + "/" + bombPair.count;
		}

		text.text += " - Mistakes: " + game.mistakes;
	}
}
