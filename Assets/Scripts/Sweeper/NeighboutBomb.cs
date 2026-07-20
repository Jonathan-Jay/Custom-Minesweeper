using UnityEngine;

using Random = System.Random;

[CreateAssetMenu(fileName = "NeighbourBomb", menuName = "NeighbourBomb")]
public class NeighbourBomb : Bomb, INode
{
	NeighbourBomb() => sprite = new Sprite[4];

	public override int GetBombCount() => 2;

	// z pos stores the sprite index
	public override Vector3Int GetPos(Vector2Int size, Random random)
	{
		return Vector3Int.left;
	}

	// z pos stores the sprite index
	public override Vector3Int[] GetMultiPos(Vector2Int size, Random random)
	{
		return new Vector3Int[]{new Vector3Int(random.Next(0, size.x), random.Next(0, size.y), 0)};
	}
}
