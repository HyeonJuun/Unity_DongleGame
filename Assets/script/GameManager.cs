 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("-------[ Core ]")]
    public int maxLevel;
    public bool isOver;
    public int score;

    [Header("-------[ Object Pooling ]")]
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public GameObject effectPrefab;
    public Transform effectGroup;
    [Range(1, 30)]
    public int poolSize;
    public List<Dongle> donglePool;
    public List<ParticleSystem> effectPool;
    Dongle lastDongle;
    int poolCursor;

    [Header("-------[ Audio ]")]
    public AudioSource bgmplayer;
    public AudioSource[] sfxplayers;
    public AudioClip[] sfxClips;
    int sfxCursor;

    [Header("-------[  UI ]")]
    public GameObject line;
    public GameObject floor;
    public GameObject startGroup;
    public GameObject EndGroup;
    public Text scoreText;
    public Text MaxscoreText;
    public Text subScoreText;


    public enum Sfx { LevelUp, Next, Attach, Button, Over};

    void Awake()
    {
        // ������ ���� (FPS 60)
        Application.targetFrameRate = 60;
        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();
        
        // ������Ʈ Ǯ ����
        for(int index=0; index < poolSize; index++)
        {
            MakeDongle(index);
        }

        // �ִ� ���� ����
        if (!PlayerPrefs.HasKey("MaxScore"))
        {
            PlayerPrefs.SetInt("MaxScore", 0);
        }

        MaxscoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();
    }
    Dongle MakeDongle(int id)
    {
        // ���ο� ����Ʈ ���� + Ǯ ����
        GameObject instantEffect = Instantiate(effectPrefab, effectGroup);
        instantEffect.name = "Effect " + id;
        ParticleSystem instantEffectParticle = instantEffect.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffectParticle);

        // ���ο� ���� ����(���� -> �������� -> Ȱ��ȭ) + Ǯ ����
        GameObject instantDongle = Instantiate(donglePrefab, dongleGroup);
        Dongle instantDongleLogic = instantDongle.GetComponent<Dongle>();
        instantDongle.name = "Dongle " + id;
        instantDongleLogic.manager = this;
        instantDongleLogic.effect = instantEffectParticle;
        donglePool.Add(instantDongleLogic);

        return instantDongleLogic;

    }

    Dongle GetDongle()
    {
        for(int index=0; index<donglePool.Count; index++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count;
            if (!donglePool[poolCursor].gameObject.activeSelf)
            {
                return donglePool[poolCursor];
            }
        }

        return MakeDongle(donglePool.Count);
    }

    public void GameStart()
    {
        // UI ��Ʈ��
        startGroup.SetActive(false);
        line.SetActive(true);
        floor.SetActive(true);
        scoreText.gameObject.SetActive(true);
        MaxscoreText.gameObject.SetActive(true);

        // ȿ���� 
        PlaySfx(Sfx.Button);
        // BGM ����
        bgmplayer.Play();
        // ���� ���� ����
        Invoke("NextDongle", 1.5f);
    }

    void NextDongle()
    {
        if (isOver)
            return;

        // ���� ���� ��������
        lastDongle = GetDongle();
        lastDongle.manager = this;
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        // ���� ���� ���� ��ٸ��� �ڷ�ƾ
        StartCoroutine("WaitNext");

        // ȿ���� ���
        PlaySfx(Sfx.Next);
    }

    IEnumerator WaitNext()
    {
        // ���� ������ ����� ������ ��ٸ���
        while (lastDongle != null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);
        // ���� ���� ���� ȣ��
        NextDongle();
    }
    public void TouchDown()
    {
        if(lastDongle == null)
            return;
       
        // ���� �巡��
        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;

        // ���� ���(���� ����)
        lastDongle.Drop();
        lastDongle = null;
    }
    public void Result()
    {
        // ���� ���� �� ���
        isOver = true;
        bgmplayer.Stop();
        StartCoroutine("ResultRoutine");
    }

    IEnumerator ResultRoutine()
    {
        // ���� �ִ� ������ ���������� ����鼭 ���
        for(int index=0; index < donglePool.Count; index++)
        {
            if(donglePool[index].gameObject.activeSelf)
            {
                donglePool[index].Hide(Vector3.up* 100);
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return new WaitForSeconds(1f);
        // ���� ����
        subScoreText.text = "���� : " + scoreText.text;

        // �ִ� ���� ����
        int maxScore = Mathf.Max(PlayerPrefs.GetInt("MaxScore"), score);
        PlayerPrefs.SetInt("MaxScore", maxScore);

        // UI ����
        EndGroup.SetActive(true);

        // ȿ���� ���
        PlaySfx(Sfx.Over);
    }
    public void Reset()
    {

        // ȿ���� ���
        PlaySfx(Sfx.Button);
        StartCoroutine("ResetRoutine");

    }

    IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(1f);

        // ��� �ٽ� �ҷ�����
        SceneManager.LoadScene("New Scene");
    }
    public void PlaySfx(Sfx type)
    {
        // SFX �÷��̾� Ŀ�� �̵�
        sfxCursor = (sfxCursor + 1) % sfxplayers.Length;
        
        // ȿ���� ���� ����
        switch (type)
        {
            case Sfx.LevelUp:
                sfxplayers[sfxCursor].clip = sfxClips[Random.Range(0, 3)];
                break;
            case Sfx.Next:
                sfxplayers[sfxCursor].clip = sfxClips[3];
                break;
            case Sfx.Attach:
                sfxplayers[sfxCursor].clip = sfxClips[4];
                break;
            case Sfx.Button:
                sfxplayers[sfxCursor].clip = sfxClips[5];
                break;
            case Sfx.Over:
                sfxplayers[sfxCursor].clip = sfxClips[6];
                break;
        }
        sfxplayers[sfxCursor].Play();
    }

    void LateUpdate()
    {
        scoreText.text = score.ToString();

    }
}
