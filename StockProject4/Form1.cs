using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StockProject4
{
    public partial class Form1 : Form
    {
        List<ConditionObject> conditionObjectList;

        int rowIndex = 0; // 조건명 인덱스

        public Form1()
        {
            InitializeComponent();

            conditionObjectList = new List<ConditionObject>();

            axKHOpenAPI1.OnEventConnect += OnEventConnect;
            axKHOpenAPI1.OnReceiveConditionVer += OnReceiveConditionVer;
            axKHOpenAPI1.OnReceiveTrCondition += OnReceiveTrCondition;
            axKHOpenAPI1.OnReceiveTrData += OnReceiveTrData;
            axKHOpenAPI1.OnReceiveRealCondition += OnReceiveRealCondition;

            conditionDataGridView.SelectionChanged += SelectionChanged;

            axKHOpenAPI1.CommConnect(); // 로그인 요청

        }

        void SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender.Equals(conditionDataGridView))
                {
                    Console.WriteLine("rowIndex:@@" + rowIndex + "@@");
                    
                    rowIndex = conditionDataGridView.SelectedCells[0].RowIndex;
                    int 번호 = int.Parse(conditionDataGridView["번호", rowIndex].Value.ToString());
                    string 조건명 = (string)conditionDataGridView["조건명", rowIndex].Value;

                    int res = axKHOpenAPI1.SendCondition("0156", 조건명, 번호, 1);

                    if (res == 1) // 성공 // OnReceiveTrCondition로 전달됨 (성공할 경우)
                    {
                        Console.WriteLine("조건 검색 성공");
                    }
                    else if (res == 0 && rowIndex > -1) // 실패 (1분이내 재검색 한 경우)
                    {
                        Console.WriteLine("조건 검색 실패");
                        conditionItemDataGridView.Rows.Clear();

                        for (int i = 0; i < conditionObjectList[rowIndex].itemObjectList.Count; i++)
                        {
                            conditionItemDataGridView.Rows.Add();
                            ItemObject itemObject = conditionObjectList[rowIndex].itemObjectList[i];
                            conditionItemDataGridView["종목코드", i].Value = itemObject.종목코드;
                            conditionItemDataGridView["종목명", i].Value = itemObject.종목명;
                            conditionItemDataGridView["현재가", i].Value = itemObject.현재가;
                            conditionItemDataGridView["등락율", i].Value = itemObject.등락율;
                            conditionItemDataGridView["전일대비", i].Value = itemObject.전일대비;
                            conditionItemDataGridView["거래량", i].Value = itemObject.거래량;
                        }

                    }

                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }

        }



        void OnReceiveRealCondition(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveRealConditionEvent e)
        {
            try
            {
                string 종목코드 = e.sTrCode;
                string 조건식번호 = e.strConditionIndex;
                string 조건명 = e.strConditionName;

                ConditionObject conditionObject = conditionObjectList.Find(o => o.조건식번호 == int.Parse(조건식번호));

                if (conditionObject != null)
                {
                    if (e.strType == "I") // 종목 편입
                    {
                        insertListBox.Items.Add("편입 => 코드:" + 종목코드 + " |번호: " + 조건식번호 + " |조건명: " + 조건명);

                        axKHOpenAPI1.SetInputValue("종목코드", 종목코드);
                        axKHOpenAPI1.CommRqData("편입종목정보요청;" + 조건식번호, "opt10001", 0, "5005"); // OnReceiveTrData 로 전달됨
                    }
                    else if (e.strType == "D") // 종목 이탈
                    {
                        deleteListBox.Items.Add("이탈 => 코드: " + 종목코드 + " |번호: " + 조건식번호 + " |조건명: " + 조건명);
                        ItemObject itemObject = conditionObject.itemObjectList.Find(o => o.종목코드 == 종목코드);

                        if (itemObject != null)
                        {
                            conditionObject.itemObjectList.Remove(itemObject); // 객체에서 제거
                            for (int i = 0; i < conditionItemDataGridView.RowCount; i++) // UI에서 제거
                            {
                                if ((string)conditionItemDataGridView["종목코드", i].Value == 종목코드)
                                {
                                    conditionItemDataGridView.Rows.RemoveAt(i);
                                    break;
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine("OnReceiveRealCondition 메소드 Exception:");
                Console.WriteLine(error.ToString());
            }



        }

        void OnReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            string 현재가_ = "";
            try
            {
                if (e.sRQName.Contains("조건종목정보"))
                {
                    int codeCount = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRQName);
                    Console.WriteLine("종목수:" + codeCount);

                    if (!e.sRQName.Contains("다음"))
                    {
                        conditionItemDataGridView.Rows.Clear();
                        conditionObjectList[rowIndex].itemObjectList.Clear();
                    }

                    for (int i = 0; i < codeCount; i++)
                    {
                        string 종목코드 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "종목코드").Trim();
                        string 종목명 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "종목명").Trim();
                        long 현재가 = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "현재가").Trim());
                        double 등락율 = double.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "등락율").Trim());
                        int 전일대비 = int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "전일대비").Trim());
                        long 거래량 = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량").Trim());

                        conditionItemDataGridView.Rows.Add(종목코드, 종목코드, 종목명, 현재가, 등락율, 전일대비, 거래량);
                        /*
                        conditionItemDataGridView["종목코드", i].Value = 종목코드;
                        conditionItemDataGridView["종목명", i].Value = 종목명;
                        conditionItemDataGridView["현재가", i].Value = 현재가;
                        conditionItemDataGridView["등락율", i].Value = 등락율;
                        conditionItemDataGridView["전일대비", i].Value = 전일대비;
                        conditionItemDataGridView["거래량", i].Value = 거래량;
                        */
                        if (rowIndex > -1)
                            conditionObjectList[rowIndex].itemObjectList.Add(new ItemObject(종목코드, 종목명, 현재가, 등락율, 전일대비, 거래량));

                    }
                }
                else if (e.sRQName.Contains("편입종목정보요청"))
                {
                    string[] rqNameArray = e.sRQName.Split(';');
                    int 조건식번호 = int.Parse(rqNameArray[1]);

                    ConditionObject conditionObject = conditionObjectList.Find(o => o.조건식번호 == 조건식번호);
                    /*
                    string 종목코드 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목코드").Trim();
                    string 종목명 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목명").Trim();
                    long 현재가 = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "현재가").Trim());
                    double 등락율 = double.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "등락율").Trim());
                    int 전일대비 = int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "전일대비").Trim());
                    long 거래량 = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "거래량").Trim());
                    */

                    string 종목코드 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목코드").Trim();
                    string 종목명 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "종목명").Trim();
                    string str현재가 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "현재가").Trim();
                    long 현재가 = string.IsNullOrEmpty(str현재가) ? 0 : long.Parse(str현재가);
                    string str등락율 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "등락율").Trim();
                    double 등락율 = string.IsNullOrEmpty(str등락율) ? 0 : double.Parse(str등락율);
                    string str전일대비 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "전일대비").Trim();
                    int 전일대비 = string.IsNullOrEmpty(str전일대비) ? 0 : int.Parse(str전일대비);
                    string str거래량 = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "거래량").Trim();
                    long 거래량 = string.IsNullOrEmpty(str거래량) ? 0 : int.Parse(str거래량);

                    // 객체에 편입
                    conditionObject.itemObjectList.Add(new ItemObject(종목코드, 종목명, 현재가, 등락율, 전일대비, 거래량));

                    // UI에 편입
                    conditionItemDataGridView.Rows.Insert(0);

                    conditionItemDataGridView["종목코드", 0].Value = 종목코드;
                    conditionItemDataGridView["종목명", 0].Value = 종목명;
                    conditionItemDataGridView["현재가", 0].Value = 현재가;
                    conditionItemDataGridView["등락율", 0].Value = 등락율;
                    conditionItemDataGridView["전일대비", 0].Value = 전일대비;
                    conditionItemDataGridView["거래량", 0].Value = 거래량;
                }
            }
            catch (Exception error)
            {
                Console.WriteLine("OnReceiveTrData 메소드 Exception:");
                Console.WriteLine("현재가:@@" + 현재가_ + "@@");
                Console.WriteLine(error.ToString());
            }

        }

        void OnReceiveTrCondition(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrConditionEvent e)
        {
            try
            {
                string codeList = e.strCodeList.Trim();
                if (codeList[codeList.Length - 1] == ';')
                    codeList = codeList.Remove(codeList.Length - 1);

                int codeCount = codeList.Split(';').Length;
                string[] codeListArray = codeList.Split(';');

                int remainder = codeCount % 100;

                Console.WriteLine("종목코드:" + codeList);
                Console.WriteLine("codeCount:" + codeCount);
                Console.WriteLine("nNext:" + e.nNext);

                if (0 < codeCount && codeCount <= 100)
                    axKHOpenAPI1.CommKwRqData(codeList, 0, codeCount, 0, "조건종목정보", "5002"); //OnReceiveTrData 전달

                else if (100 < codeCount)
                {
                    string codeList100 = "";

                    for (int i = 0; i < codeCount; i++)
                    {
                        if ((i + 1) % 100 == 0 && (i + 1) != 1)
                        {
                            codeList100 += codeListArray[i];
                            axKHOpenAPI1.CommKwRqData(codeList100, 0, 100, 0, "조건종목정보_다음", (i + 1).ToString()); //OnReceiveTrData 전달
                            codeList100 = "";
                        }
                        else
                        {
                            if ((i + 1) == codeCount) // 마지막 종목코드 입력
                            {
                                codeList100 = codeList100 + codeListArray[i];
                                axKHOpenAPI1.CommKwRqData(codeList100, 0, remainder, 0, "조건종목정보_다음", (i + 1).ToString()); //OnReceiveTrData 전달
                                codeList100 = "";
                            }
                            else // 종목코드 입력 진행중
                            {
                                codeList100 = codeList100 + codeListArray[i] + ";";
                            }

                        }
                    }

                }

                if (e.nNext != 0)
                    axKHOpenAPI1.SendCondition("5001", e.strConditionName, e.nIndex, 1); //OnReceiveTrCondition 전달
            }
            catch (Exception error)
            {
                Console.WriteLine("OnReceiveTrCondition 메소드 Exception:");
                Console.WriteLine(error.ToString());
            }

        }

        void OnReceiveConditionVer(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveConditionVerEvent e)
        {
            try
            {
                if (!string.IsNullOrEmpty(axKHOpenAPI1.GetConditionNameList()))
                {
                    string nameList = axKHOpenAPI1.GetConditionNameList().Trim();
                    if (nameList[nameList.Length - 1] == ';')
                        nameList = nameList.Remove(nameList.Length - 1); // namelist 값의 마지막 ';'을 제거함
                    string[] nameListArray = nameList.Split(';');

                    for (int i = 0; i < nameListArray.Length; i++)
                    {
                        string[] conditionNameArray = nameListArray[i].Split('^');
                        conditionDataGridView.Rows.Add();
                        conditionDataGridView["번호", i].Value = int.Parse(conditionNameArray[0]);
                        conditionDataGridView["조건명", i].Value = conditionNameArray[1];

                        conditionObjectList.Add(new ConditionObject(int.Parse(conditionNameArray[0]), conditionNameArray[1]));
                    }

                    conditionDataGridView.ClearSelection();
                }

                else Console.WriteLine("조건식이 없습니다!!!");
            }
            catch (Exception error)
            {
                Console.WriteLine("OnReceiveConditionVer 메소드 Exception:");
                Console.WriteLine(error.ToString());
            }
           
        }

        void OnEventConnect(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            if (e.nErrCode == 0) // 로그인 성공
            {
                int res = axKHOpenAPI1.GetConditionLoad();

                if (res == 1)
                {
                    Console.WriteLine("조건목록 요청 성공");
                }
                else if (res == 1)
                {
                    Console.WriteLine("조건목록 요청 실패");
                }
            }
            else
            {
                MessageBox.Show("로그인 실패");
            }
        }
    }

    class ConditionObject
    {
        public int 조건식번호;
        public string 조건식명;

        public List<ItemObject> itemObjectList;

        public ConditionObject(int 조건식번호, string 조건식명)
        {
            itemObjectList = new List<ItemObject>();

            this.조건식번호 = 조건식번호;
            this.조건식명 = 조건식명;
        }
    }

    class ItemObject
    {
        public string 종목코드;
        public string 종목명;
        public long 현재가;
        public double 등락율;
        public int 전일대비;
        public long 거래량;

        public ItemObject(string 종목코드, string 종목명, long 현재가, double 등락율, int 전일대비, long 거래량)
        {
            this.종목코드 = 종목코드;
            this.종목명 = 종목명;
            this.현재가 = 현재가;
            this.등락율 = 등락율;
            this.전일대비 = 전일대비;
            this.거래량 = 거래량; ;
        }

    }
}
