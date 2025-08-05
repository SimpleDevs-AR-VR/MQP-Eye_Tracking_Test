using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="CalibrationParams", menuName="Eye Calibration/Calibration Params", order=0)]
public class CalibrationParams : ScriptableObject
{
    public string session_name;
    public Calibrator.CalibrationStep[] steps;
}
