using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class BombPair
{
	public Bomb bomb;
	public int count = 0;
	[NonSerialized] public int realCount = -1;

	[NonSerialized] public int flagCount = 0;
}


[CreateAssetMenu(fileName = "BombCategory", menuName = "BombCategory")]
public class BombCategory : ScriptableObject
{
	public List<BombPair> bombOptions;
	public int Count {get => bombOptions.Count; private set => throw new NotSupportedException("Don't"); }
	public BombPair this[int index]
	{
		get => bombOptions[index];
		set => bombOptions[index] = value;
	}
}
