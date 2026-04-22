//attach of 'NPCManager'
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System.Data;
using static UnityEngine.ParticleSystem;

public class NPCManager : MonoBehaviour
{
    //MakeNPC를 static public MakeNPC 해서 사용할 수 있게 할 것인지 고민해볼 것.
    static public NPCManager N;

    //npc_prefab    : 생성한 프리팹들을 여기에 모두 모아둠.
    //playerObj     : 플레이어 캐릭터를 잠시 여기서 관리하는 중...
    [Header("Plz Setting")]
    public GameObject door;
    public GameObject[] npc_prefab;
    public GameObject[] npc_regular_prefab;
    public GameObject speechBubble;
    public GameObject sceneChangeButton;
    public GameObject playerObj;

    //curCount      : 현재 맵에 있는 NPC의 수
    //entireCount   : 오늘 다녀온 NPC의 수
    //todayCount    : 최종 NPC의 수(계속 조절)('오늘 몇 명의 손님을 받을 수 있게 할 것인가'를 말함)
    //returning     : 돌아간 NPC의 수
    private int npc_curCount = 0;
    private int npc_entireCount = 0;
    private int npc_todayCount = 5;
    private int npc_returning = 0;

    //na = npc address / nra = npc regular address
    //'어느 NPC 프리팹을 쓸 것인가'를 설정하기 위해 지정.
    //Rating 쪽에서도 잠깐 이용됩니다.
    int na_random;
    int nra_random;

    //enableSpawn : 지금 NPC를 스폰할 수 있는 상태인가?
    bool enableSpawn = false;
    bool isLastGuest = true;
    public bool SetEnableSpawn(bool i) { return enableSpawn = i; }
    
    //isTrigger : 현재 NPC가 Player와 상호작용하는 중임을 나타냄. true면 상호작용중, false면 상호작용하지 않음.
    //npc   : 맵에 있는 모든 NPC들.
    GameObject interNPC;
    LinkedList<GameObject> npc = new LinkedList<GameObject>();
    bool isTrigger = false;

    //GameManager에서 정산이 끝나면 false로, NPC가 모두 나간 상태라면(하루가 끝났다면) true로.
    //2026: GameManager에서 관리되는 건 아니고, GameManager에서 다음 날로 넘어갈 때, false를 받게 된다. true가 되는 시점은 여기서 관리.
    bool calculating = true;

    //단골손님용
    //Intimacy  : 친밀도 값 저장
    //Stay      : 가게에 남아있으면 true, 나가면 false.
    int[] regularIntimacy;
    bool[] regularStay;

    //2026: 본 iceImg는 본인이 설정한 부분이 아니다. 아마 '주문을 받는 시점에서 얼음이 들어간 음료를 필요로 할 때' 이용하려고 붙여둔 것으로 보인다.
    public GameObject iceImg;
    //효과음 출력 관련
    AudioSource audioSoure;

    private void Awake()
    {
        if (N == null)
        {
            N = this;
        }
        else
        {
            Debug.LogError("<color=red>NPCManager.Awake()</color> - attempted to assign second NPCManager.N!");
        }
    }

    private void Start()
    {
        //설정된 프리팹 값만큼 친밀도 값 설정.
        regularIntimacy = new int[npc_regular_prefab.Length];
        regularStay = new bool[npc_regular_prefab.Length];

        audioSoure = GetComponent<AudioSource>();
    }

