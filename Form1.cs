using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using System.IO;
using System.Reflection;

namespace idojaras
{

    public partial class Form1 : Form
    {
        public struct Datastruct
        {
            public DateTime date;
            public string weathertype;
            public sbyte min_temperature, max_temperature, rain, rain_probability;
        }
        public static List<string> varosok_link = new List<string>();
        public static List<string> varos_nevek = new List<string>();
        public static byte sqlwait = 50;

        public static string query = "";
        public static string MySQLConnectionString = "datasource=127.0.0.1;port=3306;username=root;database=idojaras"; // szerver kapcsolódási adatok
        public static MySqlConnection databaseConnection = new MySqlConnection(MySQLConnectionString); //sql connection
        public static MySqlDataReader reader; //ebbe kerül a lekérdezés
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Van-e Net
            if (!CheckInternet())
            {
                MessageBox.Show("No Internet", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; //elv így kilép
            }
            //Van-e net VÉGE
            
            #region default options
            var datetimpicker_tt = new ToolTip();
            datetimpicker_tt.ToolTipIcon = ToolTipIcon.Info;
            datetimpicker_tt.SetToolTip(dateTimePicker1, "Ha az alapértéken hagyja akkor nem szűr nap szerint!");
            panel1.AutoScroll = true;
            comboBox1.Text = "Pécs";
            comboBox2.Text = "Városok";
            //comboBox2.Items.Add("Mindegyik város");
            comboBox2.Items.Add("Egyik város sem");
            //TableExist();
            //GetSqlLenght(); //ez adja meg az sql hosszát;
            varosok_link.Add(@"http://www.idokep.hu/30napos/Mór"); //https://www.idokep.hu/idojaras/Budapest
            varosok_link.Add(@"http://www.idokep.hu/30napos/Dunaújváros");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Miskolc");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Szeged");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Eger");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Kaposvár");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Szekszárd");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Veszprém");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Zalaegerszeg");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Székesfehérvár");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Győr");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Debrecen");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Kecskemét");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Nyíregyháza");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Szolnok");
            varosok_link.Add(@"http://www.idokep.hu/30napos/Pécs");
            for (int i = 0; i < varosok_link.Count; i++) //Combobox feltöltése a városok neveivel
            {
                string varos_nev = Varos_nev(varosok_link[i]);
                comboBox1.Items.Add(Varos_nev(varos_nev));
                comboBox2.Items.Add(Varos_nev(varos_nev));
                varos_nevek.Add(Varos_nev(varos_nev));
            }
            #endregion

            //Kezdő adatok
            List<Datastruct> Dbasic_data = new List<Datastruct>(ConvertInnerTextToList(@"http://www.idokep.hu/30napos/Pécs"));
            DrawToTab1(Dbasic_data);

            //Adatok feltölése  -> MySQL
            Activated += Form_Loaded;
          
            //Adatok feltölése -> MySQL VÉGE
        }
        static void Form_Loaded(object sender,EventArgs e)
        {
            for (int i = 0; i < varosok_link.Count; i++)
            {
                DataUpgrade(ConvertInnerTextToList(varosok_link[i]), Varos_nev(varosok_link[i]));
            }

        }
        static Datastruct[] ConvertInnerTextToList(string cities_link = "http://www.idokep.hu/30napos/Budapest") //alap érték ha kap értéket felül írja
        {
            string varos_nev = Varos_nev(cities_link);
            Datastruct[] sdata = new Datastruct[30]; //ebben tárolja az adatokat
            int db = 0, incasedb = 0;

            HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load(cities_link); ;

            var list = new List<string>(doc.DocumentNode.SelectNodes("//body/div/div/div/div/div").Select(div => div.InnerText)); //az oszlopok div tartalma stringet ad vissza
            string combinedString = String.Join(",", list.ToArray()).Trim().Replace("\n\n", "").Replace("\t", ""); // ez lehet nem kéne 
                                                                                                                   //string join a diveket vesszővel elválasztja így egyszerűbb a kezelésű
                                                                                                                   // String.Join első érteke mivel fűzze össze a második hogy MIT

            List<string> data = new List<string>(combinedString.Split(',')); // egy nap egy lista elem
            data.RemoveRange(30, data.Count - 30); //honnan , mennyit // 30 nap adatai


            for (int i = 0; i < data.Count; i++)
            {
                List<string> just = new List<string>(data[i].Split('\n'));  //ez egy nap !!!!

                for (int j = 0; j < just.Count; j++) //vizsgálat, hogy hány soros az országos átlag zavaró -> kivesz
                {
                    if (just[j] != "" && just[j] != "(országos átlag)") db++;
                }
                ////
                if (db == 11) //első eset amikor 2 weathertype van 
                {
                    db = 0;
                    for (int k = 0; k < just.Count; k++)
                    {
                        if (just[k] != "")
                        {
                            incasedb++; //hányadik olyan sor ami tartalmaz valamit
                            if (incasedb == 3) sdata[i].date = Convert.ToDateTime(just[k]);
                            else if (incasedb == 4) sdata[i].weathertype = just[k];  //időjárás típus -> lehet több érték is pl. esős/ködös
                            else if (incasedb == 5) sdata[i].weathertype = " " + just[k];
                            else if (incasedb == 6) sdata[i].max_temperature = Convert.ToSByte(just[k]);
                            else if (incasedb == 9)
                            {
                                sdata[i].min_temperature = Convert.ToSByte(just[k]);
                                incasedb = 0;
                                break;
                            }
                        }
                    }
                }
                else if (db == 15)
                { //
                    db = 0;
                    for (int k = 0; k < just.Count; k++)
                    {
                        if (just[k] != "")
                        {
                            incasedb++; //hányadik olyan sor ami tartalmaz valamit
                            if (incasedb == 3) sdata[i].date = Convert.ToDateTime(just[k]);
                            else if (incasedb == 4) sdata[i].weathertype = just[k];  //időjárás típus -> lehet több érték is pl. esős/ködös
                            else if (incasedb == 7) sdata[i].max_temperature = Convert.ToSByte(just[k]);
                            else if (incasedb == 10)
                            {
                                sdata[i].min_temperature = Convert.ToSByte(just[k]);
                                incasedb = 0;
                                break;
                            }
                        }
                    }


                }
                else if (db == 17)
                { //második eset amikor van eső
                    db = 0;
                    for (int k = 0; k < just.Count; k++)
                    {
                        if (just[k] != "" && just[k] != "(országos átlag)")
                        {
                            incasedb++;
                            if (incasedb == 3) sdata[i].date = Convert.ToDateTime(just[k]);
                            else if (incasedb == 4) sdata[i].weathertype = just[k];  //időjárás típus -> lehet több érték is pl. esős/ködös
                            else if (incasedb == 5)
                            {
                                string[] rain_temporary = just[k].Split(' ');
                                sdata[i].rain = Convert.ToSByte(rain_temporary[1]);
                            }
                            else if (incasedb == 6)
                            {
                                string[] temp_rain = just[k].Split();
                                temp_rain[1] = temp_rain[1].Remove(2); //  -> % jel levág
                                sdata[i].rain_probability = Convert.ToSByte(temp_rain[1]);
                            }
                            else if (incasedb == 8) sdata[i].max_temperature = Convert.ToSByte(just[k]);
                            else if (incasedb == 11)
                            {
                                sdata[i].min_temperature = Convert.ToSByte(just[k]);
                                incasedb = 0;
                                break;
                            }
                        }
                    }
                }
                else if (db == 10)  //szimpla eset 1 weathertype és nincs csapadék
                {
                    db = 0;
                    for (int k = 0; k < just.Count; k++)
                    {
                        if (just[k] != "")
                        {
                            incasedb++;
                            if (incasedb == 3) sdata[i].date = Convert.ToDateTime(just[k]);
                            else if (incasedb == 4) sdata[i].weathertype = just[k];  //időjárás típus -> lehet több érték is pl. esős/ködös
                            else if (incasedb == 5) sdata[i].max_temperature = Convert.ToSByte(just[k]);
                            else if (incasedb == 8)
                            {
                                sdata[i].min_temperature = Convert.ToSByte(just[k]);
                                incasedb = 0;
                                break;
                            }
                        }
                    }
                }
                else if (db == 19) //2 db országos átlag
                {
                    db = 0;
                    for (int k = 0; k < just.Count; k++)
                    {
                        if (just[k] != "" && just[k] != "(országos átlag)")
                        {
                            incasedb++;
                            if (incasedb == 3) sdata[i].date = Convert.ToDateTime(just[k]);
                            else if (incasedb == 4) sdata[i].weathertype = just[k];  //időjárás típus -> lehet több érték is pl. esős/ködös
                            else if (incasedb == 5)
                            {
                                string[] rain_temporary = just[k].Split(' ');
                                sdata[i].rain = Convert.ToSByte(rain_temporary[1]);
                            }
                            else if (incasedb == 6)
                            {
                                string[] temp_rain = just[k].Split();
                                temp_rain[1] = temp_rain[1].Remove(2); //  -> % jel levág
                                sdata[i].rain_probability = Convert.ToSByte(temp_rain[1]);
                            }
                            else if (incasedb == 9) sdata[i].max_temperature = Convert.ToSByte(just[k]);
                            else if (incasedb == 12)
                            {
                                sdata[i].min_temperature = Convert.ToSByte(just[k]);
                                incasedb = 0;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    db = 0;
                    sdata[i].date = Convert.ToDateTime("2000. 01. 03"); //"dddd, dd MMMM yyyy // Friday, 29 May 2015
                    sdata[i].weathertype = "Derült";
                    sdata[i].min_temperature = -100;
                    sdata[i].max_temperature = 100;
                }
            }
            return sdata;
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            panel1.Controls.Clear(); //Panel1-ről mindent töröl
            for (int i = 0; i < varosok_link.Count; i++)
            {
                if (comboBox1.Text == varos_nevek[i])
                {
                    List<Datastruct> temp = new List<Datastruct>(ConvertInnerTextToList(varosok_link[i]));
                    DrawToTab1(temp);
                }
                // else MessageBox.Show("Nincs ilyen város!", "Hiba!", MessageBoxButtons.OK);
            }

        } // Ez tölti fel a Tab1-re az adatokat
        static string Varos_nev(string link)  //linkből vissza adja a város nevét
        {
            string[] tempArray = link.Split('/');
            return tempArray[tempArray.Length - 1];
        }

        static bool Check(string varos_nev, Datastruct new_adatok)
        {
            int cities_id = varos_nevek.IndexOf(varos_nev) + 1;
            string queryDate = CDateToSqlDate(new_adatok.date);
            query = "select date from  data where  date = '" + queryDate +"' AND cities_id = "+cities_id +"";
            MySqlCommand commandDatabase = new MySqlCommand(query, databaseConnection); // ez maga a lekérdezés Objektum
            try
            {
                if (databaseConnection.State != ConnectionState.Open) databaseConnection.Open(); //checkol hogy nyitva van-e

            }
            catch (Exception)
            {

                MessageBox.Show("Az adatbázis nem sikerült elérni. Valószínüleg bezáródott az XAMPP, kérem indítsa újra a programot és az XAMPP-t");
                Application.Exit();
            }
            try
            {
                /* if (reader.HasRows) // HasRows : visszatérési érték bool , ha nem nulla akkor 1 vagy annál nagyobb érték
                 {
                     return false;
                 }*/
                var result = commandDatabase.ExecuteScalar(); // egy értékkel tér vissza vagy null-al
                    if (result == null)
                    {
                        return false;
                    }
            }
            catch (Exception e)
            {
                MessageBox.Show("Hiba a Check voidba :" + e.Message);
                databaseConnection.Close();
            }
            databaseConnection.Close();
            return true; //van-e változás

        } //ez ellenőrzi le hogy van-e valami új az új futattás során az adatbázis beli értékekhez képest
        static void DataUpgrade(Datastruct[] adatok, string varos_nev)
        {
            query = "";
            int cities_id = varos_nevek.IndexOf(varos_nev)+1;
                for (int i = 0; i < adatok.Length; i++)
                {
                string Sdate = CDateToSqlDate(adatok[i].date); 
                if (!Check(varos_nev, adatok[i]))
                    {
                        query = "INSERT INTO data(`id`,`date`,`weather_type`,`max_temperature`,`min_temperature`,`rain`,`rain_probability`,`cities_id`)" +
                           "VALUES (NULL,'" + Sdate + "','" + adatok[i].weathertype + "'," + adatok[i].max_temperature + "," + adatok[i].min_temperature + "," + adatok[i].rain + "," + adatok[i].rain_probability + "," + cities_id + ")";
                        MySqlCommand commandDatabase = new MySqlCommand(query, databaseConnection); // lekérdezés ,melyik connectionban
                        commandDatabase.CommandTimeout = 50; //ennyi időt fog várni a szerver válaszára mielött error-t dob
                        try
                        {
                            if (databaseConnection.State != ConnectionState.Open) databaseConnection.Open(); //checkol hogy nyitva van-e
                                                                                                             // MySqlDataReader myReader = commandDatabase.ExecuteReader(); //futattó objektum
                            reader = commandDatabase.ExecuteReader(); //parancs lefuttatása
                            reader.Close();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Az adatbázis nem sikerült elérni. Valószínüleg bezáródott az XAMPP, kérem indítsa újra a programot és az XAMPP-t)\n" + e.Message);
                            databaseConnection.Close();
                        }
                    }
                else  //Már létezik az napi adat és azokat felülírja -> Ez az ág sokkal gyorsabb(Amikor másodjára indul el egy nap a program sokkal gyorsabb )
                {
                    query = "Update data SET date = '"+Sdate + "', weather_type ='" + adatok[i].weathertype + "', max_temperature = "+adatok[i].max_temperature +", min_temperature = "+adatok[i].min_temperature+", rain = "+ adatok[i].rain+", rain_probability = "+ adatok[i].rain_probability+" where cities_id = "+ cities_id+ " AND date = '"+Sdate+"'";
                        MySqlCommand commandDatabase = new MySqlCommand(query, databaseConnection); // lekérdezés ,melyik connectionban
                        commandDatabase.CommandTimeout = sqlwait; //ennyi időt fog várni a szerver válaszára mielött error-t dob

                    try
                        {
                            if (databaseConnection.State != ConnectionState.Open) databaseConnection.Open(); //checkol hogy nyitva van-e
                            reader = commandDatabase.ExecuteReader(); //parancs lefuttatása
                            databaseConnection.Close(); //kapcsolat bezár
                        reader.Close(); // bezár
                        
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Az adatbázis nem sikerült elérni. Valószínüleg bezáródott az XAMPP, kérem indítsa újra a programot és az XAMPP-t" + e.Message);
                            databaseConnection.Close();
                        Application.Exit();
                        }
                    databaseConnection.Close();
                }

                databaseConnection.Close();
                    // MessageBox.Show("Succesful! DataUpgrade főág " + varos_nev);
                }
            
           
        } //Időjárás adatok feltöltőse -> MySql

        public bool CheckInternet() // van-e nete a felhasználónak
        {
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load("http://google.com/");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            query = "";
            int cities_id = varos_nevek.IndexOf(comboBox2.Text) + 1;
            try
            {
                string date_in_query = String.Empty;
                string temp = DateTime.Now.Year + ". ";
                temp += DateTime.Now.Month < 10 ? "0" + DateTime.Now.Month + ". " : DateTime.Now.Month.ToString() + ". ";
                temp += DateTime.Now.Day < 10 ? "0" + DateTime.Now.Day + "." : DateTime.Now.Day.ToString() + ".";
                date_in_query = dateTimePicker1.Value.Year.ToString()+"-";
                date_in_query += Convert.ToInt32(dateTimePicker1.Value.Month) < 10 ? "0" + dateTimePicker1.Value.Month + "-" : dateTimePicker1.Value.Month.ToString() + "-";
                date_in_query += dateTimePicker1.Value.Day < 10 ? "0" + dateTimePicker1.Value.Day : dateTimePicker1.Value.Day.ToString();

                // temp = DateTime.Now.Year.ToString() +"."+ DateTime.Now.Month.ToString() +"."+ DateTime.Now.Day.ToString();
                if (comboBox2.Text == "Egyik város sem" && dateTimePicker1.Value.ToShortDateString() != temp) //így vizsgál csak napra
                {
                    query = "SELECT  cities_name , date, max_temperature, min_temperature, rain, weather_type FROM data, cities WHERE data.cities_id = cities.id AND date = '" + date_in_query + ".' ";
                    Query(query);
                }
                else if (comboBox2.Text != "Mindegyik város" && dateTimePicker1.Value.ToShortDateString() == temp) //így vizsgál csak városra
                {
                    query = "SELECT  cities_name , date, max_temperature, min_temperature, rain, weather_type FROM data,cities WHERE data.cities_id = cities.id AND cities_id = '" + cities_id + "' ";
                    Query(query);
                }
                else if (comboBox2.Text != "Városok" && dateTimePicker1.Value.ToShortDateString() != temp) //napra és városra is vizsgál
                {
                    query = "SELECT cities_name , date, max_temperature, min_temperature, rain, weather_type FROM data,cities WHERE data.cities_id = cities.id AND cities_id = '" + cities_id + "' AND date = '" + date_in_query + "'";
                    Query(query);
                }
                else if (comboBox2.Text == "Egyik város sem" && dateTimePicker1.Value.ToShortDateString() == temp) //Összes adat kiir
                {
                    query = "SELECT cities_name, date, max_temperature, min_temperature, rain, weather_type FROM data"; 
                    Query(query);
                }
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Nem adott meg értéket");
            }
            catch (Exception qe)
            {
                MessageBox.Show(qe.Message);
            }
        } //DataGridView-ba adatok MySQL-ből
        static void Query(string query)
        {
            MySqlCommand commandDatabase = new MySqlCommand(query, databaseConnection); // ez maga a lekérdezés Objektum
            commandDatabase.CommandTimeout = sqlwait;

            dataGridView1.Columns.Add("varosnev", "Város név");
            dataGridView1.Columns.Add("datum", "Dátum");
            dataGridView1.Columns.Add("max_homerseklet", "Max hőmérséklet");
            dataGridView1.Columns.Add("min_homerseklet", "Min hőmérséklet");
            dataGridView1.Columns.Add("eso", "Eső");
            dataGridView1.Columns.Add("weather_type", "Időjárás");

            try
            {
                if (databaseConnection.State != ConnectionState.Open) databaseConnection.Open(); //checkol hogy nyitva van-e
                reader = commandDatabase.ExecuteReader();

                while (reader.Read()) //DatagridView-ba adatok beolvasása
                {
                    string[] datum = reader.GetString(1).Split(); //Így nem lesz a 00:00:00-a végén
                    dataGridView1.Rows.Add(reader.GetString(0), datum[0] + " " + datum[1] + " " + datum[2], reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5));
                }
                //MessageBox.Show("DeleteFromMySQl void sikeres!");
            }
            catch (Exception e1)
            {
                MessageBox.Show("DataGridView-s lekérdezés \n" + e1.Message);
                databaseConnection.Close();
            }
            databaseConnection.Close();

        } //Ez futatja a DataGridView-ba kért lekérdezést

        static void DrawToTab1(List<Datastruct> temp)
        {
            string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            //string asd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            byte eltolas = 0;
            for (int j = 0; j < 30; j++)
            {
                // dátum
                Label date_lbl = new Label();
                string honap;
                int hetszam = Convert.ToInt32(temp[j].date.DayOfWeek);
                string napnev = "Error!";
                string nap;
                switch (hetszam)
                {
                    case 0:
                        napnev = "Vasárnap";
                        break;
                    case 1:
                        napnev = "Hétfő";
                        break;
                    case 2:
                        napnev = "Kedd";
                        break;
                    case 3:
                        napnev = "Szerda";
                        break;
                    case 4:
                        napnev = "Csütörtök";
                        break;
                    case 5:
                        napnev = "Péntek";
                        break;
                    case 6:
                        napnev = "Szombat";
                        break;
                    default:
                        napnev = "Error";
                        break;
                }
                if (temp[j].date.Day < 10)
                    nap = "0" + Convert.ToString(temp[j].date.Day);
                else
                    nap = Convert.ToString(temp[j].date.Day);

                if (temp[j].date.Month < 10)
                    honap = "0" + Convert.ToString(temp[j].date.Month);
                else
                    honap = temp[j].date.Month.ToString();
                date_lbl.Text = honap + "." + nap + ".\n" + napnev;
                date_lbl.Name = "data_lbl" + j.ToString();
                date_lbl.Size = new Size(37, 30);
                date_lbl.Location = new Point(j * 50 + 14 + eltolas, 30); // plusz 16 miatt van középen a szöveg a képhez képest
                panel1.Controls.Add(date_lbl);

                string filepath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                //dátum vége
                try
                {
                //pcb -> képek
                PictureBox pcb = new PictureBox();
                pcb.Size = new Size(32, 32);
                if (temp[j].weathertype.Contains("Erősen felhős") || temp[j].weathertype.Contains("Borult"))
                {
                    Bitmap bit = new Bitmap(filepath+@"\borult.png");
                    pcb.Image = bit;
                }
                else if (temp[j].weathertype.Contains("Közepesen felhős") || temp[j].weathertype.Contains("Gyengén felhős"))
                {
                    Bitmap bit = new Bitmap(filepath+@"\kozepesen_felhos.png");
                    pcb.Image = bit;
                }
                else if (temp[j].weathertype.Contains("Szeles nap") || temp[j].weathertype.Contains("Viharos szél") || temp[j].weathertype.Contains("Hidegfront erős széllel"))
                {
                    Bitmap bit = new Bitmap(@filepath+@"\szeles_nap.png");
                    pcb.Image = bit;
                }
                else if (temp[j].weathertype.Contains("Zápor") || temp[j].weathertype.Contains("Gyenge eső") || temp[j].weathertype.Contains("Zivatar") || temp[j].weathertype.Contains("zivatarok"))
                {
                    Bitmap bit = new Bitmap(@filepath+@"\zapor.png");
                    pcb.Image = bit;
                }
                else if (temp[j].weathertype.Contains("Eső"))
                {
                    Bitmap bit = new Bitmap(@filepath+@"\eso.png");
                    pcb.Image = bit;
                }
                else if (temp[j].weathertype.Contains("Derült") || temp[j].weathertype.Contains("Melegfront"))
                {
                    Bitmap bit = new Bitmap(@filepath+@"\derult.png");
                    pcb.Image = bit;
                }
                else if (temp[j].weathertype.Contains("Havas eső") || temp[j].weathertype.Contains("Hózápor"))
                {
                    Bitmap bit = new Bitmap(@filepath+@"\havas.png");
                    pcb.Image = bit;
                }
                else
                {
                    Bitmap bit = new Bitmap(@filepath+@"\error.png");
                    pcb.Image = bit;
                }
                ToolTip tt = new ToolTip();
                tt.SetToolTip(pcb, temp[j].weathertype);
                pcb.SizeMode = PictureBoxSizeMode.StretchImage; //beletördeli a picutrebox-ba a képet
                pcb.Location = new Point(j * 50 + 15 + eltolas, 70);
                pcb.Name = "pcb" + j.ToString();
                panel1.Controls.Add(pcb);
                ////pcb -> képek Vége
                }
                catch (System.ArgumentException e)
                {

                    MessageBox.Show(e.ToString()+temp[j].weathertype);
                }

                //max hőfok
                Label max_hofok_lbl = new Label();
                max_hofok_lbl.Text = temp[j].max_temperature.ToString() + "°C";
                max_hofok_lbl.Size = new Size(32, 15);
                max_hofok_lbl.Location = new Point(j * 50 + 20 + eltolas, 130);
                max_hofok_lbl.Name = "max_hofok_lbl" + j.ToString();
                panel1.Controls.Add(max_hofok_lbl);
                if (temp[j].max_temperature > 10) eltolas++;
                if (temp[j].max_temperature < -9) eltolas++;
                //min hőfok vége

                //min hőfok
                Label min_hofok_lbl = new Label();
                min_hofok_lbl.Text = temp[j].min_temperature.ToString() + "°C";
                min_hofok_lbl.Size = new Size(32, 15);
                min_hofok_lbl.Location = new Point(j * 50 + 20 + eltolas, 180);
                min_hofok_lbl.Name = "min_hofok_lbl" + j.ToString();
                panel1.Controls.Add(min_hofok_lbl);
                if (temp[j].min_temperature > 9) eltolas++;
                if (temp[j].min_temperature < -9) eltolas += 2;
                //min hőfok vége

                if (j < 7)
                {
                    //rain
                    Label rain_lbl = new Label();
                    rain_lbl.Text = temp[j].rain.ToString() + " mm";
                    rain_lbl.Size = new Size(32, 15);
                    rain_lbl.Location = new Point(j * 50 + 20 + eltolas, 230);
                    rain_lbl.Name = "rain_lbl" + j.ToString();
                    panel1.Controls.Add(rain_lbl);
                    if (temp[j].rain > 10) eltolas++;
                    //rain vége

                    //rain probaility lbl
                    Label rain_probality_lbl =  new Label();
                    rain_probality_lbl.Text = temp[j].rain_probability.ToString() + "%";
                    rain_probality_lbl.Size = new Size(32, 15);
                    rain_probality_lbl.Location = new Point(j * 50 + 24 + eltolas, 280);
                    rain_probality_lbl.Name = "rain_probality_lbl" + j.ToString();
                    panel1.Controls.Add(rain_probality_lbl);
                    //rain probaility lbl vége
                }
            } // form labelek, pictureboxok}
        } //Megkap egy Lit<Datastruct>-ot és azt kirajzolja a Tab1-re
        static string CDateToSqlDate(DateTime cDate)
        {
            string Sdate = cDate.Year.ToString() + "-";
            Sdate += Convert.ToInt32(cDate.Month) < 10 ? "0" + cDate.Month + "-" : cDate.Month.ToString() + "-";
            Sdate += cDate.Day < 10 ? "0" + cDate.Day : cDate.Day.ToString();
            return Sdate;
        } // ez alakítja a pontokat "-"-re és rakja be a nullákat pl (2000.3.5) -> (2000-03-05)

        /* static void GetSqlLenght() //Megadja a adat tábla sorok számát
         {
             query = "select count(weather_type) from data";
             try
             {
                 if (databaseConnection.State != ConnectionState.Open) databaseConnection.Open(); //checkol hogy nyitva van-e
                 MySqlCommand commandDatabase = new MySqlCommand(query, databaseConnection); // ez maga a lekérdezés Objektum
                 commandDatabase.CommandTimeout = sqlwait;
                 reader = commandDatabase.ExecuteReader();
                 SQL_lenght = Convert.ToUInt64(reader.GetString(0));
             }
             catch (Exception e)
             {
                 MessageBox.Show("GetSqlLenght()-ben hiba\n" + e.Message);
             }
         }*/

        /*static void TableIsExist()
        {
            query = " CREATE TABLE IF NOT EXISTS `idojaras`.`cities` (`id` INT NOT NULL AUTO_INCREMENT,`cities_name` VARCHAR(45) NULL, PRIMARY KEY(`id`)) ENGINE = InnoDB";
            try
            {
                if (databaseConnection.State != ConnectionState.Open) databaseConnection.Open(); //checkol hogy nyitva van-e
                MySqlCommand commandDatabase = new MySqlCommand(query, databaseConnection);
                commandDatabase.CommandTimeout = sqlwait;
                reader = commandDatabase.ExecuteReader();
            }
            catch (Exception e )//ide kell hogy milyen exepction dob ha nincs database
            {
                //ide kell az sql kód ami létrehozza a két táblát és a kapcsolatot
                MessageBox.Show("CheckDataBaseIsExit()-ben hiba " + e.Message);
                databaseConnection.Close();
            }
            databaseConnection.Close();

            query = "CREATE TABLE IF NOT EXISTS `idojaras`.`data`( `id` INT NOT NULL AUTO_INCREMENT, `date` DATETIME NOT NULL, `weather_type` VARCHAR(80) NOT NULL, `max_temperature` INT NOT NULL, `min_temperature` INT NOT NULL, `rain` INT NOT NULL, `rain_probability` INT NULL, `datacol` VARCHAR(45) NULL, `cities_id` INT NOT NULL, PRIMARY KEY(`id`), INDEX `fk_data_cities_idx`(`cities_id` ASC), CONSTRAINT `fk_data_cities` FOREIGN KEY(`cities_id`) REFERENCES `idojaras`.`cities`(`id`) ON DELETE NO ACTION ON UPDATE NO ACTION ) ENGINE = INNODB";
            try
            {
                if (databaseConnection.State != ConnectionState.Open) databaseConnection.Open(); //checkol hogy nyitva van-e
                MySqlCommand commandDatabase = new MySqlCommand(query, databaseConnection);
                commandDatabase.CommandTimeout = sqlwait;
                reader = commandDatabase.ExecuteReader();
                
            }
            catch (Exception e)//ide kell hogy milyen exepction dob ha nincs database
            {
                //ide kell az sql kód ami létrehozza a két táblát és a kapcsolatot
                MessageBox.Show("CheckDataBaseIsExit()-ben hiba " + e.Message);
                databaseConnection.Close();
            }
            databaseConnection.Close();

        } //Megvizsgálja hogy a táblák léteznek e ? nem csinál semmit : létrehoz*/

        /* static void MyDeleteFromMySQL(string varos_nev)
         {
             int cities_id = varos_nevek.IndexOf(varos_nev) + 1;
             query = "";
             query = "DELETE FROM data WHERE cities_id = '" + cities_id + "' limit 30"; //ez nem fix hogy jó ez a limit lehet kell DESC vagy ASC
             MySqlCommand commandDatabase = new MySqlCommand(query, databaseConnection);
             commandDatabase.CommandTimeout = sqlwait;
             try
             {
                 if (databaseConnection.State != ConnectionState.Open) databaseConnection.Open(); //checkol hogy nyitva van-e
                 reader = commandDatabase.ExecuteReader();
                 //MessageBox.Show("DeleteFromMySQl void sikeres!");
             }
             catch (Exception e)
             {
                 MessageBox.Show("DeleteFromMysql void-ba probléma" + e.Message);
                 databaseConnection.Close();
             }
             databaseConnection.Close();
         } // Ha van egyezés a Check Voidba ezt hívja meg és törli az utoló 30-at*/

    }
}
