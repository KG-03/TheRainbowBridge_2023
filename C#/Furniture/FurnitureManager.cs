//Tag, "FurnitureTap", "Furniture", "Furniture_Table", "Furniture_Chair" Add Plz
//attach of 'FurnitureTap'

//해당 스크립트는 UI가 되는 창 중, 가구들이 담겨있는 탭에 넣어둘 것
//This Script is for UI(furnitureTap)

//IBeginDragHandler, IDragHandler, IEndDragHandler는 쓸데없이 Furniture가 선택되는 것을 방지하기 위해.
//https://higatsuryu9975.tistory.com/10
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FurnitureManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    static public FurnitureManager F;

    [Header("Plz Setting")]
    public ScrollRect[] childSR;
    public GameObject housingBtn;           //1번: On btn을 담을 것. 2번: Off btn을 담을 것.
    public GameObject furniture_prefab;
    public GameObject errorObj;
    public GameObject baseFurniture;
    public GameObject star;
    public GameObject lockup;
    public GameObject[] tagObj;
    public GameObject textArea_UI;

    [Header("Plz Setting_FWBD")]
    //아래로는 FWBD(Floor, Wall, tableBar, Door)
    public GameObject floorObj;
    public GameObject wallObj;
    public GameObject tablebarObj;
    public GameObject doorObj;

    //baseFurnitureSettingState : 이미 배치된 가구들을 코드적으로 설정해두었는가 체크.
    //SettingFurniture()를 start에 배치 시, 오류가 나서(리스트를 다 만들기 전에 해당 함수가 호출되는 것으로 보임) 변수 하나로 제어 중.
    private bool baseFurnitureSettingState = false;

    //ft                : FurnitureTap의 준말
    //furnitureState    : 하우징 모드의 상태. 1 == On, 0 == Off, 2 == ing, -1 == 설치오류
    //2026: isClickBtn  : 하우징 on, off 활성화할 때 사용.
    //chairNum          : 모든 Chair들을 조사. Direction이 0인 의자가 있을 때마다 값이 +1씩 오름.
    private Animator ft_animator;
    private int furnitureState = 0;
    private bool isCilckBtn = false;
    private int chairNum;

    GameObject ob_MCManager;            //MouseController가 있는 오브젝트.
    GameObject ob_mouseController;      //마우스 컨트롤러 오브젝트(선택상태 오브젝트)
    Sprite m_sprite;                    //마우스의 현재 스프라이트(선택상태 스프라이트)

    //furnitureNum  : 몇 개 설치했는지 명시. n개 이상 설치 불가능하게 만들 때 이용할 수 있을 것 같음.
    //isReady       : true가 아니면 가구 배치는 불가능함.
    //editFurniture : 현재 클릭한 가구는 '수정해야 하는 가구'임을 명시 true면 수정중인 가구, false면 수정중인 가구가 아님.
    private int furnitureNum;
    private bool isReady = false;
    bool editFurniture = false;

    //GameObject로 관리하지 않는 이유는 Edit 시점에 오브젝트를 삭제하는 방식을 이용하기 때문. GameObject가 Null이 되어버린다.
    //now : 현재 설치되는 가구
    /*
     * nowFurnitureSerialNum : Edit...()에서 이용됨. 잠깐 해당 가구와 연결될 UI의 시리얼 넘버를 담아둠.
     * nowPrefabAddress      : 사용할 프리팹의 주소지
     * nowFurnitureType      : Edit...()에서 이용됨. 해당 가구와 연결될 UI의 Type(어떤 가구인가)을 담아둠.
     * nowName               : ft의 이름도 담고 배치된 가구의 이름도 담음. 맨 뒤에 있는 'C'나 'T'등의 값을 읽기 위한 값.
     * furnitureBtnState     : true면 다른 가구를 선택하지 못하게끔 해야 함. (filp 등의 행동을 할 때 true가 됨)
     * installFurniture      : 현재 가구가 설치된 가구인 경우. 이 가구가 처음 설치한/설치하려는 가구인가?
     */
    int nowFurnitureSerialNum;
    int nowPrefabAddress;
    string nowFurnitureType;
    string nowName;
    bool installFurniture = false;

    //현재 화면에 놓인 가구들의 값의 총합.
    //벽지, 바닥, 테이블 바, 문의 값은 따로 저장해서 이용. FWBD
    int allPrice = 0;
    int floorPrice = 0;
    int wallPrice = 0;
    int tablebarPrice = 0;
    int doorPrice = 0;

    //furnitureTag      : 보이고자하는 가구가 '기본'인지 '용궁'인지 등을 결정. 기본값은 "Normal".
    //furnitureTag_fw   : '벽장식', '바닥장식' 버튼을 눌렀을 때 값이 설정됨. 1이면 바닥, 2면 벽.
    string furnitureTag = "Normal";
    int furnitureTag_fw = 1;
    string UIName;

    //ft_nowObj     : 방금 UI에서 누른 가구가 어떤 가구인지 저장. 이후 UI에서 가구를 몇 개 이용했는지 판별할 때 이용.
    GameObject ft_nowObj;

    //배치 정보 및 갯수를 저장해두기 위해서 따로 연결 리스트를 생성.
    //l_chiar       : 의자를 저장하기 위한 연결 리스트. NPC가 이용하기 때문에 따로 뜯어냄.
    //l_furniture   : (UI) 가구 수를 저장하기 위한 연결 리스트.
    LinkedList<FurnitureClass> l_furnitureUI = new LinkedList<FurnitureClass>();
    LinkedList<GameObject> l_chair = new LinkedList<GameObject>();

    private void Awake()
    {
        if (F == null)
        {
            F = this;
        }
        else
        {
            Debug.LogError("<color=red>FurnitureManager.Awake()</color> - attempted to assign second FurnitureManager.F!");
        }
    }

    void Start()
    {
        ft_animator = GameObject.Find("FurnitureTap").GetComponent<Animator>();
        ob_MCManager = GameObject.Find("MouseControllerManager");

        furnitureState = 0;
        isCilckBtn = false;

        //2026: 처음 시작할 때 모든 가구들에 '잠금'을 달아둔다. 본 '잠금'은 단순한 오브젝트에 불과하다.
        for (int i = 0; i < tagObj.Length; i++)
        {
            GameObject lockobj = (GameObject)Instantiate(lockup, tagObj[i].transform.position, Quaternion.identity);
            lockobj.transform.SetParent(tagObj[i].gameObject.transform);
            lockobj.transform.localScale = Vector3.one * 0.6f;
        }
    }

    void Update()
    {
        if (baseFurnitureSettingState == false) { SettingFurniture(); }

        //하우징 상태가 아닐 때: 하우징 버튼 on, 하우징 상태일 때: 되돌아가기 버튼 on

        //마우스가 어느 버튼을 누르든 눌렀다면 창이 아래로 내려가야만 함
        //돌아오는 조건은 다시한번 마우스가 클릭되었을 때.
        if (Input.GetMouseButtonUp(0))
        {
            //furnitureState: 하우징 상태를 의미
            if (furnitureState == 0) { return; }

            //가구를 눌렀는지를 체크
            Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(touchPos, Camera.main.transform.forward);
            if (hit.collider != null)
            {
                GameObject click = hit.transform.gameObject;
                
                //만약 오브젝트 이름이 바뀔 경우 수정할 것.
                if(click.name == "Door" || click.name == "PlayerPos") { return; }

                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    //furnitureBtn을 누르고 난 직후에 가구가 선택되는 걸 막기 위해 해당 if문으로 아래의 코드들을 감싸둠.
                    if (click.name == "DoorPos" || click.name == "PlayerPos") { return; }
                    if (click.tag.Substring(0, 3) == "Fur" && isReady == false) { EditFurniture(click); }
                    if ((click.tag == "Grid_Floor" || click.tag == "Grid_Wall") && isReady == true)
                    {
                        if (ob_mouseController.GetComponent<MouseTrigger>().TriggerCheck() == false) { PlacementLocation(click, touchPos); }
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            //생성 도중에 취소. 가구를 잘못 클릭했는데 배치한 뒤에 제거하기 귀찮아서 추가.
            if (isReady == true)
            {
                if (editFurniture == true)
                {
                    //현재 누른 가구 오브젝트로부터 UI 오브젝트를 참조할 수 있어야 함.
                    int fc_serial = nowFurnitureSerialNum;
                    FurnitureClass fc = FC_SearchFC(fc_serial);
                    fc.SetUseNum(fc.GetUseNum() - 1);

                    editFurniture = false;
                }

                furnitureNum--;
                furnitureState = 1;
                isReady = false;

                ob_mouseController.GetComponent<SpriteRenderer>().sprite = m_sprite;
                ob_mouseController.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }

        if (isReady == true && furnitureState == 0)
        {
            //가구 설치 중에 하우징이 꺼지는 경우
            ob_mouseController.GetComponent<SpriteRenderer>().sprite = m_sprite;
            isReady = false;
        }

        FC_FurniturePrice();
        LockdownFurnitureUI();
    }

    public void Housing()
    {
        //하우징 버튼을 누르면 호출되는 함수.

        //가구를 수정하고 있는 상태에서는 하우징이 꺼지지 않게 함.
        if (editFurniture == true) { return; }
        if (furnitureState == 2) { return; }

        //가구 수정 상태가 아님 + 가구 배치 상태가 오류가 아님 -> 하우징 on/off 활성화
        if (furnitureState != 2 || furnitureState != -1)
        {
            isCilckBtn = !isCilckBtn;
        }

        //furnitureState == -1, 배치에 문제가 생긴 상태.
        //현재는 의자-테이블이 연결되어 있지 않으면 -1이 됨.
        if (furnitureState == -1)
        {
            var _c = l_chair.GetEnumerator();

            //2026: 의자 자체가 하나도 없을 때 에러 출력.
            if (l_chair.Count == 0)
            {
                errorObj.SetActive(true);
                furnitureState = -1;
                isCilckBtn = true;
                return;
            }

            //의자의 자유로운 배치를 위해 의자-테이블이 연결상태가 아니더라도 error 메세지는 뜨지 않음.
            /*for (int i = 0; i < l_chair.Count; i++)
            {
                _c.MoveNext();
                string collision_tc = _c.Current.gameObject.GetComponent<FurnitureSet>().GetFurnitureDirection();
                if (collision_tc == "none")
                {
                    errorObj.SetActive(true);
                    furnitureState = -1;
                    isCilckBtn = true;
                    return;
                }
                else
                {
                    furnitureState = 0;
                }
            }*/

            //2026: 모든 의자들을 조사. 의자-테이블의 연결이 하나라도 있는지 확인.
            for (int i = 0; i < l_chair.Count; i++)
            {
                _c.MoveNext();

                if(_c.Current.GetComponent<FurnitureSet>().GetFurnitureDirection() == "none")
                {
                    chairNum++;
                }
                else { chairNum = 0; }
            }

            //2026: 모든 의자 조사 결과, 의자-테이블 연결이 하나도 없을 때 에러 출력.
            if(chairNum == l_chair.Count)
            {
                errorObj.SetActive(true);
                furnitureState = -1;
                isCilckBtn = true;
                return;
            }

            chairNum = 0;
            furnitureState = 0;
        }

        if (isCilckBtn == true)
        {
            furnitureState = 1;
        }
        else if (isCilckBtn == false)
        {
            //if, table과 chair가 만나는 상황이 아니라면 or 테이블 및 의자를 생성하지 않았다면
            //2026: 위의 if문(if (furnitureState == -1))과 내용이 같음. 따로 생성해둔 이유를 알 수 없음.
            var _c = l_chair.GetEnumerator();

            if(l_chair.Count == 0)
            {
                errorObj.SetActive(true);
                furnitureState = -1;
                isCilckBtn = true;
                return;
            }

            //의자의 자유로운 배치를 위해 의자-테이블이 연결상태가 아니더라도 error 메세지는 뜨지 않음.
            /*for (int i = 0; i < l_chair.Count; i++)
            {
                _c.MoveNext();

                string collision_tc = _c.Current.gameObject.GetComponent<FurnitureSet>().GetFurnitureDirection();
                if (collision_tc == "none")
                {
                    errorObj.SetActive(true);
                    furnitureState = -1;
                    isCilckBtn = true;
                    return;
                }
                else
                {
                    furnitureState = 0;
                }
            }*/

            for (int i = 0; i < l_chair.Count; i++)
            {
                _c.MoveNext();

                if (_c.Current.GetComponent<FurnitureSet>().GetFurnitureDirection() == "none")
                {
                    chairNum++;
                }
                else { chairNum = 0; }
            }

            if (chairNum == l_chair.Count)
            {
                errorObj.SetActive(true);
                furnitureState = -1;
                isCilckBtn = true;
                return;
            }

            chairNum = 0;
            furnitureState = 0;
        }

        //2026: 애니메이션 부분
        if (furnitureState == 1)
        {
            ft_animator.SetTrigger("FurnitureTapUp");
        }
        else if(furnitureState == 0)
        {
            ft_animator.SetTrigger("FurnitureTapDown");
        }
    }

    //아래로는 실제 설치 및 수정 제거와 관련된 함수들.
    public void FurnitureDeployment(GameObject obj)
    {
        //설치할 가구를 들 때(클릭했을 때) 호출되는 함수
        //UI를 눌렀을 때 호출됨.
        //2026: 아래의 if문이 왜 있는지 불명. Furniture UI에서 오브젝트의 태그를 지정하는 상황인 건 알겠으나, 그렇다면 FurnitureTap이 아닐 때 return을 하는 게 나았을 것.
        if (obj.tag == "Furniture")
        {
            return;
        }
        ob_mouseController.GetComponent<SpriteRenderer>().sprite = obj.GetComponent<Image>().sprite;
        ob_MCManager.GetComponent<MouseController>().SetSerialNumber_MC(obj.GetInstanceID());
        ob_MCManager.GetComponent<MouseController>().SetType_MC(obj.GetComponent<FurnitureData>().f_type);
        nowName = obj.name;

        //여기서 프리팹 사이즈를 기억해두어야함
        string f_size = obj.GetComponent<FurnitureData>().GetFurnitureSize();
        string[] sizes = f_size.Split('x');

        ob_mouseController.GetComponent<BoxCollider2D>().size = new Vector2(float.Parse(sizes[0]), float.Parse(sizes[1]));

        //설치하려고 누른 가구가 만약 '의자'일 때, 그리고 그것이 아닐 때를 구분.
        if (nowName.Substring(nowName.Length - 1, 1) == "C")
        {
            ob_mouseController.GetComponent<MouseTrigger>().SetFurnitureKind("C");
        }
        else
        {
            ob_mouseController.GetComponent<MouseTrigger>().SetFurnitureKind(nowName.Substring(0, 1));
        }

        furnitureState = 2;
        isReady = true;

        nowFurnitureSerialNum = obj.GetInstanceID();
    }

    public void PlacementLocation(GameObject obj, Vector2 touchPos)
    {
        //가구를 설치할 때 호출되는 함수. 실제로 오브젝트를 생성하는 함수.
        //일단 가구를 생성하고 나머지는 FurnitureBtn()쪽에서 해결함.
        //2026: 여기서 obj는 '배치하려는 장소'에 가깝다. 배치하려는 장소에 '가구'가 배치되어 있으면 계속 배치 상태에 놓이게 된다.
        if (obj.tag == "Furniture")
        {
            ob_mouseController.GetComponent<SpriteRenderer>().sprite = m_sprite;
            isReady = false;
            return;
        }

        //2026: '타일 형식으로 배치했을 때' 이용했던 코드. 언제 다시 롤백할지 알 수 없어 남겨두었다.
        //string tileLayerNameNum = obj.name.Substring(obj.name.Length - 1);
        //int tileLayer = int.Parse(tileLayerNameNum);

        //이 오브젝트의 부모의 이름이 무엇이냐로 구분지어도 OK일 것 같음.
        //벽인지 바닥인지 검사
        if (obj.tag == "Grid_Floor" && nowName.Substring(0, 1) == "F")
        {
            //FurnitureDeployment에서 기억한 프리팹 사이즈를 사용해서 몇번 프리팹을 쓸것인지 결정
            //바닥
            GameObject newFurniture = (GameObject)Instantiate(furniture_prefab, new Vector3(touchPos.x, touchPos.y), Quaternion.identity);
            newFurniture.GetComponent<SpriteRenderer>().sprite = ob_mouseController.GetComponent<SpriteRenderer>().sprite;
            //newFurniture.GetComponent<SpriteRenderer>().sortingOrder = 1;
            newFurniture.transform.position = new Vector3(newFurniture.transform.position.x, newFurniture.transform.position.y, newFurniture.transform.position.z + 5);

            //현재 가구의 이름이 '의자'라고 한다면 실행.
            //이 코드가 정상적으로 운용되기 위해서는 의자로 쓸 오브젝트(UI)명의 맨 뒤에 'C'를 달아야 함.
            if (nowName.Substring(nowName.Length - 1, 1) == "C")
            {
                newFurniture.tag = "Furniture_Chair";
                newFurniture.name = "F_Furniture_C";
                l_chair.AddLast(newFurniture.gameObject);
            }
            else if (nowName.Substring(nowName.Length - 1, 1) == "T")
            {
                newFurniture.tag = "Furniture_Table";
                newFurniture.name = "F_Furniture_T";
            }
            else
            {
                newFurniture.tag = "Furniture";
                newFurniture.name = "F_Furniture";
            }

            if (editFurniture == false)
            {
                //수정하려고 하는 가구가 아닐 때.
                newFurniture.GetComponent<FurnitureSet>().SetSerialNumber_FS(
                    ob_MCManager.GetComponent<MouseController>().GetSerialNumber_MC());
                newFurniture.GetComponent<FurnitureSet>().SetType_FS(
                    ob_MCManager.GetComponent<MouseController>().GetType_MC());
                furnitureNum++;
            }
            else if (editFurniture == true)
            {
                //수정하는 가구일 때.
                newFurniture.GetComponent<FurnitureSet>().SetSerialNumber_FS(nowFurnitureSerialNum);
                newFurniture.GetComponent<FurnitureSet>().SetType_FS(nowFurnitureType);
                editFurniture = false;
            }

            //콜라이더 사이즈 변경
            newFurniture.GetComponent<BoxCollider2D>().size = ob_mouseController.GetComponent<BoxCollider2D>().size;
        }

        if (obj.tag == "Grid_Wall" && nowName.Substring(0, 1) == "W")
        {
            //벽
            GameObject newFurniture = (GameObject)Instantiate(furniture_prefab, new Vector3(touchPos.x, touchPos.y), Quaternion.identity);
            newFurniture.GetComponent<SpriteRenderer>().sprite = ob_mouseController.GetComponent<SpriteRenderer>().sprite;
            //newFurniture.GetComponent<SpriteRenderer>().sortingOrder = 1;
            newFurniture.transform.position = new Vector3(newFurniture.transform.position.x, newFurniture.transform.position.y, newFurniture.transform.position.z + 5);

            newFurniture.tag = "Furniture";
            newFurniture.name = "W_Furniture";

            if (editFurniture == false)
            {
                newFurniture.GetComponent<FurnitureSet>().SetSerialNumber_FS(
                    ob_MCManager.GetComponent<MouseController>().GetSerialNumber_MC());
                newFurniture.GetComponent<FurnitureSet>().SetType_FS(
                    ob_MCManager.GetComponent<MouseController>().GetType_MC());
                furnitureNum++;
            }
            else if (editFurniture == true)
            {
                newFurniture.GetComponent<FurnitureSet>().SetSerialNumber_FS(nowFurnitureSerialNum);
                newFurniture.GetComponent<FurnitureSet>().SetType_FS(nowFurnitureType);
                editFurniture = false;
            }

            //콜라이더 사이즈 변경
            newFurniture.GetComponent<BoxCollider2D>().size = ob_mouseController.GetComponent<BoxCollider2D>().size;
        }

        if (editFurniture == true) { return; }

        if (installFurniture == false)
        {
            FurnitureClass fc = FC_SearchFC(ft_nowObj);
            fc.SetUseNum(fc.GetUseNum() + 1);
            installFurniture = true;
        }

        ob_mouseController.GetComponent<SpriteRenderer>().sprite = m_sprite;
        ob_mouseController.GetComponent<SpriteRenderer>().color = Color.white;
        furnitureState = 1;
        isReady = false;
    }

    public void EditFurniture(GameObject obj)
    {
        //가구 위치를 수정할 때 호출되는 함수.

        if(furnitureState == 0)
        {
            //수정해서는 안 되는 시점
            isReady = false;
            return;
        }

        editFurniture = true;
        ob_mouseController.GetComponent<SpriteRenderer>().sprite = obj.GetComponent<SpriteRenderer>().sprite;

        nowFurnitureSerialNum = obj.GetComponent<FurnitureSet>().GetSerialNumber_FS();
        nowFurnitureType = obj.GetComponent<FurnitureSet>().GetType_FS();
        nowName = obj.name;
        isReady = true;

        //여기서도 프리팹 사이즈를 기억해두어야함. FurnitureDeployment에서 썼던 변수를 이용해도 될 것 같음.
        //시리얼 넘버를 불러와서 그것과 같은 링크드 리스트를 불러옴 > 링크드 리스트에 저장된 string을 가져와서 address를 변경
        FurnitureClass f = FC_SearchFC(obj.GetComponent<FurnitureSet>().GetSerialNumber_FS());
        string f_size = f.GetFurnitureSize();
        string[] sizes = f_size.Split('x');

        ob_mouseController.GetComponent<BoxCollider2D>().size = new Vector2(float.Parse(sizes[0]), float.Parse(sizes[1]));

        if (nowName.Substring(nowName.Length - 1, 1) == "C")
        {
            ob_mouseController.GetComponent<MouseTrigger>().SetFurnitureKind("C");
        }
        else
        {
            ob_mouseController.GetComponent<MouseTrigger>().SetFurnitureKind(nowName.Substring(0, 1));
        }

        //만약 수정하려는 것이 '의자'라고 한다면
        //2026: 이것을 왜 위의 if문에서 처리하지 않았는지는 불명.
        if (obj.name.Substring(obj.name.Length - 1, 1) == "C")
        {
            l_chair.Remove(obj);
        }

        Destroy(obj);
    }

    //벽지, 바닥, 테이블바, 문 가구를 배치하려 할 때 호출됨.
    public void FWBD(GameObject obj)
    {
        //2026: 가구 배치와 다르게 벽지, 바닥, 테이블바, 문은 이미지(Sprite)만 변경하면 되기 때문에 따로 관리.
        if (obj.GetComponent<FurnitureData>().f_type == "wall")
        {
            wallObj.GetComponent<SpriteRenderer>().sprite = obj.GetComponent<Image>().sprite;
            wallPrice = obj.gameObject.GetComponent<FurnitureData>().GetPrice();
        }
        else if (obj.GetComponent<FurnitureData>().f_type == "floor")
        {
            floorObj.GetComponent<SpriteRenderer>().sprite = obj.GetComponent<Image>().sprite;
            floorPrice = obj.gameObject.GetComponent<FurnitureData>().GetPrice();
        }
        else if (obj.GetComponent<FurnitureData>().f_type == "tablebar")
        {
            tablebarObj.GetComponent<SpriteRenderer>().sprite = obj.GetComponent<Image>().sprite;
            tablebarPrice = obj.gameObject.GetComponent<FurnitureData>().GetPrice();
        }
        else if (obj.GetComponent<FurnitureData>().f_type == "door")
        {
            doorObj.GetComponent<SpriteRenderer>().sprite = obj.GetComponent<Image>().sprite;
            doorPrice = obj.gameObject.GetComponent<FurnitureData>().GetPrice();
        }
    }

    //이미 배치된 가구들을 저장함
    public void SettingFurniture()
    {
        string type_FS;
        string type_FC;

        //2026: _f는 가구 수를 저장하기 위한 연결 리스트의 주소지 역할 중. 그대로 사용하면 데이터 오염이 있기 때문.
        var _f = l_furnitureUI.GetEnumerator();

        for (int i = 0; i < l_furnitureUI.Count; i++)
        {
            _f.MoveNext();
            type_FC = _f.Current.GetFurnitureType();

            if (_f.Current.GetRank() == 0 && _f.Current.GetTheme() == "normal")
            {
                for(int j = 0; j < baseFurniture.transform.childCount; j++)
                {
                    type_FS = baseFurniture.transform.GetChild(j).GetComponent<FurnitureSet>().GetType_FS();

                    if(type_FC == type_FS)
                    {
                        //두 개의 같은 타입일 경우, BaseFurniture.GetChild(j) 오브젝트가 현재 UI로부터 파생된 가구로 판단.
                        //단, 이 방식은 '노말 타입의 0성 의자가 둘'인 경우와 같은 상황이 된다면, 제대로 동작하지 않을 것임.
                        baseFurniture.transform.GetChild(j).GetComponent<FurnitureSet>().SetSerialNumber_FS(_f.Current.GetSerialNumber());

                        //동종 가구의 구매 수를 늘림 + 사용 수를 늘림
                        _f.Current.SetBuyNum(_f.Current.GetBuyNum() + 1);
                        if (_f.Current.GetGameObject().tag != "FWBD") { _f.Current.SetUseNum(_f.Current.GetUseNum() + 1); }

                        //해당 가구 이미지 색상을 본래대로.
                        _f.Current.GetGameObject().GetComponent<Image>().color = new Color(1f, 1f, 1f);

                        installFurniture = true;
                        furnitureNum++;

                        if (type_FS == "chair") { l_chair.AddLast(baseFurniture.transform.GetChild(j).gameObject); }
                    }
                }
            }
        }

        FC_FurniturePrice();

        baseFurnitureSettingState = true;
    }

    //아래로는 UI 관련
    public void SortingFurnitureUI()
    {
        //이 함수는 Sorting에 대한 함수. 모든 tag 버튼들은 해당 함수를 쓰도록 할 것.
        //의자부터 Sorting하는 방법을 알아내야함...
        //UI를 '불러와서' 자리를 배치해야만함.
        //조건이 아닌 UI는 _f.Current.GetGameObject().SetActive(false).
        //2026: UI 내부의 '가구 아이콘'에 적용하는 함수. '가구 테마 해금 관련 함수'는 LockdownFurnitureUI)에 구현되어 있다.

        UIName = EventSystem.current.currentSelectedGameObject.name;
        var _f = l_furnitureUI.GetEnumerator();

        //2026: 게임 내 UI 이름이(테마 설정 버튼의 이름이) 'Normal', 'SeaKingdom', 'Heaven', 'Halloween', 'Floor', 'Wall'이어야만 제대로 동작한다.
        if (GameManager.GM.GetFame() < 100)
        {
            //호감도가 100 이하면 Normal 이외의 태그를 눌러도 아무런 행동을 하지 않도록 함.
            if(UIName == "SeaKingdom" || UIName == "Heaven" || UIName == "Halloween")
            {
                return;
            }
        }

        if (UIName == "Normal" || UIName == "SeaKingdom" || UIName == "Heaven" || UIName == "Halloween")
        {
            furnitureTag = UIName;
            //주석을 풀면 태그를 바꿀 때 자동으로 바닥 장식부터 보이게끔 설정됨.
            //furnitureTag_fw = 1;
        }
        else if(UIName == "Floor")
        {
            if (furnitureTag_fw == 2) furnitureTag_fw = 1;
        }
        else if(UIName == "Wall")
        {
            if (furnitureTag_fw == 1) furnitureTag_fw = 2;
        }

        for (int i = 0; i < l_furnitureUI.Count; i++)
        {
            _f.MoveNext();

            if(furnitureTag_fw == 1)
            {
                //바닥 장식만 표시
                //2026: Contains(...) : ... 안에 들어간 문자열이 있는지 확인. 있으면 true.
                //2026: decoration 키워드가 들어간 것들은 'table', 'chair', 'floor', 'tablebar' 등이 아닌 가구. plant, clock, moblie 등이 존재한다.
                if (_f.Current.GetTheme() == "normal" &&
                    (_f.Current.GetFurnitureType().Contains("floor-decoration") ||
                    _f.Current.GetFurnitureType() == "table" ||
                    _f.Current.GetFurnitureType() == "chair" ||
                    _f.Current.GetFurnitureType() == "floor" ||
                    _f.Current.GetFurnitureType() == "tablebar") &&
                    furnitureTag == "Normal")
                {
                    //기본
                    _f.Current.GetGameObject().SetActive(true);
                }
                else if (_f.Current.GetTheme() == "seakingdom" &&
                    (_f.Current.GetFurnitureType().Contains("floor-decoration") ||
                    _f.Current.GetFurnitureType() == "table" ||
                    _f.Current.GetFurnitureType() == "chair" ||
                    _f.Current.GetFurnitureType() == "floor" ||
                    _f.Current.GetFurnitureType() == "tablebar") &&
                    furnitureTag == "SeaKingdom")
                {
                    //용궁
                    _f.Current.GetGameObject().SetActive(true);
                }
                else if (_f.Current.GetTheme() == "heaven" &&
                    (_f.Current.GetFurnitureType().Contains("floor-decoration") ||
                    _f.Current.GetFurnitureType() == "table" ||
                    _f.Current.GetFurnitureType() == "chair" ||
                    _f.Current.GetFurnitureType() == "floor" ||
                    _f.Current.GetFurnitureType() == "tablebar") &&
                    furnitureTag == "Heaven")
                {
                    //천국
                    _f.Current.GetGameObject().SetActive(true);
                }
                else if (_f.Current.GetTheme() == "halloween" &&
                    (_f.Current.GetFurnitureType().Contains("floor-decoration") ||
                    _f.Current.GetFurnitureType() == "table" ||
                    _f.Current.GetFurnitureType() == "chair" ||
                    _f.Current.GetFurnitureType() == "floor" ||
                    _f.Current.GetFurnitureType() == "tablebar") &&
                    furnitureTag == "Halloween")
                {
                    //할로윈
                    _f.Current.GetGameObject().SetActive(true);
                }
                else
                {
                    _f.Current.GetGameObject().SetActive(false);
                }
            }
            else if(furnitureTag_fw == 2)
            {
                //벽장식만 표시
                if (_f.Current.GetTheme() == "normal" &&
                    (_f.Current.GetFurnitureType().Contains("wall-decoration") ||
                    _f.Current.GetFurnitureType() == "wall" ||
                    _f.Current.GetFurnitureType() == "door") &&
                    furnitureTag == "Normal")
                {
                    //기본
                    _f.Current.GetGameObject().SetActive(true);
                }
                else if (_f.Current.GetTheme() == "seakingdom" &&
                    (_f.Current.GetFurnitureType().Contains("wall-decoration") ||
                    _f.Current.GetFurnitureType() == "wall" ||
                    _f.Current.GetFurnitureType() == "door") &&
                    furnitureTag == "SeaKingdom")
                {
                    //용궁
                    _f.Current.GetGameObject().SetActive(true);
                }
                else if (_f.Current.GetTheme() == "heaven" &&
                    (_f.Current.GetFurnitureType().Contains("wall-decoration") ||
                    _f.Current.GetFurnitureType() == "wall" ||
                    _f.Current.GetFurnitureType() == "door") &&
                    furnitureTag == "Heaven")
                {
                    //천국
                    _f.Current.GetGameObject().SetActive(true);
                }
                else if (_f.Current.GetTheme() == "halloween" &&
                    (_f.Current.GetFurnitureType().Contains("wall-decoration") ||
                    _f.Current.GetFurnitureType() == "wall" ||
                    _f.Current.GetFurnitureType() == "door") &&
                    furnitureTag == "Halloween")
                {
                    //할로윈
                    _f.Current.GetGameObject().SetActive(true);
                }
                else
                {
                    _f.Current.GetGameObject().SetActive(false);
                }
            }
        }

        SortingFurnitureUITextArea();
    }

    public void BTN_GetClick(GameObject click)
    {
        //구매 수 > 사용 수 일 때 FurnitureDeployment()가 동작할 수 있도록 설계됨.
        //가구 UI 오브젝트가 Button 오브젝트로 변경되며 생성한 함수.
        //입력 파라미터의 click은 가구 UI 오브젝트 자기 자신을 받으면 됨.
        
        //2026: 카탈로그 내의 가구 이미지(게임 오브젝트, 버튼)로부터 호출되는 함수. 유니티 내에서 이 함수를 지정해두어야 한다.
        //2026: 혹시나 값이 오염될 것을 고려하여 bool 타입 변수를 선언해서 활용하려고 했던 것으로 기억한다.
        bool judge = FC_UseBuyJudge(click);
        if (judge == true)
        {
            installFurniture = false;
            ft_nowObj = click;
            if (click.tag == "FWBD") { FWBD(click); }
            else { FurnitureDeployment(click); }
        }
    }

    public void LockdownFurnitureUI()
    {
        //해당 if문은 '기본', '용궁' 등의 테마 이미지와 관련된 if문.
        //단지 회색처리를 할 건지, 안 할 건지만 결정.
        //처리가 엉성하게 되어 있으니 추후 고칠 수 있으면 고쳐둘 것.

        //2026: 하드 코딩으로 이루어져 있다. 0: Normal, 1: SeaKingdom, 2: Heaven, 3: Halloween, 4:  Floor, 5: Wall
        //2026: 인게임에서 얻을 수 있는 명성에 따라 가구가 해금되는데, 본 함수는 '가구 자체의 해금'이 아니라, '가구의 테마 해금'에 가깝다.
        //2026: 덧붙여, '특정 테마를 눌렀을 때' 현재 어떤 테마를 눌렀냐를 따져서 색상이 달라진다.
        //2026: '가구 자체의 해금'(아이콘 해금) 함수는 SortingFurnitureUI()에 구현되어 있다.
        if (GameManager.GM.GetFame() < 100)
        {
            tagObj[0].gameObject.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);
            tagObj[1].gameObject.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
            tagObj[2].gameObject.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
            tagObj[3].gameObject.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);

            tagObj[0].transform.GetChild(0).gameObject.SetActive(false);
            tagObj[4].transform.GetChild(0).gameObject.SetActive(false);
            tagObj[5].transform.GetChild(0).gameObject.SetActive(false);
        }
        else if (GameManager.GM.GetFame() >= 100)
        {
            tagObj[1].transform.GetChild(0).gameObject.SetActive(false);
            tagObj[2].transform.GetChild(0).gameObject.SetActive(false);
            tagObj[3].transform.GetChild(0).gameObject.SetActive(false);

            if (furnitureTag == "Normal")
            {
                tagObj[0].gameObject.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);
                tagObj[1].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[2].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[3].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
            }
            else if (furnitureTag == "SeaKingdom")
            {
                tagObj[0].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[1].gameObject.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);
                tagObj[2].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[3].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
            }
            else if (furnitureTag == "Heaven")
            {
                tagObj[0].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[1].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[2].gameObject.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);
                tagObj[3].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
            }
            else if (furnitureTag == "Halloween")
            {
                tagObj[0].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[1].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[2].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[3].gameObject.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);
            }
            else
            {
                tagObj[0].gameObject.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);
                tagObj[1].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[2].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
                tagObj[3].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
            }
        }

        if (furnitureTag_fw == 1)
        {
            tagObj[4].gameObject.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);
            tagObj[5].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
        }
        else if (furnitureTag_fw == 2)
        {
            tagObj[4].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f);
            tagObj[5].gameObject.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);
        }
    }

    public void SortingFurnitureUITextArea()
    {
        //하드코딩으로 하게 되었음.........
        //2026: UI 설명을 위한 칸. '테이블 항목에 있는 0성 테이블과 1성 테이블'이라는 말이 있다면, '테이블 항목'을 명시하는 코드.
        //2026: 설명 위치는 유니티에서 게임 오브젝트들로 미리 잡아둔 상태. 이 게임 오브젝트를 감싸는 부모 오브젝트를 textArea_UI로 받아서 사용 중이다.

        //기본-바닥
        if (furnitureTag == "Normal" && furnitureTag_fw == 1)
        {
            textArea_UI.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "테이블";
            textArea_UI.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = "의자";
            textArea_UI.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = "바닥 장식";
            textArea_UI.transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>().text = "바닥";
            textArea_UI.transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>().text = "테이블바";
            textArea_UI.transform.GetChild(5).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(6).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
        }
        //기본-벽
        else if (furnitureTag == "Normal" && furnitureTag_fw == 2)
        {
            textArea_UI.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "메뉴판";
            textArea_UI.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = "벽 장식";
            textArea_UI.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>().text = "벽지";
            textArea_UI.transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>().text = "문";
            textArea_UI.transform.GetChild(5).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(6).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
        }
        //용궁-바닥
        else if (furnitureTag == "SeaKingdom" && furnitureTag_fw == 1)
        {
            textArea_UI.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "테이블";
            textArea_UI.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = "의자";
            textArea_UI.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = "바닥 장식";
            textArea_UI.transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>().text = "바닥";
            textArea_UI.transform.GetChild(5).gameObject.GetComponent<TextMeshProUGUI>().text = "테이블바";
            textArea_UI.transform.GetChild(6).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
        }
        //용궁-벽
        else if (furnitureTag == "SeaKingdom" && furnitureTag_fw == 2)
        {
            textArea_UI.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "벽 장식";
            textArea_UI.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = "벽지";
            textArea_UI.transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>().text = "문";
            textArea_UI.transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(5).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(6).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
        }
        //천국-바닥
        else if (furnitureTag == "Heaven" && furnitureTag_fw == 1)
        {
            textArea_UI.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "테이블";
            textArea_UI.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = "의자";
            textArea_UI.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = "바닥 장식";
            textArea_UI.transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>().text = "바닥";
            textArea_UI.transform.GetChild(5).gameObject.GetComponent<TextMeshProUGUI>().text = "테이블바";
            textArea_UI.transform.GetChild(6).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
        }
        //천국-벽
        else if (furnitureTag == "Heaven" && furnitureTag_fw == 2)
        {
            textArea_UI.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "벽 장식";
            textArea_UI.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = "벽지";
            textArea_UI.transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>().text = "문";
            textArea_UI.transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(5).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(6).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
        }
        //할로윈-바닥
        else if(furnitureTag == "Halloween" && furnitureTag_fw == 1)
        {
            textArea_UI.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "테이블";
            textArea_UI.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = "의자";
            textArea_UI.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = "바닥 장식";
            textArea_UI.transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>().text = "벽지";
            textArea_UI.transform.GetChild(5).gameObject.GetComponent<TextMeshProUGUI>().text = "문";
            textArea_UI.transform.GetChild(6).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
        }
        //할로윈-벽
        else if(furnitureTag == "Halloween" && furnitureTag_fw == 2)
        {
            textArea_UI.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "벽 장식";
            textArea_UI.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = "벽지";
            textArea_UI.transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>().text = "문";
            textArea_UI.transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(5).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
            textArea_UI.transform.GetChild(6).gameObject.GetComponent<TextMeshProUGUI>().text = " ";
        }
    }

    //아래로부터는 get, set
    public void SetOb_mouseController(GameObject obj)
    {
        ob_mouseController = obj;
        m_sprite = ob_mouseController.GetComponent<SpriteRenderer>().sprite;
    }

    public int GetFurnitureState() { return furnitureState; }

    public void SetFurnitureState(int i) { furnitureState = i; } 

    public void SetIsClickBTN(bool b) { isCilckBtn = b; }

    public LinkedList<GameObject> GetListChair() { return l_chair; }

    public LinkedList<FurnitureClass> GetListFurniture() { return l_furnitureUI; }

    public bool GetIsReady() { return isReady; }

    //아래로부터는 FurnitureClass와 관련된 함수들.
    public void FC_SetFurnitureClass(FurnitureClass f)
    {
        //FurnitureData() 스크립트를 가지는 GameObject들은 모두 해당 함수를 호출하게 되어있음.
        //현재 UI에 있는 가구들(FurnitureTap이 적용된 오브젝트)의 값들을 연결 리스트 형태로 저장.

        var _f = l_furnitureUI.GetEnumerator();

        //if : 뭐라도 할당이 된 상황, else : 할당이 아예 되어있지 않은 상황
        if (l_furnitureUI.Count == 1)
        {
            _f.MoveNext();

            if (_f.Current.GetSerialNumber() != f.GetSerialNumber())
            {
                l_furnitureUI.AddLast(f);
            }
        }
        else
        {
            l_furnitureUI.AddLast(f);
        }
    }

    public void FC_EditFurnitureClass(FurnitureClass f)
    {
        //useNum과 buyNum을 변환하는 함수로 이용됨.
        //저장된 것과 비교
        var _f = l_furnitureUI.GetEnumerator();
        for (int i = 0; i < l_furnitureUI.Count; i++)
        {
            _f.MoveNext();
            if (_f.Current.GetSerialNumber() == f.GetSerialNumber())
            {
                //현재 UseNum과 BuyNum이 다르다면 새로 생성.
                //2026: 새로 생성한다기보다는 다시 세팅.
                if (_f.Current.GetUseNum() != f.GetUseNum()) { _f.Current.SetUseNum(f.GetUseNum()); }
                if (_f.Current.GetBuyNum() != f.GetBuyNum()) { _f.Current.SetBuyNum(f.GetBuyNum()); }
            }
        }
    }

    public bool FC_UseBuyJudge(GameObject obj)
    {
        //구매량과 사용량을 체크하는 함수.
        //2026: 구매한 수가 사용한 수 보다 적을 때 가구를 배치할 수 있어야 하기 때문에 확인한다.
        var _f = l_furnitureUI.GetEnumerator();

        for (int i = 0; i < l_furnitureUI.Count; i++)
        {
            _f.MoveNext();

            if (_f.Current.GetSerialNumber() == obj.GetInstanceID())
            {
                //구매한 적 없는 가구면 그냥 false
                //buy보다 use가 많아지려고 한다면 false
                if (_f.Current.GetBuyNum() == 0) { return false; }
                if (_f.Current.GetUseNum() >= _f.Current.GetBuyNum()) { return false; }
            }
        }

        return true;
    }

    public FurnitureClass FC_SearchFC(GameObject obj)
    {
        //지금 받은 오브젝트와 동일한 오브젝트를 가진 클래스가 있는지 조사.

        var _f = l_furnitureUI.GetEnumerator();

        for (int i = 0; i < l_furnitureUI.Count; i++)
        {
            _f.MoveNext();

            if (_f.Current.GetSerialNumber() == obj.GetInstanceID())
            {
                return _f.Current;
            }
        }

        return null;
    }

    public FurnitureClass FC_SearchFC(int sn)
    {
        //지금 받은 오브젝트와 동일한 오브젝트를 가진 클래스가 있는지 조사.
        //2026: 오브젝트라기보다는 '오브젝트의 시리얼 넘버'가 있는지 조사.

        var _f = l_furnitureUI.GetEnumerator();

        for (int i = 0; i < l_furnitureUI.Count; i++)
        {
            _f.MoveNext();

            if (_f.Current.GetSerialNumber() == sn)
            {
                return _f.Current;
            }
        }
        Debug.Log("찾으려는 FC가 없음!");
        return null;
    }

    public void FC_FurniturePrice()
    {
        //현재 맵에 보이는 가구들의 가격 총합을 구함.
        allPrice = 0;
        var _f = l_furnitureUI.GetEnumerator();

        for (int i = 0; i < l_furnitureUI.Count; i++)
        {
            _f.MoveNext();

            //UI에 적혀있는 '가구의 가격' * '배치된 가구의 숫자'로 동작.
            allPrice += _f.Current.GetUseNum() * _f.Current.GetPrice();
        }

        //2026: FWTD는 변경과 동시에 값이 바뀌기 때문에 여기서 관리하지 않는다.
        GameManager.GM.SetFurniturePrice(allPrice + floorPrice + wallPrice + tablebarPrice + doorPrice);
    }

    //아래로는 스크롤 뷰를 위한 함수들.
    public void OnBeginDrag(PointerEventData e)
    {
        for (int i = 0; i < childSR.Length; i++)
        {
            childSR[i].OnBeginDrag(e);
        }
    }

    public void OnDrag(PointerEventData e)
    {
        for (int i = 0; i < childSR.Length; i++)
        {
            childSR[i].OnDrag(e);
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        for (int i = 0; i < childSR.Length; i++)
        {
            childSR[i].OnEndDrag(e);
        }
    }

    public void OnScroll(PointerEventData e)
    {
        for (int i = 0; i < childSR.Length; i++)
        {
            childSR[i].OnScroll(e);
        }
    }
}

