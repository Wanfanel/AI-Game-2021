﻿using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

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
        [SerializeField] private Transform targetTransform = null;
        [SerializeField] private GameObject wallPrefab = null;
        [SerializeField] private GameObject WinModel = null;
        [SerializeField] private GameObject LoseModel = null;
        public float moveSpeed = 10f;
        private bool wallgenerated = false;
        private Dictionary<Wall_position, GameObject> Wall_grid = new Dictionary<Wall_position, GameObject>();
        private Queue<GameObject> walls_not_active = new Queue<GameObject>();



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

            int chose = (Random.Range(0, 4));

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
                if (Random.Range(0, 2) == 0)
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

                if (Random.Range(0, 2) == 0)
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

                if (Random.Range(0, 2) == 0)
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

                if (Random.Range(0, 2) == 0)
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
                        if (Random.Range(0, 2) == 0)
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
                        if (Random.Range(0, 2) == 0)
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
                        if (Random.Range(0, 2) == 0)
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
                        if (Random.Range(0, 2) == 0)
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

                    for (int i = -2; i < 2; i++)
                    {
                        for (int j = -3; j < 4; j++)
                        {
                            GameObject wall = Instantiate(wallPrefab,  Vector3.zero, Quaternion.identity, transform.parent) as GameObject;
                            wall.transform.localPosition = new Vector3(j << 1, 0, (i << 1) + 1);
                            Wall_grid.Add(new Wall_position(j << 1, (i << 1) + 1), wall);
                        }
                    }
                    for (int i = -2; i < 3; i++)
                    {
                        for (int j = -3; j < 3; j++)
                        {
                            GameObject wall = Instantiate(wallPrefab,  Vector3.zero, Quaternion.identity, transform.parent) as GameObject;
                            wall.transform.localPosition = new Vector3((j << 1) + 1, 0, i << 1);
                            Wall_grid.Add(new Wall_position((j << 1) + 1, i << 1), wall);
                        }
                    }


                    wallgenerated = true;
                }





        }
        // public override void CollectObservations(VectorSensor sensor)
        //  {
        //   sensor.AddObservation(transform.localPosition);
        //   sensor.AddObservation(targetTransform.localPosition);
        //     // base.CollectObservations(sensor);
        // }


        public override void OnActionReceived(float[] vectorAction)
        {
            float moveX = vectorAction[0];
            float moveZ = vectorAction[1];
            AddReward(-0.001f);
            //Debug.Log(moveX + "," + moveZ);
            // base.OnActionReceived(vectorAction);

            transform.localPosition += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;
        }

        public override void Heuristic(float[] actionsOut)
        {
            actionsOut[0] = Input.GetAxisRaw("Horizontal");
            actionsOut[1] = Input.GetAxisRaw("Vertical");
        }

        public override void OnEpisodeBegin()
        {
           // int min_x = -3, max_x = 4, min_z = -2, max_z = 3; //hard
            int min_x = -1, max_x = 2, min_z = -1, max_z = 2; //easy
            int pos_x = Random.Range(min_x, max_x) << 1,
                pos_z = Random.Range(min_z, max_z) << 1;


            transform.localPosition = new Vector3(pos_x, 0, pos_z);
            int target_x = 0, target_z = 0;
            do
            {
                target_x = Random.Range(min_x, max_z) << 1;
                target_z = Random.Range(min_z, max_z) << 1;
            } while (target_x == pos_x && target_z == pos_z);

            targetTransform.localPosition = new Vector3(target_x, 0, target_z);

            while (walls_not_active.Count > 0)
            {
                walls_not_active.Dequeue().SetActive(true);
            }
            Maze_Spiral_Algorytm(0, 0);
            Connect_side_wall(-6, 4);
            Connect_side_wall(6, -4);
        }

        private void OnTriggerEnter(Collider other)
        {
            AddReward(1f);
            WinModel.SetActive(true);
            LoseModel.SetActive(false);
            EndEpisode();
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Wall"))
            {
                AddReward(-1f);
                WinModel.SetActive(false);
                LoseModel.SetActive(true);

                EndEpisode();
            }
        }
    }
}
