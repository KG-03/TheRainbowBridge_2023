//attach of 'NPCPrefab'(Prefab)
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using TMPro;

//2026: NPC의 현재 상태. 배회하는지, 문에서 바로 이동하는지, 테이블로 이동하는지, 문으로 다시 나가는지, 기타 등등.
public enum NpcState
{
    nil,
    Wander,
    DoorToPlayer,
    PlayerToTable,
    TableToDoor,
    NoOrder
}

public class NPC_AI : MonoBehaviour
{
    //2026: 아래의 네 가지는 본인이 만든 것이 아니다. luna, choch, npcTrans, bubbleTrans와 관련된 코드가 나온다면, 타인의 작성이다.
    //2026: [정정] 튜토리얼 관련 함수는 본인이 만든 것이 맞다. luna가 어떤 행동을 하는지는 본인이 설계했다.
    //2026: 본 cs가 적용된 NPC prefab 중에서 본 prefab이 'luna'에 해당한다면 유니티 내에서 해당 bool 타입을 체크해준다.
    public bool luna;
    public bool choco;
    // 평가 대화 출력 관련
    public Transform npcTrans;
    public Transform bubbleTrans;

    //목적지 GameObject의 위치를 알기 위해 선언.
    //targetObj: 실제 이동해야 하는 오브젝트.
    //targetF : 실제 이용할 수 있는 의자.
    GameObject door;
    GameObject player;
    GameObject targetF;
    GameObject targetObj;

    Vector3 adj_doorPos;

    //NPC들의 각 상태를 지정.
    NpcState npcState;

    //NPC가 움직이는 것에 필요한 부분.
    //nSpeed        : NPC 움직이는 속도
    Animator npc_animator;
    public float nSpeed;
    Rigidbody2D nRid2D;

    //딜레이를 위해 만든 수.
    //2026: 행동이 바로 실행되지 않고 1초의 딜레이 후에 실행된다.
    public float nDelay_MAX = 1f;
    float nDelay;

    //해당 NPC가 카운터에서 주문중이라면 true.
    //2026: 주문이 완료되면 isOrder가 true로 변경된다.
    bool isOrder = false;
    bool orderComplete = false;

    //w_random : Wander에서 이용. 배회할 때 어떤 동작을 할지 결정.
    //wander_timer : 배회하는 시간
    int w_random;
    float wander_timer = 8f;

    //order_timer : 20초 동안 오더를 수락하는지 확인하기 위해 생성.
    //stand_timer : 30초 동안 앉아서 평가하는 중.
    public float order_timer = 20f;
    public float stand_timer = 30f;

    string directionF = "none";

    bool isRegular = false;
    int regularNum;

    //2026: 아래의 여섯 가지는 본인이 만든 게 아니다. funiturePrice, isShowingText, tmpText, rateStar, funitureRate, spawnPoint와 관련된 코드가 나온다면, 타인의 작성이다.
    // 가구평가 관련 
    public float funiturePrice;
    private bool isShowingText;
    public TextMeshProUGUI tmpText;
    public GameObject rateStar;
    int funitureRate = 0; // 가구점수

    public Vector3 spawnPoint = new Vector3(-480, 290, 0);

    void Start()
    {
        door = GameObject.Find("DoorPos");
        player = GameObject.Find("PlayerPos_Order");

        //2026: 문의 사이즈가 큰 편이기 때문에, 그보다 작은 NPC들이 '문으로 나간다'라는 느낌을 주기 위해 Y좌표가 조금 더 아래에 배치된다.
        adj_doorPos = new Vector3(door.transform.position.x, door.transform.position.y - 0.5f, door.transform.position.z);

        npc_animator = this.GetComponent<Animator>();
        nRid2D = GetComponent<Rigidbody2D>();
        NPCSpeedDecision(nSpeed);

        //시작하면서 어느 가구로 갈지 결정.
        //이때문에 의자에 가서 앉지 않았더라도 NPC가 나갈 때 FurnitureSet의 isNPCState를 false로 지정해야 함.
        targetF = SearchFurniure();
        if (targetF == null)
        {
            //2026: 앉을 의자가 없다면.
            targetObj = door;
            npcState = NpcState.Wander;
        }
        else
        {
            targetObj = player;
            npcState = NpcState.DoorToPlayer;
            //2026: 해당 luna는 본인이 만든 것이 아니다.
            //2026: [정정] 튜토리얼 관련 부분은 본인이 만든 것이 맞으나, 만족도 출력은 본인이 한 게 아니다.
            //if (luna == false) { Rating(); }
        }

        if (NPCManager.N.GetNPCAddress() == 5)
        {
            //단골손님 소환 시점. 해당 NPC를 '단골손님'으로 명시.
            //5번이 '단골손님 스폰'으로 명시되어 있기 때문에 NPC의 수가 늘어나면 해당 조건을 바꿔야만 함.
            //2026: GetRegularNPCAddress()은 '이 손님이 어떤 단골 손님인지'를 가져온다. 해당 값이 '이 손님이 어떤 단골 손님인지'를 구분짓는 요소다.
            isRegular = true;
            regularNum = NPCManager.N.GetRegularNPCAddress();
        }

        nDelay = nDelay_MAX;
    }

