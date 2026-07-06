using System;

[Serializable]
public class Hint : INode
{
	public enum TileStatus
	{
		Closed = 0,
		Neighbouring = 1,
		Open = 2,
		Mystery = 4,
	}
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
	public int bomb = 0;
	public TileStatus status {get; private set;} = TileStatus.Closed;
	public event Action valueChanged;

	public void UpdateValue(int value, bool flag)
	{
		if (value == 0)	return;
		if (!flag)
			actualValue += value;
		displayValue += value;
	}

	public void SetStatus(TileStatus val, bool notify = false)
	{
		status = val;
		if (notify)
			valueChanged?.Invoke();
	}

	public void Reset()
	{
		actualValue = 0;
		_displayValue = 0;
		flagValue = 0;
		bomb = 0;
		status = TileStatus.Closed;
	}
}
