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
        // 프레임 설정 (FPS 60)
        Application.targetFrameRate = 60;
        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();
        
        // 오브젝트 풀 시작
        for(int index=0; index < poolSize; index++)
        {
            MakeDongle(index);
        }

        // 최대 점수 설정
        if (!PlayerPrefs.HasKey("MaxScore"))
        {
            PlayerPrefs.SetInt("MaxScore", 0);
        }

        MaxscoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();
    }
    Dongle MakeDongle(int id)
    {
        // 새로운 이펙트 생성 + 풀 저장
        GameObject instantEffect = Instantiate(effectPrefab, effectGroup);
        instantEffect.name = "Effect " + id;
        ParticleSystem instantEffectParticle = instantEffect.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffectParticle);

        // 새로운 동글 생성(생성 -> 레벨설정 -> 활성화) + 풀 저장
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
        // UI 컨트롤
        startGroup.SetActive(false);
        line.SetActive(true);
        floor.SetActive(true);
        scoreText.gameObject.SetActive(true);
        MaxscoreText.gameObject.SetActive(true);

        // 효과음 
        PlaySfx(Sfx.Button);
        // BGM 시작
        bgmplayer.Play();
        // 동글 생성 시작
        Invoke("NextDongle", 1.5f);
    }

    void NextDongle()
    {
        if (isOver)
            return;

        // 다음 동글 가져오기
        lastDongle = GetDongle();
        lastDongle.manager = this;
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        // 다음 동글 생성 기다리는 코루틴
        StartCoroutine("WaitNext");

        // 효과음 출력
        PlaySfx(Sfx.Next);
    }

    IEnumerator WaitNext()
    {
        // 현재 동글이 드랍될 때까지 기다리기
        while (lastDongle != null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);
        // 다음 동글 생성 호출
        NextDongle();
    }
    public void TouchDown()
    {
        if(lastDongle == null)
            return;
       
        // 동글 드래그
        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;

        // 동글 드랍(변수 비우기)
        lastDongle.Drop();
        lastDongle = null;
    }
    public void Result()
    {
        // 게임 오버 및 결산
        isOver = true;
        bgmplayer.Stop();
        StartCoroutine("ResultRoutine");
    }

    IEnumerator ResultRoutine()
    {
        // 남아 있는 동글을 순차적으로 지우면서 결산
        for(int index=0; index < donglePool.Count; index++)
        {
            if(donglePool[index].gameObject.activeSelf)
            {
                donglePool[index].Hide(Vector3.up* 100);
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return new WaitForSeconds(1f);
        // 점수 적용
        subScoreText.text = "점수 : " + scoreText.text;

        // 최대 점수 갱신
        int maxScore = Mathf.Max(PlayerPrefs.GetInt("MaxScore"), score);
        PlayerPrefs.SetInt("MaxScore", maxScore);

        // UI 띄우기
        EndGroup.SetActive(true);

        // 효과음 출력
        PlaySfx(Sfx.Over);
    }
    public void Reset()
    {

        // 효과음 출력
        PlaySfx(Sfx.Button);
        StartCoroutine("ResetRoutine");

    }

    IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(1f);

        // 장면 다시 불러오기
        SceneManager.LoadScene("New Scene");
    }
    public void PlaySfx(Sfx type)
    {
        // SFX 플레이어 커서 이동
        sfxCursor = (sfxCursor + 1) % sfxplayers.Length;
        
        // 효과음 사운드 지정
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
