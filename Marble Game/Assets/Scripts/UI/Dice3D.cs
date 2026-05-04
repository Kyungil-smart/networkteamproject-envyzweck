using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Dice3D : MonoBehaviour
{
    public event System.Action<int> OnRollComplete;

    [Header("물리 설정")]
    [SerializeField] private float throwForce = 7f;    
    [SerializeField] private float torqueForce = 15f;  
    [SerializeField] private float baseY = 5f;   

    private Rigidbody rb;
    private bool isCheckingStop = false;
    private Vector3 initialScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialScale = transform.localScale;
        
        rb.isKinematic = true;
    }

    /// <summary>
    /// DiceRoller에서 호출하는 주사위 던지기 시작 함수
    /// </summary>
    public void StartRoll()
    {
        // 이미 체크 중인 코루틴이 있다면 중단 (중복 방지)
        StopAllCoroutines();
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        rb.isKinematic = true;
        
        // 중앙 부근 랜덤 위치
        Vector3 startPos = new Vector3(Random.Range(-0.5f, 0.5f), baseY, Random.Range(-0.5f, 0.5f));
        transform.position = startPos;
        
        // 랜덤 회전값
        transform.rotation = Random.rotation;

        // 프레임 대기
        yield return new WaitForFixedUpdate();

        rb.isKinematic = false;
        rb.WakeUp();
        
        // 이전 턴의 물리 제거
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 forceDirection = new Vector3(Random.Range(-1f, 1f), -1f, Random.Range(-1f, 1f)).normalized;
        rb.AddForce(forceDirection * throwForce, ForceMode.Impulse);
        
        // 랜덤 토크
        Vector3 randomTorque = new Vector3(Random.Range(0.8f, 1f), Random.Range(0.8f, 1f), Random.Range(0.8f, 1f));
        rb.AddTorque(randomTorque * torqueForce, ForceMode.Impulse);

        // 멈춤 감지
        yield return new WaitForSeconds(1.0f);
        StartCoroutine(CheckStopRoutine());
    }

    private IEnumerator CheckStopRoutine()
    {
        isCheckingStop = true;
        
        while (isCheckingStop)
        {
            // 속도와 회전이 0에 가까워지면 0으로 간주 _ 계속 모서리로 주사위가 멈춰서 더욱 미세하게 조정함
            if (rb.linearVelocity.sqrMagnitude < 0.0001f && rb.angularVelocity.sqrMagnitude < 0.0001f)
            {
                isCheckingStop = false;
                rb.isKinematic = true; 
                
                int result = GetDiceResult();
                Debug.Log($"[Dice3D] {gameObject.name} 결과: {result}");
                OnRollComplete?.Invoke(result);
            }
            // 0.2초 간격으로 체크
            yield return new WaitForSeconds(0.2f); 
        }
    }

    /// <summary>
    /// 주사위의 윗면 계산
    /// </summary>
    private int GetDiceResult()
    {
        // 각 면의 로컬 방향 벡터 정의 (표준 주사위 기준)
        Vector3[] sideVectors = new Vector3[]
        {
            Vector3.forward,     
            Vector3.back,   
            Vector3.left,   
            Vector3.right,  
            Vector3.up,
            Vector3.down    
        };

        int[] sideValues = { 1, 6, 3, 4, 2, 5 };

        float maxDot = -1f;
        int result = 1;

        for (int i = 0; i < sideVectors.Length; i++)
        {
            // 위쪽 방향과 가장 일치하는 면을 찾음
            float dot = Vector3.Dot(transform.TransformDirection(sideVectors[i]), Vector3.up);
            if (dot > maxDot)
            {
                maxDot = dot;
                result = sideValues[i];
            }
        }
        return result;
    }
}