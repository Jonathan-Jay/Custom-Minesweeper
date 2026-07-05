using System;
using System.Linq;
using UnityEngine;

public interface INode {}

[Serializable]
public class NodeGrid<T>
	where T : new()
{
	public Vector2Int size { get; private set; }
	public T[] linearGrid {get; private set;}

	public NodeGrid(Vector2Int size, bool populate = false)
	{
		this.size = size;
		if (populate)
			linearGrid = new T[size.x * size.y].Select(i => new T()).ToArray();
		else
			linearGrid = new T[size.x * size.y];
	}

	public bool CheckCell(Vector2Int pos)
	{
		if (pos.x >= 0 && pos.x < size.x && pos.y >= 0 && pos.y < size.y)
		{
			return linearGrid[pos.x + size.x * pos.y] != null;
		}
		return false;
	}

	public T GetCell(Vector2Int pos)
	{
		if (pos.x >= 0 && pos.x < size.x && pos.y >= 0 && pos.y < size.y)
		{
			return linearGrid[pos.x + size.x * pos.y];
		}
		return default(T);
	}

	public void SetCell(Vector2Int pos, T node)
	//returns true if replacing a non-null cell
	//public bool SetCell(Vector2Int pos, T node)
	{
		if (pos.x >= 0 && pos.x < size.x && pos.y >= 0 && pos.y < size.y)
			linearGrid[pos.x + size.x * pos.y] = node;
		/*
		{
			int index = pos.x + size.x * pos.y;
			bool exists = linearGrid[index] != null;
			linearGrid[index] = node;
			return exists;
		}

		return false;
		//*/
	}
}

[Serializable]
public class NodeBorderGrid<T>
	where T : new()
{
	public Vector2Int size { get; private set; }
	//formatted as {y=0, x=0 column, x=size.x-1 column, y=size.y-1}
	public T[] linearGrid {get; private set;}

	public NodeBorderGrid(Vector2Int size, bool populate = false)
	{
		this.size = size;
		if (populate)
			linearGrid = new T[2 * size.x + 2 * size.y - 4].Select(i => new T()).ToArray();
		else
			linearGrid = new T[2 * size.x + 2 * size.y - 4];
	}

	public bool CheckCell(Vector2Int pos)
	{
		if (pos.x == 0 || pos.x == size.x -1 || pos.y == 0 || pos.y == size.y - 1)
			return linearGrid[GetIndex(pos)] != null;
		return false;
	}

	public T GetCell(Vector2Int pos)
	{
		if (pos.x == 0 || pos.x == size.x - 1 || pos.y == 0 || pos.y == size.y - 1)
			return linearGrid[GetIndex(pos)];
		return default(T);
	}

	public void SetCell(Vector2Int pos, T node)
	//returns true if replacing a non-null cell
	//public bool SetCell(Vector2Int pos, T node)
	{
		if (pos.x >= 0 && pos.x < size.x && pos.y >= 0 && pos.y < size.y)
			linearGrid[GetIndex(pos)] = node;
		/* //for returning boolean
		{
			int index = GetIndex(pos);
			bool exists = linearGrid[index] != null;
			linearGrid[index] = node;
			return exists;
		}
		return false;
		//*/
	}

	/* Example: 6x4, total of 2*6+2*4-4 = 16 cells, bottom left is (0,0)
	{8,12,13,13,14,11}
	{7,            10}
	{6,             9}
	{0, 1, 2, 3, 4, 5}
	*/
	// assumed valid
	int GetIndex(Vector2Int pos)
	{
		// Because of how C# works, this isn't actually faster, might even be slower
		/* Single line logic
		return pos.x + pos.y + Convert.ToInt32(pos.y > 0) * (
			Convert.ToInt32(pos.x < size.x - 1) * (size.x - 1) + Convert.ToInt32(pos.x > 0) * (size.y - 1));
		//*/

		/* Just logic
		int index = pos.x;
		//only need more processes if not in the first row
		if (pos.y > 0)
		{
			// guarantee left column
			if (pos.x == 0)
				index = pos.y + size.x - 1;
			// guarantee right column
			else if (pos.x == size.x - 1)
				index = pos.y + size.y - 1;
			// must be top row (pos.y = size.y - 1)
			else
				index = pos.x + pos.y + size.y + size.x - 2;
		}
		/*///Smarter logic
		int index = pos.x + pos.y;
		// Only add offsets if not first row
		if (pos.y > 0)
		{
			// if not right column, add bottom row
			if (pos.x < size.x - 1)
				index += size.x - 1;
			// if not left column, add left column, as bottom row will already be added
			if (pos.x > 0)
				index += size.y - 1;
		}
		//*/
		return index;
	}
}
