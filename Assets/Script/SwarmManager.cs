using System.Collections.Generic;
using UnityEngine;

namespace SwarmTesting
{
    /// <summary>
    /// Coordinates the behavior of the swarm.
    /// </summary>
    //[CreateAssetMenu(fileName = "SwarmManager", menuName = "ScriptableObjects/SwarmManager", order = 1)]
    internal class SwarmManager : MonoBehaviour
    {
        [SerializeField] GameObject prefab;
        // The number of foes to be spawned
        [SerializeField] int spawnCount = 50;

        /// <summary>
        /// The distance SwarmObjects should attempt to keep from other objects.
        /// </summary>
        [SerializeField] public float AvoidanceDist = 5;

        /// <summary>
        /// Used to calculate how much an object should avoid things that are close to it, or go towards the center if far away.
        /// </summary>
        [SerializeField] public float WeightFactor = 2;

        /// <summary>
        /// The distance from the average at which recall weight is minimized.
        /// </summary>
        [SerializeField] public float MinRecallDistance = 25;

        /// <summary>
        /// The distance from the average at which recall weight is maximized.
        /// </summary>
        [SerializeField] public float MaxRecallDistance = 50;

        /// <summary>
        /// The speed SwarmObjects should attempt to maintain.
        /// </summary>
        [SerializeField] public float TargetSpeed = 1;

        /// <summary>
        /// The rate at which the object rotates in degrees per second.
        /// </summary>
        [SerializeField] public float RotationSpeed = 15;


        /// <summary>
        /// A global multiplier to determine how random certain things are.
        /// </summary>
        [SerializeField] public float RandomFactor = 0.05f;

        // Defines the size of the spawn area.
        [SerializeField] float spawnBounds = 50;

        /// <summary>
        /// The current size of the swarm.
        /// </summary>
        public float Count { get { return Swarm.Count; } }

        public static SwarmManager Instance;

        /// <summary>
        /// The list of all objects in the swarm.
        /// </summary>
        public List<SwarmObject> Swarm = new();

        public void Start()
        {
            if (Instance == null) { Instance = this; }

            SpawnSwarm();
        }

        /// <summary>
        /// Get the average of the position of the swarm.
        /// </summary>
        public Vector3 GetAvgPosition()
        {
            // get the sum of all positions.
            Vector3 sumVector = Vector3.zero;
            foreach (SwarmObject swarmObject in Swarm)
            {
                sumVector += swarmObject.transform.position;
            }

            float x, y, z;
            x = sumVector.x;
            y = sumVector.y;
            z = sumVector.z;

            // divide everything by count, throw into a new vector.
            Vector3 avg = new(x / Count, y / Count, z / Count);

            return avg;
        }

        /// <summary>
        /// Get average rotation vector of the swarm.
        /// </summary>
        /// <returns>The swarm's average rotation in Euler degrees.</returns>
        public Vector3 GetAvgRotation()
        {
            // get the sum of all rotations
            Vector3 sumVector = Vector3.zero;
            foreach (SwarmObject swarmObject in Swarm)
            {
                sumVector += swarmObject.transform.rotation.eulerAngles;
            }

            float x, y, z;
            x = sumVector.x;
            y = sumVector.y;
            z = sumVector.z;

            // divide everything by count, throw into new vector.
            Vector3 avg = new(x / Count, y / Count, z / Count);

            return avg;
        }

        /// <summary>
        /// This spawns the swarm.
        /// </summary>
        void SpawnSwarm()
        {
            for (int i = 0; i < spawnCount; i++)
            {
                // randomly generate position
                float x, y, z;
                x = Random.Range(-spawnBounds, spawnBounds);
                y = Random.Range(-spawnBounds, spawnBounds);
                z = Random.Range(-spawnBounds, spawnBounds);
                //x = Random.Range(-1, 1);
                //y = Random.Range(-1, 1);
                //z = Random.Range(-1, 1);
                Vector3 spawnPosition = new(x, y, z);

                // create a new object and set its position.
                SwarmObject obj = Instantiate(prefab).AddComponent<SwarmObject>();
                obj.name = i.ToString();
                obj.transform.position = spawnPosition;
                Swarm.Add(obj);
            }
        }
    }
}
