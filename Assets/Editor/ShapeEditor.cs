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
        } else
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
        for (int i = 0; i < shapeCreator.points.Count; i++)
        {
            
            Vector3 nextPoint = shapeCreator.points[(i + 1) % shapeCreator.points.Count];

            if (i == selectionInfo.lineIndex)
            {
                Handles.color = Color.red;
                Handles.DrawLine(shapeCreator.points[i], nextPoint);
            } else
            {
                Handles.color = Color.black;
                Handles.DrawDottedLine(shapeCreator.points[i], nextPoint, 4f);
            }

            if (i == selectionInfo.pointIndex)
            {
                Handles.color = (selectionInfo.pointIsSelected) ? Color.black : Color.red;
            }
            else
            {
                Handles.color = Color.white;
            }
            Handles.DrawSolidDisc(shapeCreator.points[i], Vector3.up, shapeCreator.handleRadius);
            Handles.Label(shapeCreator.points[i], i + "");

            needsRepaint = true;
        }
    }

    void HandleInput(Event guiEvent){
        //calculate clicked position (screen position = 2D), to a Vector3 position
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
        float drawPlaneHeight = 0;
        float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;

        //Vector3 mousePosition = mouseRay.origin + mouseRay.direction * dstToDrawPlane;
        //Or
        Vector3 mousePosition = mouseRay.GetPoint(dstToDrawPlane);

        //if I left click
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
        {
            HandleLeftMouseDown(mousePosition);
        }

        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
        {
            HandleLeftMouseUp(mousePosition);
        }

        if (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
        {
            HandleLeftMouseDrag(mousePosition);
        }


        if (!selectionInfo.pointIsSelected)
        {
            UpdateMouseOverInfo(mousePosition);
        }
    }


    void HandleLeftMouseDown(Vector3 mousePosition)
    {
        if (!selectionInfo.mouseIsOverPoint)
        {
            int newPointIndex = (selectionInfo.mouseIsOverLine) ? selectionInfo.lineIndex + 1 : shapeCreator.points.Count;
            //records the last state of the object, so it can be undoed by Ctrl+Z in editor
            Undo.RecordObject(shapeCreator, "Add Point");
            shapeCreator.points.Insert(newPointIndex,mousePosition);
            selectionInfo.pointIndex = newPointIndex;
        }

        selectionInfo.pointIsSelected = true;
        selectionInfo.pointPositionAtStartOfDrag = mousePosition;
        needsRepaint = true;
    }

    void HandleLeftMouseUp(Vector3 mousePosition)
    {
        if (selectionInfo.pointIsSelected)
        {
            shapeCreator.points[selectionInfo.pointIndex] = selectionInfo.pointPositionAtStartOfDrag;
            Undo.RecordObject(shapeCreator, "Move Point");
            shapeCreator.points[selectionInfo.pointIndex] = mousePosition;

            selectionInfo.pointIsSelected = false;
            selectionInfo.pointIndex = -1;
            needsRepaint = true;
        }
    }

    void HandleLeftMouseDrag(Vector3 mousePosition)
    {
        if (selectionInfo.pointIsSelected)
        {
            shapeCreator.points[selectionInfo.pointIndex] = mousePosition;
            needsRepaint = true;
        }
    }

    void UpdateMouseOverInfo(Vector3 mousePosition)
    {
        int mouseOverPointIndex = -1;

        for (int i = 0; i < shapeCreator.points.Count; i++)
        {
            if(Vector3.Distance(mousePosition, shapeCreator.points[i]) < shapeCreator.handleRadius){
                mouseOverPointIndex = i;
                break;
            }
        }

        if(mouseOverPointIndex != selectionInfo.pointIndex)
        {
            selectionInfo.pointIndex = mouseOverPointIndex;
            selectionInfo.mouseIsOverPoint = mouseOverPointIndex != -1;

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
            for (int i = 0; i < shapeCreator.points.Count; i++)
            {
                Vector3 nextPointInShape = shapeCreator.points[(i + 1) % shapeCreator.points.Count];
                
                //Parameters should be Vector2, so they are converted by an extension
                float dstFromMouseToLine = HandleUtility.DistancePointToLineSegment(mousePosition.ToXZ(), shapeCreator.points[i].ToXZ(), nextPointInShape.ToXZ());

                if(dstFromMouseToLine < closestLineDistance)
                {
                    closestLineDistance = dstFromMouseToLine;
                    mouseOverLineIndex = i;
                }
            }

            if(selectionInfo.lineIndex != mouseOverLineIndex)
            {
                selectionInfo.lineIndex = mouseOverLineIndex;
                selectionInfo.mouseIsOverLine = mouseOverLineIndex != -1;
                needsRepaint = true;
            }
        }
    }


    //status of any selected point
    public class SelectionInfo
    {
        public int pointIndex = -1;
        public bool mouseIsOverPoint;
        public bool pointIsSelected;
        public Vector3 pointPositionAtStartOfDrag;

        public int lineIndex = -1;
        public bool mouseIsOverLine;
    }
}
