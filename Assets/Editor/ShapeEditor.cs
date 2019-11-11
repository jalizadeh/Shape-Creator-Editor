using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(ShapeCreator))]
public class ShapeEditor : Editor
{

    ShapeCreator shapeCreator;

    //to fix the problem of not showing newly created points
    bool needsRepaint;

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


    //`target` is this editor
    private void OnEnable()
    {
        shapeCreator = target as ShapeCreator;
    }



    void Draw()
    {
        for (int i = 0; i < shapeCreator.points.Count; i++)
        {
            //records the last state of the object, so it can be undoed by Ctrl+Z in editor
            Undo.RecordObject(shapeCreator, "Add Point");

            Handles.color = Color.black;
            Vector3 nextPoint = shapeCreator.points[(i + 1) % shapeCreator.points.Count];
            Handles.DrawDottedLine(shapeCreator.points[i], nextPoint, 4f);
            Handles.color = Color.white;
            Handles.DrawSolidDisc(shapeCreator.points[i], Vector3.up, 0.5f);
            
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
            shapeCreator.points.Add(mousePosition);

        }
    }
}