    void Update()
    {
        if (luna == true)
        {
            tutorial_luna();
        }
        else if (choco == true) //이후에 만족도에 따라서 실행될지 안될지 결정할 것
        {
            //2026: 본 choco 관련 부분은 본인이 만든 게 아니다.
            AI_ChocoAppear();
        }
        else
        {
            NPCMovement();
        }

        NPCAnimation();
        //NPCSpriteSet();
        //if (MakingManager.mm.currentWaterAmount!= 0) { NPCManager.N.SceneChangeButton.SetActive(true); }
        //if (npcState == NpcState.TableToDoor) { LastRate(); }
        // else { return; }
    }

    //2026: 본 함수는 본인이 만든 게 아니다.
    /*private void Rating() // 만족도 출력 관련 함수
    {
        tmpText = GameObject.Find("RatingFurniture").GetComponent<TextMeshProUGUI>();
        rateStar = GameObject.Find("RateStar");
        rateStar.GetComponent<Transform>().position = new Vector3(-4.3f, 2.8f, 0);

        funiturePrice = GameManager.GM.GetFurniturePrice();

        if (funiturePrice >= 5000)
        {
            funitureRate = 8;
            tmpText.text = "+" + funitureRate.ToString();
            GameManager.GM.SetFame(8);
        }
        else if (funiturePrice >= 2000)
        {
            funitureRate = 5;
            tmpText.text = "+" + funitureRate.ToString();
            GameManager.GM.SetFame(5);
        }
        else
        {
            funitureRate = 3;
            tmpText.text = "+" + funitureRate.ToString();
            GameManager.GM.SetFame(3);
        }
        Debug.Log(funiturePrice + "점");

        StartCoroutine(ResetTextAfterDelay(tmpText, rateStar, 2.0f)); // 1초 후에 다시 초기화되도록 수정
    }*/

    //2026: 본 함수는 본인이 만든 게 아니다.
    /*private IEnumerator ResetTextAfterDelay(TextMeshProUGUI tmpText, GameObject rateStar, float delay)
    {
        yield return new WaitForSeconds(delay);

        tmpText.text = string.Empty; // 또는 다시 초기화할 텍스트를 설정
        rateStar.GetComponent<Transform>().position = new Vector3(-4.3f, 6f, 0);

        // 다시 초기화된 텍스트를 표시하고 싶다면 아래의 코드를 추가
        tmpText.gameObject.SetActive(true);
    }*/

    public void NPCSpeedDecision(float speed)
    {
        //nSpeed를 0으로 지정했다면 임의로 nSpeed의 값을 10f로 설정. 
        if (speed == 0) { nSpeed = 3.5f; }
    }