    void Update()
    {
        //이후 NPC의 맥시멈 수를 이용하여 스폰 제한을 할 것.
        if (npc_entireCount >= npc_todayCount)
        {
            //오늘 받을 수 있는 최대 인원 수에 도달했을 때.
            //2026: 오늘 받을 수 있는 최대 인원 수에 도달했을 때, 현재 들어온 손님이 '마지막 손님'인 것을 명시하는 것.
            enableSpawn = false;
            timerOn = false;
            LastGuest();

            //다음 날로 넘어갈 수 있도록 조절.
            //NPC가 모두 퇴장한 시점이어야만 함... 현재는 허술하게 만들어진 상태. 추후 보강해야 함.
            //오늘 다녀온 손님의 수와 최대 손님 수가 같고, 현재 가게에 손님이 없으며, 손님이 완전히 나갔을 때. (밖으로 나갈 때 게임 오브젝트가 삭제되므로)
            if (npc_entireCount == npc_todayCount && npc_curCount == 0 && interNPC == null)
            {
                Calculate();
            }
        }
        else if (npc_entireCount < npc_todayCount)
        {
            if (enableSpawn == true)
            {
                if (isTrigger == false)
                {
                    SpawnNPC();
                    enableSpawn = false;
                }
            }
        }

        Timer();
        
        if (isTrigger == true)
        {
            if (!playerObj.GetComponent<Animator>().GetBool("ShakeIt"))
                playerObj.GetComponent<AudioSource>().Play();
            playerObj.GetComponent<Animator>().SetBool("ShakeIt", true);
        }
        else if (isTrigger == false)
        {
            playerObj.GetComponent<Animator>().SetBool("ShakeIt", false);
            playerObj.GetComponent<AudioSource>().Stop();
        }
    }

    public float timer_MAX = 10f;
    float timer = 5f;
    bool timerOn = false;

    //2026: 손님이 들어오는 주기를 관리하는 함수. timerOn이 true일 때만 시간이 흐른다.
    //2026: timerOn을 true로 만드는 시점은 OrderReceipt()에서 관리된다.
    void Timer()
    {
        if (timerOn == false) { return; }

        if (npc_curCount <= npc_todayCount)
        {
            timer -= Time.deltaTime;
        }
        else if (npc_curCount == npc_todayCount)
        {
            enableSpawn = false;
            timer = timer_MAX;
        }

        if (timer <= 0)
        {
            enableSpawn = true;
            timer = timer_MAX;
        }
    }

    //2026: 버튼에 대입하여 쓰던 디버그용 함수. 지금은 쓰지 않는다. 
    public void SpawnButton()
    {
        timerOn = true;
    }


