using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follower : MonoBehaviour
{

    public Transform target_ref;
    public bool follow_position = true;
    public bool follow_rotation = false;
    public Quaternion rotation_offset = Quaternion.identity;

    // Start is called before the first frame update
    void Start()
    {
        Follow();
    }

    // Update is called once per frame
    void Update()
    {
        Follow();
    }

    public void Follow()
    {
        if (follow_position) transform.position = target_ref.position;
        if (follow_rotation) transform.rotation = target_ref.rotation * rotation_offset;
    }

    public void CalculateRotationOffset()
    {
        rotation_offset = Quaternion.Inverse(target_ref.rotation) * transform.rotation;
    }
}
