using CardGame.CardBattle.Cards;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CardGame.CardBattle.Editor
{
    [CustomEditor(typeof(CardEntity))]
    public sealed class CardEntityEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawLabelLayoutWarnings((CardEntity)target);
        }

        private static void DrawLabelLayoutWarnings(CardEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            var so = new SerializedObject(entity);
            var nameLabel = so.FindProperty("nameLabel").objectReferenceValue as TextMeshPro;
            var hpLabel = so.FindProperty("hpLabel").objectReferenceValue as TextMeshPro;
            var hpBar = so.FindProperty("hpBar").objectReferenceValue as CardHpBarView;

            DrawLabelWarning(nameLabel, "NameLabel", CardFaceView.NameLabelSortingOrder);
            DrawLabelWarning(hpLabel, "HpLabel", CardFaceView.HpLabelSortingOrder);

            if (hpBar == null)
            {
                EditorGUILayout.HelpBox("HpBar 참조가 없습니다.", MessageType.Error);
            }
            else if (hpBar.transform.localPosition.z < CardFaceView.FrontFaceLocalZ)
            {
                EditorGUILayout.HelpBox(
                    $"HpBar local Z({hpBar.transform.localPosition.z:F3})가 카드 앞면보다 뒤입니다. "
                    + $"권장 Z: {CardHpBarView.DefaultLocalZ:F3}.",
                    MessageType.Warning);
            }
        }

        private static void DrawLabelWarning(TextMeshPro label, string labelName, int expectedSortingOrder)
        {
            if (label == null)
            {
                EditorGUILayout.HelpBox($"{labelName} 참조가 없습니다.", MessageType.Error);
                return;
            }

            var localZ = label.transform.localPosition.z;
            if (localZ < CardFaceView.LabelLocalZ)
            {
                EditorGUILayout.HelpBox(
                    $"{labelName} local Z({localZ:F3})가 카드 앞면({CardFaceView.FrontFaceLocalZ:F3})보다 뒤입니다. "
                    + $"프리팹에서 Z를 {CardFaceView.LabelLocalZ:F3} 이상으로 설정하세요.",
                    MessageType.Warning);
            }

            if (label.sortingOrder != expectedSortingOrder)
            {
                EditorGUILayout.HelpBox(
                    $"{labelName} sortingOrder가 {label.sortingOrder}입니다. 권장값: {expectedSortingOrder}.",
                    MessageType.Info);
            }
        }
    }
}
