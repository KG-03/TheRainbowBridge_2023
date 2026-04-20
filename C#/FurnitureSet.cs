//attach of 'Furniture'(Prefab)
//2026: 화면에 배치되는 가구 오브젝트에 들어가는 클래스.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FurnitureSet : MonoBehaviour
{
    //MouseController에서 FurnitureManager를 통해 전달받음.
    int furniture_sn = 0;
    public string furniture_type = null;

    //NPC와 접촉하고 있다면 true, 접촉하고 있지 않다면 true.
    private bool isNPCState = false;

    //의자-테이블 사이의 좌측과 우측을 판별.
    //tc: table-chair
    //direction : 의자의 방향을 나타냄.
    Collider2D[] colls;
    string furnitureDirection = "none";

    private void Update()
    {
        TableCollisionChair();
    }

    public void TableCollisionChair()
    {
        //의자만 이 함수를 쓸 수 있도록 함.
        //의자 근처에 충돌하는 모든 오브젝트를 가져와서 체크.
        if (this.tag != "Furniture_Chair") { return; }

        //2026: 가로로 조금 더 넓은 충돌 검사. 좌우로 테이블이 있는지 확인하기 위한 값.
        colls = Physics2D.OverlapBoxAll(transform.position, new Vector2(1.2f, 1f), 0f);

        string tableName = "none";
        Vector2 vec;
        bool isTrigger = false;

        for (int i = 0; i < colls.Length; i++)
        {
            tableName = colls[i].gameObject.name;

            //2026: 여기서 말하는 PlayerPos는 TablebarPos를 수정하지 못한 것. 유니티 내에서도 PlayerPos로 명시되어 있기 때문에 문제가 일어나지는 않는다.
            if (tableName.Substring(tableName.Length - 1, 1) == "T" || tableName == "PlayerPos")
            {
                //이때는 테이블.
                //2026: 충돌된 오브젝트로부터 자기 위치를 빼면 자기가 왼쪽에 있는지 오른쪽에 있는지 유추 가능.
                //2026: 이후 normalized(정규화)로 방향만 남긴다.
                vec = colls[i].transform.position - this.transform.position;
                vec = vec.normalized;

                isTrigger = true;

                if (vec.x < 0f) { furnitureDirection = "L"; }
                else if (vec.x > 0f) { furnitureDirection = "R"; }

                return;
            }
        }

        if(isTrigger == false)
        {
            furnitureDirection = "none";
        }

    }

    public int GetSerialNumber_FS() {  return  furniture_sn; }
    public void SetSerialNumber_FS(int i) { furniture_sn = i; }

    public string GetType_FS() { return furniture_type; }
    public void SetType_FS(string s) { furniture_type = s; }

    public bool GetIsNPCState() { return isNPCState; }
    public void SetIsNPCState(bool b) { isNPCState = b; }

    public string GetFurnitureDirection() { return furnitureDirection; }
}
