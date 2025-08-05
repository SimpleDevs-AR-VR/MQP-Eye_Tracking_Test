using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeSession : MonoBehaviour
{
    public static EyeSession Instance;

    [Header("=== References ===")]
    public Transform head_ref;
    public Transform calibration_sphere_ref;
    public Transform gaze_target_ref;
    public Transform head_cursor_ref;
    public Transform left_cursor_ref;
    public Transform right_cursor_ref;
    public Follower calibration_follower_ref;

    [Header("=== Writer ===")]
    public CSVWriter writer;

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(string trial_name, string dir_name)
    {
        // Initialize writer
        writer.fileName = trial_name;
        writer.dirName = dir_name;
        writer.Initialize();

        // Set all references
        calibration_sphere_ref = EyeTrackingTest.Instance.cal_sphere_ref;
        gaze_target_ref = EyeTrackingTest.Instance.gaze_target_ref;
        head_cursor_ref = EyeTrackingTest.Instance.head_cursor_ref;
        left_cursor_ref = EyeTrackingTest.Instance.left_cursor_ref;
        right_cursor_ref = EyeTrackingTest.Instance.right_cursor_ref;

    }

    // When called, this forces the writer to do some things, then add those results into a row in the writer
    public void UpdateWriter(string event_description, Vector3 target)
    {
        // Check: if null, don't write
        if (calibration_sphere_ref == null || gaze_target_ref == null || head_cursor_ref == null || left_cursor_ref == null || right_cursor_ref == null)
        {
            return;
        }
        // Get local positions of gaze target, head, and eye relative to the calibration sphere
        // 1. The gaze target
        Vector3 gaze_dir = calibration_sphere_ref.InverseTransformDirection(gaze_target_ref.position);
        // 2. The head cursor
        Vector3 head_cursor_dir = calibration_sphere_ref.InverseTransformDirection(head_cursor_ref.position);
        // 3a. The left cursor
        Vector3 left_cursor_dir = calibration_sphere_ref.InverseTransformDirection(left_cursor_ref.position);
        // 3b. The right cursor
        Vector3 right_cursor_dir = calibration_sphere_ref.InverseTransformDirection(right_cursor_ref.position);
        // 4. The avg cursor
        Vector3 avg_cursor_dir = (left_cursor_dir + right_cursor_dir) / 2f;

        // Get the angle difference between the cursors and the gaze target
        float left_gaze_diff = Vector3.Angle(left_cursor_dir, gaze_dir);
        float right_gaze_diff = Vector3.Angle(right_cursor_dir, gaze_dir);
        float avg_gaze_diff = Vector3.Angle(avg_cursor_dir, gaze_dir);
        float head_gaze_diff = Vector3.Angle(head_cursor_dir, gaze_dir);

        // Get the angle difference between the eyes and head
        float left_head_diff = Vector3.Angle(left_cursor_dir, head_cursor_dir);
        float right_head_diff = Vector3.Angle(right_cursor_dir, head_cursor_dir);
        float avg_head_diff = Vector3.Angle(avg_cursor_dir, head_cursor_dir);

        // Save all results into payload, then write
        writer.AddPayload(event_description);
        writer.AddPayload(FrameCount.Instance.frame_count);
        writer.AddPayload(FrameCount.Instance.fps);
        writer.AddPayload(FrameCount.Instance.smoothed_fps);
        writer.AddPayload(target);

        writer.AddPayload(gaze_dir);
        writer.AddPayload(left_cursor_dir);
        writer.AddPayload(right_cursor_dir);
        writer.AddPayload(avg_cursor_dir);
        writer.AddPayload(head_cursor_dir);

        writer.AddPayload(left_gaze_diff);
        writer.AddPayload(right_gaze_diff);
        writer.AddPayload(avg_gaze_diff);
        writer.AddPayload(head_gaze_diff);
        writer.AddPayload(left_head_diff);
        writer.AddPayload(right_head_diff);
        writer.AddPayload(avg_head_diff);
        writer.WriteLine(true);
    }

    public void NextTrial()
    {
        // Let the system know we should move to the next trial in the next late update
        writer.Disable();
        if (EyeTrackingTest.Instance != null) EyeTrackingTest.Instance.NextTrial();
    }
}
