using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//using Random = UnityEngine.Random;
using Random = System.Random;
using URandom = UnityEngine.Random;

public class Minesweeper : MonoBehaviour
{
	public int seed;

	public int initialIslandRadius = 2;
	public Vector2Int size = Vector2Int.one * 10;
	public List<BombCategory> bombCategories;
	NodeGrid<Hint> hintGrid;
	NodeGrid<bool> visibleGrid;

	public event Action<Vector2Int> visibilityChanged;
	public event Action<Vector2Int> flagChanged;
	public event Action<Bomb> mistakeMade;
	public event Action<Vector2Int> firstBreak;
	public event Action winEvent;
	Dictionary<Vector2Int, Bomb> bombList = new Dictionary<Vector2Int, Bomb>();

	public float deathAnimDuration = 2f;

	public Vector2Int firstClick {get; private set;} = -Vector2Int.one;
	public bool waitingForClick {get => firstClick == -Vector2Int.one; private set {if (value) firstClick = -Vector2Int.one;}}
	private float winTime = 0f;
	public float time {get => winTime >= 0f ? winTime : Time.unscaledTime + winTime; set => winTime = -value;}
	public int tileCount {get; private set;} = 0;
	public int totalTileCount {get; private set;} = 0;
	public int mistakes {get; private set;} = 0;
	public int bombCount {get; private set;} = 0;

	public bool chordsValidateFlags { get; private set; } = true;
	public void SetChords(bool val) => chordsValidateFlags = val;
	public bool animated { get; private set; } = false;
	public void SetAnimated(bool val) => animated = val;
	public bool randomBreak { get; private set; } = false;
	public void SetRandomBreak(bool val) => randomBreak = val;
	public bool revealOnDeath { get; private set; } = true;
	public void SetRevealOnDeath(bool val) => revealOnDeath = val;
	public bool animating {get; private set;} = false;

	[NonSerialized] Random random = new Random();
	//Random.State heldState;

	void Awake()
	{
		foreach(BombCategory bombCategory in bombCategories)
		{
			foreach (BombPair bombPair in bombCategory.bombOptions)
			{
				bombPair.flagCount = 0;
				bombPair.realCount = bombPair.count;
			}
		}
		
		SetRadius(initialIslandRadius);
		Vector2Int newSize = size;
		size = Vector2Int.zero;
		SetSize(newSize);

		visibilityChanged += (Vector2Int pos) => {
			if (!bombList.ContainsKey(pos))
				if (--tileCount <= 0)
				{
					winTime = time;
					winEvent?.Invoke();
				}
		};
	}

	public void SetSize(Vector2Int newSize)
	{
		if (animating || newSize.x < 1 || newSize.y < 1)	return;

		// If changing, reset values
		if (size != newSize)
		{
			size = newSize;
			hintGrid = new NodeGrid<Hint>(size, true);
			visibleGrid = new NodeGrid<bool>(size);
			bombList.Clear();

			mistakes = 0;
			time = 0;
			//firstClick = -Vector2Int.one;
			waitingForClick = true;
		}
		totalTileCount = size.x * size.y;

		bombCount = 0;
		foreach (BombCategory bombCategory in bombCategories)
		{
			foreach (BombPair bombPair in bombCategory.bombOptions)
			{
				bombCount += bombPair.realCount;
			}
		}

		SetRadius(initialIslandRadius);

		//precount bombs. We do this often, but it's good for data verification
		int radius = initialIslandRadius + initialIslandRadius + 1;
		radius *= radius;
		bombCount = Mathf.Min(bombCount, totalTileCount - radius);
	}

	public void SetRadius(int value)
	{
		initialIslandRadius = Mathf.Clamp(value, -1, (Mathf.Min(size.x, size.y) - 1) / 2);
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

		int radius = initialIslandRadius + initialIslandRadius + 1;
		radius *= radius;
		totalTileCount = size.x * size.y - radius;

		bombCount = 0;
		foreach (BombCategory bombCategory in bombCategories)
		{
			foreach (BombPair bombPair in bombCategory.bombOptions)
			{
				bombPair.flagCount = 0;
				bombPair.count = bombPair.realCount;

				bombCount += bombPair.count;
				if (bombCount > totalTileCount)
				{
					bombCount -= bombPair.count;
					bombPair.count = totalTileCount - bombCount;
					bombCount += bombPair.count;
				}


				for (int c = 0; c < bombPair.count; ++c)
				{
					SpawnBomb(bombPair.bomb, Vector2Int.left, Vector2Int.left);
				}
			}
		}
		totalTileCount += radius - bombCount;
		tileCount = totalTileCount;

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
			Vector2Int minBound = pos - Vector2Int.one * initialIslandRadius;
			Vector2Int maxBound = pos + Vector2Int.one * initialIslandRadius;

			foreach (Vector2Int bombPos in bombList.Keys.ToArray())
			{
				if (InBounds(bombPos, minBound, maxBound))
				{
					Bomb bomb = bombList[bombPos];
					DespawnBomb(bombPos);

					SpawnBomb(bomb, minBound, maxBound);
				}
			}

			firstClick = pos;
			time = Time.unscaledTime;

			firstBreak?.Invoke(pos);
		}

