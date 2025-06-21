using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Friendly":
                Debug.Log("Everything is looking good!");
                break;
            case "Finish":
                Debug.Log("You're all done, Welcome to our country");
                break;
            case "Fuel":
                Debug.Log("You collected fuel!");
                break;
            default:
                Debug.Log("You bumped into an unknown object!");
                break;
        }
    }
}
