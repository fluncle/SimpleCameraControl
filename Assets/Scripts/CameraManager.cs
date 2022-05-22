using System;
using UnityEngine;

// シーンを実行しなくてもカメラワークが反映されるよう、ExecuteInEditModeを付与
[ExecuteInEditMode]
public class CameraManager : MonoBehaviour
{
    [SerializeField]
    private Transform _parent;

    [SerializeField]
    private Transform _child;

    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private Parameter _parameter;

    private void LateUpdate()
    {
        if(_parent == null || _child == null || _camera == null)
        {
            return;
        }

        if(_parameter.trackTarget != null)
        {
            // 被写体がTransformで指定されている場合、positionパラメータに座標を上書き
            _parameter.position = Vector3.Lerp(
                a: _parameter.position,
                b: _parameter.trackTarget.position,
                t: Time.deltaTime * 4f
            );
        }

        // パラメータを各種オブジェクトに反映
        _parent.position = _parameter.position;
        _parent.eulerAngles = _parameter.angles;

        var childPos = _child.localPosition;
        childPos.z = -_parameter.distance;
        _child.localPosition = childPos;

        _camera.fieldOfView = _parameter.fieldOfView;
        _camera.transform.localPosition = _parameter.offsetPosition;
        _camera.transform.localEulerAngles = _parameter.offsetAngles;
    }

     /// <summary> カメラのパラメータ </summary>
    [Serializable]
    public class Parameter
    {
        public Transform trackTarget;
        public Vector3 position;
        public Vector3 angles = new Vector3(10f, 0f, 0f);
        public float distance = 7f;
        public float fieldOfView = 45f;
        public Vector3 offsetPosition = new Vector3(0f, 1f, 0f);
        public Vector3 offsetAngles;
    }
}
