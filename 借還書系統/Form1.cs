using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 借還書系統
{
    public partial class Form1 : Form
    {
        int index;
        public class DBConfig
        {

            public static string dbFile = Application.StartupPath + @"\library.db";

            public static string dbPath = "Data source=" + dbFile;
            public static SQLiteConnection sqlite_connect;
            public static SQLiteCommand sqlite_cmd;
            public static SQLiteDataReader sqlite_dataReader;
        }
        private void Load_DB()
        {
            DBConfig.sqlite_connect = new SQLiteConnection(DBConfig.dbPath);
            DBConfig.sqlite_connect.Open();
        }

        // 啟動時需要的所有DB資料
        private void Show_DB()
        {
            // 書籍資料
            this.dataGridView1.Rows.Clear();

            string sql = @"SELECT * FROM log;";
            string table = "library";
            string[] col = new string[] { "library.*", "stock.in_stock", "book_type.type_name " };
            string joinSql = " INNER JOIN stock ON library.id=stock.book_id ";
            joinSql += "INNER JOIN book_type ON library.type=book_type.id";
            string condition = "";
            querySql(table, col, condition, joinSql);


            if (DBConfig.sqlite_dataReader.HasRows)
            {
                while(DBConfig.sqlite_dataReader.Read())
                {
                    int id = Convert.ToInt32(DBConfig.sqlite_dataReader["id"]);
                    string type = DBConfig.sqlite_dataReader["type_name"].ToString();
                    string name = (DBConfig.sqlite_dataReader["Name"]).ToString();
                    string author = DBConfig.sqlite_dataReader["author"].ToString();
                    string publish = DBConfig.sqlite_dataReader["publish"].ToString();
                    int in_stock = Convert.ToInt32(DBConfig.sqlite_dataReader["in_stock"]);
                    string publishDate = (DBConfig.sqlite_dataReader["publication_date"]).ToString();
                    string purchaseDate = (DBConfig.sqlite_dataReader["purchase_date"]).ToString();

                    string type_str = "";
                    

                    DataGridViewRowCollection rows = dataGridView1.Rows;
                    rows.Add(new object[] { id, type, name, publish, author, in_stock, publishDate, purchaseDate });
                }
                DBConfig.sqlite_dataReader.Close();
            }

            //書名
            sql = @"SELECT name FROM library";
            table = "library";
            col = new string[] { "name" };
            joinSql = "";
            condition = "";
            querySql(table, col, condition, joinSql);

            //清除下拉式資料
            comboBox1.Items.Clear();

            //填充資料
            if (DBConfig.sqlite_dataReader.HasRows)
            {
                while (DBConfig.sqlite_dataReader.Read())
                {
                    string name = (DBConfig.sqlite_dataReader["Name"]).ToString();
                    comboBox1.Items.Add(name);
                }
                DBConfig.sqlite_dataReader.Close();
            }
        }
        
        private void querySql(String table, String[] col, String condition, String joinSql)
        {
            //製作需要欄位
            String colString = string.Join(",",col);
            // 串接sql
            string sql = String.Format("SELECT {0} FROM {1} {2} WHERE 1=1 {3};",colString,table,joinSql,condition);
            DBConfig.sqlite_cmd = new SQLiteCommand(sql, DBConfig.sqlite_connect);
            DBConfig.sqlite_dataReader = DBConfig.sqlite_cmd.ExecuteReader();
        }

        public Form1()
        {
            InitializeComponent();
            Load_DB();
            Show_DB();
            this.idLabel.Text = index.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //借書
            if (radioButton1.Checked)
            {
                // 檢查stock
                string sql = "select in_stock from stock where book_id={idLabel}";
                string table = "stock";
                string[] col = new string[1] { "in_stock" };
                string joinSql = "";
                string condition = String.Format("AND book_id={0}", idLabel.Text);
                querySql(table, col, condition, joinSql);

                int stock = 0;
                //填充資料
                if (DBConfig.sqlite_dataReader.HasRows)
                {
                    while (DBConfig.sqlite_dataReader.Read())
                    {
                        stock = Convert.ToInt32(DBConfig.sqlite_dataReader["in_stock"]);
                    }
                    DBConfig.sqlite_dataReader.Close();
                }
                //stock還有，更改stock及記錄到lend_return_list
                if(stock > 0)
                {
                    sql = String.Format("UPDATE stock SET in_stock={0} WHERE book_id={1}", --stock, idLabel.Text);
                    DBConfig.sqlite_cmd = new SQLiteCommand(sql, DBConfig.sqlite_connect);
                    DBConfig.sqlite_cmd.ExecuteNonQuery();

                    sql = String.Format("INSERT INTO lend_return_list(type, book_id, time) VALUES ('{0}','{1}','{2}')", 1, idLabel.Text, DateTime.Now.ToString("yyyy-M-D HH:mm:ss"));
                    DBConfig.sqlite_cmd = new SQLiteCommand(sql, DBConfig.sqlite_connect);
                    DBConfig.sqlite_cmd.ExecuteNonQuery();
                    Show_DB();
                }

                //stock沒了，跳alert
                if(stock == 0)
                {
                    MessageBox.Show("已無庫存");
                }
            }
            else //還書
            {
                //檢查lend_return_list此書相加是否為正數
                string sql = "select book_id,SUM(type) as result FROM lend_return_list WHERE book_id={0} group by book_id";
                string table = "lend_return_list";
                string[] col= new string[] {"SUM(type) as result ", "book_id" };
                string condition = String.Format("AND book_id={0} group by book_id",idLabel.Text);
                string joinSql = "";
                querySql(table, col, condition, joinSql);

                int sumResult = 0;
                if (DBConfig.sqlite_dataReader.HasRows)
                {
                    while (DBConfig.sqlite_dataReader.Read() )
                    {
                        sumResult = Convert.ToInt32(DBConfig.sqlite_dataReader["result"]);
                    }
                }
                //為正數，更改stock及記錄到lend_return_list
                if(sumResult > 0)
                {
                    // 檢查stock
                    sql = "select in_stock from stock where book_id={idLabel}";
                    table = "stock";
                    col = new string[1] { "in_stock" };
                    joinSql = "";
                    condition = String.Format("AND book_id={0}", idLabel.Text);
                    querySql(table, col, condition, joinSql);

                    int stock = 0;
                    //填充資料
                    if (DBConfig.sqlite_dataReader.HasRows)
                    {
                        while (DBConfig.sqlite_dataReader.Read())
                        {
                            stock = Convert.ToInt32(DBConfig.sqlite_dataReader["in_stock"]);
                        }
                        DBConfig.sqlite_dataReader.Close();
                    }

                    sql = String.Format("UPDATE stock SET in_stock={0} WHERE book_id={1}", ++stock, idLabel.Text);
                    DBConfig.sqlite_cmd = new SQLiteCommand(sql, DBConfig.sqlite_connect);
                    DBConfig.sqlite_cmd.ExecuteNonQuery();

                    sql = String.Format("INSERT INTO lend_return_list(type, book_id, time) VALUES ('{0}','{1}','{2}')", -1, idLabel.Text, DateTime.Now.ToString("yyyy-M-D HH:mm:ss"));
                    DBConfig.sqlite_cmd = new SQLiteCommand(sql, DBConfig.sqlite_connect);
                    DBConfig.sqlite_cmd.ExecuteNonQuery();
                    Show_DB();
                }
                else//為0，跳alert
                {
                    MessageBox.Show("請勿憑空生有");

                }
                
            }
        }

        private void dGV1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewCellCollection selRowData = dataGridView1.Rows[e.RowIndex].Cells;

            string type = "";
            type = Convert.ToString(selRowData[2].Value);

            if (type.Equals("借書"))
            {
                radioButton1.Checked = true;
            }
            else
            {
                radioButton2.Checked = true;
            }

            this.comboBox1.Text = Convert.ToString(selRowData[2].Value);
            this.idLabel.Text = Convert.ToString(selRowData[0].Value);
            this.typeLabel.Text = Convert.ToString(selRowData[1].Value);
        }
    }
}
