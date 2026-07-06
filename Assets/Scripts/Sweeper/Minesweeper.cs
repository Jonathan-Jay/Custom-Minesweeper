using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//using Random = UnityEngine.Random;
using Random = System.Random;

public class Minesweeper : MonoBehaviour
{
	[Serializable]
	public class BombPair
	{
		public Bomb bomb;
		public int count;

		[NonSerialized] public int flagCount = 0;
	}

	public int seed;

	public int initialIslandRadius = 2;
	public Vector2Int size = Vector2Int.one * 10;
	public List<BombPair> bombOptions;
	NodeGrid<Hint> hintGrid;
	NodeGrid<bool> visibleGrid;

	public event Action<Vector2Int> visibilityChanged;
	public event Action<Vector2Int> flagChanged;
	public event Action<Bomb> mistakeMade;
	public event Action winEvent;
	Dictionary<Vector2Int, Bomb> bombList = new Dictionary<Vector2Int, Bomb>();

	public Vector2Int firstClick {get; private set;} = -Vector2Int.one;
	public bool waitingForClick {get => firstClick == -Vector2Int.one; private set => waitingForClick = value;}
	private float winTime = 0f;
	public float time {get => winTime >= 0f ? winTime : Time.time + winTime; private set => winTime = -value;}
	public int tileCount {get; private set;} = 0;
	public int mistakes {get; private set;} = 0;
	//public int bombCount {get; private set;} = 0;

	public bool animating {get; private set;} = false;
	public bool animated = false;
	public void SetAnimated(bool val) => animated = val;

	[NonSerialized] Random random = new Random();
	//Random.State heldState;

	void Awake()
	{
		hintGrid = new NodeGrid<Hint>(size, true);
		visibleGrid = new NodeGrid<bool>(size);

		bombOptions.Insert(0, null);

		visibilityChanged += (Vector2Int pos) => {
			if (!bombList.ContainsKey(pos))
				if (--tileCount <= 0)
				{
					winTime = time;
					winEvent?.Invoke();
				}
		};
	}

	public void Generate()
	{
		if (seed == 0)
			seed = random.Next();
			//seed = Random.Range(int.MinValue, int.MaxValue);
		random = new Random(seed);
		//Random.InitState(seed);

		bombList.Clear();

		if (!waitingForClick)
			Array.Clear(visibleGrid.linearGrid, 0, size.x * size.y);
		
		foreach (Hint hint in hintGrid.linearGrid)
			hint.Reset();

		int bombCount = 0;
		foreach (BombPair bombPair in bombOptions)
		{
			if (bombPair == null)	continue;

			bombPair.flagCount = 0;
			bombCount += bombPair.count;
			for (int c = 0; c < bombPair.count;)
			{
				Vector2Int newPos = GetPos(bombPair.bomb);
				if (AddBomb(bombPair.bomb, newPos))
					++c;
			}
		}
		tileCount = size.x * size.y - bombCount;

		//heldState = Random.state;
		firstClick = -Vector2Int.one;
		time = 0f;
		mistakes = 0;
	}

	public void Click(Vector2Int pos)
	{
		if (animating)
			return;
		
		// create the game field, while ensuring no bombs are around the click point
		if (waitingForClick)
		{
			//Random.state = heldState;
			Vector2Int minBound = pos - Vector2Int.one * (initialIslandRadius - 1);
			Vector2Int maxBound = pos + Vector2Int.one * (initialIslandRadius - 1);

			foreach (Vector2Int bombPos in bombList.Keys.ToArray())
			{
				if (InBounds(bombPos, minBound, maxBound))
				{
					Bomb bomb = bombList[bombPos];
					RemoveBomb(bombPos);

					Vector2Int newPos = GetPos(bomb);
					while (bombList.ContainsKey(newPos) || InBounds(newPos, minBound, maxBound))
						newPos = GetPos(bomb);

					AddBomb(bomb, newPos);
				}
			}

			firstClick = pos;
			time = Time.time;
		}

		if (visibleGrid.GetCell(pos) || hintGrid.GetCell(pos).flagValue > 0)
			return;

		//Deal with spread, just check for neighbouring unchecked
		visibleGrid.SetCell(pos, true);
		visibilityChanged?.Invoke(pos);

		if (bombList.ContainsKey(pos))
		{
			mistakes += 1;
			Hint hint = hintGrid.GetCell(pos);

			//guaranteed flags are marked with negative values
			hint.flagValue = -hint.bomb;
			BombPair bomb = bombOptions[hint.bomb];
			bomb.flagCount += 1;
			bomb.bomb.UpdateHints(pos, hintGrid, true, -1);
			flagChanged?.Invoke(pos);

			mistakeMade?.Invoke(bomb.bomb);
			return;
		}

		if (hintGrid.GetCell(pos).actualValue != 0)
			return;
		
		StartCoroutine(BreakChainCoroutine(pos));
	}

