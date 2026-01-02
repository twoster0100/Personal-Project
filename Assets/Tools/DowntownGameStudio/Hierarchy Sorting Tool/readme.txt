# Hierarchy Sorting Tool

This script is useful for organizing game objects in the Unity Editor hierarchy, especially when dealing with complex scenes with many objects.

## How to use?

In the toolbar click: Tools/Hierarchy Sorting Tool

Window will pop up with options to select the sorting target (either selected game objects or their children), the sorting mode (by name, position, or in reverse), and the order of the sorting criteria.

### Sorting Target

- **Selected Game Objects** - Sorts only the selected game objects
- **Children** - Sort child objects of selected game objects

### Sorting Type

- **By Name** - Sort alphabetically
- **Position** - Sort based on X, Y, or Z position
- **Reverse** - Reverse current order

### Sorting Order

When sorting by name or position you also have an option to choose whether to sort in **Ascending** or **Descending** order.

## Notes

- Game objects that don't have the same parent object can't be sorted at once
- Game objects that are at the top of hierarchy can't be sorted using this tool

## Materials in example project

Example scene: HierarchySortingToolExample.unity uses materials that use Standard shader.

To make these shaders work in other pipelines do the following:

1. Select materials from the Materials folder
2. Go to: Edit > Rendering > Materials
3. Click "Convert Selected Built-In Materials"

For more info visit:
- https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@7.1/manual/upgrading-your-shaders.html
- https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/manual/Upgrading-To-HDRP.html