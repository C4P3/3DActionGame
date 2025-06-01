using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
 

public class PlayerController : MonoBehaviour
{
    //! HPバーのスライダー.
    [SerializeField] Slider hpBar = null;
    // -------------------------------------------------------
    /// <summary>
    /// ステータス.
    /// </summary>
    // -------------------------------------------------------
    [System.Serializable]
    public class Status
    {
        // 体力.
        public int Hp = 10;
        // 攻撃力.
        public int Power = 1;
    }
 
    // 攻撃HitオブジェクトのColliderCall.
    [SerializeField] ColliderCallReceiver attackHitCall = null;
    // 基本ステータス.
    [SerializeField] Status DefaultStatus = new Status();
    // 現在のステータス.
    public Status CurrentStatus = new Status();

    Animator animator = null;
    // カメラコントローラー.
    [SerializeField] PlayerCameraController cameraController = null;
    [SerializeField] GameObject attackHit = null; // 攻撃判定用オブジェクト.
    // 接地判定用ColliderCall.
    [SerializeField] ColliderCallReceiver footColliderCall = null;
    // タッチマーカー.
    [SerializeField] GameObject touchMarker = null;
    [SerializeField] float jumpPower = 0.1f; //ジャンプ力
    [SerializeField] int mouseCameraSpeed = 150;
    [SerializeField] bool touchMode = false;//強制的にタッチモードに変更
　　// 自身のコライダー.
    [SerializeField] Collider myCollider = null;
    // 攻撃を食らったときのパーティクルプレハブ.
    [SerializeField] GameObject hitParticlePrefab = null;

    //! ゲームオーバー時イベント.
    public UnityEvent GameOverEvent = new UnityEvent();
    // 開始時位置.
    Vector3 startPosition = new Vector3();
    // 開始時角度.
    Quaternion startRotation = new Quaternion();

    // パーティクルオブジェクト保管用リスト.
    List<GameObject> particleObjectList = new List<GameObject>();
    Rigidbody rigid = null; //リジッドボディ
    // PCキー横方向入力
    float horizontalKeyInput = 0;
    //　PCキー縦方向入力
    float verticalKeyInput = 0;
    bool isAttack = false; //! 攻撃アニメーション中フラグ.
    // 接地フラグ.
    bool isGround = false;
    bool isTouch = false;
    // 左半分タッチ入力.
    Vector2 leftTouchInput = new Vector2();
    // 左半分タッチスタート位置.
    Vector2 leftStartTouch = new Vector2();
    // タッチスタート位置.
    Vector2 rightStartTouch = Vector2.zero;
    // 現在のタッチ位置.
    Vector2 rightTouchInput = Vector2.zero;
    // Start is called before the first frame update
    void Start()
    {
        // スライダーを初期化.
        hpBar.maxValue = DefaultStatus.Hp;
        hpBar.value = CurrentStatus.Hp;
　　　　// 開始時の位置回転を保管.
　　　  startPosition = this.transform.position;
        startRotation = this.transform.rotation;
        // 攻撃判定用コライダーイベント登録.
        attackHitCall.TriggerEnterEvent.AddListener( OnAttackHitTriggerEnter );
        // 現在のステータスの初期化.
        CurrentStatus.Hp = DefaultStatus.Hp;
        CurrentStatus.Power = DefaultStatus.Power;
        animator = GetComponent<Animator>();

        // 攻撃判定用オブジェクトを非表示に.
        // Rigidbodyの取得.
        rigid = GetComponent<Rigidbody>();
        attackHit.SetActive( false );
        // FootSphereのイベント登録.
        footColliderCall.TriggerStayEvent.AddListener( OnFootTriggerStay );
        footColliderCall.TriggerExitEvent.AddListener( OnFootTriggerExit );
    }

