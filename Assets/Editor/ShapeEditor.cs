using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(ShapeCreator))]
public class ShapeEditor : Editor
{

    ShapeCreator shapeCreator;
    SelectionInfo selectionInfo;

    //to fix the problem of not showing newly created points
    bool needsRepaint;



    //`target` is this editor
    private void OnEnable()
    {
        shapeCreator = target as ShapeCreator;
        selectionInfo = new SelectionInfo();
        Undo.undoRedoPerformed += OnUndoOrRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoOrRedo;
    }


    //It is called when there is an input in the scene. Like: mouse movement, keyboard or ...
    private void OnSceneGUI()
    {
        Event guiEvent = Event.current;

        if(guiEvent.type == EventType.Repaint)
        {
            Draw();
        }
        //When I click in the scene, the selected object will be deselected
        //it forces the object to be selcted, even after clicking on empty spaces in the scene
        else if (guiEvent.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
        else
        {
            HandleInput(guiEvent);

            //Repaint the current view
            if (needsRepaint)
            {
                HandleUtility.Repaint();
                needsRepaint = false;
            }
        }
    }



    void Draw()
    {
        for (int shapeIndex = 0; shapeIndex < shapeCreator.shapes.Count; shapeIndex++)
        {
            Shape currentShape = shapeCreator.shapes[shapeIndex];
            bool shapeIsSelected = shapeIndex == selectionInfo.selectedShapeIndex;
            bool mouseOverShape = shapeIndex == selectionInfo.mouseOverShapeIndex;
            Color deselctedShapeColor = Color.gray;

            for (int i = 0; i < currentShape.points.Count; i++)
            {

                Vector3 nextPoint = currentShape.points[(i + 1) % currentShape.points.Count];

                if (i == selectionInfo.lineIndex && mouseOverShape)
                {
                    Handles.color = Color.red;
                    Handles.DrawLine(currentShape.points[i], nextPoint);
                }
                else
                {
                    Handles.color = (shapeIsSelected)?Color.black : deselctedShapeColor;
                    Handles.DrawDottedLine(currentShape.points[i], nextPoint, 4f);
                }

                if (i == selectionInfo.pointIndex && mouseOverShape)
                {
                    Handles.color = (selectionInfo.pointIsSelected) ? Color.black : Color.red;
                }
                else
                {
                    Handles.color = (shapeIsSelected) ? Color.white : deselctedShapeColor;
                }
                Handles.DrawSolidDisc(currentShape.points[i], Vector3.up, shapeCreator.handleRadius);
                Handles.Label(currentShape.points[i], i + "");
            }
        }

        needsRepaint = true;
    }


    void HandleInput(Event guiEvent){
        //calculate clicked position (screen position = 2D), to a Vector3 position
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
        float drawPlaneHeight = 0;
        float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;

        //Vector3 mousePosition = mouseRay.origin + mouseRay.direction * dstToDrawPlane;
        //Or
        Vector3 mousePosition = mouseRay.GetPoint(dstToDrawPlane);


        //if I Shift+left click
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Shift)
        {
            HandleShiftLeftMouseDown(mousePosition);
        }


        //if I left click
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
        {
            HandleLeftMouseDown(mousePosition);
        }

        //If I release the mouse click
        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
        {
            HandleLeftMouseUp(mousePosition);
        }

        //If I click and drag
        if (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
        {
            HandleLeftMouseDrag(mousePosition);
        }


        if (!selectionInfo.pointIsSelected)
        {
            UpdateMouseOverInfo(mousePosition);
        }
    }



    void CreateNewShape()
    {
        Undo.RecordObject(shapeCreator, "New Shape");
        shapeCreator.shapes.Add(new Shape());
        selectionInfo.selectedShapeIndex = shapeCreator.shapes.Count - 1;
    }



    void CreateNewPoint(Vector3 mousePosition)
    {
        bool mouseIsOverSelectedShape = selectionInfo.mouseOverShapeIndex == selectionInfo.selectedShapeIndex;
        int newPointIndex = (selectionInfo.mouseIsOverLine && mouseIsOverSelectedShape) ? selectionInfo.lineIndex + 1 : SelectedShape.points.Count;
        //records the last state of the object, so it can be undoed by Ctrl+Z in editor
        Undo.RecordObject(shapeCreator, "Add Point");
        SelectedShape.points.Insert(newPointIndex, mousePosition);
        selectionInfo.pointIndex = newPointIndex;
        selectionInfo.mouseOverShapeIndex = selectionInfo.selectedShapeIndex;

        needsRepaint = true;

        SelectPointUnderMouse();
    }


    void SelectPointUnderMouse()
    {
        selectionInfo.pointIsSelected = true;
        selectionInfo.mouseIsOverPoint = true;
        selectionInfo.mouseIsOverLine = false;
        selectionInfo.lineIndex = -1;

        selectionInfo.pointPositionAtStartOfDrag = SelectedShape.points[selectionInfo.pointIndex];
    }

    void HandleLeftMouseDown(Vector3 mousePosition)
    {
        if(shapeCreator.shapes.Count == 0)
        {
            CreateNewShape();
        }

        SelectShapeUnderMouse();

        if (selectionInfo.mouseIsOverPoint)
        {
            SelectPointUnderMouse();
        } else
        {
            CreateNewPoint(mousePosition);
        }
    }

    void HandleLeftMouseUp(Vector3 mousePosition)
    {
        if (selectionInfo.pointIsSelected)
        {
            SelectedShape.points[selectionInfo.pointIndex] = selectionInfo.pointPositionAtStartOfDrag;
            Undo.RecordObject(shapeCreator, "Move Point");
            SelectedShape.points[selectionInfo.pointIndex] = mousePosition;

            selectionInfo.pointIsSelected = false;
            selectionInfo.pointIndex = -1;
            needsRepaint = true;
        }
    }

    void HandleLeftMouseDrag(Vector3 mousePosition)
    {
        if (selectionInfo.pointIsSelected)
        {
            SelectedShape.points[selectionInfo.pointIndex] = mousePosition;
            needsRepaint = true;
        }
    }


    void HandleShiftLeftMouseDown(Vector3 mousePosition)
    {
        CreateNewShape();
        CreateNewPoint(mousePosition);
    }


    void SelectShapeUnderMouse()
    {
        if(selectionInfo.mouseOverShapeIndex != -1)
        {
            selectionInfo.selectedShapeIndex = selectionInfo.mouseOverShapeIndex;
            needsRepaint = true;
        }
    }

    void UpdateMouseOverInfo(Vector3 mousePosition)
    {
        int mouseOverPointIndex = -1;
        int mouseOverShapeIndex = -1;

        for (int shapeIndex = 0; shapeIndex < shapeCreator.shapes.Count; shapeIndex++)
        {
            Shape currentShape = shapeCreator.shapes[shapeIndex];

            for (int i = 0; i < currentShape.points.Count; i++)
            {
                if (Vector3.Distance(mousePosition, currentShape.points[i]) < shapeCreator.handleRadius)
                {
                    mouseOverPointIndex = i;
                    mouseOverShapeIndex = shapeIndex;
                    break;
                }
            }
        }

        if(mouseOverPointIndex != selectionInfo.pointIndex || mouseOverShapeIndex != selectionInfo.mouseOverShapeIndex)
        {
            selectionInfo.pointIndex = mouseOverPointIndex;
            selectionInfo.mouseIsOverPoint = mouseOverPointIndex != -1;
            selectionInfo.mouseOverShapeIndex = mouseOverShapeIndex;
            needsRepaint = true;
        }

        if (selectionInfo.mouseIsOverPoint)
        {
            selectionInfo.mouseIsOverLine = false;
            selectionInfo.lineIndex = -1;
        }
        else
        {

            int mouseOverLineIndex = -1;
            float closestLineDistance = shapeCreator.handleRadius;

            for (int shapeIndex = 0; shapeIndex < shapeCreator.shapes.Count; shapeIndex++)
            {
                Shape currentShape = shapeCreator.shapes[shapeIndex];

                for (int i = 0; i < currentShape.points.Count; i++)
                {
                    Vector3 nextPointInShape = currentShape.points[(i + 1) % currentShape.points.Count];

                    //Parameters should be Vector2, so they are converted by an extension
                    float dstFromMouseToLine = HandleUtility.DistancePointToLineSegment(mousePosition.ToXZ(), currentShape.points[i].ToXZ(), nextPointInShape.ToXZ());

                    if (dstFromMouseToLine < closestLineDistance)
                    {
                        closestLineDistance = dstFromMouseToLine;
                        mouseOverShapeIndex = shapeIndex;
                        mouseOverLineIndex = i;
                    }
                }
            }


            if(selectionInfo.lineIndex != mouseOverLineIndex || selectionInfo.mouseOverShapeIndex != mouseOverShapeIndex)
            {
                selectionInfo.lineIndex = mouseOverLineIndex;
                selectionInfo.mouseIsOverLine = mouseOverLineIndex != -1;
                selectionInfo.mouseOverShapeIndex = mouseOverShapeIndex;
                needsRepaint = true;
            }
        }
    }


    //If I undo alot, so the shape counter is decreased, I have to set the active shape to tha last one,
    // otherwise there will be Out Of Range exception
    void OnUndoOrRedo() {
        if(selectionInfo.selectedShapeIndex >= shapeCreator.shapes.Count)
        {
            selectionInfo.selectedShapeIndex = shapeCreator.shapes.Count - 1;
        }
    }



    Shape SelectedShape
    {
        get
        {
            return shapeCreator.shapes[selectionInfo.selectedShapeIndex];
        }
    }

    //status of any selected point
    public class SelectionInfo
    {
        public int selectedShapeIndex;
        public int mouseOverShapeIndex;

        public int pointIndex = -1;
        public bool mouseIsOverPoint;
        public bool pointIsSelected;
        public Vector3 pointPositionAtStartOfDrag;

        public int lineIndex = -1;
        public bool mouseIsOverLine;
    }
}
