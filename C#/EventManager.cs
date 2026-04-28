//https://github.com/tikonen/blog/tree/master/csvreader
//엑셀의 첫번째 줄은 index로 이용함. 따라서 두번째 줄부터 값을 입력해야 함.
//2026: CSVReader 코드가 있어야 실행할 수 있는 클래스. CSVReader는 위의 링크에서 확인할 수 있다.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class EventManager : MonoBehaviour
{
    static public EventManager E;

    //readingState      : 읽을 수 있는 상태인지 명시.
    //scriptPointer     : 클릭하면 1씩 오름. 어딜 읽고 있는지 가리킴.
    //nowReading        : 지금 '어느 스크립트'를 읽고 있는지 명시. 튜토리얼이면 튜토리얼, 초코면 초코.
    bool readingState = false;
    int scriptPointer = -1;
    string nowReading;

    [Header("Plz Setting")]
    public GameObject canvas;
    public GameObject standingObject;
    public GameObject textingObejct;

    //각 스탠딩 일러스트 설정.
    public Sprite[] loi;
    public Sprite[] king;
    public Sprite[] luna;
    public Sprite[] choco;
    public Sprite[] hotDog;

    //각 이벤트에서 생성하거나 보여줘야하는 기타 오브젝트들.
    public GameObject[] using_prologue;
    public GameObject[] using_tutorial;
    public GameObject[] using_chocoAppear;

    //Player의 행동에 따라 대화 출력이 달라지는 경우, 해당 값으로 뒷일 조정
    //ex: 루나가 술을 받고 난 뒤의 대사 출력
    int plusPointer;

    //CSV
    List<Dictionary<string, object>> csv_prologue;

    //자주 불러지는 변수들을 미리 선언해둔 상태.
    //speaker   : 발화자의 이름 표시
    //dialogue  : 실제로 하는 말.
    Image standing;
    TextMeshProUGUI speakerText;
    TextMeshProUGUI dialogueText;

    private void Awake()
    {
        if (E == null)
        {
            E = this;
        }
        else
        {
            Debug.LogError("<color=red>EventManager.Awake()</color> - attempted to assign second EventManager.E!");
        }
    }

    private void Start()
    {
        //시작할 때 스크립트들을 모두 읽어옴.
        csv_prologue = CSVReader.Read("Test_Prologue");

        standing = standingObject.GetComponent<Image>();
        speakerText = textingObejct.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        dialogueText = textingObejct.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

        //초코 해금 
        GameObject chocoDictionaryImg = GameObject.Find("chocoDic");
        if (chocoDictionaryImg != null)
        {
            chocoDicColor = chocoDictionaryImg.GetComponent<Image>();
        }
    }

    void Update()
    {
        if ((Input.GetMouseButtonUp(0) && readingState == true) || ((Input.GetKeyDown(KeyCode.Space)) && readingState))
        {
            scriptPointer++;

            //선택지 값 변경.
            //2026: 선택지에 따라 출력이 달라질 때, 달라진 만큼 아래로 더 읽어내려가기 위해서.
            if (plusPointer != 0)
            {
                scriptPointer += plusPointer;
                plusPointer = 0;
            }
        }

        //어떤 상황일 때 어떤 이벤트를 열어줄 것인지 설정할 것.
        //2026: 구조는 본인이 만들었지만, choco2()와 관련된 함수는 본인이 만든 게 아니다.
        if (GameManager.GM.GetPrologueCheck() == false && SceneManager.GetActiveScene().name == "Prologue") { Prologue(); }
        if (GameManager.GM.GetTutorialCheck() == false && SceneManager.GetActiveScene().name == "MainScene") { Tutorial(); }
        if (GameManager.GM.GetChocoAppearCheck() == false && chocoButtonClicked == true) { ChocoAppear(); }
        if (chocoButtonClicked2 == true&&choco2end==false) { Choco2(); }
    }

    Vector3 pro_targetPos = new Vector3(0, 0, -10);
    float pro_speed = 1f;

    //아래로부터 진짜 이벤트 구현.
    public void Prologue()
    {
        //스크립트 읽기 전에 값 초기화
        ReadingInitialization(0, "prologue");

        //어느 스크립트를 읽어낼 것인지 지정
        //2026: Reading() 함수는 '읽어야 하는 모든 값을 읽으면 true를 반환'하기 때문에, 아래의 if문은 내용을 읽는 도중이다.
        if (Reading(csv_prologue) == false)
        {
            //아래로는 '해당 이벤트에서 보여줘야 하는 것들'
            //프롤로그 씬(넘버 0~3)

            if ((int)csv_prologue[scriptPointer]["Number"] == 0 && using_prologue[0].transform.position.y != 0)
            {
                //2026: 시작 컷씬을 출력하는 부분
                //Time.timeScale이 1이 아니면 카메라가 움직이지 않음...
                Time.timeScale = 1;
                readingState = false;

                //누를 때 속도를 높이고 누르지 않을 때 원상복구.
                if (Input.GetMouseButton(0)) { pro_speed = 30f; }
                else { pro_speed = 1f; }

                using_prologue[0].transform.position = Vector3.MoveTowards(using_prologue[0].transform.position, pro_targetPos, pro_speed * Time.deltaTime);
            }
            else if ((int)csv_prologue[scriptPointer]["Number"] == 0 && using_prologue[0].transform.position.y == 0)
            {
                //2026: 컷씬을 모두 확인한 시점
                readingState = true;
                Transition(2);
            }

            if ((int)csv_prologue[scriptPointer]["Number"] == 3)
            {
                //프롤로그 이야기가 끝나면 Time.timeScale 값 원상복구.
                Time.timeScale = 0;
                using_prologue[1].gameObject.SetActive(false);
            }

            //넘버 13번 주석 구현
            if ((int)csv_prologue[scriptPointer]["Number"] == 13)
            {
                using_prologue[3].gameObject.SetActive(true);
            }
            else if ((int)csv_prologue[scriptPointer]["Number"] == 14)
            {
                using_prologue[3].gameObject.SetActive(false);
            }
        }
    }

    //tutorial에서 잠시 이용할 변수값들 선언.
    //Appearance    : NPC 생성을 단 한 번만 할 수 있게 해줌.
    //Director      : Luna가 Player와 만났는지 체크
    //Timer         : 화살표 깜빡이는 걸 언제동안 보여줄 것인가
    //Makeing       : 음료를 제공받으면 true
    //Correct       : 음료를 잘 받았으면 true, 음료 상태가 별로면 false
    //Animation     : 하우징 버튼 애니메이션 때문에 생성...
    bool lunaAppearance = false;
    public bool lunaDirector = false;
    float lunaTimer = 2f;
    public bool lunaMakeing = false;
    public bool lunaCorrect = false;
    bool lunaAnimation = false;
    bool loiAnimation = false;

    public void Tutorial()
    {
        //이벤트 시작 시점.
        //2026: 이 시작 지점은 'csv 따라 다른 상황'이므로, csv가 변형되면 같이 수정되어야 한다.
        ReadingInitialization(23, "tutorial");

        if (Reading(csv_prologue) == false)
        {
            //아래로는 '해당 이벤트에서 보여줘야 하는 것들'
            //2026: 'break' 등에서 처리되는 'NPC 이동'은 NPC_AI.cs에서 관리하는 중.
            if ((int)csv_prologue[scriptPointer]["Number"] == 2 && lunaAppearance != true)
            {
                //2번째 파라미터는 문 위치임.
                GameObject newNpc = (GameObject)Instantiate(using_tutorial[0], new Vector3(-3.9f, 1.5f, 0), Quaternion.identity);
                using_tutorial[3].GetComponent<AudioSource>().Play();
                lunaAppearance = true;
            }
            else if ((int)csv_prologue[scriptPointer]["Number"] == 15)
            {
                //2026: 화면을 좀 더 잘 보여주기 위해서 기본적으로 '좌측'에 배치된 NPC 포트레이트를 '우측'에 배치하는 상황.
                standingObject.transform.position = new Vector3(6.15f, 1);
                standingObject.transform.localScale = new Vector3(-1, 1);

                //using_tutorial[1]에 NPCBubble > SpeechBubble을 설정해두었음.
                using_tutorial[1].SetActive(true);
                //using_tutorial[1].transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Order/LiqColor");
            }
            else if ((int)csv_prologue[scriptPointer]["Number"] == 16)
            {
                //2026: 위에서 NPC 포트레이트를 이동시켰으니, 다시 원상복구 시키는 것.
                standingObject.transform.position = new Vector3(-6.15f, 1);
                standingObject.transform.localScale = new Vector3(1, 1);
            }
            else if ((int)csv_prologue[scriptPointer]["Number"] == 18)
            {
                using_tutorial[2].SetActive(true);
                canvas.transform.GetChild(0).gameObject.SetActive(true);
                NPCManager.N.SetNPCManagerCalculating(false);

                if (loiAnimation == false)
                {
                    NPCManager.N.SetisTrigger(true);
                    loiAnimation = true;
                }
            }
            else if ((int)csv_prologue[scriptPointer]["Number"] == 19)
            {
                NPCManager.N.SetNPCManagerCalculating(true);
                //2026: 19번에는 '음료 제작이 실패한 경우'를 담도록 되어 있으나, 회의 끝에 해당 내용은 파기하기로 결정되었다.
                //2026: 때문에 어떤 경우든 바로 20번째 줄을 읽게 된다.
                if (lunaMakeing == true && lunaCorrect == true)
                {
                    Transition(1);
                }
                else
                {
                    //남은 선택지를 뛰어넘기 위해 값변경.
                    plusPointer = 1;
                }
            }
            else if ((int)csv_prologue[scriptPointer]["Number"] == 27)
            {
                //2026: 잠깐 모든 대화창을 내리고 화면에서 강조해야 하는 부분을 강조.
                using_tutorial[2].SetActive(true);
                using_tutorial[2].transform.GetChild(0).transform.localPosition = new Vector3(165, -355);
                using_tutorial[2].transform.GetChild(0).transform.localRotation = Quaternion.identity;
                using_tutorial[2].transform.GetChild(1).gameObject.SetActive(false);
                using_tutorial[2].transform.GetChild(2).gameObject.SetActive(false);

                //'정산표'를 보여주지만 '다음날 버튼'은 보이지 않게
                canvas.transform.GetChild(1).gameObject.SetActive(true);
                canvas.transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(false);
            }
            else if ((int)csv_prologue[scriptPointer]["Number"] == 31 && lunaAnimation == false)
            {
                //하우징 닫는 시점
                canvas.transform.GetChild(2).GetComponent<Animator>().SetTrigger("FurnitureTapDown");
                FurnitureManager.F.SetFurnitureState(0);
                FurnitureManager.F.SetIsClickBTN(false);
                lunaAnimation = true;
            }

            //이동 직후. 다시 스크립트를 진행시키는 시점. lunaDirector는 NPC_AI.cs의 OnTriggerEnter2D() 부분을 확인.
            //UI를 내렸다가 다시 올리는 시점. 자주 이용될 것 같다면 함수로 만들어둘 것.
            if (lunaDirector == true)
            {
                //다시 스크립트를 진행시키는 시점.
                Time.timeScale = 0f;

                readingState = true;

                standingObject.SetActive(true);
                textingObejct.SetActive(true);

                scriptPointer++;
                lunaDirector = false;
            }
        }
    }

    //2026: choco 관련 변수 및 함수는 본인이 만든 함수는 아닌 것으로 보인다.
    //초코 등장
    bool ChocoAppearance = false;
    public bool ChocoDirector = false;
    float ChocoTimer = 2f;
    public bool ChocoMaking = false;
    public bool ChocoCorrect = false;
    // public GameObject prefabToReplace; //쓸지 안쓸지 결정
    string objectName = "NPC_Regular_Prefab_1(Clone)";
    bool chocoButtonClicked = false;
    public Image chocoDicColor;
    public TextMeshProUGUI chocoFriendlyText;
    private int chocoF=1;

    //초코 첫 등장 씬
    public void ChocoAppear()
    {
        //이벤트 시작 시점.
        ReadingInitialization(64, "ChocoAppear");

        if (Reading(csv_prologue) == false)
        {
            if ((int)csv_prologue[scriptPointer]["Number"] == 0 && ChocoAppearance != true) //초코 멈추기 해야함
            {
                GameObject newNpc = (GameObject)Instantiate(using_chocoAppear[0], new Vector3(-3.9f, 1.5f, 0), Quaternion.identity);
                using_tutorial[3].GetComponent<AudioSource>().Play();
                ChocoAppearance = true;
                //using_chocoAppear[1].SetActive(false);

            }
            /*else if ((int)csv_prologue[scriptPointer]["Number"] == 3)
            {
                //대화창 띄움
                Time.timeScale = 0;
                readingState = true;

                standingObject.SetActive(true);
                textingObejct.SetActive(true);

                scriptPointer++;
                ChocoDirector = false;
            }*/
        
            else if ((int)csv_prologue[scriptPointer]["Number"] == 11)
            {
                canvas.transform.GetChild(0).gameObject.SetActive(true);
                NPCManager.N.SetNPCManagerCalculating(false);

                if (loiAnimation == false)
                {
                    NPCManager.N.SetisTrigger(true);
                    loiAnimation = true;
                }
                NPCManager.N.SetEnableSpawn(false);
                //using_chocoAppear[1].SetActive(true);

            }
            else if ((int)csv_prologue[scriptPointer]["Number"] == 12)
            {
                NPCManager.N.SetNPCManagerCalculating(true);
                if (ChocoMaking == true && ChocoCorrect == true)
                {
                    Transition(1);
                }
                else
                {
                    //남은 선택지를 뛰어넘기 위해 값변경.
                    plusPointer = 1;
                }
                ChocoDirector=true;
                NPCManager.N.SetEnableSpawn(false);

            }


            if (ChocoDirector == true)
            {
                //다시 스크립트를 진행시키는 시점.
                Time.timeScale = 0f;

                readingState = true;

                standingObject.SetActive(true);
                textingObejct.SetActive(true);

                scriptPointer++;
                ChocoDirector = false;
            }

        }
      
        chocoDicColor.color = new Color(255f, 255f, 255f); //초코 해금
        //chocoF = RatingSystem.rs.GetChocoF();
        chocoFriendlyText.text = "초코 \n 호감도 :" + chocoF;
    }
    public void ChocoAppearButton()
    {
        if (GameManager.GM.GetChocoAppearCheck() == false && GameManager.GM.GetFame() > 30)
        {
            //ChocoAppear();
            chocoButtonClicked = true;
        }
        else Debug.Log("초코 등장 안됨");

    }
    public void Choco2()
    {
        ReadingInitialization(76, "Choco2");
        if ((int)csv_prologue[scriptPointer]["Number"] == 13)
        {
            //다시 스크립트를 진행시키는 시점.
            Time.timeScale = 0f;

            readingState = true;

            standingObject.SetActive(true);
            textingObejct.SetActive(true);

            scriptPointer++;
            ChocoDirector = false;
            NPCManager.N.SetEnableSpawn(false);
        }
        else if((int)csv_prologue[scriptPointer]["Number"] == 29)
        {
            NPCManager.N.SetEnableSpawn(true);
            choco2end = true;
            Destroy(GameObject.Find("NPC_Regular_Prefab_1(Clone)"));
        }

    }
    bool chocoButtonClicked2=false;
    bool choco2end = false;
    public void Choco2Button()
    {
        if (chocoButtonClicked == true)
        { chocoButtonClicked2 = true; }
        readingState = true;
    }

    //2026: 튜토리얼 용으로 만든 버튼. '음료를 제공하는 버튼'에 적용시켜 두었다.
    public void Tutorial_BTN()
    {
        if (GameManager.GM.GetDebugging() == true) { return; }
        if (GameManager.GM.GetTutorialCheck() == true) { return; }

        standingObject.SetActive(true);
        textingObejct.SetActive(true);

        readingState = true;

        //2026: 현재 읽고 있는 스크립트가 '어떤 스크립트인지' 명시가 되지 않았다.
        //2026: 다만, 위에서 TutorialCheck가 되어 있다면 아래로는 실행되지 않으므로, '튜토리얼' 전용 버튼으로 동작할 수는 있을 것으로 보인다.
        if ((int)csv_prologue[scriptPointer]["Number"] == 18)
        {
            lunaMakeing = true;
            
            //로이 셰이킹 애니메이션 때문에 여기서 설정.
            NPCManager.N.SetisTrigger(false);
            using_tutorial[2].SetActive(false);

            //제조 값이 맞는 순간이 언제인지 알 수 없어 버튼을 누르면 자동으로 성공했다고 판정.
            //이후 값 위치를 꼭 바꿀 것.
            lunaCorrect = true;
        }

        if ((int)csv_prologue[scriptPointer]["Number"] == 27)
        {
            using_tutorial[2].transform.GetChild(2).gameObject.SetActive(true);
            using_tutorial[2].SetActive(false);

            //여기서 return하는 이유는 하우징 애니메이션이 제대로 동작하지 못하기 때문.
            return;
        }

        //2026: 해당 부분은 본인이 만든 부분은 아닌 것으로 보인다.
        if((int)csv_prologue[scriptPointer]["Number"] == 11) //초코가 음료를 제공받을 때
        {
            ChocoMaking = true;

            NPCManager.N.SetisTrigger(false);

            ChocoCorrect = true;
        }
        
        Time.timeScale = 0;
    }

    //아래의 코드는 중복될 코드들을 묶어둔 것.

    //값 초기화.
    public void ReadingInitialization(int scriptP, string nowR)
    {
        //값 초기화는 '스크립트 미진행'인 상황에서만 가능하도록 설정.
        if (scriptPointer != -1) { return; }

        scriptPointer = scriptP;
        readingState = true;
        nowReading = nowR;

        if (nowReading != "prologue") { canvas.SetActive(false); }
        standingObject.SetActive(true);
        textingObejct.SetActive(true);

        if (nowReading == "tutorial")
        {
            //캔버스가 필요해서 켜긴 했으나, '보이지 말아야 할 것'들은 보이지 않게 설정.
            canvas.SetActive(true);
            canvas.transform.GetChild(0).gameObject.SetActive(false);
            canvas.transform.GetChild(1).gameObject.SetActive(false);

            //9번은 나중에 없앨 것.
            canvas.transform.GetChild(9).gameObject.SetActive(false);
        }

        //초코 등장 (임시)
        if (nowReading == "ChocoAppear")
        {
            canvas.SetActive(true);

        }

        if (nowReading == "Choco2")
        {
            canvas.SetActive(true);

        }
        Time.timeScale = 0;
    }

    //실제 스크립트를 읽는 함수. 모두 읽었으면 true를 반환.
    public bool Reading(List<Dictionary<string, object>> csv)
    {
        //Number 라인의 scriptPointer 번째 줄을 읽었을 때, end가 있으면 실행.
        if (csv[scriptPointer]["Number"].ToString() == "end" && scriptPointer != -1)
        {
            //2026: 읽어야 하는 대사를 모두 읽은 시점
            Time.timeScale = 1;
            readingState = false;

            //이벤트용 대화 오브젝트 모두 비활성화.
            standingObject.SetActive(false);
            textingObejct.SetActive(false);

            //아래는 프롤로그에서 넘어갈 때 변해야 하는 값들을 설정.
            if (GameManager.GM.GetPrologueCheck() == false && nowReading == "prologue")
            {
                GameManager.GM.SetPrologueCheck(true);

                this.gameObject.GetComponent<SceneChange>().ChangeScene();
            }

            //아래는 튜토리얼에서 넘어갈 때 변해야 하는 값들을 설정.
            if (GameManager.GM.GetTutorialCheck() == false && nowReading == "tutorial")
            {
                //필요없어지면 꼭 없앨 것. 테스트용의 그것임
                canvas.transform.GetChild(9).gameObject.SetActive(true);

                //'정산표'의 '다음날' 버튼.
                canvas.transform.GetChild(1).transform.GetChild(1).gameObject.SetActive(true);

                GameManager.GM.SetTutorialCheck(true);
            }

            if (GameManager.GM.GetChocoAppearCheck() == false && nowReading == "ChocoAppear")
            {
                GameManager.GM.SetChocoAppearCheck(true);
                //초코 돌려보내는거 추가해야함(음료 안받고)
            }

            if (nowReading == "Choco2")
            {
                GameManager.GM.SetChocoAppearCheck(true);

            }

            //초기화가 모두 끝나면 포인터 값을 -1로 지정.
            scriptPointer = -1;
            return true;
        }
        else if(csv[scriptPointer]["Number"].ToString() == "end" && nowReading == "Choco2")
        {
            //2026: 아마 해당 부분은 본인이 만든 부분이 아닐 것으로 보인다.
            Time.timeScale = 1;
            readingState = false;

            //이벤트용 대화 오브젝트 모두 비활성화.
            standingObject.SetActive(false);
            textingObejct.SetActive(false);
        }

        //이벤트 진행 중에 잠깐 끊어야 할 때. 'break' 값이 1일 때.
        if ((int)csv[scriptPointer]["break"] == 1 && scriptPointer != -1)
        {
            Time.timeScale = 1;
            readingState = false;

            standingObject.SetActive(false);
            textingObejct.SetActive(false);
        }

        //이름 부분과 말하는 부분의 텍스트를 가져옴.
        speakerText.text = csv[scriptPointer]["Name"].ToString();
        dialogueText.text = csv[scriptPointer]["Content"].ToString();

        //스프라이트 변환 부분.
        if (speakerText.text == "로이")
        {
            standing.GetComponent<Image>().sprite = loi[(int)csv[scriptPointer]["Face"]];
        }
        else if (speakerText.text == "견왕")
        {
            //따로 지정된 스프라이트가 없어 0번으로 처리하는 중.
            standing.GetComponent<Image>().sprite = king[0];
        }
        else if (speakerText.text == "루나")
        {
            standing.GetComponent<Image>().sprite = luna[(int)csv[scriptPointer]["Face"]];
        }
        else if (speakerText.text == "초코")
        {
            standing.GetComponent<Image>().sprite = choco[(int)csv[scriptPointer]["Face"]];
        }

        return false;
    }

    //값을 몇 칸 앞당길 것인지 결정
    public void Transition(int val)
    {
        //2026: 단순히 값을 더해주는 함수.
        //2026: 따라서, '얼마나 값을 더할 것인가'는 넣어둘 csv 파일이 어떻냐에 따라 계속 바뀌게 된다.
        //2026: 다만 Update()에서 plusPointer를 통해 값을 바꿀 수 있기 때문에, 해당 함수가 어떤 의미가 있을지는 알 수 없다.
        scriptPointer += val;
    }



}
