using UnityEngine;

public class LerpToTransform : MonoBehaviour
{
    public Transform Target;
    public bool ReachImmediately = false;

    public float SlerpSpeed = 0.1f;
    public float LerpSpeed = 0.1f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Both position and rotation

        if (ReachImmediately)
        {
            // Reach immediately
            transform.position = Target.position;
            transform.rotation = Target.rotation;
        }

        {
            // Slerp
            var tPosition = Time.deltaTime * SlerpSpeed;
            var tRotation = Time.deltaTime * SlerpSpeed;
            transform.position = Vector3.Slerp(transform.position, Target.position, tPosition);
            transform.rotation = Quaternion.Lerp(transform.rotation, Target.rotation, tRotation);
        }

        {
            // Lerp
            var tPosition = Time.deltaTime * LerpSpeed;
            var tRotation = Time.deltaTime * LerpSpeed;
            transform.position = Vector3.Lerp(transform.position, Target.position, tPosition);
            transform.rotation = Quaternion.Lerp(transform.rotation, Target.rotation, tRotation);
        }
    }
}