    public void NPCMovement()
    {
        NPCFilp();

        if (npcState == NpcState.DoorToPlayer)
        {
            nDelay -= Time.deltaTime;
            if (nDelay <= 0)
            {
                this.transform.position = Vector3.MoveTowards(this.transform.position, targetObj.transform.position, nSpeed * Time.deltaTime);
            }
        }
        else if (npcState == NpcState.PlayerToTable)
        {
            if (isOrder == false)
            {
                //2026: 본래 게임 구성이 '메뉴를 일정 시간 내에 받고 주문을 처리한다'라는 구조를 가지고 있었는데, 해당 내용이 변경되며 몇몇 변수의 사용이 달라진 상황이다.
                //오더 대기시간(20f)이 넘는지 체크
                order_timer -= Time.deltaTime;

                if (order_timer < 0)
                {
                    //오더 대기시간을 넘어버린 경우.
                    NPCManager.N.speechBubble.SetActive(false);
                    NPCManager.N.iceImg.SetActive(false);
                    NPCManager.N.NPCCountDeduction();
                    NPCManager.N.SetNPCReturning(NPCManager.N.GetNPCReturning() + 1);
                    NPCManager.N.SetTimerOn(true);
                    NPCManager.N.SetisTrigger(false);
                    targetF.GetComponent<FurnitureSet>().SetIsNPCState(false);

                    targetObj = door;
                    npcState = NpcState.NoOrder;
                }
            }
            else
            {
                nDelay -= Time.deltaTime;
            }

            //이전에 음료를 주문받고 의자가 없을 시 문으로 이동하기 < 로 코딩해두어서 targetObj의 이름을 물어보는 조건문이 붙은 상황.
            //나중에 줄일 수 있으면 줄여볼 것.
            //주문중이 아니라면 아래의 프로그램이 작동하기 때문에 NPC 누구라도 Player와 상호작용하면 시간이 지나지 않는 중.
            if (NPCManager.N.GetisTrigger() == false && order_timer > 0 && nDelay <= 0)
            {
                Vector3 targetPos;
                targetObj = targetF;
                targetPos = new Vector3(targetF.transform.position.x, targetF.transform.position.y + 0.5f, targetF.transform.position.z);

                this.transform.position = Vector3.MoveTowards(this.transform.position, targetPos, nSpeed * Time.deltaTime);

                stand_timer -= Time.deltaTime;
                NPCStayTable();
            }

            if (stand_timer <= 0)
            {
                //음료를 마시고 난 뒤, 퇴장해야 한다고 알려주는 시점.
                if (isRegular == true)
                {
                    //호감도를 임의로 10 올리고 있는 중. 추후 꼭 조절해야 함.
                    //2026: 본 NPC가 '단골 손님'일 때 실행되는 부분.
                    NPCManager.N.SetRegularStay(regularNum, false);
                    NPCManager.N.SetRegularNPCIntimacy(regularNum, 10);
                }

                targetObj = door;
                npc_animator.SetBool("Chair", false);
                //Satisfaction();
                npcState = NpcState.TableToDoor;
            }
        }
        else if (npcState == NpcState.Wander)
        {
            //2026: 게임 내에 배치된 의자에 손님이 모두 앉아있는 경우, NPC는 배회하게 된다. 
            NPCWander(w_random);
            RandomNum();

            wander_timer -= Time.deltaTime;
            if (wander_timer <= 0)
            {

                //삭제 시점 직전에 NPC의 수를 조절하는 이유: 삭제 시점에 올리려니 Count가 미친듯이 감소해서
                if (isRegular == true)
                {
                    NPCManager.N.SetRegularStay(regularNum, false);
                }

                //2026: '현재 가게 안에 있는 손님 수 감소', '되돌아간 손님 수 증가', 결과적으로 'NPC가 밖으로 나서게 된다'.
                NPCManager.N.NPCCountDeduction();
                NPCManager.N.SetNPCReturning(NPCManager.N.GetNPCReturning() + 1);
                npcState = NpcState.NoOrder;
            }
        }
        else if (npcState == NpcState.TableToDoor)
        {
            //제대로된 주문이 끝난 뒤의 퇴장시점.
            this.transform.position = Vector3.MoveTowards(this.transform.position, adj_doorPos, nSpeed * Time.deltaTime);
            //NPCManager.N.RateBubble.SetActive(false);
        }
        else if (npcState == NpcState.NoOrder)
        {
            //제대로된 주문이 끝나지 않은 상황에서의 퇴장시점.
            this.transform.position = Vector3.MoveTowards(this.transform.position, adj_doorPos, nSpeed * Time.deltaTime);
            NPCManager.N.DestroyNPC(this.gameObject);
            Destroy(this.gameObject, 1f);
        }
    }

