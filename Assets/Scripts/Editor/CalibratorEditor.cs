using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Calibrator))]
public class CalibratorEditor : Editor
{
    Calibrator calibrator;

    public void OnEnable() {
        calibrator = (Calibrator)target;
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        if (calibrator.session_parameters == null) return; 

        if (GUILayout.Button("Initialize")) {
            calibrator.Initialize();
        }
        if (GUILayout.Button("Previous Step")) {
            calibrator.PrevStep();
        }
        if (GUILayout.Button("Next Step")) {
            calibrator.NextStep();
        }
        if(GUILayout.Button("Play All Steps")) {
            calibrator.Play();
        }
    }

}
