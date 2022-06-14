using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dongle : MonoBehaviour
{
    public GameManager manager;
    public ParticleSystem effect;
    public int level;

    bool isDrag;
    bool isMerge;
    bool isAttach;
    public float deadTime;

    Rigidbody2D rigid;
    Animator anim;
    SpriteRenderer spriteRenderer;
    CircleCollider2D circleCollider;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
    }   

    void OnEnable()
    {
        // 레벨 애니메이션 실행
        anim.SetInteger("Level",level);
    }
    void Update()
    {
        // 드래그 상태일 때만 마우스 x 축 따라가기
        if (isDrag)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // X축 최대 값 적용
            float LeftBorder = -8.8f + transform.localScale.x / 2f;
            float RightBorder = 8.8f- transform.localScale.x / 2f;

            if(mousePos.x < LeftBorder)
            {
                mousePos.x = LeftBorder;
            }
            else if( mousePos.x > RightBorder)
            {
                mousePos.x = RightBorder;
            }
            mousePos.y = 4;
            mousePos.z = 0;

            transform.position = Vector3.Lerp(transform.position, mousePos, 0.1f);
        }
        
    }
    public void Drag()
    {
        // 드래그 플래그 on
        isDrag = true;
    }
    public void Drop()
    {
        // 드래그 플래그 off + 물리효과 on
        isDrag = false;
        rigid.simulated = true;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        StartCoroutine("AttachRoutine");
    }

    IEnumerator AttachRoutine()
    {
        if (isAttach)
            yield break;
        isAttach = true;
        manager.PlaySfx(GameManager.Sfx.Attach);

        yield return new WaitForSeconds(0.2f);
        isAttach = false;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // 충돌 상대편이 동글이면...
        if(collision.gameObject.tag == "Dongle")
        {
            Dongle other = collision.gameObject.GetComponent<Dongle>();
            // 조건 비교 ( 같은 레벨인가 + 지금 합쳐지는 중이 아닌가 + 만렙이 아닌가)
            if(level == other.level && !isMerge && !other.isMerge && level < 7)
            {
                // 나와 상대편 위치 가져오기
                float meX = transform.position.x;
                float meY = transform.position.y;
                float otherX = other.transform.position.x;
                float otherY = other.transform.position.y;
                // 내가 위에 있거나 또는 같은 높이지만 오른쪽에 있을 때
                if (meY < otherY || (meY == otherY && meX > otherX))
                {
                    other.Hide(transform.position);
                    LevelUp();
                }
            }
            else if(other.level == 7 && level == 7)
            {
                Hide(transform.position);
                other.Hide(transform.position);
                manager.PlaySfx(GameManager.Sfx.LevelUp);
                EffectPlay();
            }
        }
    }
    void LevelUp()
    {
        // 잠금 On
        isMerge = true;
        // 물리 속도 초기화
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;

        StartCoroutine("LevelUpRoutine");
    }
    IEnumerator LevelUpRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        // 레벨업 애니매이션
        anim.SetInteger("Level", level+1);
        manager.PlaySfx(GameManager.Sfx.LevelUp);
        EffectPlay();

        yield return new WaitForSeconds(0.3f);
        //레벨업
        level++;
        // 최대 레벨 갱신
        manager.maxLevel = Mathf.Max(level, manager.maxLevel);
        // 잠금 OFF
        isMerge = false;
    }
    public void Hide(Vector3 targetPos)
    {
        // 잠금 ON
        isMerge = true;
        // 물리 효과 off
        rigid.simulated = false;
        circleCollider.enabled = false;

        StartCoroutine("HideRoutine", targetPos);

        // 게임 오버일 때는 이펙트 실행
        if(targetPos == Vector3.up * 100)
        {
            EffectPlay();
        }
    }

    IEnumerator HideRoutine(Vector3 targetPos)
    {
        int timeCount = 0;

        while (timeCount < 20)
        {
            timeCount++;
            // 상대가 있을 때
            if(targetPos != Vector3.up * 100)
            {
            transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);
            yield return null;
            }
            // 게임 오버일 때
            else if (targetPos == Vector3.up * 100)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);
            }
            yield return null;
        }
        // 점수 증가
        manager.score += (int) Mathf.Pow(2, level);
         
        // 비활성화
        gameObject.SetActive(false);
        // 잠금 OFF
        isMerge = false;
    }
    

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Finish")
        {
            // 데드타임 증가
            deadTime += Time.deltaTime;
            // 2초 지나면 색상 변경으로 경고
            if (deadTime > 2)
            {
                spriteRenderer.color = new Color(0.9f, 0.2f, 0.2f);

            }
            // 5초 지나면 게임 오버
            if (deadTime > 5)
            {
                manager.Result();
            }
        }
    }
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Finish")
        {
            // 데드 타임 및 색상 초기화
            deadTime = 0;
            spriteRenderer.color = Color.white;
        }

    }

    void OnDisable()
    {
        // 동글 속성 초기화
        level = 0;
        deadTime = 0;

        // 동글 트랜스폼 초기화
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 동글 물리 초기화
        rigid.simulated = false;
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;
        circleCollider.enabled = true;
    }

    void EffectPlay()
    {
        // 파티클 위치와 크기 설정
        effect.transform.position = transform.position;
        effect.transform.localScale = transform.localScale;
        // 파티클 플레이
        effect.Play();
    }
}
