
using UnityEngine;

public class SimpleDoor : MonoBehaviour
{
    public Transform door;

    private Vector3 closedPosition;

    void Start()
    {
        if (door == null)
        {
            Debug.LogError("Assign the door in Inspector!");
            return;
        }

        closedPosition = door.localPosition;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<RigidbodyMovement>() != null)
        {
            door.localPosition = closedPosition + Vector3.up * 2f;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<RigidbodyMovement>() != null)
        {
            door.localPosition = closedPosition;
        }
    }
}