    void SpawnNPC()
    {
        audioSoure.Play();

        //추후 na_random 할 때, 0~6까지 읽어들이게끔 하여 6번이 나오면 단골 손님이 스폰될 수 있도록 조정할 것.
        //2026: 확률이 난해한 쪽에 속한다고 생각한다. 0~100까지의 수를 받아, 그 중에 20%만 단골 손님이 나오게끔 하는 방법도 있었을 것으로 보인다.
        //2026: 본래 있던 코드를 수정한 상태. 가지고 있는 prefab의 수를 넘어가는 값을 받거나, 나올 수 없는 수에 접근하여 값을 관리하는 상황이었다.
        na_random = (int)UnityEngine.Random.Range(0, npc_prefab.Length);
        
        if (na_random != npc_prefab.Length - 1)
        {
            GameObject newNpc = (GameObject)Instantiate(npc_prefab[na_random],
                                                    new Vector3(door.transform.position.x, door.transform.position.y - 0.5f, 0),
                                                    Quaternion.identity);
            npc.AddLast(newNpc);
        }
        else if (na_random == npc_prefab.Length - 1)
        {
            nra_random = (int)UnityEngine.Random.Range(0, npc_regular_prefab.Length);

            if (nra_random == 4 && GameManager.GM.GetFame() >= 700 && regularStay[4] == false)
            {
                //단골손님, '장군'을 포함
                //전체 단골 손님을 스폰할 수 있음.
                GameObject newRegular = (GameObject)Instantiate(npc_regular_prefab[nra_random],
                                    new Vector3(door.transform.position.x, door.transform.position.y - 0.5f, 0),
                                    Quaternion.identity);

                regularStay[4] = true;
                npc.AddLast(newRegular);
            }
            else if (nra_random == 3 && GameManager.GM.GetFame() >= 400 && regularStay[3] == false)
            {
                //단골손님, '레오'를 포함.
                //'장군'을 제외한 모든 단골 손님을 스폰할 수 있음.
                GameObject newRegular = (GameObject)Instantiate(npc_regular_prefab[nra_random],
                                    new Vector3(door.transform.position.x, door.transform.position.y - 0.5f, 0),
                                    Quaternion.identity);

                regularStay[3] = true;
                npc.AddLast(newRegular);
            }
            else if (nra_random == 2 && GameManager.GM.GetFame() >= 300 && regularStay[2] == false)
            {
                //단골손님, '쿠키'를 포함.
                //'초코', '핫독', '쿠키'를 스폰할 수 있음.
                GameObject newRegular = (GameObject)Instantiate(npc_regular_prefab[nra_random],
                                    new Vector3(door.transform.position.x, door.transform.position.y - 0.5f, 0),
                                    Quaternion.identity);

                regularStay[2] = true;
                npc.AddLast(newRegular);
            }
            else if (nra_random == 1 && GameManager.GM.GetFame() >= 150 && regularStay[1] == false)
            {
                //단골손님, '핫독'을 포함.
                //'초코', '핫독'만을 스폰할 수 있음.
                GameObject newRegular = (GameObject)Instantiate(npc_regular_prefab[nra_random],
                                    new Vector3(door.transform.position.x, door.transform.position.y - 0.5f, 0),
                                    Quaternion.identity);

                regularStay[1] = true;
                npc.AddLast(newRegular);
            }
            else if (nra_random == 0 && GameManager.GM.GetFame() >= 30 && regularStay[0] == false)
            {
                //단골손님, '초코'를 스폰.
                GameObject newRegular = (GameObject)Instantiate(npc_regular_prefab[0],
                                                        new Vector3(door.transform.position.x, door.transform.position.y - 0.5f, 0),
                                                        Quaternion.identity);

                regularStay[0] = true;
                npc.AddLast(newRegular);
            }
            else
            {
                //만족도가 아직 30을 넘지 않을 경우, 혹은 소환하려는 NPC의 Stay 값이 true인 경우
                //다시 랜덤값을 산정해서 NPC 소환.
                na_random = (int)UnityEngine.Random.Range(0, npc_prefab.Length);

                GameObject newNpc = (GameObject)Instantiate(npc_prefab[na_random],
                                    new Vector3(door.transform.position.x, door.transform.position.y - 0.5f, 0),
                                    Quaternion.identity);

                npc.AddLast(newNpc);
            }
        }

        npc_curCount++;
        npc_entireCount++;
    }

    //주문을 접수하는 코드. 대화창에서 'Yes'를 눌렀을 시점.
    //2026: 게임의 구조가 변경됨에 따라 본 함수는 '제출 버튼'의 용도로 이용된다.
    public void OrderReceipt()
    {
        //주문 접수. 대화창에서 'Yes' 버튼을 눌렀을 때.
        //씬변환을 설치한다면 여기에 설치하는게 좋을 것 같음.
        speechBubble.SetActive(false);
        iceImg.SetActive(false);
        interNPC.GetComponent<NPC_AI>().SetIsOrder(true);
        //아래의 두 줄은 나중에 위치 변경(술을 주는 당시)할 것!

        //GameManager.GM.SetCoin(100);

        //2026: 아래의 두 if문은 본인이 만든 부분이 아닌 것으로 보인다.
        if (interNPC.GetComponent<NPC_AI>().luna == false)
        {
            timerOn = true;
            isTrigger = false;
        }
        else if (interNPC.GetComponent<NPC_AI>().choco == true)
        {
            timerOn = true;
            isTrigger = false;
        }

    }

    //SpawnNPC에 있던 랜덤 주문 코드 이전.

    //2026: Calculate()와 LastGuset() 사이의 순서가 다소 헷갈림.
    void Calculate()
    {
        //2026: 두 번 실행되지 않도록 한 것. Calculate() 함수를 지나가야 'NPO가 모두 나갔다'라고 명시된다.
        if (calculating == true) { return; }
        calculating = true;
        isLastGuest = true;

        GameManager.GM.Calculate();

        //나중에 GamaManager로 오늘 온 손님과 떠난 손님을 전달할 수 있도록 할 것.
        //2026: GameManager에서 NPCManager에 있는 값을 Get...하여 전달받는 중.
        //현재는 그냥 초기화.
        npc_entireCount = 0;

        //일단 종료시킴. 재시작시키려면 스폰 버튼을 누르면 됨.
        timerOn = false;
    }

