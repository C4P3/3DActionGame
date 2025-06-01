using UnityEngine;
using UnityEngine.UI;
using System.Collections;
 
public class EnemyBase : MonoBehaviour
{
    //! HPバーのスライダー.
    [SerializeField] Slider hpBar = null;
    // 攻撃状態フラグ.
    public bool IsBattle = false;
　  // 開始時位置.
    Vector3 startPosition = new Vector3();
    // 開始時角度.
    Quaternion startRotation = new Quaternion();
    // ----------------------------------------------------------
    /// <summary>
    /// ステータス.
    /// </summary>
    // ----------------------------------------------------------
    [System.Serializable]
    public class Status
    {
        // HP.
        public int Hp = 10;
        // 攻撃力.
        public int Power = 1;
    }
 
    // 基本ステータス.
    [SerializeField] Status DefaultStatus = new Status();
    // 現在のステータス.
    public Status CurrentStatus = new Status();

    // 周辺レーダーコライダーコール.
    [SerializeField] ColliderCallReceiver aroundColliderCall = null;
    // 攻撃間隔.
    [SerializeField] float attackInterval = 3f;
    //! 攻撃判定用コライダーコール.
    [SerializeField] ColliderCallReceiver attackHitColliderCall = null;
    //! 自身のコライダー.
    [SerializeField] Collider myCollider = null;
    //! 攻撃ヒット時エフェクトプレハブ.
    [SerializeField] GameObject hitParticlePrefab = null;
    // 攻撃時間計測用.
    float attackTimer = 0f;
    // アニメーター.
    Animator animator = null;
 
    void Start()
    {
        // スライダーを初期化.
        hpBar.maxValue = DefaultStatus.Hp;
        hpBar.value = CurrentStatus.Hp;
        // 開始時の位置回転を保管.
        startPosition = this.transform.position;
        startRotation = this.transform.rotation;
        // Animatorを取得し保管.
        animator = GetComponent<Animator>();
        // 最初に現在のステータスを基本ステータスとして設定.
        CurrentStatus.Hp = DefaultStatus.Hp;
        CurrentStatus.Power = DefaultStatus.Power;
        // 攻撃コライダーイベント登録.
        attackHitColliderCall.TriggerEnterEvent.AddListener( OnAttackTriggerEnter );
        // 周辺コライダーイベント登録.
        aroundColliderCall.TriggerStayEvent.AddListener( OnAroundTriggerStay );
        // 周辺コライダーイベント登録.
        aroundColliderCall.TriggerEnterEvent.AddListener( OnAroundTriggerEnter );
        aroundColliderCall.TriggerStayEvent.AddListener( OnAroundTriggerStay );
        aroundColliderCall.TriggerExitEvent.AddListener( OnAroundTriggerExit );

        attackHitColliderCall.gameObject.SetActive( false );
    }

    void Update()
    {
        // 攻撃できる状態の時.
        if( IsBattle == true )
        {
            attackTimer += Time.deltaTime;
 
            if( attackTimer >= 3f )
            {
                animator.SetTrigger( "isAttack" );
                attackTimer = 0;
            }
        }
        else
        {
            attackTimer = 0;
        }
    }
 
    // ----------------------------------------------------------
    /// <summary>
    /// 攻撃ヒット時コール.
    /// </summary>
    /// <param name="damage"> 食らったダメージ. </param>
    // ----------------------------------------------------------
    public void OnAttackHit( int damage, Vector3 attackPosition )
    {
        CurrentStatus.Hp -= damage;
        hpBar.value = CurrentStatus.Hp;
        Debug.Log( "Hit Damage " + damage + "/CurrentHp = " + CurrentStatus.Hp );
 
        var pos = myCollider.ClosestPoint( attackPosition );
        var obj = Instantiate( hitParticlePrefab, pos, Quaternion.identity );
        var par = obj.GetComponent<ParticleSystem>();
        StartCoroutine( WaitDestroy( par ) );

        if( CurrentStatus.Hp <= 0 )
        {
            OnDie();
        }        
        else
        {
            animator.SetTrigger( "isHit" );
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
        Destroy( particle.gameObject );
    }
 
    // ----------------------------------------------------------
    /// <summary>
    /// 死亡時コール.
    /// </summary>
    // ----------------------------------------------------------
    void OnDie()
    {
        Debug.Log( "死亡" );
        animator.SetBool( "isDie", true );
    }
 
    // ----------------------------------------------------------
    /// <summary>
    /// 死亡アニメーション終了時コール.
    /// </summary>
    // ----------------------------------------------------------
    void Anim_DieEnd()
    {
        this.gameObject.SetActive( false );
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 周辺レーダーコライダーエンターイベントコール.
    /// </summary>
    /// <param name="other"> 接近コライダー. </param>
    // ------------------------------------------------------------
    void OnAroundTriggerEnter( Collider other )
    {
        if( other.gameObject.tag == "Player" )
        {
            IsBattle = true;
        }
    }
    // ------------------------------------------------------------
    /// <summary>
    /// 周辺レーダーコライダーステイイベントコール.
    /// </summary>
    /// <param name="other"> 接近コライダー. </param>
    // ------------------------------------------------------------
    void OnAroundTriggerStay( Collider other )
    {
        if( other.gameObject.tag == "Player" )
        {
            var _dir = ( other.gameObject.transform.position - this.transform.position ).normalized;
            _dir.y = 0;
            this.transform.forward = _dir;
        }
    }
        // ------------------------------------------------------------
    /// <summary>
    /// 周辺レーダーコライダー終了イベントコール.
    /// </summary>
    /// <param name="other"> 接近コライダー. </param>
    // ------------------------------------------------------------
    void OnAroundTriggerExit( Collider other )
    {
        if( other.gameObject.tag == "Player" )
        {
            IsBattle = false;
        }
    }

    // ----------------------------------------------------------
    /// <summary>
    /// 攻撃Hitアニメーションコール.
    /// </summary>
    // ----------------------------------------------------------
    void Anim_AttackHit()
    {
        attackHitColliderCall.gameObject.SetActive( false );
    }
    // ----------------------------------------------------------
    /// <summary>
    /// 攻撃アニメーション終了時コール.
    /// </summary>
    // ----------------------------------------------------------
    void Anim_AttackEnd()
    {
        attackHitColliderCall.gameObject.SetActive( true );
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 攻撃コライダーエンターイベントコール.
    /// </summary>
    /// <param name="other"> 接近コライダー. </param>
    // ------------------------------------------------------------
    void OnAttackTriggerEnter( Collider other )
    {
        if( other.gameObject.tag == "Player" )
        {
            var player = other.GetComponent<PlayerController>();
            player?.OnEnemyAttackHit( CurrentStatus.Power, this.transform.position );
            Debug.Log( "プレイヤーに敵の攻撃がヒット！" + CurrentStatus.Power + "の力で攻撃！" );
            attackHitColliderCall.gameObject.SetActive( false );
        }
    }

    // ----------------------------------------------------------
    /// <summary>
    /// プレイヤーリトライ時の処理.
    /// </summary>
    // ----------------------------------------------------------
    public void OnRetry()
    {
        // 現在のステータスを基本ステータスとして設定.
        CurrentStatus.Hp = DefaultStatus.Hp;
        CurrentStatus.Power = DefaultStatus.Power;
        hpBar.value = CurrentStatus.Hp;
        // 開始時の位置回転を保管.
        this.transform.position = startPosition;
        this.transform.rotation = startRotation;
 
        //敵を再度表示
        this.gameObject.SetActive(true);
    }
}