//attach of 'GameManager'

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.VisualScripting.Antlr3.Runtime.Tree.TreeWizard;

public class GameManager : MonoBehaviour
{
    private static GameManager _gm;
    public static GameManager GM
    {
        get
        {
            if (!_gm)
            {
                _gm = FindObjectOfType(typeof(GameManager)) as GameManager;

                if (_gm != null) { Debug.Log("<color=red>GameManager.GM.get</color> - no Singleton Object"); }
            }
            return _gm;
        }
    }

    //자금을 임의로 1000G로 지정.
    [Header("Plz Setting")]
    public int coin = 1000;
    public GameObject coinObj;
    public GameObject fameObj;
    public GameObject calculate;

    bool setObj_main = false;

    //만족도
    public int fame;
    int todayFame;

    //calculate에서 상호작용 되어야만 하는 오브젝트들을 저장.
    TextMeshProUGUI today_t;
    TextMeshProUGUI sales_t;
    TextMeshProUGUI visitor_t;
    TextMeshProUGUI returning_t;
    TextMeshProUGUI fame_t;

    private int today = 0;
    private int coinConsumption = 0;
    private int coinSales = 0;

    //배치된 가구들의 가격 총합.
    private int furniturePrice = 0;

    //이벤트 관련
    //regularEventNum   : 단골손님 이벤트 때마다 값을 +1씩.
    //                  이렇게 관리하면 해당 값을 읽어왔을 때 '어떤 이벤트를 열어야 하는지'를 알아낼 수 있을 것임.
    //tutorial          : 예외적으로 튜토리얼 등은 한 번만 출력하면 될 것이기에 bool로 생성.
    //C                 : 실행했는지 Check
    int[] regularEventNum;
    bool prologue_C = false;
    bool tutorial_C = false;
    bool chocoAppear_C = false;
    public bool debugging = false;

    private void Awake()
    {
        if (_gm == null)
        {
            _gm = this;
        }
        else if (_gm != this)
        {
            Debug.LogError("<color=red>GameManager.Awake()</color> - attempted to assign second GameManager.gm!");
        }

        //씬이 전환되더라도 선언되었던 인스턴스는 파괴되지 않는다고 한다.
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "MainScene")
        {
            //2026: 씬이 전환되면서 설정되어야 하는 부분.
            if (setObj_main == false)
            {
                calculate = GameObject.Find("Calculate");
                coinObj = GameObject.Find("CoinUI");
                fameObj = GameObject.Find("FameUI");

                //calculateTable 아래의 TextArea 아래의 Text들을 읽어오는 것.
                //2026: calculate 오브젝트의 하위, 자식 오브젝트의 순서가 어긋나면 코드도 같이 어긋나게 되는 구조니 주의해야 한다.
                today_t = calculate.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                sales_t = calculate.transform.GetChild(0).gameObject.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                fame_t = calculate.transform.GetChild(0).gameObject.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>();
                visitor_t = calculate.transform.GetChild(0).gameObject.transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>();
                returning_t = calculate.transform.GetChild(0).gameObject.transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>();

                setObj_main = true;
            }

            coinObj.GetComponent<TextMeshProUGUI>().text = coin.ToString();
            fameObj.GetComponent<TextMeshProUGUI>().text = fame.ToString();
        }
    }

    public void Calculate()
    {
        //2026: 마지막 손님이 나간 시점에 호출되어, 정산표가 화면에 보이게 된다.
        //2026: NPCManager.cs의 Calculate() 함수에서 호출된다.
        calculate.SetActive(true);
        today_t.text = "Day " + ++today;
        sales_t.text = "당일 매출 : " + coinSales + "C";
        fame_t.text = "명성 상승 : " + todayFame;

        visitor_t.text = "방문한 손님 : " + NPCManager.N.GetEntireNPCCount();
        returning_t.text = "돌아간 손님 : " + NPCManager.N.GetNPCReturning();

        Animator cal = calculate.GetComponent<Animator>();
        cal.SetTrigger("CalculateDown");
    }

    public void NextDay()
    {
        //다음날로 넘어가면서 초기화되어야 하는 값들은 초기화.
        //2026: 다음날로 넘어가는 버튼에 적용되어 있는 함수. 현재는 Calculate.cs를 따로 만들어서 함수를 사용하고 있기 때문에 해당 함수는 쓰이지 않는다.
        //2026: 이러한 수정은 본인이 시도한 것이 아니기 때문에 이유를 알 수 없다.
        NPCManager.N.SetNPCManagerCalculating(false);
        NPCManager.N.SetNPCReturning(0);

        coinConsumption = 0;
        coinSales = 0;
        todayFame = 0;

        calculate.gameObject.SetActive(false);
    }

    public int GetCoin() { return coin; }
    public void SetCoin(int value)
    {
        //소비할 만큼의 값만 담아주면 OK.
        //감소값이면 앞에 -를 붙여야 함.
        if (value > 0) { coinSales += value; }
        if (value < 0) { coinConsumption += Mathf.Abs(value); }

        coin += value;
    }
    public void SetCoin(int value, bool furnitureDataValueCheck)
    {
        //FurnitureData의 가구 구매는 정산표의 지출 및 수익과 관계없기 때문에 따로 만들어냄.
        coin = value;
    }

    public int GetFame() { return fame; }
    public void SetFame(int value)
    {
        todayFame += value;
        fame += value;
    }

    //2026: 본인이 만든 부분은 아니나, 해당 부분이 Calculate.cs에서 호출되어 다음날에 초기화되어야 하는 값을 초기화시킨다.
    public void ResetValues() { coinSales = 0; todayFame = 0; calculate.gameObject.SetActive(false); }

    public int GetFurniturePrice() { return furniturePrice; }
    public void SetFurniturePrice(int value) { furniturePrice = value; }

    public bool GetPrologueCheck() { return prologue_C; }
    public void SetPrologueCheck(bool b) { prologue_C = b; }

    public bool GetTutorialCheck() { return tutorial_C; }
    public void SetTutorialCheck(bool b) { tutorial_C = b; }

    public bool GetChocoAppearCheck() { return chocoAppear_C; }
    public void SetChocoAppearCheck(bool b) { chocoAppear_C = b; }

    public bool GetDebugging() { return debugging; }

    public int GetRegularEventNum(int address) { return regularEventNum[address]; }
    public void IncreaseRegularEventNum(int address) { regularEventNum[address]++; }


}
