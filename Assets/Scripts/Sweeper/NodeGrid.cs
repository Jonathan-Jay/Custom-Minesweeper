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

	//returns true if replacing a non-null cell
	public bool SetCell(Vector2Int pos, T node)
	{
		if (pos.x >= 0 && pos.x < size.x && pos.y >= 0 && pos.y < size.y)
		{
			bool exists = linearGrid[pos.x + size.x * pos.y] != null;
			linearGrid[pos.x + size.x * pos.y] = node;
			return exists;
		}

		return false;
	}
}
