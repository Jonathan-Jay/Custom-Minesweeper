using UnityEngine;

[CreateAssetMenu(fileName = "Bomb", menuName = "Bomb")]
public class Bomb : ScriptableObject, INode
{
	public Sprite sprite;
	public int damage = 1;
	[SerializeField] Vector2Int offset = new Vector2Int(1, 1);
	
	[SerializeField] Vector2Int hintSize = new Vector2Int(3, 3);
	[SerializeField] int[] hintGrid = {1, 1, 1, 1, 0, 1, 1, 1, 1};

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
				gameHintGrid.GetCell(pos)?.UpdateValue(hintGrid[hintPos.x + hintSize.x * hintPos.y] * multiplier, flag);
				++hintPos.y;
			}
			++hintPos.x;
		}
	}
}
