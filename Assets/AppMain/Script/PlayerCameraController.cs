using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{

    // 回転操作用トランスフォーム.
    [SerializeField] Transform rotationRoot = null;
    // 高さ操作用トランスフォーム.
    [SerializeField] Transform heightRoot = null;
    // カメラ回転スピード.
    [SerializeField] float rotationSpeed = 0.01f;
    // カメラ高さ変化スピード.
    [SerializeField] float heightSpeed = 0.001f;
    // カメラ移動制限MinMax.
    [SerializeField] Vector2 heightLimit_MinMax = new Vector2( -1f, 3f );
    // プレイヤーカメラ.
    [SerializeField] Camera mainCamera = null;
    // カメラが写す中心のプレイヤーから高さ.
    [SerializeField] float lookHeight = 1.0f;
    public void UpdateCameraLook( Transform player )
    {
        // カメラをキャラの少し上に固定.
        var cameraMarker = player.position;
        cameraMarker.y += lookHeight; 
        var _camLook = ( cameraMarker - mainCamera.transform.position ).normalized;
        mainCamera.transform.forward = _camLook;
    }
    public void FixedUpdateCameraPosition( Transform player )
    {
        this.transform.position = player.position;
    }
    public void UpdateCameraRotation( Vector2 rotateVector )
    {
        var yRot = new Vector3( 0, rotateVector.x * rotationSpeed, 0 );
        var rResult = rotationRoot.rotation.eulerAngles + yRot;
        var qua = Quaternion.Euler( rResult );
        rotationRoot.rotation = qua;

        // カメラ高低.
        var yHeight = new Vector3( 0, - rotateVector.y * heightSpeed, 0 );
        var hResult = heightRoot.transform.localPosition + yHeight;
        if( hResult.y > heightLimit_MinMax.y ) hResult.y = heightLimit_MinMax.y;
        else if( hResult.y <= heightLimit_MinMax.x ) hResult.y = heightLimit_MinMax.x;
        heightRoot.localPosition = hResult;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }
}
