using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NeighbourBomb))]
public class NeighbourBombEditor : Editor
{
	SerializedProperty hintGrid;
	SerializedProperty hintSize;
	SerializedProperty offsetProp;
	SerializedProperty rightSpriteProp;
	SerializedProperty downSpriteProp;
	SerializedProperty leftSpriteProp;
	SerializedProperty upSpriteProp;
	SerializedProperty damageProp;
	Vector2Int size;
	Vector2Int offset;

	void OnEnable()
	{
		hintGrid = this.serializedObject.FindProperty("hintGrid");
		hintSize = this.serializedObject.FindProperty("hintSize");
		offsetProp = this.serializedObject.FindProperty("offset");
		rightSpriteProp = this.serializedObject.FindProperty("sprite").GetArrayElementAtIndex(0);
		downSpriteProp = this.serializedObject.FindProperty("sprite").GetArrayElementAtIndex(1);
		leftSpriteProp = this.serializedObject.FindProperty("sprite").GetArrayElementAtIndex(2);
		upSpriteProp = this.serializedObject.FindProperty("sprite").GetArrayElementAtIndex(3);
		damageProp = this.serializedObject.FindProperty("damage");
		size = hintSize.vector2IntValue;
	}

	public override void OnInspectorGUI()
	{
		this.serializedObject.Update();

		GUILayoutOption[] cellWidth = new GUILayoutOption[] { GUILayout.Width(30), GUILayout.Height(30) };
		GUILayoutOption[] spacingHeight = new GUILayoutOption[] { GUILayout.Height(0) };
		GUIStyle style = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter};
		GUIStyle cellStyle = new GUIStyle(GUI.skin.textField) {alignment = TextAnchor.MiddleCenter};

		EditorGUILayout.LabelField("Sprites", style);
		EditorGUILayout.PropertyField(rightSpriteProp, new GUIContent("Right"));
		EditorGUILayout.PropertyField(downSpriteProp, new GUIContent("Down"));
		EditorGUILayout.PropertyField(leftSpriteProp, new GUIContent("Left"));
		EditorGUILayout.PropertyField(upSpriteProp, new GUIContent("Up"));
		EditorGUILayout.LabelField("Bombs default to facing each other (left faces right)");
		EditorGUILayout.LabelField("These bombs spawn besides each other");
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("WARNING!", style);
		EditorGUILayout.LabelField("Put multibombs at the start of bomb lists", style);
		EditorGUILayout.LabelField("monitoring bomb count also helps", style);
		EditorGUILayout.LabelField("can result in infinite loops", style);
		EditorGUILayout.Space();

		EditorGUILayout.PropertyField(damageProp);
		EditorGUILayout.Space();

		EditorGUILayout.PropertyField(offsetProp);
		offset = offsetProp.vector2IntValue;
		EditorGUILayout.PropertyField(hintSize);

		if (size != hintSize.vector2IntValue)
		{
			if (hintSize.vector2IntValue.x <= 0 || hintSize.vector2IntValue.y <= 0 ||
				hintSize.vector2IntValue.x >= 10 || hintSize.vector2IntValue.y >= 10)
			{
				hintSize.vector2IntValue = size;
			}
			else
			{
				Vector2Int newSize = hintSize.vector2IntValue;
				//Debug.Log(size + ", " + newSize);
				int dir = newSize.y - size.y;
				for (int y = 0; y < Mathf.Abs(dir); ++y)
				{
					for (int x = 0; x < size.x; ++x)
					{
						if (dir > 0)
							hintGrid.InsertArrayElementAtIndex((size.y + y) * size.x + x);
						else
							hintGrid.DeleteArrayElementAtIndex((size.y - y + 1) * size.x - x);
					}
				}
				dir = newSize.x - size.x;
				for (int y = 0; y < newSize.y; ++y)
				{
					for (int x = 0; x < Mathf.Abs(dir); ++x)
					{
						if (dir > 0)
							hintGrid.InsertArrayElementAtIndex(x + y * newSize.x);
						else
							hintGrid.DeleteArrayElementAtIndex(x + y * newSize.x);
					}
				}
				size = newSize;
			}
		}

		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Hint pattern:", style);

		SerializedProperty val;
		for (int y = size.y - 1; y >= 0; --y)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			for (int x = 0; x < size.x; ++x)
			{
				if (x == offset.x && y == offset.y)
				{
					EditorGUILayout.LabelField("X", style, cellWidth);
				}
				else
				{
					val = hintGrid.GetArrayElementAtIndex(x + y * size.x);
					val.intValue = EditorGUILayout.IntField(val.intValue, cellStyle, cellWidth);
				}
			}
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField("", spacingHeight);
		}
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("You can use any positive or negative integer for hints");
		EditorGUILayout.LabelField("Hints at 0 will return a blank (or 0 with dynamic hint flags)");

		this.serializedObject.ApplyModifiedProperties();
	}
}
