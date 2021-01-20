using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

namespace Game.Scene01
{
    public class PlayerAgent : Agent
    {
        [SerializeField] private Transform targetTransform = null;
        public float moveSpeed = 10f;

        /// <summary>
        /// “Initialize” functions similar to the “Start” method 
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.localPosition);
            sensor.AddObservation(targetTransform.localPosition);
            
           // base.CollectObservations(sensor);
        }


        public override void OnActionReceived(ActionBuffers actions)
        {
           
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];
           // Debug.Log(moveX + "," + moveZ);
            // base.OnActionReceived(vectorAction);

            transform.localPosition += new Vector3(moveX, 0, moveZ)* Time.deltaTime * moveSpeed;
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
    
       
             actionsOut.ContinuousActions.Array[0] = -Input.GetAxisRaw("Horizontal");
             actionsOut.ContinuousActions.Array[1] = Input.GetAxisRaw("Vertical");
        }

        public override void OnEpisodeBegin()
        {
            transform.localPosition = new Vector3(Random.Range((int)-1, 2),0, Random.Range((int)-1, 2));
            int target_x = 0, target_z = 0;
            while ((target_x<2&&target_x>-2)&&(target_z < 2 && target_z > -2))
            {
                target_x = Random.Range(-4, 5);
                target_z = Random.Range(-4, 6);
            }
            targetTransform.localPosition = new Vector3(target_x, 0, target_z);
        }

        private void OnTriggerEnter(Collider other)
        {
            SetReward(1f);
            EndEpisode();
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Wall"))
            {
                SetReward(-1f);
                EndEpisode();
            }
        }
    }
}