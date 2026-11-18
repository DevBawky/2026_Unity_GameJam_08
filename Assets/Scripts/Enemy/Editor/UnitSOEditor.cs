using System.Collections.Generic;
using UnityEditor;

// UnitSO 인스펙터: 선택한 몬스터 종류에 필요한 칸만 보여준다.
[CustomEditor(typeof(UnitSO))]
public class UnitSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var type = (UnitType)serializedObject.FindProperty(nameof(UnitSO.type)).enumValueIndex;

        var hidden = new List<string>();
        if (type != UnitType.Archer) hidden.AddRange(new[] { nameof(UnitSO.arrowPrefab), nameof(UnitSO.arrowSpeed) });
        if (type != UnitType.Chaser) hidden.Add(nameof(UnitSO.ropeSkill));
        if (type == UnitType.Chaser) hidden.AddRange(new[] { nameof(UnitSO.attackPower), nameof(UnitSO.attackInterval) });

        DrawPropertiesExcluding(serializedObject, hidden.ToArray());
        serializedObject.ApplyModifiedProperties();
    }
}
