using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

using Random = UnityEngine.Random;

public class Minesweeper : MonoBehaviour
{
	[Serializable] public class BombPair
	{
		public Bomb bomb;
		public int count;

		[NonSerialized]
		public int flagCount = 0;
	}

	public int seed;

	public int initialIslandRadius = 2;
	public Vector2Int size = Vector2Int.one * 10;
	public List<BombPair> bombOptions;
	NodeGrid<Hint> hintGrid;
	NodeGrid<bool> visibleGrid;
	public event Action<Vector2Int> visibilityChanged;
	public event Action<Vector2Int> flagChanged;
	Dictionary<Vector2Int, Bomb> bombList = new Dictionary<Vector2Int, Bomb>();

	public bool waitingForClick {get => firstClick == -Vector2Int.one; private set => waitingForClick = value;}
	public Vector2Int firstClick {get; private set;} = -Vector2Int.one;

	void Awake()
	{
		hintGrid = new NodeGrid<Hint>(size, true);
		visibleGrid = new NodeGrid<bool>(size);

		bombOptions.Insert(0, null);
	}

	public void Generate()
	{
		if (seed == 0)
			seed = Random.Range(int.MinValue, int.MaxValue);
		Random.InitState(seed);

		bombList.Clear();

		if (!waitingForClick)
		{
			Array.Clear(visibleGrid.linearGrid, 0, size.x * size.y);
			foreach (Hint hint in hintGrid.linearGrid)
			{
				hint.Reset();
			}
		}

		foreach (BombPair bombPair in bombOptions)
		{
			if (bombPair == null)	continue;

			bombPair.flagCount = 0;
			for (int c = 0; c < bombPair.count;)
			{
				Vector2Int newPos = GetPos(bombPair.bomb);
				if (AddBomb(bombPair.bomb, newPos))
					++c;
			}
		}

		firstClick = -Vector2Int.one;
	}

	public void Click(Vector2Int pos)
	{
		// create the game field, while ensuring no bombs are around the click point
		if (waitingForClick)
		{
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
			
			foreach (Vector2Int bombPos in bombList.Keys)
			{
				hintGrid.GetCell(bombPos).actualValue = -1;
				hintGrid.GetCell(bombPos).displayValue = -1;
			}

			firstClick = pos;
		}

		if (visibleGrid.GetCell(pos) || hintGrid.GetCell(pos).flagValue > 0)	return;

		//Deal with spread, just check for neighbouring unchecked
		visibleGrid.SetCell(pos, true);
		visibilityChanged?.Invoke(pos);
		if (hintGrid.GetCell(pos).actualValue != 0)
			return;

		Stack<Vector2Int> scanPoses = new Stack<Vector2Int>();
		scanPoses.Push(pos);
		
		while (scanPoses.Count != 0)
		{
			Vector2Int cur = scanPoses.Pop();
			//Try to add the 8 surround cells to the stack
			Vector2Int offset = Vector2Int.zero;
			for (offset.x = Mathf.Max(0, cur.x - 1); offset.x < Mathf.Min(size.x, cur.x + 2); ++offset.x)
			{
				for (offset.y = Mathf.Max(0, cur.y - 1); offset.y < Mathf.Min(size.y, cur.y + 2); ++offset.y)
				{
					if (offset == cur || visibleGrid.GetCell(offset)
							|| hintGrid.GetCell(offset).flagValue > 0 || bombList.ContainsKey(offset))	continue;

					visibleGrid.SetCell(offset, true);
					visibilityChanged?.Invoke(offset);
					
					if (hintGrid.GetCell(offset).actualValue == 0)
						scanPoses.Push(offset);
				}
			}
		}
	}

	public void Flag(Vector2Int pos)
	{
		if ((visibleGrid.GetCell(pos) && !bombList.ContainsKey(pos)) || waitingForClick)	return;
		
		Hint cell = hintGrid.GetCell(pos);
		if (cell.flagValue != 0)
		{
			bombOptions[cell.flagValue].flagCount -= 1;
			bombOptions[cell.flagValue].bomb.UpdateHints(pos, hintGrid, true, 1);
		}
		cell.flagValue = (cell.flagValue + 1) % bombOptions.Count;
		if (cell.flagValue != 0)
		{
			bombOptions[cell.flagValue].flagCount += 1;
			bombOptions[cell.flagValue].bomb.UpdateHints(pos, hintGrid, true, -1);
		}
		
		flagChanged?.Invoke(pos);
	}

	public Hint GetHint(Vector2Int pos)
	{
		return hintGrid.GetCell(pos);
	}

	public Sprite GetFlag(Vector2Int pos)
	{
		return bombOptions[hintGrid.GetCell(pos).flagValue]?.bomb.sprite;
	}

	bool InBounds(Vector2Int pos, Vector2Int minBounds, Vector2Int maxBounds)
	{
		return pos.x >= minBounds.x && pos.x <= maxBounds.x && pos.y >= minBounds.y && pos.y <= maxBounds.y;
	}

	bool AddBomb(Bomb bomb, Vector2Int pos)
	{
		if (bombList.ContainsKey(pos))
			return false;

		bomb.UpdateHints(pos, hintGrid, false, 1);

		bombList.Add(pos, bomb);

		return true;
	}

	bool RemoveBomb(Vector2Int pos)
	{
		if (!bombList.ContainsKey(pos))
			return false;

		bombList[pos].UpdateHints(pos, hintGrid, false, -1);

		bombList.Remove(pos);

		return true;
	}

	// To allow custom bomb spawn conditions
	Vector2Int GetPos(Bomb bomb)
	{
		return new Vector2Int(Random.Range(0, size.x), Random.Range(0, size.y));
	}
}