    public void LastGuest()
    {
        //2026: 호출 당시, '현재 마지막 손님이 들어왔다'라는 명시가 없으면 함수는 실행되지 않는다.
        //2026: isLastGuest를 true로 만드는 시점은 Calculation() 함수가 제대로 실행되는 시점이다.
        //2026: isLastGuest는 게임 내내 true 값으로 동작하다가, 해당 함수를 실행하고 잠시 false로 전환된다. (두 번 실행되지 않기 위해서) 이후 Calcutate()에서 다시 true로 변경된다.
        //2026: 최하단에 isLastsGuest를 false로 바꾸는 코드가 있는데, (두 번 실행되지 않기 위해서) 그럴 거면 해당 변수 이름을 LastGuestTimerSetting 정도로 해도 괜찮지 않았을까 싶다.
        //2026: isLastGuest가 게임 내내 true로 동작해도 괜찮은 이유는 해당 함수 실행 조건 자체가 '받아야 하는 손님을 모두 받았을 때'이기 때문. 정산하며 해당 값이 모두 초기화되기 때문에 true로 진행되어도 문제 없다.
        if (isLastGuest == false) { return; }

        Debug.Log("확인");

        var _npc = npc.GetEnumerator();
        float random;

        //2026: 마지막 손님이 들어왔을 때, 이미 들어와서 의자에 앉아있던 손님들의 대기 시간을 줄인다.
        //2026: 마지막 손님이 타 손님들과 비슷한 시각에 나가게 하기 위함이다.
        for (int i = 0; i < npc.Count; i++)
        {
            random = UnityEngine.Random.Range(0, 3);
            _npc.MoveNext();

            if (_npc.Current.GetComponent<NPC_AI>().GetStandTimer() > 10)
            {
                random += 5;
                Debug.Log(_npc.Current.GetComponent<NPC_AI>().GetStandTimer());
            }

            _npc.Current.GetComponent<NPC_AI>().SetStandTimer(random);
        }

        Debug.Log(npc.Count);
        isLastGuest = false;
    }

    //아래로 get, set
    public int GetNPCCount() { return npc_curCount; }
    public void NPCCountDeduction() { npc_curCount--; }

    public int GetEntireNPCCount() { return npc_entireCount; }

    public void SetNPCReturning(int i) { npc_returning = i; }
    public int GetNPCReturning() { return npc_returning; }

    public void SetInterNPC(GameObject obj) { interNPC = obj; }

    public void SetisTrigger(bool l) { isTrigger = l; }
    public bool GetisTrigger() { return isTrigger; }

    public bool GetTimerOn() { return timerOn; }
    public void SetTimerOn(bool b) { timerOn = b; }

    public bool GetNPCManagerCalculating() { return calculating; }
    public void SetNPCManagerCalculating(bool b) { calculating = b; }

    public int GetNPCAddress() { return na_random; }
    public int GetRegularNPCAddress() { return nra_random; }

    //NPC_AI에 단골 손님 Address를 함께 전달하여 둘 것.
    public void SetRegularStay(int regularNum, bool b) { regularStay[regularNum] = b; }

    public int GetRegularNPCIntimacy(int regularNum) { return regularIntimacy[regularNum]; }
    public void SetRegularNPCIntimacy(int regularNum, int val) { regularIntimacy[regularNum] += val; }

    public int GetRegularNPCNum() { return npc_regular_prefab.Length; }

    public void DestroyNPC(GameObject obj)
    {
        var _npc = npc.GetEnumerator();

        for (int i = 0; i < npc.Count; i++)
        {
            _npc.MoveNext();

            if (_npc.Current.gameObject == obj)
            {
                npc.Remove(_npc.Current);
                return;
            }
        }
    }
}
