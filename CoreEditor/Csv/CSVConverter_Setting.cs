using UnityEngine;

[CreateAssetMenu(fileName = "CSVConverter_Setting", menuName = "Scriptable Objects/CSVConverter_Setting")]
public class CSVConverter_Setting : ScriptableObject
{
    public string saveDirectory = "Assets";
    public string typeName = "";
    public int attributeCount = 2; // id와 name을 기본값으로
}