	WaitForFixedUpdate wffu = new WaitForFixedUpdate();

	IEnumerator BreakChainCoroutine(Vector2Int pos)
	{
		animating = true;

		Stack<Vector2Int> scanPoses = new Stack<Vector2Int>();
		scanPoses.Push(pos);
		
		while (scanPoses.Count != 0)
		{
			if (animated)
				yield return wffu;

			Vector2Int cur = scanPoses.Pop();
			//Try to add the 8 surround cells to the stack
			Vector2Int offset = Vector2Int.zero;
			for (offset.x = Mathf.Max(0, cur.x - 1); offset.x < Mathf.Min(size.x, cur.x + 2); ++offset.x)
			{
				for (offset.y = Mathf.Max(0, cur.y - 1); offset.y < Mathf.Min(size.y, cur.y + 2); ++offset.y)
				{
					if (offset == cur || visibleGrid.GetCell(offset) || hintGrid.GetCell(offset).flagValue > 0 || bombList.ContainsKey(offset))
						continue;

					visibleGrid.SetCell(offset, true);
					visibilityChanged?.Invoke(offset);
					
					if (hintGrid.GetCell(offset).actualValue == 0)
						scanPoses.Push(offset);
				}
			}
		}

		animating = false;
	}

	public void Flag(Vector2Int pos)
	{
		SetFlag(pos, (hintGrid.GetCell(pos).flagValue + 1) % bombOptions.Count);
	}

	public void ClearFlag(Vector2Int pos)
	{
		SetFlag(pos, 0);
	}

	public void SetFlag(Vector2Int pos, int index)
	{
		if ((visibleGrid.GetCell(pos) && !bombList.ContainsKey(pos)) || waitingForClick) return;

		Hint hint = hintGrid.GetCell(pos);
		if (hint.flagValue < 0) return;

		if (hint.flagValue != 0)
		{
			bombOptions[hint.flagValue].flagCount -= 1;
			bombOptions[hint.flagValue].bomb.UpdateHints(pos, hintGrid, true, 1);
		}
		hint.flagValue = index;
		if (hint.flagValue != 0)
		{
			bombOptions[hint.flagValue].flagCount += 1;
			bombOptions[hint.flagValue].bomb.UpdateHints(pos, hintGrid, true, -1);
		}

		flagChanged?.Invoke(pos);
	}

	public Hint GetHint(Vector2Int pos)
	{
		return hintGrid.GetCell(pos);
	}

	public int GetBombIndex(Bomb bomb)
	{
		for (int i = 1; i < bombOptions.Count; ++i)
			if (bombOptions[i].bomb == bomb)
				return i;
		return 0;
	}

	public Sprite GetFlag(Vector2Int pos)
	{
		return bombOptions[Mathf.Abs(hintGrid.GetCell(pos).flagValue)]?.bomb.sprite;
	}

	bool InBounds(Vector2Int pos, Vector2Int minBounds, Vector2Int maxBounds)
	{
		return pos.x >= minBounds.x && pos.x <= maxBounds.x && pos.y >= minBounds.y && pos.y <= maxBounds.y;
	}

	bool AddBomb(Bomb bomb, Vector2Int pos)
	{
		if (bombList.ContainsKey(pos))
			return false;

		GetHint(pos).bomb = GetBombIndex(bomb);

		bomb.UpdateHints(pos, hintGrid, false, 1);
		bombList.Add(pos, bomb);

		return true;
	}

	bool RemoveBomb(Vector2Int pos)
	{
		if (!bombList.ContainsKey(pos))
			return false;

		GetHint(pos).bomb = 0;

		bombList[pos].UpdateHints(pos, hintGrid, false, -1);
		bombList.Remove(pos);

		return true;
	}

	// To allow custom bomb spawn conditions
	Vector2Int GetPos(Bomb bomb)
	{
		//return new Vector2Int(Random.Range(0, size.x), Random.Range(0, size.y));
		return new Vector2Int(random.Next(0, size.x), random.Next(0, size.y));
	}
}
