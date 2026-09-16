#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MitsuboshiStudio
{
    public class AnimatorWriteDefaultsBulk_v1 : EditorWindow
    {
        private AnimatorController targetController;
        private int totalStates;
        private int wdOnStates;
        private int wdOffStates;

        [MenuItem("Tools/Mitsuboshi_Studio/Animator Write Defaults Bulk")]
        private static void OpenWindow()
        {
            var window = GetWindow<AnimatorWriteDefaultsBulk_v1>("Animator WD Bulk");
            window.minSize = new Vector2(430f, 230f);
            window.TryUseSelectedController();
        }

        private void OnEnable()
        {
            TryUseSelectedController();
            RefreshCounts();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is AnimatorController controller)
            {
                targetController = controller;
                RefreshCounts();
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Animator Write Defaults Bulk", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "指定したAnimator Controller内の全Stateを走査し、" +
                "Sub-State Machine内も含めてWrite Defaultsを一括変更します。",
                MessageType.Info);

            EditorGUILayout.Space(6);

            EditorGUI.BeginChangeCheck();
            targetController = (AnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller",
                targetController,
                typeof(AnimatorController),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                RefreshCounts();
            }

            if (GUILayout.Button("Use Selected Controller"))
            {
                TryUseSelectedController();
                RefreshCounts();
            }

            EditorGUILayout.Space(10);

            if (targetController == null)
            {
                EditorGUILayout.HelpBox(
                    "ProjectビューでAnimator Controllerを選択するか、上の欄に指定してください。",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total States : {totalStates}");
            EditorGUILayout.LabelField($"WD ON        : {wdOnStates}");
            EditorGUILayout.LabelField($"WD OFF       : {wdOffStates}");

            EditorGUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = wdOffStates > 0;

                if (GUILayout.Button("Set All Write Defaults ON", GUILayout.Height(32f)))
                {
                    SetAllWriteDefaults(true);
                }

                GUI.enabled = wdOnStates > 0;

                if (GUILayout.Button("Set All Write Defaults OFF", GUILayout.Height(32f)))
                {
                    SetAllWriteDefaults(false);
                }

                GUI.enabled = true;
            }

            EditorGUILayout.Space(6);

            if (GUILayout.Button("Refresh"))
            {
                RefreshCounts();
            }
        }

        private void TryUseSelectedController()
        {
            if (Selection.activeObject is AnimatorController controller)
            {
                targetController = controller;
            }
        }

        private void RefreshCounts()
        {
            totalStates = 0;
            wdOnStates = 0;
            wdOffStates = 0;

            if (targetController == null)
                return;

            foreach (var layer in targetController.layers)
            {
                if (layer.stateMachine != null)
                {
                    CountStatesRecursive(layer.stateMachine);
                }
            }
        }

        private void CountStatesRecursive(AnimatorStateMachine stateMachine)
        {
            foreach (var childState in stateMachine.states)
            {
                var state = childState.state;
                if (state == null)
                    continue;

                totalStates++;

                if (state.writeDefaultValues)
                    wdOnStates++;
                else
                    wdOffStates++;
            }

            foreach (var childMachine in stateMachine.stateMachines)
            {
                if (childMachine.stateMachine != null)
                {
                    CountStatesRecursive(childMachine.stateMachine);
                }
            }
        }

        private void SetAllWriteDefaults(bool value)
        {
            if (targetController == null)
                return;

            var states = new List<AnimatorState>();

            foreach (var layer in targetController.layers)
            {
                if (layer.stateMachine != null)
                {
                    CollectStatesRecursive(layer.stateMachine, states);
                }
            }

            if (states.Count == 0)
                return;

            string actionName = value
                ? "Set All Write Defaults ON"
                : "Set All Write Defaults OFF";

            Undo.RecordObjects(states.ToArray(), actionName);

            int changedCount = 0;

            foreach (var state in states)
            {
                if (state.writeDefaultValues == value)
                    continue;

                state.writeDefaultValues = value;
                EditorUtility.SetDirty(state);
                changedCount++;
            }

            AssetDatabase.SaveAssets();

            RefreshCounts();
            Repaint();

            Debug.Log(
                $"[Animator WD Bulk] {targetController.name}: " +
                $"{changedCount} State(s) changed to Write Defaults {(value ? "ON" : "OFF")}.");
        }

        private void CollectStatesRecursive(
            AnimatorStateMachine stateMachine,
            List<AnimatorState> states)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null)
                {
                    states.Add(childState.state);
                }
            }

            foreach (var childMachine in stateMachine.stateMachines)
            {
                if (childMachine.stateMachine != null)
                {
                    CollectStatesRecursive(childMachine.stateMachine, states);
                }
            }
        }
    }
}
#endif
