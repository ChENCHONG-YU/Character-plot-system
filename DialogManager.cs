using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;                        //這邊要先引用TextMeshPro
using UnityEngine.UI;
using UnityEngine.EventSystems;     //引用EventSystems
using UnityEditor;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEditor.SearchService;


//為了讓數據層及顯示層連接起來而創建這個腳本
public class DialogManager : MonoBehaviour
{
    public TextAsset dialogDataFile;                                         //對話文本文件.csv格式
    public SpriteRenderer spriteLeft;                                        //左側角色圖像
    public SpriteRenderer spriteRight;                                       //右側角色圖像
    public SpriteRenderer spriteMiddle;                                      //中間角色圖像
    public TMP_Text nameText;                                                //角色名字文本(建立TextMeshPro變數)
    public TMP_Text schoolnameText;                                          //角色學校名字文本
    public TMP_Text dialogText;                                              //對話內容文本
    public List<Sprite> sprites = new List<Sprite>();                        //角色圖片列表
    Dictionary<string, Sprite> imogeDic = new Dictionary<string, Sprite>();  //角色名字對應圖片的字典
    public int dialogIndex;                                                  //當前的對話索引值
    public string[] dialogRows;                                              //對話文本，按行分割
    public GameObject optionButton;                                          //選項按鈕預制體
    public Transform buttonGroup;                                            //選項按鈕父節點，用於自動排序
    public Transform content;                                                //保存所有對話紀錄
    public GameObject characterPortraitPrefab;
    public GameObject playerDialoguePrefab;
    public GameObject Narration;

    public float autoPlayDelay = 3f;            //自動播放每句對話的延遲時間
    private bool isAutoPlay = false;            //是否自動播放
    private bool isOptionActive = false;        //選項是否處於激活狀態
    private bool allowScreenClick = true;       //點擊螢幕進行下一句是否處於激活狀態
    private bool isMenu = false;                //選單相關bool
    private bool isShowDisplay = false;
    private bool isDialogueHistory = false;

    public Canvas MenuCanves;           //選單
    public Canvas SummaryWindow;        //概要視窗
    public Canvas Canves;               //對話系統介面(不包含背景及人物)
    public Canvas DialogueCanvas;       //對話紀錄視窗

    public Button AutoButton;  //自動按鈕
    public Button MenuButton;  //選單按鈕
    public Text MenuText;
    public GameObject Panel;

    public GameObject LoadingScene;

    //打字機效果相關變數宣告
    public float typingSpeed = 2.5f; // 每個字元顯示的間隔時間
    private string fullText;         // 完整的對話文本
    private int currentCharIndex;    // 當前顯示的字元索引
    private bool isType;             // 是否正在執行打字機效果
    private float typingTimer;       // 計時器，用來控制文字顯示速度

    public TMP_Text dialogueRecordPrefab; //用於顯示對話紀錄的預製件

    //Start is called before the first frame update
    private void Awake()
    {
        imogeDic["伊吹"] = sprites[0];
    }
    void Start()
    {
        // 初始化變量
        fullText = "";
        dialogText.text = "";
        isType = false;

        ReadText(dialogDataFile);
        ShowDialogRow();
    }

    // Update is called once per frame
    void Update()
    {
        //打字機效果設定
        if (isType)
        {
            //計時器增加時間
            typingTimer += Time.deltaTime;

            if (typingTimer >= typingSpeed)     //(計時器，用來控制文字顯示速度 >= 每個字元顯示的間隔時間)
            {
                //顯示下一個字元
                currentCharIndex++;
                dialogText.text = fullText.Substring(0, currentCharIndex);

                //重製計時器
                typingTimer = 0f;

                //檢查是否已經顯示完所有字符
                if (currentCharIndex >= fullText.Length)
                {
                    isType = false;
                }
            }
        }

        //快速跳過打字機效果並顯示完整對話
        if (Input.GetMouseButtonDown(0) && isType)
        {
            dialogText.text = fullText;     //顯示完整文字
            isType = false;                 //停止打字機效果
        }

        //檢查是否有點擊螢幕  GetMouseButtonDown(0)表示點擊滑鼠左鍵
        if (allowScreenClick && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) {
            
            OnClickNext();
        }

        // 隱藏後點擊螢幕顯示
        if (isMenu && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            //OnClickMenu(); // 自動關閉選單
            allowScreenClick = false;
            Canves.gameObject.SetActive(true);
            MenuButton.GetComponent<Image>().color = Color.grey;
            MenuText.GetComponent<Text>().color = Color.white;
            SummaryWindow.gameObject.SetActive(false);
        }

    }

