using UnityEngine;

using Random = System.Random;

[CreateAssetMenu(fileName = "Bomb", menuName = "Bomb")]
public class Bomb : ScriptableObject, INode
{
	public Bomb() => sprite = new Sprite[1];

	public Sprite[] sprite;
	public int damage = 1;
	[SerializeField] Vector2Int offset = new Vector2Int(1, 1);
	public Vector2Int GetOffset() => offset;
	[SerializeField] Vector2Int hintSize = new Vector2Int(3, 3);
	public Vector2Int GetHintSize() => hintSize;
	[SerializeField] int[] hintGrid = {1, 1, 1, 1, 0, 1, 1, 1, 1};
	
	// If larger than 1, will use MultiPos
	public virtual int GetBombCount() => 1;

	// z pos stores the sprite index
	public virtual Vector3Int GetPos(Vector2Int size, Random random) => new Vector3Int(random.Next(0, size.x), random.Next(0, size.y), 0);

	// z pos stores the sprite index
	public virtual Vector3Int[] GetMultiPos(Vector2Int size, Random random) => null;

	public void UpdateHints(Vector2Int bombPos, NodeGrid<Hint> gameHintGrid, bool flag, int multiplier)
	{
		bombPos -= offset;
		Vector2Int pos = Vector2Int.zero;
		Vector2Int hintPos = Vector2Int.left * Mathf.Min(0, bombPos.x);

		for (pos.x = Mathf.Max(0, bombPos.x); pos.x < Mathf.Min(gameHintGrid.size.x, bombPos.x + hintSize.x); ++pos.x)
		{
			hintPos.y = -Mathf.Min(0, bombPos.y);
			for (pos.y = Mathf.Max(0, bombPos.y); pos.y < Mathf.Min(gameHintGrid.size.y, bombPos.y + hintSize.y); ++pos.y)
			{
				//enforce 0 hint on top of a bomb
				if (hintPos != offset)
					gameHintGrid.GetCell(pos)?.UpdateValue(hintGrid[hintPos.x + hintSize.x * hintPos.y] * multiplier, flag);
				++hintPos.y;
			}
			++hintPos.x;
		}
	}

	public string GetHintVisual(Vector3Int pos)
	{
		if (pos.x < 0 || pos.y < 0 || pos.x >= hintSize.x || pos.y >= hintSize.y)	return "0";
		if ((Vector2Int)pos == offset)
			return "<sprite=\"" + sprite[pos.z].name + "\" index=0>";
		return hintGrid[pos.x + hintSize.x * pos.y].ToString();
	}
}