    public void UpdateRightTouch( Touch touch )
    {
        // タッチ開始.
        if( touch.phase == TouchPhase.Began )
        {
            // 開始位置を保管.
            rightStartTouch = touch.position;
        }
        // タッチ中.
        else if( touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary )
        {
            // 現在の位置を随時保管.
            Vector2 position = touch.position;
            // 開始位置からの移動ベクトルを算出.
            rightTouchInput = position - rightStartTouch;
            cameraController.UpdateCameraRotation(rightTouchInput);
        }
        // タッチ終了.
        else if( touch.phase == TouchPhase.Ended )
        {
            rightTouchInput = Vector2.zero;
        }
    }
    // Update is called once per frame
    void Update()
    {
        // カメラをプレイヤーに向ける. 
        cameraController.UpdateCameraLook( this.transform );
        if( touchMode || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer )
        {
            // スマホタッチ操作.
            // タッチしている指の数が０より多い.
            if( Input.touchCount > 0 )
            {
                isTouch = true;
                // タッチ情報をすべて取得.
                Touch[] touches = Input.touches;
                // 全部のタッチを繰り返して判定.
                foreach( var touch in touches )
                {
                    bool isLeftTouch = false;
                    bool isRightTouch = false;
                    // タッチ位置のX軸方向がスクリーンの左側.
                    if( touch.position.x > 0 && touch.position.x < Screen.width / 2 )
                    {
                        isLeftTouch = true;
                    }
                    // タッチ位置のX軸方向がスクリーンの右側.
                    else if( touch.position.x > Screen.width / 2 && touch.position.x < Screen.width )
                    {
                        isRightTouch = true;;
                    }
                    // 左タッチ.
                    if( isLeftTouch == true )
                    {
                        // 左半分をタッチした際の処理.
                        if( isLeftTouch == true )
                        {
                            // タッチ開始.
                            if( touch.phase == TouchPhase.Began )
                            {
                                // 開始位置を保管.
                                leftStartTouch = touch.position;
                                // 開始位置にマーカーを表示.
                                touchMarker.SetActive( true );
                                Vector3 touchPosition = touch.position;
                                touchPosition.z = 1f;
                                Vector3 markerPosition = Camera.main.ScreenToWorldPoint( touchPosition );
                                touchMarker.transform.position = markerPosition;
                            }
                            // タッチ中.
                            else if( touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary )
                            {
                                // 現在の位置を随時保管.
                                Vector2 position = touch.position;
                                // 移動用の方向を保管.
                                leftTouchInput = position - leftStartTouch;
                            }
                            // タッチ終了.
                            else if( touch.phase == TouchPhase.Ended )
                            {
                                leftTouchInput = Vector2.zero;
                                // マーカーを非表示.
                                touchMarker.gameObject.SetActive( false );
                            }
                        }
                    }
    
                    // 右タッチ.
                    if( isRightTouch == true )
                    {
                        UpdateRightTouch( touch );
                        cameraController.UpdateCameraRotation(rightTouchInput);
                    }
                }
            }
            else
            {
                isTouch = false;
            }
        }
        else
        {
            //PC入力
            if(Input.GetKeyDown(KeyCode.J))
            {
                OnAttackButtonClicked();
            }
            if(Input.GetKeyDown(KeyCode.Space))
            {
                OnJumpButtonClicked();
            }
            horizontalKeyInput = Input.GetAxis( "Horizontal" );
            verticalKeyInput = Input.GetAxis( "Vertical" );
            if(Input.GetKeyDown(KeyCode.W)){
                verticalKeyInput += 1;
            }
            if(Input.GetKeyDown(KeyCode.A)){
                horizontalKeyInput -= 1;
            }
            if(Input.GetKeyDown(KeyCode.S)){
                verticalKeyInput -= 1;
            }
            if(Input.GetKeyDown(KeyCode.D)){
                horizontalKeyInput += 1;
            }
            //　カメラ回転
            float mx = Input.GetAxis("Mouse X")*mouseCameraSpeed;
	        float my = Input.GetAxis("Mouse Y")*mouseCameraSpeed;
            if (Mathf.Abs(my) > 0.001f || Mathf.Abs(mx) > 0.001f)// Y方向に一定量移動していれば縦回転
            {
                cameraController.UpdateCameraRotation(new Vector2(mx, my));
            }
        }
        // プレイヤーの向きを調整.
        bool isKeyInput = horizontalKeyInput != 0 || verticalKeyInput != 0 || leftTouchInput != Vector2.zero;
        if( isKeyInput == true && isAttack == false )
        {
            bool currentIsRun = animator.GetBool("isRun");
            if(currentIsRun == false) animator.SetBool("isRun", true);
            Vector3 dir = rigid.velocity.normalized;
            dir.y = 0;
            if(dir != Vector3.zero){
                this.transform.forward = dir;
            }
        }else{
            bool currentIsRun = animator.GetBool("isRun");
            if( currentIsRun == true ) animator.SetBool("isRun", false);
        }
    }

    void FixedUpdate()
    {
        // カメラの位置をプレイヤーに合わせる.
        cameraController.FixedUpdateCameraPosition( this.transform );
        if( isAttack == false )
        {
            Vector3 input = new Vector3();
            Vector3 move = new Vector3();
            if(touchMode || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer )
            {
                input = new Vector3( leftTouchInput.x, 0, leftTouchInput.y );
                move = input.normalized * 4f;
            }
            else
            {
                input = new Vector3( horizontalKeyInput, 0, verticalKeyInput );
                move = input.normalized * 4f;
            }
            Vector3 cameraMove = Camera.main.gameObject.transform.rotation * move;
            cameraMove.y = 0;
            Vector3 currentRigidVelocity = rigid.velocity;
            currentRigidVelocity.y = 0;
        
            rigid.AddForce(cameraMove - currentRigidVelocity, ForceMode.VelocityChange);
        }
    }
    public void OnAttackButtonClicked()
    {
        if( isAttack == false )
        {
            // AnimationのisAttackトリガーを起動.
            animator.SetTrigger( "isAttack" );
            // 攻撃開始.
            isAttack = true;
        }
    }
    public void Anim_AttackHit()
    {
        // 攻撃判定用オブジェクトを表示.
        attackHit.SetActive( true );
    }
    public void Anim_AttackEnd()
    {
        // 攻撃判定用オブジェクトを非表示に.
        attackHit.SetActive( false );
        // 攻撃終了.
        isAttack = false;
    }
        // ---------------------------------------------------------------------
    /// <summary>
    /// ジャンプボタンクリックコールバック.
    /// </summary>
    // ---------------------------------------------------------------------
    public void OnJumpButtonClicked()
    {
        if( isGround == true )
        {
            rigid.AddForce( Vector3.up * jumpPower, ForceMode.Impulse );
        }
    }

