
using System.Data;
using System.Data.OleDb;

namespace _4._1
{
    internal class NotizenModel
    {
        public static DataTable DataTableNotizen { get; private set; }
        public static DataTable DataTableKategorien { get; private set; }
        // Methode zum Laden der Notizen und Kategorien aus der Datenbank
        public static void LadenNotizenAusDB(string connectionString)
    {
        using (OleDbConnection connection = new OleDbConnection(connectionString))
        {
                // Adapter zum Abrufen der Notizen-Daten aus der Tabelle "Notizen"
                connection.Open();
            OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT * FROM Notizen", connection);
            DataTableNotizen = new DataTable();
                // Auffüllen der DataTableNotizen mit den Daten aus der Datenbank
                adapter.Fill(DataTableNotizen); 
                OleDbDataAdapter adapter1 = new OleDbDataAdapter("SELECT * FROM Kategorien", connection);
                DataTableKategorien= new DataTable();
                adapter1.Fill(DataTableKategorien);
        }
    }
    }
}