		if (visibleGrid.GetCell(pos) || GetHint(pos).flagValue != Vector2Int.left)
			return;

		Hint hint = GetHint(pos);

		//Must be visible
		OpenCell(pos, hint);

		Queue<Tuple<Vector2Int, bool>> scanPoses = new Queue<Tuple<Vector2Int, bool>>();
		scanPoses.Enqueue(new Tuple<Vector2Int, bool>(pos, hint.actualValue != 0 || hint.flagValue != Vector2Int.left));

		StartCoroutine(BreakChainCoroutine(scanPoses));
	}

	public void BigClick(Vector2Int pos)
	{
		if (animating || waitingForClick || (chordsValidateFlags && GetHint(pos).displayValue != 0))
			return;

		Queue<Tuple<Vector2Int, bool>> scanPoses = new Queue<Tuple<Vector2Int, bool>>();
		Vector2Int offset = Vector2Int.zero;
		for (offset.x = Mathf.Max(0, pos.x - 1); offset.x < Mathf.Min(size.x, pos.x + 2); ++offset.x)
		{
			for (offset.y = Mathf.Max(0, pos.y - 1); offset.y < Mathf.Min(size.y, pos.y + 2); ++offset.y)
			{
				Hint hint = GetHint(offset);

				if (offset == pos || visibleGrid.GetCell(offset) || hint.flagValue != Vector2Int.left)
					continue;

				OpenCell(offset, hint);
				scanPoses.Enqueue(new Tuple<Vector2Int, bool>(offset, hint.actualValue != 0 || hint.flagValue != Vector2Int.left));
			}
		}

		StartCoroutine(BreakChainCoroutine(scanPoses));
	}

	void OpenCell(Vector2Int pos, Hint hint)
	{

		visibleGrid.SetCell(pos, true);
		hint.SetStatus(Hint.TileStatus.Open);

		if (bombList.ContainsKey(pos))
		{
			mistakes += 1;

			//guaranteed flags are marked with negative values
			hint.flagValue = (Vector2Int)hint.bomb;
			BombPair bomb = bombCategories[hint.bomb.x][hint.bomb.y];
			bomb.flagCount += 1;
			bomb.bomb.UpdateHints(pos, hintGrid, true, -1);
			flagChanged?.Invoke(pos);

			mistakeMade?.Invoke(bomb.bomb);
		}
		
		visibilityChanged?.Invoke(pos);
	}

	WaitForFixedUpdate wffu = new WaitForFixedUpdate();

	IEnumerator BreakChainCoroutine(Queue<Tuple<Vector2Int, bool>> scanPoses)
	{
		if (scanPoses.Count == 0)
			yield break;

		animating = true;
		int frameCount = 0;

		if (animated && !scanPoses.First().Item2)
			yield return wffu;
		
		while (scanPoses.Count > 0)
		{
			bool openedTile = false;

			//Try to add the 8 surround cells to the stack
			Tuple<Vector2Int, bool> cur = scanPoses.Dequeue();

			//* Random break pattern
			if (randomBreak && !cur.Item2)
			{
				scanPoses.Enqueue(cur);
				for (int i = URandom.Range(0, scanPoses.Count); i > 0; --i)
					scanPoses.Enqueue(scanPoses.Dequeue());
				cur = scanPoses.Dequeue();
			}//*/
			
			Vector2Int offset = Vector2Int.zero;
			for (offset.x = Mathf.Max(0, cur.Item1.x - 1); offset.x < Mathf.Min(size.x, cur.Item1.x + 2); ++offset.x)
			{
				for (offset.y = Mathf.Max(0, cur.Item1.y - 1); offset.y < Mathf.Min(size.y, cur.Item1.y + 2); ++offset.y)
				{
					if (offset == cur.Item1 || visibleGrid.GetCell(offset))
						continue;

					Hint hint = GetHint(offset);
					if (offset.x == cur.Item1.x || offset.y == cur.Item1.y)
						hint.SetStatus(Hint.TileStatus.Neighbouring, true);

					if (cur.Item2 || hint.flagValue != Vector2Int.left || bombList.ContainsKey(offset))
						continue;

					openedTile = true;

					visibleGrid.SetCell(offset, true);
					hint.SetStatus(Hint.TileStatus.Open);
					visibilityChanged?.Invoke(offset);

					scanPoses.Enqueue(new Tuple<Vector2Int, bool>(offset, hint.actualValue != 0));
				}
			}

			if (animated && !cur.Item2 && openedTile && scanPoses.Count > 0)
			{
				if (++frameCount < 100 || frameCount % (frameCount / 75) == 0)
					yield return wffu;
			}
		}

		animating = false;
	}


	//currently doesn't actually do anything besides
	public void Lose()
	{
		time = -time;
		if (!revealOnDeath) return;

		StartCoroutine(LoseAnimationCoroutine());
	}

	IEnumerator LoseAnimationCoroutine()
	{
		// Seems the break animations break this lmao
		animating = true;
		yield return wffu;
		animating = true;

		WaitForSecondsRealtime wait = null;
		if (animated)
			wait = new WaitForSecondsRealtime(deathAnimDuration / (bombCount - mistakes));

		foreach (Vector2Int pos in bombList.Keys)
		{
			Hint hint = GetHint(pos);

			// Skip already opened bombs
			if (hint.status == Hint.TileStatus.Open)	continue;
			
			if (animated)
				yield return wait;

			visibleGrid.SetCell(pos, true);
			hint.SetStatus(Hint.TileStatus.Open);
			visibilityChanged?.Invoke(pos);

			if (hint.flagValue != Vector2Int.left)
				bombCategories[hint.flagValue.x][hint.flagValue.y].bomb.UpdateHints(pos, hintGrid, true, 1);

			//guaranteed flags are marked with negative values
			hint.flagValue = (Vector2Int)hint.bomb;
			BombPair bomb = bombCategories[hint.bomb.x][hint.bomb.y];
			bomb.bomb.UpdateHints(pos, hintGrid, true, -1);
			flagChanged?.Invoke(pos);
		}

		animating = false;
	}

	public void ClearFlag(Vector2Int pos)
	{
		SetFlag(pos, Vector2Int.left);
	}

	public void SetFlag(Vector2Int pos, Vector2Int index)
	{
		if ((visibleGrid.GetCell(pos) && !bombList.ContainsKey(pos)) || waitingForClick) return;

		Hint hint = GetHint(pos);
		if (hint.flagValue != Vector2Int.left && hint.status == Hint.TileStatus.Open) return;

		if (hint.flagValue != Vector2Int.left)
		{
			bombCategories[hint.flagValue.x][hint.flagValue.y].flagCount -= 1;
			bombCategories[hint.flagValue.x][hint.flagValue.y].bomb.UpdateHints(pos, hintGrid, true, 1);
		}
		hint.flagValue = index;
		if (hint.flagValue != Vector2Int.left)
		{
			bombCategories[hint.flagValue.x][hint.flagValue.y].flagCount += 1;
			bombCategories[hint.flagValue.x][hint.flagValue.y].bomb.UpdateHints(pos, hintGrid, true, -1);
		}

		flagChanged?.Invoke(pos);
	}

	public Hint GetHint(Vector2Int pos)
	{
		return hintGrid.GetCell(pos);
	}

	public Vector3Int GetBombIndex(Bomb bomb, int z)
	{
		for (int c = 0; c < bombCategories.Count; ++c)
			for (int i = 0; i < bombCategories[c].Count; ++i)
				if (bombCategories[c][i].bomb == bomb)
					return new Vector3Int(c, i, z);
		return Vector3Int.left;
	}

	public Sprite GetFlag(Vector2Int pos)
	{
		Hint hint = GetHint(pos);
		if (hint.flagValue == Vector2Int.left)	return null;
		return bombCategories[hint.flagValue.x][hint.flagValue.y].bomb.sprite[hint.status == Hint.TileStatus.Open ? hint.bomb.z : 0];
	}

	bool InBounds(Vector2Int pos, Vector2Int minBounds, Vector2Int maxBounds)
	{
		return pos.x >= minBounds.x && pos.x <= maxBounds.x && pos.y >= minBounds.y && pos.y <= maxBounds.y;
	}

	// To allow custom bomb spawn conditions
	bool SpawnBomb(Bomb bomb, Vector2Int minBound, Vector2Int maxBound)
	{
		//if maxed out bomb list, shouldn't ever trigger though
		if (bombList.Count == visibleGrid.linearGrid.Length)
			return false;

		//customize to support multibombs
		Vector3Int newPos = bomb.GetPos(size, random);
		while (bombList.ContainsKey((Vector2Int)newPos) || InBounds((Vector2Int)newPos, minBound, maxBound))
			newPos = bomb.GetPos(size, random);
		
		return AddBomb(bomb, (Vector2Int)newPos, newPos.z);
	}

	bool AddBomb(Bomb bomb, Vector2Int pos, int z)
	{
		if (bombList.ContainsKey(pos))
			return false;

		GetHint(pos).bomb = GetBombIndex(bomb, z);

		bomb.UpdateHints(pos, hintGrid, false, 1);
		bombList.Add(pos, bomb);

		return true;
	}

	bool DespawnBomb(Vector2Int pos)
	{
		if (!bombList.ContainsKey(pos))
			return false;

		//Customize to support multibombs (remove linked bombs if rules broken)
		return RemoveBomb(pos);
	}

	bool RemoveBomb(Vector2Int pos)
	{
		if (!bombList.ContainsKey(pos))
			return false;

		GetHint(pos).bomb = Vector3Int.left;

		bombList[pos].UpdateHints(pos, hintGrid, false, -1);
		bombList.Remove(pos);

		return true;
	}
}
