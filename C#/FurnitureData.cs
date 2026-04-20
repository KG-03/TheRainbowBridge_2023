//attach of 'W(F)_Furniture...' (UI)
//나중에 Save-Load를 생각하면 불가피하게 만들어야만 했던 스크립트.
//UI에서 구매한 수와 사용하는 수 출력은 해당 스크립트에서 실행함.

//세팅법 예시: WallScrollView -> Viewport -> Content ->
    // W_Furniture_0M(하위 BuyBtn, Price, Number) 에서 W_Furniture_0M에 해당 스크립트를 적용 +
    // BuyBtn onClick() 메소드에 W_furniture_0M 오브젝트를 넣고, FurnitureData -> BuyFurniture() 함수가 작동하게 설정.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class FurnitureData : MonoBehaviour
{
    //useNum : 사용하고 있는 가구의 수, buyNum : 구매한 가구의 수
    //접근하려고 하는 가구 기준으로 값이 계속 변동함.
    //parentObj : 현재 보이는 가구의 이미지.
    FurnitureClass f;
    GameObject parentObj, btnObj, priceObj, numberObj;
    int useNum, buyNum, price;

    [Header("Plz Setting")]
    public string f_theme = "normal";       //알아서 세팅해야 함.
    public int f_rank;                      //알아서 이하생략.
    public string f_type;
    public string f_size;                   //1x1 등으로 설정하면 됨

    //자물쇠 표시
    GameObject lockup;

    void Start()
    {
        //2026: 아래의 방식이 유효하려면, 모든 가구 오브젝트가 '게임 오브젝트 -> 버튼 오브젝트 -> 가격, 수량'이라는 순서로 구성되어야 한다.
        //2026: 가격 아래에 수량이 붙지 않으면 오류가 날 수 있을 것.
        parentObj = this.gameObject;
        btnObj = parentObj.transform.GetChild(0).gameObject;
        priceObj = btnObj.transform.GetChild(0).gameObject;
        numberObj = btnObj.transform.GetChild(1).gameObject;

        //2026: UI에 설정한 가격을 가져와서 int로 Parse하는 것. '60G' 등으로 적혀 있는데, 이를 '60'으로 만들어서 int로 만든다.
        string priceValue_s = priceObj.GetComponent<TextMeshProUGUI>().text;
        priceValue_s = priceValue_s.Substring(0, priceValue_s.Length - 1);
        price = int.Parse(priceValue_s);

        //2026: 저장된 가구 오브젝트 전체를 검사해서 현재 오브젝트가 저장되지 않았다는 것이 확인되었다면
        if (DuplicateFurniture() == false)
        {
            f = new FurnitureClass(this.gameObject.GetInstanceID(), this.gameObject, useNum, buyNum, price, f_theme, f_rank, f_type, f_size);
            FurnitureManager.F.FC_SetFurnitureClass(f);
        }

        //벽 장식은 잠시 꺼둠.
        //2026: 처음 하우징 UI를 열었을 때, 보여야 하는 게 '노말 타입 바닥 가구들'이기 때문에 벽 관련 가구를 모두 비활성화 하는 것.
        if(f_type.Contains("wall-decoration") ||
            f_type =="wall" ||
            f_type == "door" ||
            f_theme != "normal")
        {
            this.gameObject.SetActive(false);
        }

        FurnitureRank();

        //시작 시점에 이미지를 회색으로 지정.
        //2026: 좀 더 명확히 말하자면, 구매가 없었던 가구를 모두 회색처리 하는 것. 처음에 기본적으로 제공하는 가구들은 회색으로 바뀌지 않는다.
        if (buyNum <= 0)
        {
            this.gameObject.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
        }
    }

    private void Update()
    {
        FurnitureOpen();

        //2026: 바닥, 벽, 테이블바, 문은 단 하나만 있으면 상관 없기 때문에 사용 수를 받지 않는다.
        if (this.tag != "FWBD")
        {
            useNum = f.GetUseNum();
            buyNum = f.GetBuyNum();
            numberObj.GetComponent<TextMeshProUGUI>().text = "[" + useNum + "/" + buyNum + "]";
        }
        else if(this.tag == "FWBD")
        {
            buyNum = f.GetBuyNum();
            numberObj.GetComponent<TextMeshProUGUI>().text = "[" + buyNum + "]";
        }
    }

    //가구를 구매할 때 사용하는 함수. 버튼 클릭용 함수.
    //추후 두 가지 함수 모두 수정해야 함.
    //구매 확인 메세지를 띄우게끔 수정되어야 함.
    public void BuyFurniture()
    {
        //2026: '살 수 없는 가구'를 구매하려 할 때를 대비한 코드.
        if (GameManager.GM.GetFame() < 200 && f_rank >=2)
        {
            return;
        }
        else if (GameManager.GM.GetFame() < 500 && f_rank == 3)
        {
            return;
        }
        
        if (this.tag == "FWBD")
        {
            //2026: 한 번 이상 구매했다면 더 구매하지 못하도록. 벽, 바닥, 테이블바, 문은 하나만 있으면 되니까.
            if(buyNum == 1) { return; }

            //coin을 차감. 만약 코인이 부족하면 구매 불가.
            int _purchasePrice = GameManager.GM.GetCoin() - price;

            if (_purchasePrice < 0) { return; }
            else if (_purchasePrice >= 0) { GameManager.GM.SetCoin(_purchasePrice, true); }

            //2026: 현재 가구 개수, 구매한 가구 개수
            string _nowNumStr = numberObj.GetComponent<TextMeshProUGUI>().text;
            string _buyNum_s = _nowNumStr.Substring(_nowNumStr.LastIndexOf('[') + 1, 1);
            
            buyNum = int.Parse(_buyNum_s);
            buyNum++;
            _buyNum_s = buyNum.ToString();

            numberObj.GetComponent<TextMeshProUGUI>().text = "[" + _buyNum_s + "]";

            f.SetBuyNum(buyNum);
            FurnitureManager.F.FC_EditFurnitureClass(f);

            //2026: 첫 구매시, 색상이 회색에서 원본 색으로 바뀌어야 하기 때문에.
            if (this.gameObject.GetComponent<Image>().color != new Color(1f, 1f, 1f))
            {
                this.gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
            }

            return;
        }

        //2026: 가구는 10개 넘게 구매하지 못하도록 관리.
        else if (buyNum == 10) { return; }

        //coin을 차감. 만약 코인이 부족하면 구매 불가.
        int purchasePrice = GameManager.GM.GetCoin() - price;

        if (purchasePrice < 0) { return; }
        else if(purchasePrice > 0) { GameManager.GM.SetCoin(purchasePrice, true); }

        //현재 Str을 가져와서 사용중인 가구 값, 구매한 가구 값을 분리. 이후 구매한 가구 값에 1을 추가하여 다시 현재 Str로 반환.
        string nowNumStr = numberObj.GetComponent<TextMeshProUGUI>().text;      //TextMeshPro를 가져온 것.
        string useNum_s = nowNumStr.Substring(nowNumStr.LastIndexOf('[') + 1, nowNumStr.LastIndexOf('/') - 1);
        string buyNum_s = nowNumStr.Substring(nowNumStr.LastIndexOf('/') + 1, nowNumStr.LastIndexOf(']') - nowNumStr.LastIndexOf('/') - 1);

        //useNum도 Parse하는 이유: 혹시나 오류가 날 수도 있기 때문.
        useNum = int.Parse(useNum_s);
        buyNum = int.Parse(buyNum_s);

        //구매했으면 1 값을 올림.
        buyNum++;
        buyNum_s = buyNum.ToString();

        numberObj.GetComponent<TextMeshProUGUI>().text = "[" + useNum_s + "/" + buyNum_s + "]";

        f.SetBuyNum(buyNum);
        f.SetUseNum(useNum);
        FurnitureManager.F.FC_EditFurnitureClass(f);

        //이미지 색상 변경
        if (this.gameObject.GetComponent<Image>().color != new Color(1f, 1f, 1f))
        {
            this.gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
        }
    }

    public bool DuplicateFurniture()
    {
        //중복 가구가 있다면(중복된 이름이 있다면) true를 반환. 그렇지 않으면(중복된 가구가 아니라면) false 반환.

        var _f = FurnitureManager.F.GetListFurniture().GetEnumerator();

        for (int i = 0; i < FurnitureManager.F.GetListFurniture().Count; i++)
        {
            _f.MoveNext();

            if (_f.Current.GetGameObject().name == this.gameObject.name)
            {
                f = _f.Current;
                return true;
            }
        }

        return false;
    }

    //별 넣는 작업. 하는김에 자물쇠도 같이 만듦.
    public void FurnitureRank()
    {
        //2026: FurnitureManager 안에 있는 lockup을 복사해서 사용. 아래의 별들도 같은 구조로 생성된다.
        //2026: 하우징 UI의 가구에 달린 자물쇠가 언제 없어지냐는 FurnitureOpen()에서 관리한다.
        lockup = (GameObject)Instantiate(FurnitureManager.F.lockup, this.transform.position, Quaternion.identity);
        lockup.transform.SetParent(this.gameObject.transform);
        lockup.transform.localScale = Vector3.one;

        if (f_rank == 1)
        {
            GameObject star_1 = (GameObject)Instantiate(FurnitureManager.F.star, this.transform.position, Quaternion.identity);

            star_1.transform.SetParent(this.gameObject.transform);

            star_1.transform.localScale = Vector3.one;
            star_1.transform.localPosition = new Vector3(0, -70, 0);
        }
        else if (f_rank == 2)
        {
            GameObject star_1 = (GameObject)Instantiate(FurnitureManager.F.star, this.transform.position, Quaternion.identity);
            GameObject star_2 = (GameObject)Instantiate(FurnitureManager.F.star, this.transform.position, Quaternion.identity);

            star_1.transform.SetParent(this.gameObject.transform);
            star_2.transform.SetParent(this.gameObject.transform);

            star_1.transform.localScale = Vector3.one;
            star_1.transform.localPosition = new Vector3(-10, -70, 0);
            star_2.transform.localScale = Vector3.one;
            star_2.transform.localPosition = new Vector3(10, -70, 0);
        }
        else if (f_rank == 3)
        {
            GameObject star_1 = (GameObject)Instantiate(FurnitureManager.F.star, this.transform.position, Quaternion.identity);
            GameObject star_2 = (GameObject)Instantiate(FurnitureManager.F.star, this.transform.position, Quaternion.identity);
            GameObject star_3 = (GameObject)Instantiate(FurnitureManager.F.star, this.transform.position, Quaternion.identity);

            star_1.transform.SetParent(this.gameObject.transform);
            star_2.transform.SetParent(this.gameObject.transform);
            star_3.transform.SetParent(this.gameObject.transform);

            star_1.transform.localScale = Vector3.one;
            star_1.transform.localPosition = new Vector3(-15, -70, 0);
            star_2.transform.localScale = Vector3.one;
            star_2.transform.localPosition = new Vector3(0, -70, 0);
            star_3.transform.localScale = Vector3.one;
            star_3.transform.localPosition = new Vector3(15, -70, 0);
        }
    }

    public void FurnitureOpen()
    {
        //2026: 명성에 따라 값 변환. 여기서 보이는 200, 500과 같은 수는 위에서 변수로 관리하는 게 좋았을 것으로 보인다.
        if (GameManager.GM.GetFame() < 200)
        {
            //2성 가구 이상은 잠금을 달고 구매 버튼을 비활성화.
            if (f_rank >= 2)
            {
                this.gameObject.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
                btnObj.SetActive(false);
            }
            else
            {
                lockup.gameObject.SetActive(false);
            }
        }

        if (GameManager.GM.GetFame() >= 500)
        {
            //4성 미만의 가구는 잠금을 해제하고 구매 버튼 활성화.
            if (f_rank < 4)
            {
                lockup.gameObject.SetActive(false);
                btnObj.SetActive(true);
            }
            else
            {
                this.gameObject.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
                btnObj.SetActive(false);
            }
        }
        else if (GameManager.GM.GetFame() >= 200)
        {
            //200~500 사이의 명성을 가졌을 때.
            if (f_rank <3)
            {
                lockup.gameObject.SetActive(false);
                btnObj.SetActive(true);
            }
            else
            {
                this.gameObject.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
                btnObj.SetActive(false);
            }
        }
    }

    //아래로는 get, set
    public FurnitureClass GetFC() { return f; }
    public string GetFurnitureSize() { return f_size; }
    public int GetPrice() { return price; }
}

