﻿using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using System;

namespace Game.Maze
{

    public struct Wall_position
    {
        public int x; public int z;
        public Wall_position(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Wall_position))
            {
                return false;
            }

            var position = (Wall_position)obj;
            return x == position.x &&
                   z == position.z;
        }

        public override int GetHashCode()
        {
            var hashCode = 1553271884;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }
    }
    public class PlayerMazeAgent : Agent
    {
        const int k_NoAction = 0;  // do nothing!
        const int k_Up = 1;
        const int k_Down = 2;
        const int k_Left = 3;
        const int k_Right = 4;

        [SerializeField] private Transform targetTransform = null;
        [SerializeField] private GameObject wallPrefab = null;
        [SerializeField] private GameObject WinModel = null;
        [SerializeField] private GameObject LoseModel = null;
        [SerializeField] private GameObject TimeOutModel = null;
        [SerializeField] private float timeBetweenDecisionsAtInference = 0.15f;
        private float m_TimeSinceDecision = 0;


        private float reward = 0;
        public float moveSpeed = 10f;
        private bool wallgenerated = false;
        private Dictionary<Wall_position, GameObject> Wall_grid = new Dictionary<Wall_position, GameObject>();
        private Queue<GameObject> walls_not_active = new Queue<GameObject>();
        private int[,] map_checkpoints = new int[13, 9];
        private float minimal_score;

        private int GetMapCheckpoint(int x, int z)
        {
            try
            {
                return map_checkpoints[x + 6, z + 4];
            }
            catch (Exception e)
            {
                Debug.LogWarning("GetMapCheckpoint(" + (x + 6) + "," + (z + 4) + ")");
                Debug.LogWarning(e.Message);
                return -1;
            }


        }
        private int SetMapCheckpoint(int x, int z)

        {
            try
            {
                return map_checkpoints[x + 6, z + 4] = 1;
            }
            catch (Exception e)
            {
                Debug.LogWarning("SetMapCheckpoint(" + (x + 6) + "," + (z + 4) + ")");
                Debug.LogWarning(e.Message);
                return -1;
            }
        }
        private bool Connect_z_plus(int x, int z)
        {
            if (Wall_grid.TryGetValue(new Wall_position(x, z + 1), out GameObject temp))
            {
                temp.SetActive(false);
                walls_not_active.Enqueue(temp);
                return true;
            }
            return false;
        }
        private bool Connect_x_plus(int x, int z)
        {
            if (Wall_grid.TryGetValue(new Wall_position(x + 1, z), out GameObject temp))
            {
                temp.SetActive(false);
                walls_not_active.Enqueue(temp);
                return true;
            }
            return false;
        }
        private bool Connect_z_minus(int x, int z)
        {
            if (Wall_grid.TryGetValue(new Wall_position(x, z - 1), out GameObject temp))
            {
                temp.SetActive(false);
                walls_not_active.Enqueue(temp);
                return true;
            }
            return false;
        }
        private bool Connect_x_minus(int x, int z)
        {
            if (Wall_grid.TryGetValue(new Wall_position(x - 1, z), out GameObject temp))
            {
                temp.SetActive(false);
                walls_not_active.Enqueue(temp);
                return true;
            }
            return false;
        }
        private void Maze_Spiral_Algorytm(int x, int z)
        {
            int z_min, z_max, x_min, x_max;
            z_max = z_min = z;
            x_max = x_min = x;

            int chose = (UnityEngine.Random.Range(0, 4));

            if (chose == 0)
            {
                goto Up;
            }
            if (chose == 1)
            {
                goto Left;
            }
            if (chose == 2)
            {
                goto Right;
            }


        Down:
            while (z_min < z)
            {
                z -= 2;
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    Connect_z_plus(x, z);

                }
                else
                {
                    Connect_x_minus(x, z);
                }

            }
            z -= 2;
            z_min = z;

            if (!Connect_z_plus(x, z))
                return;


            Left:
            while (x_min < x)
            {
                x -= 2;

                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    Connect_x_plus(x, z);
                }
                else
                {
                    Connect_z_plus(x, z);
                }
            }
            x -= 2;
            x_min = x;

            if (!Connect_x_plus(x, z))
                return;


            Up:
            while (z_max > z)
            {
                z += 2;

                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    Connect_z_minus(x, z);
                }
                else
                {
                    Connect_x_plus(x, z);
                }
            }
            z += 2;
            z_max = z;

            if (!Connect_z_minus(x, z))
                return;

            Right:
            while (x_max > x)
            {
                x += 2;

                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    Connect_x_minus(x, z);
                }
                else
                {
                    Connect_z_minus(x, z);
                }
            }
            x += 2;
            x_max = x;

            if (!Connect_x_minus(x, z))
                return;

            goto Down;


        }
        private void Connect_side_wall(int x, int z)
        {
            if (x > 0)
            {
                if (z > 0)
                {
                    Connect_x_minus(x, z);
                    while (true)
                    {
                        z -= 2;
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            if (!Connect_x_minus(x, z))
                            { return; }
                        }
                        else
                            if (!Connect_z_plus(x, z))
                        { return; }


                    }


                }
                else
                {
                    Connect_x_minus(x, z);
                    while (true)
                    {
                        z += 2;
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            if (!Connect_x_minus(x, z))
                            { return; }
                        }
                        else
                            if (!Connect_z_minus(x, z))
                        { return; }


                    }
                }

            }
            else
            {
                if (z > 0)
                {
                    Connect_x_plus(x, z);
                    while (true)
                    {
                        z -= 2;
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            if (!Connect_x_plus(x, z))
                            { return; }
                        }
                        else
                            if (!Connect_z_plus(x, z))
                        { return; }


                    }
                }
                else
                {
                    Connect_x_plus(x, z);
                    while (true)
                    {
                        z += 2;
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            if (!Connect_x_plus(x, z))
                            { return; }
                        }
                        else
                            if (!Connect_z_minus(x, z))
                        { return; }


                    }
                }

            }

        }
        public override void Initialize()
        {

            if (wallPrefab)
                if (!wallgenerated)
                {
                    int y = +200;
                    for (int i = -2; i < 2; i++)
                    {
                        for (int j = -3; j < 4; j++)
                        {
                            GameObject wall = Instantiate(wallPrefab, Vector3.zero, Quaternion.identity, transform.parent) as GameObject;
                            wall.transform.localPosition = new Vector3(j << 1, y, (i << 1) + 1);
                            Wall_grid.Add(new Wall_position(j << 1, (i << 1) + 1), wall);
                        }
                    }
                    for (int i = -2; i < 3; i++)
                    {
                        for (int j = -3; j < 3; j++)
                        {
                            GameObject wall = Instantiate(wallPrefab, Vector3.zero, Quaternion.identity, transform.parent) as GameObject;
                            wall.transform.localPosition = new Vector3((j << 1) + 1, y, i << 1);
                            Wall_grid.Add(new Wall_position((j << 1) + 1, i << 1), wall);
                        }
                    }


                    wallgenerated = true;
                }





        }
        /*public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.localPosition.x);
            sensor.AddObservation(transform.localPosition.z);
        }*/
        public override void OnActionReceived(ActionBuffers actions)
        {

            //moveX = actions.ContinuousActions[0];
            // moveZ = actions.ContinuousActions[1];
            switch (actions.DiscreteActions[0])
            {
                case k_NoAction:
                    // do nothing
                    break;
                case k_Right:
                    transform.position = transform.position + new Vector3(1f, 0, 0f);
                    break;
                case k_Left:
                    transform.position = transform.position + new Vector3(-1f, 0, 0f);
                    break;
                case k_Up:
                    transform.position = transform.position + new Vector3(0f, 0, 1f);
                    break;
                case k_Down:
                    transform.position = transform.position + new Vector3(0f, 0, -1f);
                    break;
                default:
                    throw new ArgumentException("Invalid action value");
            }


            if (0 == GetMapCheckpoint((int)(transform.localPosition.x + 0.5f), (int)(transform.localPosition.z + 0.5f)))
            {
                SetMapCheckpoint((int)(transform.localPosition.x + 0.5f), (int)(transform.localPosition.z + 0.5f));

                reward += 0.1f;
                //  AddReward(0.1f);
                minimal_score = reward - 0.1f;
            }
            else
            {
                AddReward(-0.0001f);
                reward -= 0.0001f;
                if (minimal_score > reward)
                {
                    WinModel.SetActive(false);
                    LoseModel.SetActive(false);
                    TimeOutModel.SetActive(true);
                    EndEpisode();
                }
            }


        
        }
        public override void Heuristic(in ActionBuffers actionsOut)
        {

            var discreteActionsOut = actionsOut.DiscreteActions;
            discreteActionsOut[0] = k_NoAction;
            if (Input.GetKey(KeyCode.D))
            {
                discreteActionsOut[0] = k_Right;
            }
            if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[0] = k_Up;
            }
            if (Input.GetKey(KeyCode.A))
            {
                discreteActionsOut[0] = k_Left;
            }
            if (Input.GetKey(KeyCode.S))
            {
                discreteActionsOut[0] = k_Down;
            }
        
        // actionsOut.ContinuousActions.Array[0] = Input.GetAxisRaw("Horizontal");
        // actionsOut.ContinuousActions.Array[1] = Input.GetAxisRaw("Vertical");
    }
        public override void OnEpisodeBegin()
        {

            Debug.Log(reward);
            reward = 0f;
            minimal_score = reward - 0.1f;
            map_checkpoints = new int[13, 9];



            // int min_x = -3, max_x = 4, min_z = -2, max_z = 3; //hard
            int min_x = -1, max_x = 2, min_z = -1, max_z = 2; //easy
            int pos_x = UnityEngine.Random.Range(min_x, max_x) << 1,
                pos_z = UnityEngine.Random.Range(min_z, max_z) << 1;


            transform.localPosition = new Vector3(pos_x, 0, pos_z);
            SetMapCheckpoint((int)(transform.localPosition.x + 0.5f), (int)(transform.localPosition.z + 0.5f));
            int target_x = 0, target_z = 0;
            do
            {
                target_x = UnityEngine.Random.Range(min_x, max_z) << 1;
                target_z = UnityEngine.Random.Range(min_z, max_z) << 1;
            } while (target_x == pos_x && target_z == pos_z);

            targetTransform.localPosition = new Vector3(target_x, 0, target_z);




            while (walls_not_active.Count > 0)
            {
                walls_not_active.Dequeue().SetActive(true);
            }
            Maze_Spiral_Algorytm(0, 0);
            Connect_side_wall(-6, 4);
            Connect_side_wall(6, -4);
            /*
            /// Super Easy
            ///  1 Extrime easy 2 Super Easy
            int distance = 1;
            List<Vector3> positions_for_target = new List<Vector3>();
            if ((Wall_grid.TryGetValue(new Wall_position(pos_x, pos_z + 1), out GameObject temp)))
            {
                if (!temp.activeSelf)
                    positions_for_target.Add(new Vector3(pos_x, 0, pos_z + distance));
            }
            if ((Wall_grid.TryGetValue(new Wall_position(pos_x, pos_z - 1), out temp)))
            {
                if (!temp.activeSelf)
                    positions_for_target.Add(new Vector3(pos_x, 0, pos_z - distance));
            }
            if ((Wall_grid.TryGetValue(new Wall_position(pos_x + 1, pos_z), out temp)))
            {
                if (!temp.activeSelf)
                    positions_for_target.Add(new Vector3(pos_x + distance, 0, pos_z));
            }
            if ((Wall_grid.TryGetValue(new Wall_position(pos_x - 1, pos_z), out temp)))
            {
                if (!temp.activeSelf)
                    positions_for_target.Add(new Vector3(pos_x - distance, 0, pos_z));
            }
            targetTransform.localPosition = positions_for_target[UnityEngine.Random.Range(0, positions_for_target.Count)];*/

        }
        private void OnTriggerEnter(Collider other)
        {
            reward += 1f;
            AddReward(1f);
            WinModel.SetActive(true);
            LoseModel.SetActive(false);
            TimeOutModel.SetActive(false);
            EndEpisode();
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Wall"))
            {
                AddReward(-1f);
                reward -= 1f;
                WinModel.SetActive(false);
                LoseModel.SetActive(true);
                TimeOutModel.SetActive(false);

                EndEpisode();
            }
        }
        public void FixedUpdate()
        {
            if (Academy.Instance.IsCommunicatorOn)
            {
                RequestDecision();
            }
            else
            {
                if (m_TimeSinceDecision >= timeBetweenDecisionsAtInference)
                {
                    m_TimeSinceDecision = 0f;
                    RequestDecision();
                }
                else
                {
                    m_TimeSinceDecision += Time.fixedDeltaTime;
                }
            }
        }

    }
}