    GameObject SearchFurniure()
    {
        /*바닥에 깔린 가구를 모두 찾아내는 함수가 필요함...

        //이 함수를 부르는 당시에 Search를 해서 '몇 개의 가구가 있나 확인하고 > 가장 가까이에 있는 가구로 이동'한다면?
        //단, 아무도 앉지 않은 가구에 가서 앉아야 함.
        //그러려면 Furniture에서 몇 가지의 수정을 거쳐야 할 것 같음.*/

        LinkedList<GameObject> c = FurnitureManager.F.GetListChair();
        var _c = c.GetEnumerator();        //== IEnumerator<GameObject> c_e = c.GetEnumerator();

        float minDis = 10000000f;
        float nowDis;
        GameObject nowChair = null;

        for (int i = 0; i < c.Count; i++)
        {
            _c.MoveNext();

            nowDis = Vector3.Distance(_c.Current.gameObject.transform.position, player.transform.position);

            //2026: 저장된 거리보다 현재 거리가 짧고, 의자에 그 어떤 NPC도 할당되지 않았다면
            if (minDis > nowDis && _c.Current.gameObject.GetComponent<FurnitureSet>().GetIsNPCState() == false)
            {
                //2026: 가까운 거리의 의자를 찾아서 저장했다가, 그 뒤에 더 가까운 의자를 찾았다면, 그 경우, 현재 의자의 상태를 원래대로 되돌려두어야 한다.
                if (nowChair != null) { nowChair.GetComponent<FurnitureSet>().SetIsNPCState(false); }

                minDis = nowDis;

                nowChair = _c.Current.gameObject;
                _c.Current.gameObject.GetComponent<FurnitureSet>().SetIsNPCState(true);
            }
        }

        if (nowChair == null)
        {
            //의자가 없는 경우.
            return null;
        }

        //2026: 테이블과 연결되지 않은 의자인 경우.
        if (nowChair.GetComponent<FurnitureSet>().GetFurnitureDirection() == "none")
        {
            return null;
        }

        return nowChair;
    }

    void NPCStayTable()
    {
        //2026: FurnitureSet 함수에 있는 좌우 구분 함수를 이용해서 '어느 방향으로 앉을 것인지'를 결정한다. directionF는 이후 의자에 실제로 접촉했을 때 다시 이용된다.
        //평가를 어떻게 하는지는 모르겠으나... 평가?를 할 수 있도록 할 것.
        //앉아있는 스프라이트로 변경 + 앉아있는 것의 방향도 결정.
        if (directionF == "L")
        {
            this.transform.localScale = new Vector3(-1f, 1f, 1f);

        }
        else if (directionF == "R")
        {
            this.transform.localScale = new Vector3(1f, 1f, 1f);
        }

    }

    //랜덤한 값이 결정되는 시간. 3초 뒤에 랜덤하게 값을 하나 결정함.
    float random_timer_MAX = 3f;
    float random_timer = 3f;
    int random_start = 0;

    void RandomNum()
    {
        //2026: 여기서 3초를 주기로 어떤 행동을 할 것인지 랜덤으로 결정한다.
        //2026: 이 값은 NPCWander()로 전달되어 우로 움직이기, 좌로 움직이기, 가만히 서 있기의 세 동작을 하게 된다.
        if (random_start == 0)
        {
            w_random = UnityEngine.Random.Range(1, 4);
            random_start++;
        }

        random_timer -= Time.deltaTime;

        if (random_timer <= 0)
        {
            w_random = UnityEngine.Random.Range(1, 4);
            random_timer = random_timer_MAX;
        }
    }

    void NPCWander(int random)
    {
        //random의 값은 RandomNum()에서 결정됨.
        Vector3 npcVec = Vector3.zero;

        //만약 Floor의 위치가 바뀐다면 같이 바뀌어야만 함.
        if (random == 1)
        {
            //우로 이동
            if (this.transform.position.x < door.transform.position.x + 1.1f)
            {
                this.transform.localScale = new Vector3(-1f, 1f, 1f);
                npcVec = Vector3.right;
            }
            else if (this.transform.position.x >= door.transform.position.x + 1.1f)
            {
                //2026: 너무 우로 이동하지 않게끔 고정.
                random = 3;
            }

        }
        else if (random == 2)
        {
            //좌로 이동
            if (this.transform.position.x > door.transform.position.x - 1.1f)
            {
                this.transform.localScale = new Vector3(1f, 1f, 1f);
                npcVec = Vector3.left;
            }
            else if (this.transform.position.x <= door.transform.position.x - 1.1f)
            {
                //2026: 너무 좌로 이동하지 않게끔 고정.
                random = 3;
            }

        }
        else if (random == 3)
        {
            npcVec = Vector3.zero;
        }

        this.transform.position += npcVec * nSpeed * Time.deltaTime * 0.05f;
    }

