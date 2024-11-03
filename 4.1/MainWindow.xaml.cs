using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data;
using System.Data.OleDb;



namespace _4._1
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        // Statische Felder zum Arbeiten mit den Tabellen der Datenbank
        public static DataTable NotizenTabelle;
        public static DataTable KategorienTabelle;

        // Eigenschaften für den Zugriff auf die Tabellen
        public static DataTable Notizen => NotizenTabelle;
        public static DataTable Kategorien => KategorienTabelle;

        // Aktuell ausgewählte Notiz
        Notiz _AktuelleNotiz;
        public Notiz AktuelleNotiz
        {
            get => _AktuelleNotiz;
            set
            {
                _AktuelleNotiz = value;
                if (_AktuelleNotiz != null)
                {
                    tbxNotiz.Text = value.Inhalt;
                }
                else
                {
                    tbxNotiz.Text = value?.Inhalt ?? "";
                    tbxNotiz.IsEnabled = value != null;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            // Verbindungszeichenfolge zur Datenbank
            string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Notizen.accdb";

            // Lädt Notizen und Kategorien aus der Datenbank in die Anwendung
            NotizenModel.LadenNotizenAusDB(connectionString);

            // Initialisieren und Auffüllen der Datenbanktabellen
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();

                OleDbDataAdapter notizenAdapter = new OleDbDataAdapter("SELECT * FROM Notizen", conn);
                OleDbDataAdapter kategorienAdapter = new OleDbDataAdapter("SELECT * FROM Kategorien", conn);

                NotizenTabelle = new DataTable();
                KategorienTabelle = new DataTable();

                notizenAdapter.Fill(NotizenTabelle);
                kategorienAdapter.Fill(KategorienTabelle);

                // Benachrichtigung über geladene Daten
                MessageBox.Show($"Gelesene Kategorien: {KategorienTabelle.Rows.Count}, Gelesene Notizen: {NotizenTabelle.Rows.Count}");
                if (NotizenTabelle.Rows.Count == 0)
                {
                    MessageBox.Show("NotizenTabelle ist leer. Überprüfen Sie die Datenbankverbindung und die Abfrage.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Setzt die Kategorienliste im ComboBox für die Auswahl
                cbxKategorie.ItemsSource = NotizenModel.DataTableKategorien.AsEnumerable()
                                   .Select(row => row.Field<string>("Kategorie"))
                                    .ToList();
            }
            ListeAktualisieren("Alle");
        }

        // Aktualisiert die Tabelle der Notizen aus der Datenbank
        private void AktualisierenNotizenTabelle()
        {
            string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Notizen.accdb";
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                OleDbDataAdapter notizenAdapter = new OleDbDataAdapter("SELECT * FROM Notizen", conn);
                NotizenTabelle.Clear();
                notizenAdapter.Fill(NotizenTabelle);
            }
        }

        // Aktualisiert die Notizenliste basierend auf der ausgewählten Kategorie
        private void ListeAktualisieren(string kategorieName = "Alle")
        {
            lbxNotizen.ItemsSource = null;

            // Holt die ID der ausgewählten Kategorie
            string kategorieId = kategorieName == "Alle"
                ? ""
                : KategorienTabelle.AsEnumerable()
                      .FirstOrDefault(row => row.Field<string>("Kategorie") == kategorieName)?["ID"].ToString();

            // Filtert die Notizen nach der gewählten Kategorie
            var gefilterteNotizen = NotizenTabelle.AsEnumerable()
                .Where(row =>
                    string.IsNullOrEmpty(kategorieId) || row.Field<string>("Kategorie") == kategorieId)
                .Select(row =>
                {
                    Kategorie kategorieEnum = Enum.TryParse(kategorieName, out Kategorie result)
                        ? result
                        : Kategorie.Sonstiges;

                    return new Notiz
                    {
                        ID = Guid.TryParse(row["ID"].ToString(), out Guid id) ? id : Guid.Empty,
                        Inhalt = row.Field<string>("Inhalt"),
                        Kategorie = kategorieEnum,
                        ErstelltAm = row.IsNull("ErstelltAm") ? DateTime.MinValue : Convert.ToDateTime(row["ErstelltAm"])
                    };
                })
                .OrderBy(notiz => notiz.Kategorie)
                .ToList();

            lbxNotizen.ItemsSource = gefilterteNotizen;

            // Zeigt eine Nachricht an, wenn keine Notizen gefunden wurden
            if (!gefilterteNotizen.Any())
            {
                MessageBox.Show($"Keine Notizen in der Kategorie: {kategorieName}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Event-Handler für Auswahländerungen in der Notizenliste
        private void lbxNotizen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AktuelleNotiz = lbxNotizen.SelectedItem as Notiz;
            btnLöschen.IsEnabled = AktuelleNotiz != null;

            if (lbxNotizen.SelectedItem != null)
            {
                Notiz selectedNotiz = (Notiz)lbxNotizen.SelectedItem;
                tbxNotiz.Text = selectedNotiz.Inhalt;
                tbxNotiz.IsEnabled = true;
                AktuelleNotiz = selectedNotiz;
                btnSpeichern.IsEnabled = false;
            }
            else
            {
                tbxNotiz.Text = string.Empty;
                tbxNotiz.IsEnabled = false;
            }
        }

        // Speichert die aktuelle Notiz in der Datenbank
        private void btnSpeichern_Click(object sender, RoutedEventArgs e)
        {
            if (AktuelleNotiz == null)
            {
                MessageBox.Show("Keine Notiz zum Speichern ausgewählt.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AktuelleNotiz.Inhalt = tbxNotiz.Text;
            bool erfolgreichGespeichert = SpeichernNotizInDatenbank(AktuelleNotiz);

            if (erfolgreichGespeichert)
            {
                MessageBox.Show("Die Notiz wurde erfolgreich gespeichert.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                ListeAktualisieren();
                btnSpeichern.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Fehler beim Speichern der Notiz.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            AktualisierenNotizenTabelle();
            ListeAktualisieren();
        }

        // Implementiert die Logik zum Speichern einer Notiz in der Datenbank
        private bool SpeichernNotizInDatenbank(Notiz notiz)
        {
            string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Notizen.accdb";
            string query;

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    query = "SELECT COUNT(*) FROM Notizen WHERE ID = ?";
                    using (OleDbCommand checkCmd = new OleDbCommand(query, conn))
                    {
                        checkCmd.Parameters.Add("@ID", OleDbType.VarChar, 36).Value = notiz.ID.ToString();
                        int count = (int)checkCmd.ExecuteScalar();
                        query = count > 0
                            ? "UPDATE Notizen SET Inhalt = ?, Kategorie = ?, ErstelltAm = ? WHERE ID = ?"
                            : "INSERT INTO Notizen (Inhalt, Kategorie, ErstelltAm, ID) VALUES (?, ?, ?, ?)";
                    }

                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Inhalt", OleDbType.LongVarChar, 536870910).Value = notiz.Inhalt;
                        cmd.Parameters.Add("@Kategorie", OleDbType.VarChar, 255).Value = ((int)notiz.Kategorie).ToString();
                        cmd.Parameters.Add("@ErstelltAm", OleDbType.Date).Value = notiz.ErstelltAm;
                        cmd.Parameters.Add("@ID", OleDbType.VarChar, 36).Value = notiz.ID.ToString();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Speichern in der Datenbank: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        }

        // Event-Handler für Textänderungen im Eingabefeld
        private void tbxNotiz_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnSpeichern.IsEnabled = tbxNotiz.Text.Length > 0 && lbxNotizen != null;
        }


        private void btnNeu_Click(object sender, RoutedEventArgs e)
        {
            // Holen Sie den Namen der ausgewählten Kategorie aus der ComboBox
            var selectedCategoryName = (string)cbxKategorie.SelectedItem;

            // Suchen Sie die Zeile in der Kategorie-Tabelle, die dem ausgewählten Namen entspricht
            var selectedCategoryRow = KategorienTabelle.AsEnumerable()
                .FirstOrDefault(row => row.Field<string>("Kategorie") == selectedCategoryName);

            // Abrufen der ID der Kategorie oder Standardwert "0" setzen, falls nicht gefunden
            var selectedCategoryId = selectedCategoryRow?["ID"].ToString() ?? "0";

            // Erstellen einer neuen Notiz mit generierter ID, leerem Inhalt und aktueller Uhrzeit
            AktuelleNotiz = new Notiz
            {
                ID = Guid.NewGuid(), // Neue eindeutige ID für die Notiz
                Inhalt = "", // Anfangs leerer Inhalt für die neue Notiz
                Kategorie = Enum.TryParse(selectedCategoryId, out Kategorie result) ? result : Kategorie.Sonstiges,
                ErstelltAm = DateTime.Now // Festlegen des aktuellen Datums und der Uhrzeit
            };

            // Aktivieren des Textfeldes für die Eingabe und Fokussieren
            tbxNotiz.IsEnabled = true;
            tbxNotiz.Focus();
            tbxNotiz.SelectAll();

            // Deaktivieren der Schaltfläche "Neu" nach dem Hinzufügen der Notiz
            btnNeu.IsEnabled = false;
        }

        private void btnLöschen_Click(object sender, RoutedEventArgs e)
        {
            // Überprüfen, ob eine Notiz ausgewählt ist, um sie zu löschen
            if (AktuelleNotiz != null)
            {
                // Bestätigung der Löschung durch den Benutzer
                var result = MessageBox.Show("Möchten Sie diese Notiz wirklich löschen?", "Bestätigung", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    // Löschen der Notiz aus der Datenbank und Aktualisierung der Anzeige
                    if (EntfernenNotizAusDatenbank(AktuelleNotiz))
                    {
                        AktuelleNotiz = null;
                        AktualisierenNotizenTabelle(); // Tabelle in der Datenbank aktualisieren
                        ListeAktualisieren(); // Notizliste aktualisieren
                    }
                    else
                    {
                        MessageBox.Show("Fehler beim Löschen der Notiz.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private bool EntfernenNotizAusDatenbank(Notiz notiz)
        {
            // Verbindungszeichenfolge für den Zugriff auf die Datenbank
            string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Notizen.accdb";
            string query = "DELETE FROM Notizen WHERE ID = ?";

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        // Parameter für die Notiz-ID hinzufügen
                        cmd.Parameters.Add("@ID", OleDbType.VarChar, 36).Value = notiz.ID.ToString();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Löschen aus der Datenbank: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        }

        private void btnBeenden_Click(object sender, RoutedEventArgs e)
        {
            // Schließen des Fensters
            this.Close();
        }

        private void cbxKategorie_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Holen Sie den Namen der ausgewählten Kategorie aus der ComboBox
            var selectedCategoryName = (string)cbxKategorie.SelectedItem;

            // Suchen der Kategoriezeile nach Namen in der Kategorien-Tabelle
            var selectedCategoryRow = KategorienTabelle.AsEnumerable()
                .FirstOrDefault(row => row.Field<string>("Kategorie") == selectedCategoryName);

            // Holen der Kategorie-ID oder setzen von "Alle" als Standard
            var selectedCategoryId = selectedCategoryRow?["ID"].ToString() ?? "Alle";

            // Aktualisieren der Notizliste basierend auf der ausgewählten Kategorie
            ListeAktualisieren(selectedCategoryName);

            // Bedingte Formatierung des ComboBox-Inhalts basierend auf der Kategorie
            if (selectedCategoryName != "Alle")
            {
                btnNeu.IsEnabled = true;
                cbxKategorie.Foreground = new SolidColorBrush(Colors.DarkGray); // Dunkelgraue Schriftfarbe
                cbxKategorie.FontWeight = FontWeights.Bold; // Fettgedruckte Schrift
            }
            else
            {
                btnNeu.IsEnabled = false;
                cbxKategorie.Foreground = new SolidColorBrush(Colors.Red); // Rote Schriftfarbe
                cbxKategorie.FontWeight = FontWeights.Normal; // Normale Schriftart
            }
        }

        private void btnSuche_Click(object sender, RoutedEventArgs e)
        {
            // Aktualisieren der Notizliste basierend auf dem Suchtext
            listeAktualisieren(tbxSuche.Text);
        }

        private void SucheAufhebenButton_Click(object sender, RoutedEventArgs e)
        {
            // Zurücksetzen des Suchfelds und der Kategorieauswahl
            tbxSuche.Text = "";
            cbxKategorie.SelectedIndex = 0;
            listeAktualisieren();
        }

        void listeAktualisieren(string suchtext = "")
        {
            // Leeren der aktuellen Datenquelle der Notizliste
            lbxNotizen.ItemsSource = null;

            // Filtern der Notizen nach Suchtext und Erstellen einer gefilterten Liste
            var gefilterteNotizen = NotizenTabelle.AsEnumerable()
                .Where(row =>
                    string.IsNullOrEmpty(suchtext) ||
                    (row.Field<string>("Inhalt")?.IndexOf(suchtext, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(row =>
                {
                    // Konvertieren der Kategorie in ein Enum-Wert oder Festlegen auf 'Sonstiges' als Standard
                    Kategorie kategorieEnum = Enum.TryParse(row.Field<string>("Kategorie"), out Kategorie result)
                        ? result
                        : Kategorie.Sonstiges;

                    // Erstellen und Rückgabe eines Notizobjekts
                    return new Notiz
                    {
                        ID = Guid.TryParse(row["ID"].ToString(), out Guid id) ? id : Guid.Empty,
                        Inhalt = row.Field<string>("Inhalt"),
                        Kategorie = kategorieEnum,
                        ErstelltAm = row.IsNull("ErstelltAm") ? DateTime.MinValue : Convert.ToDateTime(row["ErstelltAm"])
                    };
                })
                .OrderBy(notiz => notiz.Kategorie)
                .ThenBy(notiz => notiz.Inhalt)
                .ToList();

            // Aktualisieren der Datenquelle der Notizliste
            lbxNotizen.ItemsSource = gefilterteNotizen;

            // Überprüfen, ob die gefilterte Liste leer ist, und eine Nachricht anzeigen
            if (!gefilterteNotizen.Any())
            {
                MessageBox.Show($"Es gibt keine Notizen, die enthalten: {suchtext}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Wenn eine aktuelle Notiz ausgewählt ist, versuchen, sie in der gefilterten Liste zu finden
            if (AktuelleNotiz != null)
            {
                var selectedNotiz = gefilterteNotizen.FirstOrDefault(n => n.ID == AktuelleNotiz.ID);
                if (selectedNotiz != null)
                {
                    lbxNotizen.SelectedItem = selectedNotiz;
                }
            }

            // Auffrischen der Listbox-Anzeige
            lbxNotizen.Items.Refresh();
        }
    }
}
