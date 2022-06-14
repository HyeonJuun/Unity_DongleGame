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
        // ���� �ִϸ��̼� ����
        anim.SetInteger("Level",level);
    }
    void Update()
    {
        // �巡�� ������ ���� ���콺 x �� ���󰡱�
        if (isDrag)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // X�� �ִ� �� ����
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
        // �巡�� �÷��� on
        isDrag = true;
    }
    public void Drop()
    {
        // �巡�� �÷��� off + ����ȿ�� on
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
        // �浹 ������� �����̸�...
        if(collision.gameObject.tag == "Dongle")
        {
            Dongle other = collision.gameObject.GetComponent<Dongle>();
            // ���� �� ( ���� �����ΰ� + ���� �������� ���� �ƴѰ� + ������ �ƴѰ�)
            if(level == other.level && !isMerge && !other.isMerge && level < 7)
            {
                // ���� ����� ��ġ ��������
                float meX = transform.position.x;
                float meY = transform.position.y;
                float otherX = other.transform.position.x;
                float otherY = other.transform.position.y;
                // ���� ���� �ְų� �Ǵ� ���� �������� �����ʿ� ���� ��
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
        // ��� On
        isMerge = true;
        // ���� �ӵ� �ʱ�ȭ
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;

        StartCoroutine("LevelUpRoutine");
    }
    IEnumerator LevelUpRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        // ������ �ִϸ��̼�
        anim.SetInteger("Level", level+1);
        manager.PlaySfx(GameManager.Sfx.LevelUp);
        EffectPlay();

        yield return new WaitForSeconds(0.3f);
        //������
        level++;
        // �ִ� ���� ����
        manager.maxLevel = Mathf.Max(level, manager.maxLevel);
        // ��� OFF
        isMerge = false;
    }
    public void Hide(Vector3 targetPos)
    {
        // ��� ON
        isMerge = true;
        // ���� ȿ�� off
        rigid.simulated = false;
        circleCollider.enabled = false;

        StartCoroutine("HideRoutine", targetPos);

        // ���� ������ ���� ����Ʈ ����
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
            // ��밡 ���� ��
            if(targetPos != Vector3.up * 100)
            {
            transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);
            yield return null;
            }
            // ���� ������ ��
            else if (targetPos == Vector3.up * 100)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);
            }
            yield return null;
        }
        // ���� ����
        manager.score += (int) Mathf.Pow(2, level);
         
        // ��Ȱ��ȭ
        gameObject.SetActive(false);
        // ��� OFF
        isMerge = false;
    }
    

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Finish")
        {
            // ����Ÿ�� ����
            deadTime += Time.deltaTime;
            // 2�� ������ ���� �������� ���
            if (deadTime > 2)
            {
                spriteRenderer.color = new Color(0.9f, 0.2f, 0.2f);

            }
            // 5�� ������ ���� ����
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
            // ���� Ÿ�� �� ���� �ʱ�ȭ
            deadTime = 0;
            spriteRenderer.color = Color.white;
        }

    }

    void OnDisable()
    {
        // ���� �Ӽ� �ʱ�ȭ
        level = 0;
        deadTime = 0;

        // ���� Ʈ������ �ʱ�ȭ
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // ���� ���� �ʱ�ȭ
        rigid.simulated = false;
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;
        circleCollider.enabled = true;
    }

    void EffectPlay()
    {
        // ��ƼŬ ��ġ�� ũ�� ����
        effect.transform.position = transform.position;
        effect.transform.localScale = transform.localScale;
        // ��ƼŬ �÷���
        effect.Play();
    }
}
