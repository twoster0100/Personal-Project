using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [SerializeField] private MoveInputResolver input;
    [SerializeField] private float speed = 5f;

    private void Update()
    {
        Vector3 move = input.GetMoveVector();
        transform.position += move * (speed * Time.deltaTime);
    }
}
