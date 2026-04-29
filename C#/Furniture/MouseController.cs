using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    //mousePointerSprite    : 마우스 위치를 보여주기 위해 넣은 이미지.
    //Floor                 : 바닥 오브젝트. mousePointerSprite의 위치를 제한하기 위해서 가져왔음.
                            //만약 따로 오브젝트 설정을 하지 않는다면 new GameObject를 생성해야 함.

    GameObject mousePointer;    //생성할 '마우스 포인터'

    //2026: 기본적으로 '인게임에서 사용하는 마우스 커서 이미지'를 사용하나, 가구 배치 때에는 해당 이미지를 '가구 이미지'로 교체하여 사용한다.
    [Header("Plz Setting")]
    public Sprite mousePointerSprite;

    //현재 마우스 포인터가 가르키는 지점을 저장
    float curMousePosX;
    float curMousePosY;

    Vector3 lastMousePos;

    //UI에서 누른 가구의 시리얼 넘버를 저장. 이후 FurnitureSet에 FurnitureManager를 통해 전달함.
    int furniture_sn;
    string furniture_type;

    void Start()
    {
        mousePointer = new GameObject("MousePointer");
        mousePointer.AddComponent<MouseTrigger>();

        //2026: 마우스 포인터를 생성하며 '마우스 포인터 오브젝트의 위치 확인'과 '충돌 검사'만 하기 위한 작업.
        mousePointer.AddComponent<SpriteRenderer>().sprite = mousePointerSprite;
        mousePointer.AddComponent<BoxCollider2D>().isTrigger = true;
        mousePointer.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        mousePointer.GetComponent<Rigidbody2D>().simulated = false;

        mousePointer.transform.localScale = mousePointer.transform.localScale * 0.89f;

        FurnitureManager.F.SetOb_mouseController(mousePointer);
    }

    void Update()
    {
        //마우스 포인터가 보일지 말지 결정하는 것. FurnitureManager.cs의 furnitureState를 받아와서 결정.
        //2026: FurnitureState가 0이면 하우징을 하지 않는 때, 0이 아니면 하우징을 하는 때.
        if (FurnitureManager.F.GetFurnitureState() != 0)
        {
            mousePointer.gameObject.SetActive(true);
            lastMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lastMousePos.z = 0;

            //칸 단위 지정이 너무 어려워져서 기능 포기.
            mousePointer.transform.position = lastMousePos;
        }
        else if (FurnitureManager.F.GetFurnitureState() == 0)
        {
            mousePointer.gameObject.SetActive(false);
        }
    }

    public void SetSerialNumber_MC(int i) { furniture_sn = i; }
    public int GetSerialNumber_MC() {  return furniture_sn; }

    public void SetType_MC(string s) { furniture_type = s; }
    public string GetType_MC() { return furniture_type; }
}
