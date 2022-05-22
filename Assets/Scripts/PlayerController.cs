using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    #region Definitions
    private static readonly int IS_MOVE_HASH = Animator.StringToHash("IsMove");

    private enum CameraModeType
    {
        Default,
        LookItem,
        Aim,
    }
    #endregion // Definitions

    #region Variables Move
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
    #endregion //  Variables Move

    #region Variables Camera
    [SerializeField]
    private CameraManager _cameraMgr;

    private CameraModeType _cameraModeType;

    private CameraManager.Parameter _defaultCamParam;

    [SerializeField]
    private CameraManager.Parameter _itemCamParam;

    [SerializeField]
    private CameraManager.Parameter _aimCamParam;

    private Sequence _cameraSeq;

    [SerializeField]
    private Image _cursor;
    #endregion // Variables Camera

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _defaultCamParam = _cameraMgr.Param.Clone();
    }

    private void Update()
    {
        ControlMove();
        ControlCamera();
    }

    #region Methods Move
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
            _animator.SetBool(IS_MOVE_HASH, isMove);
        }

        if(isMove)
        {
            transform.position += moveVector * Time.deltaTime * _speed;

            // エイムモード時はカメラの向きにプレイヤーの向きを合わせるため、処理しない
            if(_cameraModeType != CameraModeType.Aim)
            {
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(moveVector.x, 0f, moveVector.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * _rollSpeed);
            }

            // テレインに沿って高さを合わせる
            if(_terrain != null)
            {
                Vector3 position = transform.position;
                position.y = _terrain.SampleHeight(position);
                transform.position = position;
            }
        }
    }
    #endregion // Methods Move

    #region Methods Camera
    private CameraManager.Parameter GetCameraParameter(CameraModeType type)
	{
        switch(type)
        {
            case CameraModeType.Default:
                return _defaultCamParam;
            case CameraModeType.LookItem:
                return _itemCamParam;
            case CameraModeType.Aim:
                return _aimCamParam;
            default:
                return null;
        }
    }

    private void SwitchCamera(CameraModeType type)
    {
        float duration = 2f;
        // エイムモード切り替え時は素早くカメラを遷移させる
        if(type == CameraModeType.Aim || _cameraModeType == CameraModeType.Aim)
		{
            duration = 0.3f;
		}

        switch(type)
        {
            case CameraModeType.Default:
                _defaultCamParam.position = _defaultCamParam.trackTarget.position;
                switch(_cameraModeType)
                {
                    case CameraModeType.LookItem:
                        _defaultCamParam.angles = new Vector3(15f, transform.eulerAngles.y, 0f);
                        break;
                    default:
                        _defaultCamParam.angles = _cameraMgr.Param.angles;
                        break;
                }
                break;
            case CameraModeType.Aim:
                _aimCamParam.position = _aimCamParam.trackTarget.position;
                _aimCamParam.angles = _cameraMgr.Param.angles;
                transform.eulerAngles = new Vector3(0f, _cameraMgr.Param.angles.y, 0f);
                break;
        }

        _cameraModeType = type;
        _cursor.enabled = _cameraModeType == CameraModeType.Aim;

        _cameraMgr.Param.trackTarget = null;
        CameraManager.Parameter startCamParam = _cameraMgr.Param.Clone();
        CameraManager.Parameter endCamParam = GetCameraParameter(_cameraModeType);

        _cameraSeq?.Kill();
        _cameraSeq = DOTween.Sequence();
        _cameraSeq.Append(DOTween.To(() => 0f, t => CameraManager.Parameter.Lerp(startCamParam, endCamParam, t, _cameraMgr.Param), 1f, duration).SetEase(Ease.OutQuart));

		switch(_cameraModeType)
		{
            case CameraModeType.Default:
                _cameraSeq.OnUpdate(() => CameraManager.UpdateTrackTargetBlend(_defaultCamParam));
                break;
            case CameraModeType.Aim:
                _cameraSeq.OnUpdate(() => _aimCamParam.position = _aimCamParam.trackTarget.position);
                break;
        }

        _cameraSeq.AppendCallback(() => _cameraMgr.Param.trackTarget = endCamParam.trackTarget);
    }

    private void ControlCamera()
    {
        // デフォルト／エイムモード時のみマウス操作によるカメラ回転を受け付ける
        if((_cameraModeType == CameraModeType.Default || _cameraModeType == CameraModeType.Aim) &&
           (_cameraSeq == null || !_cameraSeq.IsPlaying()))
        {
            Vector3 diffAngles = new Vector3(
                x: -Input.GetAxis("Mouse Y"),
                y: Input.GetAxis("Mouse X")
            ) * 5f;
            _cameraMgr.Param.angles += diffAngles;

            // エイムモード時はカメラはイージング無しで追いかけさせる
            if(_cameraModeType == CameraModeType.Aim)
            {
                _cameraMgr.Param.position = _cameraMgr.Param.trackTarget.position;
                transform.eulerAngles = new Vector3(0f, _cameraMgr.Param.angles.y, 0f);
            }
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            switch(_cameraModeType)
            {
                case CameraModeType.Default:
                    SwitchCamera(CameraModeType.LookItem);
                    break;
                case CameraModeType.LookItem:
                    SwitchCamera(CameraModeType.Default);
                    break;
            }
        }

        if(Input.GetKeyDown(KeyCode.Return))
        {
            switch(_cameraModeType)
            {
                case CameraModeType.Default:
                    SwitchCamera(CameraModeType.Aim);
                    break;
                case CameraModeType.Aim:
                    SwitchCamera(CameraModeType.Default);
                    break;
            }
        }
    }
    #endregion // #region Methods Camera
}