    // ---------------------------------------------------------------------
    /// <summary>
    /// FootSphereトリガーステイコール.
    /// </summary>
    /// <param name="col"> 侵入したコライダー. </param>
    // ---------------------------------------------------------------------
    void OnFootTriggerStay( Collider col )
    {
        if( col.gameObject.tag == "Ground" )
        {
            if( isGround == false ) isGround = true;
            if( animator.GetBool( "isGround" ) == false ) animator.SetBool( "isGround", true );
        }
    }

    // ---------------------------------------------------------------------
    /// <summary>
    /// FootSphereトリガーイグジットコール.
    /// </summary>
    /// <param name="col"> 侵入したコライダー. </param>
    // ---------------------------------------------------------------------
    void OnFootTriggerExit( Collider col )
    {
        if( col.gameObject.tag == "Ground" )
        {
            isGround = false;
            animator.SetBool( "isGround", false );
        }
    }

    // ---------------------------------------------------------------------
    /// <summary>
    /// 攻撃判定トリガーエンターイベントコール.
    /// </summary>
    /// <param name="col"> 侵入したコライダー. </param>
    // ---------------------------------------------------------------------
    void OnAttackHitTriggerEnter( Collider col )
    {
        if( col.gameObject.tag == "Enemy" )
        {
            var enemy = col.gameObject.GetComponent<EnemyBase>();
            enemy?.OnAttackHit( CurrentStatus.Power, this.transform.position );
            attackHit.SetActive( false );
        }
    }

    // ---------------------------------------------------------------------
    /// <summary>
    /// 敵の攻撃がヒットしたときの処理.
    /// </summary>
    /// <param name="damage"> 食らったダメージ. </param>
    // ---------------------------------------------------------------------
    public void OnEnemyAttackHit( int damage, Vector3 attackPosition )
    {
        CurrentStatus.Hp -= damage;
        hpBar.value = CurrentStatus.Hp;

        var pos = myCollider.ClosestPoint( attackPosition );
        var obj = Instantiate( hitParticlePrefab, pos, Quaternion.identity );
        var par = obj.GetComponent<ParticleSystem>();
        StartCoroutine( WaitDestroy( par ) );
        particleObjectList.Add( obj );
 
        if( CurrentStatus.Hp <= 0 )
        {
            OnDie();
        }
        else
        {
            Debug.Log( damage + "のダメージを食らった!!残りHP" + CurrentStatus.Hp );
        }
    }

　　// ---------------------------------------------------------------------
    /// <summary>
    /// パーティクルが終了したら破棄する.
    /// </summary>
    /// <param name="particle"></param>
    // ---------------------------------------------------------------------
    IEnumerator WaitDestroy( ParticleSystem particle )
    {
        yield return new WaitUntil( () => particle.isPlaying == false );
        if( particleObjectList.Contains( particle.gameObject ) == true ) particleObjectList.Remove( particle.gameObject );
        Destroy( particle.gameObject );
    }

    // ---------------------------------------------------------------------
    /// <summary>
    /// 死亡時処理.
    /// </summary>
    // ---------------------------------------------------------------------
    void OnDie()
    {
        Debug.Log( "死亡しました。" );

        StopAllCoroutines();
        if( particleObjectList.Count > 0 )
        {
            foreach( var obj in particleObjectList ) Destroy( obj );
            particleObjectList.Clear();
        }
        GameOverEvent?.Invoke();
    }
    // ---------------------------------------------------------------------
    /// <summary>
    /// リトライ処理.
    /// </summary>
    // ---------------------------------------------------------------------
    public void Retry()
    {
        // 現在のステータスの初期化.
        CurrentStatus.Hp = DefaultStatus.Hp;
        CurrentStatus.Power = DefaultStatus.Power;
        hpBar.value = CurrentStatus.Hp;
        // 位置回転を初期位置に戻す.
        this.transform.position = startPosition;
        this.transform.rotation = startRotation;
 
        //攻撃処理の途中でやられた時用
        isAttack = false;
    }
}