    public void UpdateText(string _Sign,string _name,string _school, string _text)
    {

        nameText.text = _name;          //角色名字=_name輸入參數
        schoolnameText.text = _school;  //角色學校名字=_school輸入參數
        dialogText.text = _text;        //內文=_text輸入參數

        fullText = _text;               //保存完整的文本內容
        dialogText.text = "";           //開始時清空文本
        currentCharIndex = 0;           //重置索引
        isType = true;                  //開始打字機效果
        typingTimer = 0f;               //重置計時器

        AddToDialogueRecord(_Sign, _name, _text); // 新增對話紀錄
    }
    //讀取文件
    public void ReadText(TextAsset _textAsset)
    {
        //先以_textAsset參數傳遞進來，讀取他的文本訊息，最後以換行分割
        dialogRows = _textAsset.text.Split("\n");  //Split就是分割的意思
        /* (var row in rows)
        {
            string[] cell = row.Split(",");

        }*/
        Debug.Log("讀取成功");
    }
    #region 所有功能腳本

    #region 顯示對話框腳本
    public void ShowDialogRow()
    {
        for (int i = 0; i< dialogRows.Length; i++)
        {
            string[] cells = dialogRows[i].Split(",");
            //這個if判斷式是用來判定這個文本的標誌是"#"還是"&"，如果是"#"就是角色對話，"&"表示玩家對話選項
            //第0個元素是否為"#"且第一個元素是否是匹配到dialogRows
            if (cells[0] == "#" && int.Parse(cells[1]) == dialogIndex)
            {
                //更新文本
                UpdateText(cells[0], cells[2], cells[3], cells[5]);
                allowScreenClick = true;                    //開啟點擊螢幕進行下一句對話功能
                //更新id 設定跳轉
                dialogIndex = int.Parse(cells[6]);
                AutoButton.gameObject.SetActive(true);      //顯示Auto按鈕
                isOptionActive = false;                     //選項屬於非激活狀態
                //AUTO設定
                if (isAutoPlay)
                {
                    Invoke("OnClickNext", autoPlayDelay);   //自動播放下一句

                    allowScreenClick = false;               //關閉點擊螢幕進行下一句對話功能
                }
                break;
            }
            else if(cells[0] == "$" && int.Parse(cells[1]) == dialogIndex)
            {
                //更新文本
                UpdateText(cells[0], cells[2], cells[3], cells[5]);
                allowScreenClick = true;                    //開啟點擊螢幕進行下一句對話功能
                //更新id 設定跳轉
                dialogIndex = int.Parse(cells[6]);
                AutoButton.gameObject.SetActive(true);      //顯示Auto按鈕
                isOptionActive = false;                     //選項屬於非激活狀態
                //AUTO設定
                if (isAutoPlay)
                {
                    Invoke("OnClickNext", autoPlayDelay);   //自動播放下一句

                    allowScreenClick = false;               //關閉點擊螢幕進行下一句對話功能
                }
                break;
            }
            else if (cells[0] == "&" && int.Parse(cells[1]) == dialogIndex)
            {
                AutoButton.gameObject.SetActive(false);     //隱藏Auto按鈕

                allowScreenClick = false;

                GenerateOption(i);
                isOptionActive = true;                      //選項屬於激活狀態
            }
            else if (cells[0] == "END" && int.Parse(cells[1]) == dialogIndex)
            {
                Debug.Log("劇情結束");
                isOptionActive = false;
                LoadSenec(1);                               //回到遊戲首頁(Scene的FrontPage)
            }
        }
    }
    #endregion

    #region 對話進行下一句腳本
    public void OnClickNext()
    {
        //AUTO設定
        if (isAutoPlay) {
            CancelInvoke("OnClickNext");        //取消自動播放的調用，以避免重複調用
        }

        if (!isOptionActive)                    //檢查是否有選項在激活狀態
        {
            ShowDialogRow();
        }
        
    }
    #endregion

    #region 生成選項腳本 & 選項按鈕點擊腳本
    public void GenerateOption(int _index)
    {
        string[] cells = dialogRows[_index].Split(",");

        if (cells[0] == "&") 
        {
            GameObject button = Instantiate(optionButton, buttonGroup);         //把按鈕生成到buttonGroup

            //綁定按鈕事件
            string optionText = cells[5];                                       //選項內容
            button.GetComponentInChildren<TMP_Text>().text = optionText;
            
            int nextDialogueId = int.Parse(cells[6]);
            button.GetComponent<Button>().onClick.AddListener
                (
                   delegate {
                       AddToPlayerDialogueRecord(cells[2], optionText);         // 新增玩家回覆到紀錄
                       OnOptionClick(int.Parse(cells[6]));
                });
            GenerateOption(_index + 1);
            
        }
        
    }
    
    //選項按鈕點擊腳本
    public void OnOptionClick(int _Id)
    {

        string playerResponse = GetPlayerResponse(_Id);

        // 設定下一句對話的索引值
        dialogIndex = _Id;



        ShowDialogRow();
        //點擊完了以後把選項按鈕刪除
        for (int i = 0; i < buttonGroup.childCount; i++)
        {
            
            Destroy(buttonGroup.GetChild(i).gameObject);
        }

    }

