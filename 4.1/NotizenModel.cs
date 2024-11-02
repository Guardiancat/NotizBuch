using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace _4._1
{
    internal class NotizenModel
    {
        public static DataTable DataTableNotizen { get; private set; }
        public static DataTable DataTableKategorien { get; private set; }
    // Метод для инициализации данных из базы
    public static void LadenNotizenAusDB(string connectionString)
    {
        using (OleDbConnection connection = new OleDbConnection(connectionString))
        {
            connection.Open();
            OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT * FROM Notizen", connection);
            DataTableNotizen = new DataTable();

            adapter.Fill(DataTableNotizen); // Заполнение таблицы данными из базы
                OleDbDataAdapter adapter1 = new OleDbDataAdapter("SELECT * FROM Kategorien", connection);
                DataTableKategorien= new DataTable();
                adapter1.Fill(DataTableKategorien);
        }
    }
    }
}
