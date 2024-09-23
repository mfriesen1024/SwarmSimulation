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

        List<GameObject> avoidanceList = new List<GameObject>();

        public void Start()
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = swarmManager.AvoidanceDist / 2;
            sphereCollider.isTrigger = true;
        }

        public void Update()
        {
            // Documenting what i want to happen...
            // Rotation should first, adjust to average.
            // If far from center, rotate towards center.
            // Then, rotate away from anything absurdly close.
            // Give it some randomness.
            // Lastly, move the object

            float delta = Time.deltaTime;

            // Set to avg rotation in eulers. If nothing else, we should go towards the average.
            Vector3 avgRot = swarmManager.AvgRotation;
            Vector3 targetRot = avgRot;

            // Based on the distance, determine how much we should rotate towards the average.
            Vector3 avgPos = swarmManager.AvgPosition;
            targetRot = TargetAvg(targetRot, avgPos);
            targetRot = AvoidAvoidances(targetRot);

            // Add a random value based on random factor.
            float x = Random.Range(-1, 1) * 180 * swarmManager.RandomFactor;
            float y = Random.Range(-1, 1) * 180 * swarmManager.RandomFactor;
            float z = Random.Range(-1, 1) * 180 * swarmManager.RandomFactor;
            targetRot += new Vector3(x, y, z);

            // Apply the rotation.
            Quaternion newRot = new(); newRot.eulerAngles = targetRot;
            gameObject.transform.rotation = newRot;

            // Move the object in the given direction by target speed divided by delta.
            float moveRandFactor = 1 + (Random.Range(-0.5f,0.5f) * swarmManager.RandomFactor);
            float finalSpeed = swarmManager.TargetSpeed * delta * moveRandFactor;
            transform.position += transform.forward * finalSpeed;
        }

        private Vector3 TargetAvg(Vector3 targetRot, Vector3 avgPos)
        {
            float dist = Vector3.Distance(transform.position, avgPos);

            // Scale and clamp distance to be used as a lerp factor
            float offset = swarmManager.MinRecallDistance;
            float max = swarmManager.MaxRecallDistance - offset;
            float recallLerpFactor = Mathf.Clamp01((dist - offset) / max);

            Vector3 recallTarget = Quaternion.FromToRotation(transform.position, avgPos).eulerAngles;
            Debug.Log($"Recall target is {recallTarget}");

            // Update target.
            targetRot = Vector3.Slerp(targetRot, recallTarget, recallLerpFactor);
            return targetRot;
        }

        private Vector3 AvoidAvoidances(Vector3 targetRot)
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

            if (closestObj == null) { return transform.rotation.eulerAngles; }

            // divide by the threshold to get a number from 0 to 1, then use as factor.
            float avoidanceLerpFactor = closestObjDist / swarmManager.AvoidanceDist;

            // Get a target and log it, then update.
            Vector3 avoidanceTarget = Quaternion.Inverse(
                Quaternion.FromToRotation(
                    transform.position, closestObj.transform.position)
                ).eulerAngles;
            Debug.Log($"Avoidance target is {avoidanceTarget}");

            // Update target.
            targetRot = Vector3.Slerp(targetRot, avoidanceTarget, avoidanceLerpFactor);
            return targetRot;
        }

        void RemoveOldAvoidances()
        {
            bool recurse = false;
            do
            {
                foreach (GameObject obj in avoidanceList)
                {
                    if (Vector3.Distance(transform.position, obj.transform.position) > swarmManager.AvoidanceDist)
                    {
                        avoidanceList.Remove(obj);
                        // Because c# can't continue iteration after modification, set the recurse flag so we can restart the loop.
                        recurse = true; break;
                    }
                }
            } while (recurse);
        }

        private void OnTriggerEnter(Collider other)
        {
            avoidanceList.Add(other.gameObject);
        }
    }
}
