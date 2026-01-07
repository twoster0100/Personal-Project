using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace DowntownGameStudio {

    public class HierarchySortingToolEditor : EditorWindow
    {
        SortTarget sortTarget;
        SortMode sortMode;
        SortOrder sortOrder;

        Vector2 scrollPosition;

        bool howToFileNotFound = false;

        Color primaryButtonColor = new Color(1.000f, 0.820f, 0.550f);
        Color secondaryButtonColor = new Color(1.000f, 0.946f, 0.865f);

        // Specifies that this editor tool can be opened up by clicking: Tools/Hierarchy Sorting Tool
        [MenuItem("Tools/Hierarchy Sorting Tool")]
        public static void ToggleWindow()
        {
            GetWindow<HierarchySortingToolEditor>("Hierarchy Sorting Tool");
        }

        // The OnGUI method is responsible for rendering the UI elements for the tool
        // The user can select the sorting target (i.e., selected game objects or their child objects),
        // the sorting type (i.e., by name or by position), and the sorting order (i.e., ascending or descending).
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(5f);

            sortTarget = (SortTarget)EditorGUILayout.EnumPopup("Sorting target", sortTarget);
            EditorGUILayout.HelpBox(new GUIContent("Selected Game Objects - Sort selected game objects\nChildren - Sort child objects of selected game objects"));

            EditorGUILayout.Space(10f);

            sortMode = (SortMode)EditorGUILayout.EnumPopup("Sorting type", sortMode);
            EditorGUILayout.HelpBox(
                new GUIContent(
                "By Name - Sort alphabetically\n" +
                "Position - Sort based on X, Y, or Z position\n" +
                "Reverse - Reverse current order"));

            EditorGUILayout.Space(20f);

            if (sortMode == SortMode.ByPositionX || sortMode == SortMode.ByPositionY || sortMode == SortMode.ByPositionZ)
            {
                sortOrder = (SortOrder)EditorGUILayout.EnumPopup("Sorting order", sortOrder);

                string axisBeingSorted = (sortMode == SortMode.ByPositionX ? "x" : (sortMode == SortMode.ByPositionY ? "y" : "z"));
                if (sortOrder == SortOrder.Descending)
                {
                    EditorGUILayout.LabelField($"Game objects with larger position.{axisBeingSorted} will be positioned in front", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"Game objects with smaller position.{axisBeingSorted} will be positioned in front", EditorStyles.miniLabel);
                }
            }
            else if (sortMode == SortMode.ByName)
            {
                sortOrder = (SortOrder)EditorGUILayout.EnumPopup("Character order", sortOrder);
                if (sortOrder == SortOrder.Ascending)
                {
                    EditorGUILayout.LabelField("ABCD...", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("ZYXW...", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(20f);

            

            GUI.backgroundColor = primaryButtonColor;

            if (GUILayout.Button("Sort objects", GUILayout.Height(40f)) && HasSelection())
            {
                if (sortTarget == SortTarget.Children)
                {
                    SortSelectedChildObjects();
                } else
                {
                    SortSelected();
                }
            }

            EditorGUILayout.Space(10f);

            GUI.backgroundColor = secondaryButtonColor;

            if (GUILayout.Button("How to use?"))
            {
                MonoScript ms = MonoScript.FromScriptableObject(this);
                string guideFilePath = AssetDatabase.GetAssetPath(ms).Substring(6);
                guideFilePath = guideFilePath.Substring(0, guideFilePath.Length - 36);
                guideFilePath += "Getting started - Hierarchy Sorting Tool.pdf";

                try
                {
                    System.Diagnostics.Process.Start(Application.dataPath + guideFilePath);
                    howToFileNotFound = false;
                }
                catch 
                {
                    howToFileNotFound = true;
                }; 
            }
            if (howToFileNotFound)
            {
                Color defaultTextColor = GUI.color;
                GUI.color = Color.red;
                EditorGUILayout.LabelField("Getting started - Hierarchy Sorting Tool.pdf could not be found. Check if you moved it from Hierarchy Sorting Tool folder", EditorStyles.wordWrappedMiniLabel);
                GUI.color = defaultTextColor;
            }
            EditorGUILayout.LabelField("*Opens up file: Getting started - Hierarchy Sorting Tool.pdf in default browser", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.EndScrollView();
        }

        // Used to sort the selected game objects ie. when Sort Target is set to 'SelectedGameObjects'
        void SortSelected()
        {
            Transform[] selectedTransforms = new Transform[Selection.gameObjects.Length];
            Transform parent = null;

            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                selectedTransforms[i] = Selection.gameObjects[i].transform;

                if (i == 0)
                {
                    parent = selectedTransforms[i].parent;
                } else if (selectedTransforms[i].parent != parent)
                {
                    Debug.LogWarning("Hierarchy Sorting Tool: Can't sort objects that don't have the same parent");
                    return;
                }
            }

            SortObjects(selectedTransforms);
        }

        // Used to sort the selected game objects' child objects ie. when Sort Target is set to 'Children'
        void SortSelectedChildObjects()
        {
            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                Transform[] childObjects = Selection.gameObjects[i].transform.Cast<Transform>().ToArray();
                SortObjects(childObjects);
            }
        }

        // Helper method that returns a Sorted Dictionary with game objects' Transforms as values and their sibling indexes as keys
        SortedDictionary<int, Transform> GetTransformIndexDictionary(Transform[] objects)
        {
            SortedDictionary<int, Transform> siblingIndexes = new SortedDictionary<int, Transform>();

            for (int i = 0; i < objects.Length; i++)
            {
                siblingIndexes.Add(objects[i].GetSiblingIndex(), objects[i]);
            }

            return siblingIndexes;
        } 

        // Takes an array of Transform objects as input and sorts them based on the user-selected sorting mode
        // by calling a method associated with that sorting mode
        void SortObjects(Transform[] objects)
        {
            if (objects.Length <= 0)
            {
                return;
            }

            // Saving a copy of the initial order of elements that will be sorted
            SortedDictionary<int, Transform> transformIndexPairs = GetTransformIndexDictionary(objects);

            // Syncing up objects array order with transformIndexPairs dictionary
            int index = 0;
            foreach (KeyValuePair<int, Transform> entry in transformIndexPairs)
            {
                objects[index] = entry.Value;
                index++;
            }

            // Calling sorting method based on selected Sort Mode
            switch (sortMode)
            {
            case SortMode.ByName:
                objects = SortObjectsByName(objects);
                break;
            case SortMode.ByPositionX:
                objects = SortObjectsByPositionX(objects);
                break;
            case SortMode.ByPositionY:
                objects = SortObjectsByPositionY(objects);
                break;
            case SortMode.ByPositionZ:
                objects = SortObjectsByPositionZ(objects);
                break;
            case SortMode.Reverse:
                objects = SortObjectsReverse(objects);
                break;
            }

            // Setting up final order of elements in hierarchy
            SetUpTransformOrder(objects, transformIndexPairs);
        }

        // Sorts given objects by object.transform.position.x
        Transform[] SortObjectsByPositionX(Transform[] objects)
        {
            if (sortOrder == SortOrder.Ascending)
            {
                return objects.OrderBy(t => t.position.x).ToArray();
            } 
            else
            {
                return objects.OrderByDescending(t => t.position.x).ToArray();
            }
        }

        // Sorts given objects by object.transform.position.y
        Transform[] SortObjectsByPositionY(Transform[] objects)
        {
            if (sortOrder == SortOrder.Ascending)
            {
                return objects.OrderBy(t => t.position.y).ToArray();
            }
            else
            {
                return objects.OrderByDescending(t => t.position.y).ToArray();
            }
        }

        // Sorts given objects by object.transform.position.z
        Transform[] SortObjectsByPositionZ(Transform[] objects)
        {
            if (sortOrder == SortOrder.Ascending)
            {
                return objects.OrderBy(t => t.position.z).ToArray();
            }
            else
            {
                return objects.OrderByDescending(t => t.position.z).ToArray();
            }
        }

        // Sorts given objects by name (either in Ascending order > ABCD... or in Descending order ZYXW...)
        Transform[] SortObjectsByName(Transform[] objects)
        {
            if (sortOrder == SortOrder.Ascending)
            {
                return objects.OrderBy(t => t.name).ToArray();
            } 
            else
            {
                return objects.OrderByDescending(t => t.name).ToArray();
            }
        }

        // Reverses current hierachy order of elements in the given array
        Transform[] SortObjectsReverse(Transform[] objects)
        {
            return objects.Reverse().ToArray();
        }

        // Used to update the sorting order of the Transform objects in the hierarchy
        void SetUpTransformOrder(Transform[] arrayAfter, SortedDictionary<int, Transform> originalList)
        {
            SortedDictionary<int, Transform> newList = new SortedDictionary<int, Transform>();

            Transform parent = arrayAfter[0].parent;
            if (parent == null)
            {
                Debug.LogWarning("Hierarchy Sorting Tool: Can't sort objects that are at top level of hierarchy. Make sure to only select objects that have a parent object.");
                return;
            }

            // Adding each game object from sorted list to new list with correct sibling index
            int index = 0;
            foreach (KeyValuePair<int, Transform> entry in originalList)
            {
                newList.Add(entry.Key, arrayAfter[index]);
                index++;
            }

            // Adding all other game objects at the same level in hierarchy to the list as well
            // This step is needed so that when changing selected objets' sibling index we won't mix up objects that were not selected and sorted
            foreach(Transform child in parent)
            {
                if (!newList.ContainsValue(child)) {
                    newList.Add(child.GetSiblingIndex(), child);
                }
            }

            // Setting sibling index values for each object
            foreach (var entry in newList)
            {
                Undo.RegisterCompleteObjectUndo(entry.Value.root, "ChangeSibling");
                entry.Value.SetSiblingIndex(entry.Key);
            }
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        // Checks if there are any selected objects in hierachy
        bool HasSelection()
        {
            return Selection.activeGameObject != null;
        }

        // Used to specify which parameter will be used to sort objects
        enum SortMode
        {
            ByName, ByPositionX, ByPositionY, ByPositionZ, Reverse
        }

        // Used to specify in which order objects should be sorted
        enum SortOrder
        {
            Ascending, Descending
        }

        // Used to specify which objects will be sorted
        enum SortTarget
        {
            SelectedGameObjects, Children
        }

    }
}