    void NPCFilp()
    {
        //2026: 이동 방향에 따라서 게임 오브젝트를 가로로 뒤집는 함수.
        //벡터 내적을 이용함.
        Vector3 dir = (targetObj.transform.position - this.transform.position).normalized;
        float dirDot = Vector3.Dot(this.transform.right.normalized, dir);

        if (dirDot > 0) { this.transform.localScale = new Vector3(-1f, 1f, 1f); }
        else if (dirDot < 0) { this.transform.localScale = new Vector3(1f, 1f, 1f); }

        //이동하는 방향이 어디냐를 판별해서 filp할 수 있게 할 것
    }

    public void NPCAnimation()
    {
        //spriteAddress 이용. 
        if (npcState == NpcState.DoorToPlayer ||
            npcState == NpcState.Wander ||
            npcState == NpcState.TableToDoor ||
            npcState == NpcState.NoOrder ||
            (npcState == NpcState.PlayerToTable && isOrder == true))
        {
            npc_animator.SetBool("MoveState", true);
        }
        else if (npcState == NpcState.PlayerToTable ||
            npcState == NpcState.nil)
        {
            npc_animator.SetBool("MoveState", false);
        }
    }

    public void Satisfaction()
    {
        //2026: 과거에 쓰였다가 지금은 쓰이지 않는 함수로 보인다.
        //이후 조건문에 따라서 값을 달리하면 될듯함.
        //현재 호출하면 그냥 만족도를 상승시키는 중.

        //임의의 값으로 설정해두었음!!! 추후에 꼭 바꿔야 함.
        GameManager.GM.SetFame(100);
        GameManager.GM.SetCoin(100);
    }

    //2026: 아래의 코드는 본인이 만든 코드가 아닌 것으로 보인다.
    public void tutorial_luna()
    {
        NPCFilp();

        if (npcState == NpcState.DoorToPlayer)
        {
            nDelay -= Time.deltaTime;
            if (nDelay <= 0)
            {
                this.transform.position = Vector3.MoveTowards(this.transform.position, targetObj.transform.position, nSpeed * Time.deltaTime);
            }
        }
        else if (npcState == NpcState.nil)
        {
            if (EventManager.E.lunaMakeing == true)
            {
                npcState = NpcState.PlayerToTable;
            }
        }
        else if (npcState == NpcState.PlayerToTable)
        {
            nDelay -= Time.deltaTime;

            if (nDelay <= 0)
            {
                Vector3 targetPos;
                targetObj = targetF;
                targetPos = new Vector3(targetF.transform.position.x, targetF.transform.position.y + 0.5f, targetF.transform.position.z);

                this.transform.position = Vector3.MoveTowards(this.transform.position, targetPos, nSpeed * Time.deltaTime);

                stand_timer -= Time.deltaTime;
                NPCStayTable();
            }

            if (GameManager.GM.GetTutorialCheck() == true)
            {
                targetObj = door;
                npc_animator.SetBool("Chair", false);
                npcState = NpcState.TableToDoor;
            }
        }
        else if (npcState == NpcState.TableToDoor)
        {
            this.transform.position = Vector3.MoveTowards(this.transform.position, adj_doorPos, nSpeed * Time.deltaTime);
        }
    }

