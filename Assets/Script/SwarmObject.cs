
using System.Collections.Generic;
using UnityEngine;

namespace SwarmTesting
{
    /// <summary>
    /// An object to be controlled by the SwarmManager
    /// </summary>
    internal class SwarmObject : MonoBehaviour
    {
        // Apparently using unity, i dont need to assign the singleton thingy for the scriptable object. Idk?
        SwarmManager swarmManager = SwarmManager.Instance;

        // This will be a trigger to determine if something is too close.
        SphereCollider sphereCollider;
        Rigidbody rb;

        [SerializeField] List<GameObject> avoidanceList = new List<GameObject>();

        public void Start()
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = swarmManager.AvoidanceDist / 2;
            sphereCollider.isTrigger = true;
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        public void Update()
        {
            float delta = Time.deltaTime;

            // Set to avg rotation in eulers. If nothing else, we should go towards the average.
            Vector3 avgRot = swarmManager.AvgRotation;
            Vector3 targetRot = avgRot;

            // Based on the distance, determine how much we should rotate towards the average.
            Vector3 avgPos = swarmManager.AvgPosition;
            targetRot = TargetPos(targetRot, Vector3.zero);
            targetRot = TargetPos(targetRot, avgPos);
            targetRot = AvoidAvoidances(targetRot);

            // Add a random value based on random factor.
            float x = Random.Range(-1, 1) * 180 * swarmManager.RandomFactor;
            float y = Random.Range(-1, 1) * 180 * swarmManager.RandomFactor;
            float z = Random.Range(-1, 1) * 180 * swarmManager.RandomFactor;
            targetRot += new Vector3(x, y, z);

            // Ensure rotation isn't rotating too fast, then apply it.
            Quaternion newRot = new(); newRot.eulerAngles = targetRot;
            float cAngle = Quaternion.Angle(transform.rotation, newRot);
            float maxAngleThisFrame = SwarmManager.Instance.RotationSpeed * delta;
            float slerpFactor = Mathf.Clamp01(maxAngleThisFrame/cAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, newRot, slerpFactor);

            // Move the object in the given direction by target speed divided by delta.
            float moveRandFactor = 1 + (Random.Range(-0.5f, 0.5f) * swarmManager.RandomFactor);
            float finalSpeed = swarmManager.TargetSpeed * delta * moveRandFactor;
            transform.position += transform.forward * finalSpeed;
        }

        private Vector3 TargetPos(Vector3 currentTarget, Vector3 avgPos)
        {
            Quaternion currentRot = new Quaternion();
            currentRot.eulerAngles = currentTarget;
            Vector3 vectorToAvgPos = avgPos-transform.position;
            Quaternion rotToAvgPos = Quaternion.LookRotation(vectorToAvgPos.normalized);

            // Calculate distance crap.
            float dist = Vector3.Distance(transform.position, avgPos);
            float offset = swarmManager.MinRecallDistance;
            float max = swarmManager.MaxRecallDistance - offset;
            float recallLerpFactor = Mathf.Clamp01((dist - offset) / max);

            // Get our new rotation
            Quaternion newRot = Quaternion.Slerp(currentRot, rotToAvgPos, recallLerpFactor);
            Vector3 d_vToCenter = (transform.position - avgPos).normalized;

            //Debug.Log($"{name} TargetAvg: Currentpos is {transform.position}, center is {avgPos}, current vector is {transform.forward}" +
            //    $", vector to center is {d_vToCenter}");
            //Debug.DrawRay(transform.position, transform.forward, Color.blue);
            //Debug.DrawLine(transform.position, avgPos, Color.green);
            //Debug.Log($"{name} TargetAvg: Target is {rotToAvgPos.eulerAngles}, lerped is {newRot.eulerAngles} with factor of {recallLerpFactor} " +
            //    $"current rot is {currentRot.eulerAngles}");

            return newRot.eulerAngles;
        }

        private Vector3 AvoidAvoidances(Vector3 currentTarget)
        {
            // Deal with avoidances.
            RemoveOldAvoidances();
            GameObject closestObj = null;
            float closestObjDist = 0;
            foreach (GameObject obj in avoidanceList)
            {
                // Get distance between us and obj.
                float distance = Vector3.Distance(transform.position, obj.transform.position);
                // Track closest object.
                if (distance > closestObjDist) { closestObjDist = distance; closestObj = obj; }
            }

            if (closestObj == null) { return currentTarget; }

            // divide by the threshold to get a number from 0 to 1, then use as factor.
            float avoidanceLerpFactor = closestObjDist / swarmManager.AvoidanceDist;

            // Do quaternion math.
            Quaternion currentRot = new Quaternion();
            currentRot.eulerAngles = currentTarget;
            Vector3 vectorToAvoidance = closestObj.transform.position-transform.position;
            Quaternion rotToAvoidance = Quaternion.LookRotation(vectorToAvoidance.normalized);
            Quaternion rotAvoidAvoidance = Quaternion.Inverse(rotToAvoidance);

            // Slerp and return.
            Quaternion newRot = Quaternion.Slerp(currentRot, rotAvoidAvoidance, avoidanceLerpFactor);
            //Debug.Log($"AvoidanceCheck: Current is {currentTarget}, target is {rotAvoidAvoidance.eulerAngles}," +
            //    $"lerped is {newRot.eulerAngles}, factor was {avoidanceLerpFactor}");
            return newRot.eulerAngles;
        }

        void RemoveOldAvoidances()
        {
            bool recurse;
            int count = 0;
            do
            {
                recurse = false;
                foreach (GameObject obj in avoidanceList)
                {
                    if (Vector3.Distance(transform.position, obj.transform.position) > swarmManager.AvoidanceDist)
                    {
                        avoidanceList.Remove(obj);
                        // Because c# can't continue iteration after modification, set the recurse flag so we can restart the loop.
                        recurse = true; count++; break;
                    }
                }
            } while (recurse && count < avoidanceList.Count);
        }

        private void OnTriggerEnter(Collider other)
        {
            avoidanceList.Add(other.gameObject);
        }
    }
}
