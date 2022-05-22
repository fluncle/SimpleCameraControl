using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private static readonly int IS_MOVE_HASH = Animator.StringToHash("IsMove");

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private Transform _camera;

    [SerializeField]
    private Terrain _terrain;

    [SerializeField]
    private float _speed = 3f;

    [SerializeField]
    private float _rollSpeed = 360f;

    private void Update()
    {
        ControlMove();
    }

    private Vector3 GetMoveVector()
    {
        Vector3 moveVector = Vector3.zero;
        if(Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            moveVector += Vector3.forward;
        }
        if(Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            moveVector += Vector3.back;
        }
        if(Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            moveVector += Vector3.left;
        }
        if(Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            moveVector += Vector3.right;
        }
        Quaternion cameraRotate = Quaternion.Euler(0f, _camera.eulerAngles.y, 0f);
        return cameraRotate * moveVector.normalized;
    }

    private void ControlMove()
	{
        Vector3 moveVector = GetMoveVector();
        bool isMove = moveVector != Vector3.zero;

        if(_animator != null)
        {
            _animator?.SetBool(IS_MOVE_HASH, isMove);
        }

        if(isMove)
        {
            transform.position += moveVector * Time.deltaTime * _speed;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(moveVector.x, 0f, moveVector.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * _rollSpeed);
            // テレインに沿って高さを合わせる
            if(_terrain != null)
            {
                Vector3 position = transform.position;
                position.y = _terrain.SampleHeight(position);
                transform.position = position;
            }
        }
    }
}
