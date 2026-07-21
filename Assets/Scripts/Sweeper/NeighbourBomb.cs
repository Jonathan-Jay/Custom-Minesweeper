using NUnit.Framework;
using UnityEngine;

using Random = System.Random;

[CreateAssetMenu(fileName = "NeighbourBomb", menuName = "NeighbourBomb")]
public class NeighbourBomb : Bomb, INode
{
	public NeighbourBomb() => sprite = new Sprite[4];

	public override int GetBombCount() => 2;

	// z pos stores the sprite index
	public override Vector3Int[] GetMultiPos(Vector2Int size, Random random)
	{
		Vector3Int first = new Vector3Int(random.Next(0, size.x), random.Next(0, size.y), 0);

		Vector3Int second;
		do
		{
			first.z = random.Next(0, 4);
			second = first + directions[first.z];
		}
		while (second.x < 0 || second.x >= size.x || second.y < 0 || second.y >= size.y);
		// Make them face each other
		// Order is 0 right, 1 down, 2 left, 3 up
		second.z = (first.z + 2) % 4;
		return new Vector3Int[]{ first, second };
	}

	static Vector3Int[] directions = new Vector3Int[]{ Vector3Int.right, Vector3Int.down, Vector3Int.left, Vector3Int.up };
}
