using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sebastian.Geometry;

[CustomEditor(typeof(ShapeCreator))]
public class ShapeEditor : Editor
{

    ShapeCreator shapeCreator;
    SelectionInfo selectionInfo;

    //to fix the problem of not showing newly created points
    bool shapeChangedSinceLastRepaint;



    //`target` is this editor
    private void OnEnable()
    {
        shapeChangedSinceLastRepaint = true;
        shapeCreator = target as ShapeCreator;
        selectionInfo = new SelectionInfo();
        Undo.undoRedoPerformed += OnUndoOrRedo;

        //Hide the axis gizmo
        Tools.hidden = true;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoOrRedo;
        Tools.hidden = false;
    }


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        string msg = "Click to add point\nShift-Click on empty space to create new shape\nShift-Click on a point to delete";
        EditorGUILayout.HelpBox(msg, MessageType.Info);

        int deleteShapeIndex = -1;

        shapeCreator.showShapeList = EditorGUILayout.Foldout(shapeCreator.showShapeList, "Show List of Shapes");
        if (shapeCreator.showShapeList) {
            for (int i = 0; i < shapeCreator.shapes.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("#" + (i + 1) + ": " + shapeCreator.shapes[i].points.Count);

                GUI.enabled = i != selectionInfo.selectedShapeIndex;
                if (GUILayout.Button("Select"))
                {
                    selectionInfo.selectedShapeIndex = i;
                }
                GUI.enabled = true;

                if (GUILayout.Button("Delete"))
                {
                    deleteShapeIndex = i;
                }
                GUILayout.EndHorizontal();
            }

            GUI.enabled = shapeCreator.shapes.Count > 0;
            if (GUILayout.Button("Delete All"))
            {
                deleteShapeIndex = -2;
            }
            GUI.enabled = true;
        }

        if (deleteShapeIndex > -1)
        {
            Undo.RecordObject(shapeCreator, "Delete Shape");
            shapeCreator.shapes.RemoveAt(deleteShapeIndex);
            selectionInfo.selectedShapeIndex = Mathf.Clamp(selectionInfo.selectedShapeIndex, 0, shapeCreator.shapes.Count - 1);
        }

        //delete all shapes
        if (deleteShapeIndex == -2)
        {

            for (int i = shapeCreator.shapes.Count - 1; i >= 0 ; i--)
            {
                Undo.RecordObject(shapeCreator, "Delete All Shapes");
                shapeCreator.shapes.RemoveAt(i);
                selectionInfo.selectedShapeIndex = 0;
                Debug.Log("deleted " + i);
            }
        }


        //Force scene view to repaint
        if (GUI.changed)
        {
            shapeChangedSinceLastRepaint = true;
            SceneView.RepaintAll();
        }
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
            if (shapeChangedSinceLastRepaint)
            {
                HandleUtility.Repaint();
                shapeChangedSinceLastRepaint = false;
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

        if (shapeChangedSinceLastRepaint)
        {
            shapeCreator.UpdateMeshDisplay();
        }

        shapeChangedSinceLastRepaint = true;
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

        shapeChangedSinceLastRepaint = true;

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
            shapeChangedSinceLastRepaint = true;
        }
    }

    void HandleLeftMouseDrag(Vector3 mousePosition)
    {
        if (selectionInfo.pointIsSelected)
        {
            SelectedShape.points[selectionInfo.pointIndex] = mousePosition;
            shapeChangedSinceLastRepaint = true;
        }
    }


    //If it is Shift+Left Click is pressed on an empty space, it creates a new shape
    // otherwise, it deletes the point
    void HandleShiftLeftMouseDown(Vector3 mousePosition)
    {
        if (selectionInfo.mouseIsOverPoint)
        {
            SelectPointUnderMouse();
            DeletePointUnderMouse();
        }
        else
        {
            CreateNewShape();
            CreateNewPoint(mousePosition);
        }
    }


    void SelectShapeUnderMouse()
    {
        if(selectionInfo.mouseOverShapeIndex != -1)
        {
            selectionInfo.selectedShapeIndex = selectionInfo.mouseOverShapeIndex;
            shapeChangedSinceLastRepaint = true;
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
            shapeChangedSinceLastRepaint = true;
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
                shapeChangedSinceLastRepaint = true;
            }
        }
    }



    void DeletePointUnderMouse()
    {
        Undo.RecordObject(shapeCreator, "Delete Point");
        SelectedShape.points.RemoveAt(selectionInfo.pointIndex);
        selectionInfo.mouseIsOverPoint = false;
        selectionInfo.pointIsSelected = false;
        shapeChangedSinceLastRepaint = true;
    }


    //If I undo alot, so the shape counter is decreased, I have to set the active shape to tha last one,
    // otherwise there will be Out Of Range exception
    void OnUndoOrRedo() {
        if(selectionInfo.selectedShapeIndex >= shapeCreator.shapes.Count || selectionInfo.selectedShapeIndex == -1)
        {
            selectionInfo.selectedShapeIndex = shapeCreator.shapes.Count - 1;
        }

        shapeChangedSinceLastRepaint = true;
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