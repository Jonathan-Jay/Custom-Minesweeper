using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Hint : INode
{
	private int _displayValue;
	public int displayValue {
		get => _displayValue;
		set
		{
			_displayValue = value;
			valueChanged?.Invoke();
		}
	}
	public int actualValue;
	//Use negative values for found mines
	public int flagValue = 0;
	public event Action valueChanged;

	public void UpdateValue(int value, bool flag)
	{
		if (value == 0)	return;
		if (!flag)
			actualValue += value;
		displayValue += value;
	}

	public void Reset()
	{
		actualValue = 0;
		_displayValue = 0;
		flagValue = 0;
	}
}
