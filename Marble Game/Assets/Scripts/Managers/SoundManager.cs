using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 게임 전체 사운드 관리 싱글톤
/// - BGM: 페이드 인/아웃 지원
/// - SFX: 풀링 방식으로 효율적 재생 (중복 생성 방지 및 최적화)
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // ── BGM ──────────────────────────────────
    [Header("BGM")]
    public AudioClip bgmMain;       // 메인 게임 BGM
    public AudioClip bgmTitle;      // 타이틀 BGM
    public AudioClip bgmResult;     // 결과 화면 BGM
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    public float bgmFadeDuration = 1.0f;

    // ── SFX 클립 ─────────────────────────────
    [Header("SFX - 주사위")]
    public AudioClip sfxDiceRoll;   // 주사위 굴리기
    public AudioClip sfxDiceLand;   // 주사위 착지

    [Header("SFX - 말 이동")]
    public AudioClip sfxPawnMove;   // 말 한 칸 이동
    public AudioClip sfxPawnLand;   // 말 착지

    [Header("SFX - 돈")]
    public AudioClip sfxMoneyGet;   // 돈 입금
    public AudioClip sfxMoneyPay;   // 돈 출금
    public AudioClip sfxBankrupt;   // 파산

    [Header("SFX - 이벤트")]
    public AudioClip sfxBuyProperty;   // 땅 구매
    public AudioClip sfxBuildHouse;    // 건물 건설
    public AudioClip sfxEventCard;     // 이벤트 카드
    public AudioClip sfxJail;          // 감옥
    public AudioClip sfxButtonClick;   // UI 버튼

    [Header("SFX - 게임")]
    public AudioClip sfxGameStart;     // 게임 시작
    public AudioClip sfxGameOver;      // 게임 종료 (승리 팡파레)
    public AudioClip sfxTurnChange;    // 턴 변경

    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    // ── 풀링 ─────────────────────────────────
    [Header("Pool Config")]
    [SerializeField] int sfxPoolSize = 10; // 동시 재생 가능한 효과음 개수

    private AudioSource _bgmSource;
    private Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
    private Coroutine _bgmFadeCoroutine;

    void Awake()
    {
        // 싱글톤 및 파괴 방지 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitPool();
    }

    void Start()
    {
        Debug.Log("사운드 매니저 시작: BGM 재생 시도");
        PlayMainBGM();
    }

    // ── 풀 초기화 ────────────────────────────
    void InitPool()
    {
        // BGM 전용 소스 추가
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume = bgmVolume;

        // SFX 풀 생성
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            _sfxPool.Enqueue(src);
        }
    }

    // ════════════════════════════════════════
    // BGM 제어 로직
    // ════════════════════════════════════════

    public void PlayBGM(AudioClip clip, bool fade = true)
    {
        if (clip == null) return;
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
        
        if (fade)
            _bgmFadeCoroutine = StartCoroutine(FadeBGM(clip));
        else
            CrossBGMImmediate(clip);
    }

    public void StopBGM(bool fade = true)
    {
        if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
        
        if (fade) 
            _bgmFadeCoroutine = StartCoroutine(FadeOut(_bgmSource, bgmFadeDuration));
        else 
            _bgmSource.Stop();
    }

    private void CrossBGMImmediate(AudioClip clip)
    {
        _bgmSource.Stop();
        _bgmSource.clip = clip;
        _bgmSource.volume = bgmVolume;
        _bgmSource.Play();
    }

    IEnumerator FadeBGM(AudioClip clip)
    {
        // 페이드 아웃
        if (_bgmSource.isPlaying)
        {
            yield return FadeOut(_bgmSource, bgmFadeDuration * 0.5f);
        }

        _bgmSource.Stop();
        _bgmSource.clip = clip;
        _bgmSource.Play();

        // 페이드 인
        yield return FadeIn(_bgmSource, bgmVolume, bgmFadeDuration * 0.5f);
    }

    IEnumerator FadeOut(AudioSource src, float duration)
    {
        float startVol = src.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            src.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        src.volume = 0f;
    }

    IEnumerator FadeIn(AudioSource src, float target, float duration)
    {
        src.volume = 0f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            src.volume = Mathf.Lerp(0f, target, t / duration);
            yield return null;
        }
        src.volume = target;
    }

    // ════════════════════════════════════════
    // SFX 제어 로직 (풀링 방식)
    // ════════════════════════════════════════

    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource src = GetPooledSource();
        src.clip = clip;
        src.volume = sfxVolume * volumeScale;
        src.pitch = pitch;
        src.Play();
    }

    private AudioSource GetPooledSource()
    {
        // 1. 현재 재생 중이지 않은 소스 찾기
        for (int i = 0; i < _sfxPool.Count; i++)
        {
            AudioSource src = _sfxPool.Dequeue();
            _sfxPool.Enqueue(src); // 다시 뒤로 보냄

            if (!src.isPlaying)
            {
                return src;
            }
        }

        // 2. 모든 소스가 사용 중이라면 가장 오래된 것(맨 앞)을 강제 정지 후 사용
        AudioSource oldest = _sfxPool.Dequeue();
        oldest.Stop();
        _sfxPool.Enqueue(oldest);
        return oldest;
    }

    // ════════════════════════════════════════
    // 편의 메서드 (외부 호출용)
    // ════════════════════════════════════════

    // 주사위 관련
    public void PlayDiceRoll()   => PlaySFX(sfxDiceRoll, 1.0f, Random.Range(0.95f, 1.05f));
    public void PlayDiceLand()   => PlaySFX(sfxDiceLand, 1.0f, Random.Range(0.9f, 1.1f));

    // 이동 관련
    public void PlayPawnMove()   => PlaySFX(sfxPawnMove, 0.7f, Random.Range(0.9f, 1.15f));
    public void PlayPawnLand()   => PlaySFX(sfxPawnLand, 1.0f, Random.Range(0.95f, 1.05f));

    // 경제 관련
    public void PlayMoneyGet()   => PlaySFX(sfxMoneyGet);
    public void PlayMoneyPay()   => PlaySFX(sfxMoneyPay);
    public void PlayBankrupt()   => PlaySFX(sfxBankrupt);

    // 이벤트/시스템 관련
    public void PlayBuyProperty() => PlaySFX(sfxBuyProperty);
    public void PlayBuildHouse()  => PlaySFX(sfxBuildHouse);
    public void PlayEventCard()   => PlaySFX(sfxEventCard);
    public void PlayJail()        => PlaySFX(sfxJail);
    public void PlayButtonClick() => PlaySFX(sfxButtonClick, 0.6f);
    public void PlayGameStart()   => PlaySFX(sfxGameStart);
    public void PlayGameOver()    => PlaySFX(sfxGameOver);
    public void PlayTurnChange()  => PlaySFX(sfxTurnChange, 0.5f);

    // BGM 호출 편의 기능
    public void PlayMainBGM()     => PlayBGM(bgmMain);
    public void PlayTitleBGM()    => PlayBGM(bgmTitle);
    public void PlayResultBGM()   => PlayBGM(bgmResult);

    // 볼륨 조절용 (옵션창 UI 등에서 활용)
    public void SetBGMVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        _bgmSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
    }
}