public class FurnitureClass
{
    //이후 Save-Load에서 구매한 가구의 수를 저장해두어야 하기 때문에 따로 클래스를 생성.
    int f_serialNum;
    GameObject f_gameObject;
    int f_useNum;
    int f_buyNum;
    int f_price;
    string f_theme;
    int f_rank;
    string f_type;
    string f_size;

    public FurnitureClass(int f_serialNum, GameObject f_gameObject, int f_useNum, int f_buyNum, int f_price, string f_theme, int f_rank, string f_type, string f_size)
    {
        this.f_serialNum = f_serialNum;
        this.f_gameObject = f_gameObject;
        this.f_useNum = f_useNum;
        this.f_buyNum = f_buyNum;
        this.f_price = f_price;
        this.f_theme = f_theme;
        this.f_rank = f_rank;
        this.f_type = f_type;
        this.f_size = f_size;
    }

    public int GetSerialNumber() { return f_serialNum; }
    public GameObject GetGameObject() { return f_gameObject; }
    public int GetUseNum() { return f_useNum; }
    public int GetBuyNum() { return f_buyNum; }
    public int GetPrice() { return f_price; }
    public string GetTheme() { return f_theme;}
    public int GetRank() { return f_rank;}
    public string GetFurnitureType() { return f_type; }
    public string GetFurnitureSize() {  return f_size;}

    //시리얼 넘버와 게임 오브젝트, 테마와 랭크는 다시 설정할 필요가 없으므로 생성해두지 않았음.
    public void SetUseNum(int i) {  f_useNum = i; }
    public void SetBuyNum(int i) { f_buyNum = i; }
}
