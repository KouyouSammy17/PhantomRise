using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sample {
public class PublicValueScript : MonoBehaviour
{
    // Key
    private int key_num = 0;
    private Sprite[] ImageNum = new Sprite[10];
    private Image Num;
    public int HaveKeyNum_Public{
        get { return key_num; }
        set
        {
            key_num = value;
            Num.sprite = ImageNum[key_num];
        }
    }
    // HP
    private const int max_hp = 3;
    public int MaxHP_Public{
        get { return max_hp; }
    }
    private int hp_num = max_hp;
    private Image[] Heart = new Image[max_hp];
    private Sprite ImageHeartGrey;
    private Sprite ImageHeartRed;
    public int HP_Public{
        get { return hp_num; }
        set
        {
            hp_num = value;
            if(hp_num < 0)
            {
                hp_num = 0;
            }
            int n = 3 - hp_num;
            if(hp_num < max_hp)
            {
                for(int i=2; i >= Heart.Length-n; i--)
                {
                    Heart[i].sprite = ImageHeartGrey;
                }
            }
            else if(hp_num == max_hp)
            {
                for(int i=0; i < Heart.Length; i++)
                {
                    Heart[i].sprite = ImageHeartRed;
                }
            }
        }
    }
    // Player
    private GameObject Player;
    public GameObject Player_Public{
        get { return Player; }
        set { Player = value; }
    }
    // respawn
    private bool RespawnFlg = false;
    public bool RespawnFlg_Public {
        get { return RespawnFlg; }
        set { RespawnFlg = value; }
    }
    // Clear
    private bool clear = false;
    private GameObject Clear_ui;
    public bool Clear_Public {
        get { return clear; }
        set
        {
            clear = value;
            if(clear)
            {
                Clear_ui.gameObject.SetActive(true);
            }
        }
    }

    void Start ()
    {
        // images of numaber
        for(int i=0; i < ImageNum.Length; i++)
        {
            string path = "Images/icon_" + i.ToString();
            ImageNum[i] = Resources.Load<Sprite>(path);
        }
        Num = GameObject.Find("Canvas/Icon_Key/Num").GetComponent<Image>();

        // images of heart
        for(int i=0; i < Heart.Length; i++)
        {
            string name = "Canvas/HP/Heart" + i.ToString();
            Heart[i] = GameObject.Find(name).GetComponent<Image>();
        }
        ImageHeartGrey = Resources.Load<Sprite>("Images/icon_HeartGrey");
        ImageHeartRed = Resources.Load<Sprite>("Images/icon_HeartRed");

        // Clear
        Clear_ui = GameObject.Find("Canvas/Clear");
        Clear_ui.gameObject.SetActive(false);
    }
}
}