    //2026: 아래의 코드는 본인이 만든 코드가 아닌 것으로 보인다.
    public void AI_ChocoAppear()
    {
        NPCFilp();

        if (npcState == NpcState.DoorToPlayer)
        {
            nDelay -= Time.deltaTime;
            if (nDelay <= 0)
            {
                this.transform.position = Vector3.MoveTowards(this.transform.position, targetObj.transform.position, nSpeed * Time.deltaTime);
            }
        }
        else if (npcState == NpcState.nil)
        {
            if (EventManager.E.ChocoMaking == true)
            {
                npcState = NpcState.PlayerToTable;
            }
        }
        else if (npcState == NpcState.PlayerToTable)
        {
            nDelay -= Time.deltaTime;

            if (nDelay <= 0)
            {
                Vector3 targetPos;
                targetObj = targetF;
                targetPos = new Vector3(targetF.transform.position.x, targetF.transform.position.y + 0.5f, targetF.transform.position.z);

                this.transform.position = Vector3.MoveTowards(this.transform.position, targetPos, nSpeed * Time.deltaTime);

                stand_timer -= Time.deltaTime;
                NPCStayTable();
            }

            if (GameManager.GM.GetChocoAppearCheck() == true)
            {
                targetObj = door;
                npc_animator.SetBool("Chair", false);
                npcState = NpcState.TableToDoor;
            }
        }
        else if (npcState == NpcState.TableToDoor)
        {
            this.transform.position = Vector3.MoveTowards(this.transform.position, adj_doorPos, nSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //2026: 오브젝트-오브젝트 사이에 충돌이 일어나면 호출되는 유니티 내부 함수.
        //2026: 이 함수가 실행되려면 게임 오브젝트에 Collider이 필요하다.
        GameObject trigger = other.gameObject;

        if (trigger.name == "PlayerPos" && isOrder == false)
        {
            //2026: luna와 choco 부분은 본인이 만든 것으로 보이지는 않는다.
            if (luna == true)
            {
                if (nDelay <= 0) { nDelay = nDelay_MAX; }
                npcState = NpcState.nil;

                NPCManager.N.SetInterNPC(this.gameObject);
                EventManager.E.lunaDirector = true;
                return;
            }

            else if (choco == true)
            {
                if (nDelay <= 0) { nDelay = nDelay_MAX; }
                npcState = NpcState.nil;

                NPCManager.N.SetInterNPC(this.gameObject);
                EventManager.E.ChocoDirector = true;
                return;
            }

            //NPCManager.N.iceImg.SetActive(true);

            //말풍선이 보이게 함.
            //플레이어랑 만나기 전까지 가구랑 부딪쳐도 무시할 수 있도록 코드가 짜여야만 함.
            NPCManager.N.speechBubble.SetActive(true);

            //2026: 본래 '음료 제작 창으로 이동했다가 다시 메인 화면으로 돌아오는 구조'로 게임이 구상되어 있었는데, 한 화면에 모두 담게끔 변경되었다.
            //2026: sceneChangeButton라는 이름은 그 흔적에 가깝다.
            NPCManager.N.sceneChangeButton.SetActive(true);
            NPCManager.N.SetInterNPC(this.gameObject);
            NPCManager.N.SetisTrigger(true);
            NPCManager.N.SetTimerOn(false);
            //isOrder = true; 해당 코드는 NPCManager로 이전.

            if (nDelay <= 0) { nDelay = nDelay_MAX; }

            npcState = NpcState.PlayerToTable;
        }

        if (trigger.gameObject == targetF && isOrder == true)
        {
            if (luna == true)
            {
                EventManager.E.lunaDirector = true;
            }
            else if (choco == true)
            {
                EventManager.E.ChocoDirector = true;
            }

            orderComplete = true;
            npc_animator.SetBool("Chair", true);
            directionF = targetF.GetComponent<FurnitureSet>().GetFurnitureDirection();
        }

        if (trigger.name == "DoorPos" && orderComplete == true)
        {
            //만약 루나나 초코가 NPCManager에서 생성되어 'npc'라는 연결 리스트에 연결될 경우,
            //꼭 NPCManager.N.DestroyNPC(this.gameObject); 할 것. (아닐 경우에는 안 해도 됨!)
            if (luna == true)
            {
                targetF.GetComponent<FurnitureSet>().SetIsNPCState(false);
                Destroy(this.gameObject, 1f);
                return;
            }
            else if (choco == true)
            {
                targetF.GetComponent<FurnitureSet>().SetIsNPCState(false);
                Destroy(this.gameObject, 1f);
                return;
            }

            //2026: 가게 안의 현재 손님 수 감소, 의자-NPC 사이의 연결 끊기, 연결 리스트에서 삭제 후 게임 오브젝트 삭제.
            NPCManager.N.NPCCountDeduction();
            targetF.GetComponent<FurnitureSet>().SetIsNPCState(false);
            NPCManager.N.DestroyNPC(this.gameObject);
            Destroy(this.gameObject, 1f);
        }
    }

    public bool GetIsOrder() { return isOrder; }
    public void SetIsOrder(bool b) { isOrder = b; }

    public float GetStandTimer() { return stand_timer; }
    public void SetStandTimer(float timer) { stand_timer = timer; }
}
