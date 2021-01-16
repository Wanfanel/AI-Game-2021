
using UnityEngine;

public class GameSpeed : MonoBehaviour
{
  public float time_speed = 1f;
    void Start()
    {
        Time.timeScale = time_speed;
    }


}
