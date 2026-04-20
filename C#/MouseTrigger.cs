using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class MouseTrigger : MonoBehaviour
{
    //마우스 범위와 충돌하는 모든 콜라이더 가져옴.
    Collider2D[] colls;

    //가구가 바닥용인지 벽용인지에 따라서 FurnitureManager에서 값을 받아와서 벽이면 벽에만, 바닥이면 바닥에만...
    //아닐시 붉어지게끔 할 수 있지 않을까?
    string f_kind;
    bool isTrigger;

    //의자용
    //t_trigger : 테이블-의자 연결 범위 안에 의자가 있을 시, true. 아니면 false.
    Collider2D[] t_colls;
    bool t_trigger = false;

    private void Update()
    {
        if (FurnitureManager.F.GetIsReady() == false)
        {
            //2026: 가구 배치가 불가능할 때
            this.gameObject.GetComponent<SpriteRenderer>().color = new UnityEngine.Color(1, 1, 1, 0);
            return;
        }
        else
        {
            //2026: 가구 배치가 가능할 때, '배치할 수 없는 위치에 들어가면 빨간색', '배치할 수 있을 때 흰색', '의자가 테이블에 연결되는 위치라면 초록색'
            if (TriggerCheck() == true)
            {
                this.gameObject.GetComponent<SpriteRenderer>().color = UnityEngine.Color.red;
            }
            else if (t_trigger == true && TriggerCheck() == false)
            {
                this.gameObject.GetComponent<SpriteRenderer>().color = UnityEngine.Color.green;
            }
            else if (TriggerCheck() == false)
            {
                this.gameObject.GetComponent<SpriteRenderer>().color = UnityEngine.Color.white;
            }
        }
    }

    public bool TriggerCheck()
    {
        //가구를 설치할 수 '없는' 상황이면 true, 설치할 수 '있는' 상황이면 false.
        //가구를 클릭했을 때도 색상이 붉어질 수 있게 고쳐져야만 함.

        //현재 위치에서부터, 지금 콜라이더의 사이즈-임의의 값(클 수록 오브젝트끼리 붙여서 배치 가능)까지 조사
        colls = Physics2D.OverlapBoxAll(this.transform.position, this.GetComponent<BoxCollider2D>().size - new Vector2(0.5f, 0.5f), 0f);
        t_colls = Physics2D.OverlapBoxAll(this.transform.position, new Vector2(1.2f, 1f), 0f);

        string tag;

        if(f_kind == "C")
        {
            for (int j = 0; j < t_colls.Length; j++)
            {
                if (t_colls[j].gameObject.name.Substring(t_colls[j].gameObject.name.Length - 1, 1) == "T" ||
                    t_colls[j].gameObject.name == "PlayerPos")
                {
                    //테이블-의자 연결 범위 안에 의자가 놓인 상황

                    for (int i = 0; i < colls.Length; i++)
                    {
                        //그중에서 '배치해서는 안 되는 상황'이 생긴다면
                        tag = colls[i].gameObject.tag;

                        if (tag.Substring(0, 3) == "Fur") { return true; }
                        if (tag == "Grid_None") { return true; }

                        if (tag == "Grid_Wall") { return true; }
                    }

                    //테이블-의자 연결 범위 안에 있으면서 '배치해서는 안 되는 상황'이 아닐 때 false.
                    t_trigger = true;
                    return false;
                }
                else
                {
                    t_trigger = false;
                }
            }
        }

        for (int i = 0; i < colls.Length; i++)
        {
            //의자를 수정하고 난 뒤, t_trigger가 true로 설정된 상황.
            //이때 '의자'가 아닌 오브젝트를 수정하려고 하ㅡ면 '초록색'이 떠서 한번 false로 초기화.
            t_trigger = false;

            tag = colls[i].gameObject.tag;

            if (tag.Substring(0, 3) == "Fur") { return true; }
            if (tag == "Grid_None") { return true; }

            //2026: 바닥 가구면서 벽에 붙지 않도록, 벽 가구면서 바닥에 붙지 않도록 하는 것.
            if((f_kind == "F" || f_kind == "C") && tag == "Grid_Wall") { return true; }
            if(f_kind == "W" && tag == "Grid_Floor") { return true; }
        }

        return false;
    }

    public void SetFurnitureKind(string s) { f_kind = s; }

    public bool GetIsTrigger() { return false; }
}