    // 新增方法，根據選項ID獲取玩家的回覆
    private string GetPlayerResponse(int id)
    {
        foreach (var row in dialogRows)
        {
            string[] cells = row.Split(",");
            if (cells[0] == "&" && int.Parse(cells[1]) == id)
            {
                return cells[5];
            }
        }
        return "";
    }
    #endregion


    #region AUTO腳本
    public void ToggleAutoPlay()
    {

        isAutoPlay = !isAutoPlay;
        if (isAutoPlay)
        {
            OnClickNext();                      //開啟自動播放，立即播放下一句
            
            AutoButton.GetComponent<Image>().color= Color.yellow;
            allowScreenClick = false;           //禁止點擊螢幕進行下一句對話
        }
        else
        {
            CancelInvoke("OnClickNext");        //關閉自動播放
            allowScreenClick = true;
            AutoButton.GetComponent<Image>().color = Color.white;

        }
    }
    #endregion

    #region 選單功能腳本
    //選單按鈕
    public void OnClickMenu()
    {
        isMenu = !isMenu;
        if (isMenu)
        {
            MenuButton.GetComponent<Image>().color = Color.grey;
            MenuText.GetComponent<Text>().color = Color.white;
            MenuCanves.gameObject.SetActive(true);
            allowScreenClick = false; //禁止點擊螢幕進行下一句對話
        }
        else
        {
            MenuButton.GetComponent<Image>().color = Color.white;
            MenuText.GetComponent<Text>().color = Color.black;
            MenuCanves.gameObject.SetActive(false);
           allowScreenClick = true;  //恢復點擊螢幕進行下一句對話
        }

    }
    //隱藏按鈕
    public void OnClickShowDisplay()
    {
        isShowDisplay = !isShowDisplay;
        if (isShowDisplay)
        {
            Canves.gameObject.SetActive(false);
            MenuButton.GetComponent<Image>().color = Color.white;
            MenuText.GetComponent<Text>().color = Color.black;

        }
         
    }
    #region 劇情Skip相關功能 & Loaing畫面
    //劇情略過按鈕(這邊點擊按鈕以後會先跳出劇情概要視窗)
    public void SkipButton()
    {
        SummaryWindow.gameObject.SetActive(true);
    }
    //關閉視窗
    public void Closure()
    {
        SummaryWindow.gameObject.SetActive(false);
    }

    //Loading UI
    public void LoadSenec(int sceneId)
    {
        StartCoroutine(LoadSceeneAsync(sceneId));
    }
    IEnumerator LoadSceeneAsync(int sceneId)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneId);

        LoadingScene.SetActive(true);

        yield return null;
    }
    #endregion

    #region 遊戲對話紀錄 & 關閉視窗
    public void DialogueHistory()
    {
        isDialogueHistory = !isDialogueHistory;
        if (isDialogueHistory)
        {
            DialogueCanvas.gameObject.SetActive(true);
        }
    }
    
    public void ClearButton() //視窗關閉功能
    {
        isDialogueHistory = !isDialogueHistory;
        if (!isDialogueHistory)
        {
            DialogueCanvas.gameObject.SetActive(false);
        }
    }
    #endregion

    #endregion

    #region 對話內容添加到對話紀錄中
    private void AddToDialogueRecord(string symbol, string speaker, string dialogue)
    {
        if (symbol == "#" && dialogue != " ")
        {
            GameObject record = Instantiate(characterPortraitPrefab, content);
            // 獲取预制件中的所有 TMP_Text 组件
            TMP_Text[] textComponents = record.GetComponentsInChildren<TMP_Text>();

            // 假設第一個 TMP_Text 是名字，第二個 TMP_Text 是對話内容
            TMP_Text nameComponent = textComponents[0];
            TMP_Text dialogueComponent = textComponents[1];
            // 設置名字和對話内容
            nameComponent.text = speaker;
            dialogueComponent.text = dialogue;
        }
        else if (symbol == "$")
        {
            GameObject record = Instantiate(Narration, content);
            // 獲取预制件中的所有 TMP_Text 组件
            TMP_Text[] textComponents = record.GetComponentsInChildren<TMP_Text>();

            // 假設第一個 TMP_Text 是名字，第二個 TMP_Text 是對話内容
            TMP_Text dialogueComponent = textComponents[0];
            // 設置名字和對話内容
            dialogueComponent.text = dialogue;
        }

    }
    private void AddToPlayerDialogueRecord(string speaker, string dialogue)
    {

        GameObject record = Instantiate(playerDialoguePrefab, content);
        // 獲取预制件中的所有 TMP_Text 组件
        TMP_Text[] textComponents = record.GetComponentsInChildren<TMP_Text>();


        // 假設第一個 TMP_Text 是名字，第二個 TMP_Text 是對話内容
        TMP_Text nameComponent = textComponents[0];
        TMP_Text dialogueComponent = textComponents[1];

        // 設置名字和對話内容
        nameComponent.text = speaker;
        dialogueComponent.text = dialogue;

        // 確保生成的對話紀錄項目會被立即顯示
        record.transform.SetAsLastSibling();

    }
    #endregion

    #endregion



}
