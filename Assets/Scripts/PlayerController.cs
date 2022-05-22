using UnityEngine;
using DG.Tweening;

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

    private void Awake()
    {
        _defaultCamParam = _cameraMgr.Param.Clone();
    }

    private void Update()
    {
        ControlMove();
        ControlCamera();
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

    [SerializeField]
    private CameraManager _cameraMgr;

    [SerializeField]
    private bool _useMouseRoll;

    private bool _isLookItem;

    private CameraManager.Parameter _defaultCamParam;

    [SerializeField]
    private CameraManager.Parameter _itemCamParam;

    private Sequence _cameraSeq;

    private void SwitchCamera()
    {
        _isLookItem = !_isLookItem;
        if(!_isLookItem)
        {
            _defaultCamParam.position = _defaultCamParam.trackTarget.position;
            _defaultCamParam.angles = new Vector3(15f, transform.eulerAngles.y, 0f);
        }

        _cameraMgr.Param.trackTarget = null;

        _cameraSeq?.Kill();
        _cameraSeq = DOTween.Sequence();
        CameraManager.Parameter startCamParam = _cameraMgr.Param.Clone();
        _cameraSeq.Append(DOTween.To(() => 0f, t =>
        {
            if(_isLookItem)
            {
                CameraManager.Parameter.Lerp(startCamParam, _itemCamParam, t, _cameraMgr.Param);
            }
            else
            {
                CameraManager.Parameter.Lerp(startCamParam, _defaultCamParam, t, _cameraMgr.Param);
            }
        }, 1f, 2f).SetEase(Ease.OutQuart));

        _cameraSeq.OnUpdate(() =>
        {
            if(!_isLookItem) CameraManager.UpdateTrackTargetBlend(_defaultCamParam);
        });

        _cameraSeq.AppendCallback(() =>
        {
            _cameraMgr.Param.trackTarget = _isLookItem ? _itemCamParam.trackTarget : _defaultCamParam.trackTarget;
        });
    }

    private void ControlCamera()
    {
        if(_useMouseRoll && !_isLookItem && (_cameraSeq == null || !_cameraSeq.IsPlaying()))
        {
            Vector3 diffAngles = new Vector3(
                x: -Input.GetAxis("Mouse Y"),
                y: Input.GetAxis("Mouse X")
            ) * 5f;
            _cameraMgr.Param.angles += diffAngles;
        }

        if(Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            SwitchCamera();
        }
    }
}
