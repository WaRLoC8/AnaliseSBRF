using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AnaliseSBRF
{
    public partial class Form1 : Form
    {
        #region Переменные формы
        //Максимальное количество циклов
        int npp = 0;       
        //Минимальная и максимальная цены
        int pricemin = 0;
        int pricemax = 0;
        //Выгруженный массив первичных данных
        int[][] prices;
        //Количество сделок в периоде цикла
        int cycle = 50;
        //Количество периодов
        int per = 0;
        //Имя текущей таблицы
        string TableName = "";
        //Массив индексов по тренду
        //double[] indicate;       
        //Параметр нормировки индикатора
        double param = 14;
        double multPorog = 2;
        public static string dataBase = "sbrf";
        
        Portfel portfel;
        double bestParam = 21;
        double bestPorog = 40;
        double spread = 12;
        int smth = 3;
        int coreCount = 0;
        int threadCount = 0;
        int metod = 0;
        double profitPrice = 80;
        bool enableLog = false;

        int limTmax = (int)TimeSpan.Parse("14:00:00").TotalMilliseconds;
        int limTmin = (int)TimeSpan.Parse("09:00:00").TotalMilliseconds;
        int orderIsp = 20000;

        //Параметры подключения к MySQL
        MySqlConnection conn = DBUtils.GetDBConnection();
        MySqlCommand comm;
        MySqlDataReader reader;
        string query;

        #endregion

        public Form1()
        {
            InitializeComponent();

            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfCores"].ToString());
            }
            
            label20.Text = "Количество ядер процессора = " + coreCount;

            threadCount = Environment.ProcessorCount;

            label20.Text += "  Количество потоков = " + threadCount + "  Имя компьютера - " + Environment.MachineName;

            //coreCount--;


            DateTime tm = DateTime.Now;
            
           
            comboBox2.SelectedIndex = metod;
            comboBox3.SelectedIndex = 0;

        }

        #region Элементы управления формы
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            TableName = "futsbrfarch_" + comboBox1.SelectedItem.ToString();
            prices = GetPrices(TableName);
            NewTrendToChart(prices[0], 50, this.chart1);                
            double[] indicate = IndicateTime(prices, param);
            NewDataToChart(indicate, 20, this.chart2);
            //NewSpektrToChart(Spektr(indicate, spektrCount), this.chart2);
            numericUpDown6.Maximum = comboBox1.Items.Count;
            numericUpDown6.Value = comboBox1.Items.Count;
            numericUpDown5.Maximum = comboBox1.Items.Count - 1;
            numericUpDown5.Value = 1;
            //numericUpDown1.Maximum = numericUpDown6.Value - 1;
            label13.Text = "npp = " + prices[0].Length.ToString() + " minPrice = " + pricemin + " maxPrice = " + pricemax;
            //button1_Click(null, null);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            metod = comboBox2.SelectedIndex;
            if (metod == 4 | metod == 5)
            {
                comboBox3.Items.Clear();
                comboBox3.Items.Add("09:00:00 - 14:00:00");
                comboBox3.Items.Add("14:00:00 - 19:00:00");
                comboBox3.Items.Add("19:00:00 - 24:00:00");
                comboBox3.SelectedIndex = 0;
                
            }
            else 
            {
                comboBox3.Items.Clear();
                comboBox3.Items.Add("sbrf");
                comboBox3.Items.Add("sbrfdata");
                comboBox3.Items.Add("quik_data");
                comboBox3.SelectedIndex = 0;
                
            }
            if (metod == 6)
            {
                numericUpDown11.Value = 2;
                spread = 2;
            }
            else
            {
                numericUpDown11.Value = 12;
                spread = 12;
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (metod == 4 | metod == 5)
            {
                if (comboBox3.SelectedIndex == 0)
                {
                    limTmin = (int)TimeSpan.Parse("09:00:00").TotalMilliseconds;
                    limTmax = (int)TimeSpan.Parse("14:00:00").TotalMilliseconds;
                }
                else if (comboBox3.SelectedIndex == 1)
                {
                    limTmin = (int)TimeSpan.Parse("14:00:00").TotalMilliseconds;
                    limTmax = (int)TimeSpan.Parse("19:00:00").TotalMilliseconds;
                }
                else
                {
                    limTmin = (int)TimeSpan.Parse("19:00:00").TotalMilliseconds;
                    limTmax = (int)TimeSpan.Parse("24:00:00").TotalMilliseconds;
                }
            }
            else
            {
                query = "show tables like 'futsbrfarch_%';";
                if (conn.State.ToString().Equals("Open"))
                {
                    conn.Close();
                }
                dataBase = comboBox3.SelectedItem.ToString();
                conn = DBUtils.GetDBConnection();
                conn.Open();
                comboBox1.Items.Clear();
                comm = new MySqlCommand(query, conn);
                reader = comm.ExecuteReader();
                while (reader.Read())
                {
                    comboBox1.Items.Add(reader[0].ToString().Remove(0, 12));
                }
                reader.Close();
                comboBox1.SelectedIndex = comboBox1.Items.Count - 1;
            }

        }

        private void chart1_MouseWheel(object sender, MouseEventArgs e) //Изменение масштаба от прокрутки колеса мыши
        {
            ScaleChart(this.chart1, e);
        }

        private void chart2_MouseWheel(object sender, MouseEventArgs e) //Изменение масштаба от прокрутки колеса мыши
        {
            /*if (e.Delta > 0)
            {
                if (pricePartStart < pricePart - 1)
                {
                    pricePartStart++;
                    button1_Click(null, null);
                }
            }
            else
            {
                if (pricePartStart >0)
                {
                    pricePartStart--;
                    button1_Click(null, null);
                }
            }  */          
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            multPorog = (double)numericUpDown1.Value / 10;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
           numericUpDown4.Minimum = numericUpDown2.Value + 1;
           numericUpDown7.Minimum = numericUpDown2.Value * 10;
        }
              
        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown2.Maximum = numericUpDown4.Value - 1;
            numericUpDown7.Maximum = numericUpDown4.Value * 10;
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown6.Minimum = numericUpDown5.Value + 1;
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown5.Maximum = numericUpDown6.Value - 1;
            //numericUpDown1.Maximum = numericUpDown6.Value - 1;
        }

        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            bestPorog = Convert.ToDouble(numericUpDown7.Value) / 10;
        }

        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {
            bestParam = Convert.ToDouble(numericUpDown8.Value);
        }
                
        private void numericUpDown9_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown8.Maximum = numericUpDown9.Value;
            numericUpDown10.Maximum= numericUpDown9.Value - 1;
        }

        private void numericUpDown10_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown9.Minimum = numericUpDown10.Value + 1;
            numericUpDown8.Minimum= numericUpDown10.Value;
        }
                
        private void numericUpDown11_ValueChanged(object sender, EventArgs e)
        {
            spread = (double)numericUpDown11.Value;
        }

        private void numericUpDown12_ValueChanged(object sender, EventArgs e)
        {
            smth = (int)numericUpDown12.Value;
        }
                
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            enableLog = true;
            portfel = takeResult(prices);
            int besti = (int)(numericUpDown8.Value) - (int)(numericUpDown10.Value);
            int bestj = ((int)((Convert.ToDouble(numericUpDown7.Value) / 10) - (double)(numericUpDown2.Value)) * 10);
            //int bestj = (int)((bestPorog - portfel.minPorog) * portfel.maxPart / (portfel.maxPorog - portfel.minPorog));
            int testDeposit = 0;
            double[] maxDep = new double[10];
            string[] maxDepParam = new string[10];
            maxDep[0] = portfel.deposit[0][0, 0];
            maxDep[1] = portfel.deposit[0][0, (int)(portfel.porogPart / 2)];
            maxDepParam[0] = "param = " + (portfel.minParam).ToString("0.00") + " porog = " + (portfel.minPorog).ToString("0.00") + " operation = " + portfel.deposit[1][0, 0].ToString() + " | ";
            double minDep = 0;
            double loss = 0;
            int losscount = 0;
            double profit = 0;
            int profitcount = 0;
            double[] depspektr = new double[portfel.paramPart * portfel.porogPart];
            for (int i = 0; i < portfel.paramPart; i++)
                for (int j = 0; j < portfel.porogPart; j++)
                {
                    depspektr[i * portfel.paramPart + j] = portfel.deposit[0][i, j];
                    if (portfel.deposit[0][i, j] > maxDep[0])
                    {
                        for (int k = maxDep.Length - 1; k > 0; k--)
                        {
                            maxDep[k] = maxDep[k - 1];
                            maxDepParam[k] = maxDepParam[k - 1];
                        }
                        maxDep[0] = portfel.deposit[0][i, j];
                        maxDepParam[0] = "param = " + (portfel.minParam + Convert.ToDouble(i) * (portfel.maxParam - portfel.minParam) / (portfel.paramPart - 1)).ToString("0.00") + " porog = " + (portfel.minPorog + j * (portfel.maxPorog - portfel.minPorog) / (portfel.porogPart - 1)).ToString("0.00") + " operation = " + portfel.deposit[1][i, j].ToString() + " | ";
                    }
                    if (portfel.deposit[0][i, j] < minDep) minDep = portfel.deposit[0][i, j];
                    if (portfel.deposit[0][i, j] < 0)
                    {
                        loss += portfel.deposit[0][i, j];
                        losscount++;
                    }
                    if (portfel.deposit[0][i, j] > 0)
                    {
                        profit += portfel.deposit[0][i, j];
                        profitcount++;
                    }
                }
            string msg = "Max deposites = ";
            for (int i = 0; i < maxDep.Length; i++) msg += maxDep[i].ToString() + "  ";
            label6.Text = msg;
            label8.Text = maxDepParam[0] + maxDepParam[1];
            label9.Text = " minDep = " + minDep.ToString() + " loss = " + (loss / Convert.ToDouble(losscount)).ToString("0.00") + " loss count = " + losscount + " profit = " + (profit / Convert.ToDouble(profitcount)).ToString("0.00") + " profit count = " + profitcount;
            testDeposit = (int)portfel.deposit[0][besti, bestj];
            label12.Text = "Доходность = " + testDeposit + "   порог = " + bestPorog + "  параметр = " + bestParam + " сглаживание = " + smth+ " время отсечки = " + numericUpDown3.Value.ToString()  + "  операций = " + portfel.deposit[1][besti, bestj];
            Log.LogWrite(label12.Text, enableLog);
            Log.LogWrite("Обработана таблица = " + TableName, enableLog);
            Log.LogWrite("___________________________________________________________________", enableLog);

            //Log.LogWrite(testOperation);
            NewSpektrToChart(Spektr(depspektr, 50), chart2);
            label13.Text = "maxPrice = " + pricemax + " minPrice = " + pricemin + " npp = " + prices[0].Length;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            enableLog = false;
            DateTime analiseTime = DateTime.Now;
            double[,] totalDeposit = new double[(int)(numericUpDown9.Value) - (int)(numericUpDown10.Value) + 1, ((int)numericUpDown4.Value - (int)numericUpDown2.Value) * 10 + 1];
            int[,] totalDeals = new int[(int)(numericUpDown9.Value) - (int)(numericUpDown10.Value) + 1, ((int)numericUpDown4.Value - (int)numericUpDown2.Value) * 10 + 1];

            //Доходность лучшего портфеля, пока так
            double[] bestDeposit = new double[comboBox1.Items.Count];

            int besti = (int)(numericUpDown8.Value) - (int)(numericUpDown10.Value);
            int bestj = (int)((((double)(numericUpDown7.Value) / 10) - (double)(numericUpDown2.Value)) * 10);

            Log.LogWrite("best I = " + besti + " best J = " + bestj, true);

            int startDay = 0;
            
            Form2 form = new Form2();
            this.Cursor = Cursors.WaitCursor;
            form.Cursor = Cursors.WaitCursor;
            form.progressBar1_setmax((int)numericUpDown6.Value - (int)numericUpDown5.Value + 1);            
            form.Show();
            form.progressBar1_setvol(0);

            #region проверяем существование таблицы с расчетами по заданным алгоритму и параметрам

            string resultPortfl = "";
            if (metod == 0) resultPortfl += "lazz";
            else if (metod == 1) resultPortfl += "lazzinv";
            else if (metod == 2) resultPortfl += "lazzprft";
            else if (metod == 3) resultPortfl += "lazz2";
            else if (metod == 4) resultPortfl += "lazztime";
            else if (metod == 5) resultPortfl += "lazztimeinv";
            else if (metod == 6) resultPortfl += "lazzordr";
            resultPortfl += "_" + smth + "_" + numericUpDown3.Value.ToString();


            MySqlConnection testConn = new MySqlConnection("Server=localhost;Database=portfel;port=3306;User Id=root;password=Loco8360!");
            testConn.Open();
            string sqlCommand = "show tables;";
            MySqlCommand cmnd = new MySqlCommand(sqlCommand, testConn);
            MySqlDataReader rdr = cmnd.ExecuteReader();
            bool findTabls = false;

            
            while (rdr.Read())
            {
                string TN = rdr[0].ToString();
                if (TN == resultPortfl) findTabls = true;
            }
            rdr.Close();            

            //if (findTabls) MessageBox.Show("Обнаружена таблица " + resultPortfl + " с расчетом по заданным параметрам");
            //else MessageBox.Show("Не обнаружена таблица " + resultPortfl + " с расчетом по заданным параметрам");

            if (!findTabls)
            {
                sqlCommand = "create table " + resultPortfl + " (npp INT PRIMARY KEY AUTO_INCREMENT, param INT, porog INT);";
                cmnd = new MySqlCommand(sqlCommand, testConn);
                cmnd.ExecuteNonQuery();
            }
            else
            {
                sqlCommand = "SELECT ORDINAL_POSITION, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + resultPortfl + "' order by ORDINAL_POSITION desc limit 1;";
                cmnd = new MySqlCommand(sqlCommand, testConn);
                rdr = cmnd.ExecuteReader();
                while (rdr.Read())
                {
                    startDay = Convert.ToInt32(rdr[0]) - 3;
                                       
                }
                rdr.Close();
                int bestnpp = besti * 601 + (int)(numericUpDown7.Value) - 299;
                
                sqlCommand = "SELECT * FROM " + resultPortfl + " where npp = " + bestnpp + ";";
                
                cmnd = new MySqlCommand(sqlCommand, testConn);
                rdr = cmnd.ExecuteReader();
                while (rdr.Read())
                {
                    for (int i = 3; i < startDay + 3; i++)
                    {
                        double bdep = Convert.ToDouble(rdr[i]);
                        if (bdep < 0)
                        {
                            bestDeposit[i - 3] = bdep + (double)(10000 * (int)(-bdep / 10000));
                        }           
                        else
                        {
                            bestDeposit[i - 3] = bdep - (double)(10000 * (int)(bdep / 10000));
                        }
                    }
                }
                rdr.Close();
            }
            //testConn.Close();
            #endregion


            if (startDay < (int)numericUpDown5.Value - 1) startDay = (int)numericUpDown5.Value - 1;
            
            for (int i = startDay; i < (int)numericUpDown6.Value; i += threadCount)
            {
                
                Portfel[] portfels = new Portfel[threadCount];

                sqlCommand = "alter table " + resultPortfl + " add column (" + comboBox1.Items[i].ToString() + " INT);";
                cmnd = new MySqlCommand(sqlCommand, testConn);
                cmnd.ExecuteNonQuery();
                
                #region Таски на парралельные вычисления
                Task task1 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 1].ToString();
                    portfels[1] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task2 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 2].ToString();
                    portfels[2] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task3 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 3].ToString();
                    portfels[3] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task4 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 4].ToString();
                    portfels[4] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task5 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 5].ToString();
                    portfels[5] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task6 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 6].ToString();
                    portfels[6] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task7 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 7].ToString();
                    portfels[7] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task8 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 8].ToString();
                    portfels[8] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task9 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 9].ToString();
                    portfels[9] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task10 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 10].ToString();
                    portfels[10] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task11 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 11].ToString();
                    portfels[11] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task12 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 12].ToString();
                    portfels[12] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task13 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 13].ToString();
                    portfels[13] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task14 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 14].ToString();
                    portfels[14] = takeResult(GetPricesInNewThread(tabName));
                });
                Task task15 = new Task(() =>
                {
                    string tabName = "futsbrfarch_" + comboBox1.Items[i + 15].ToString();
                    portfels[15] = takeResult(GetPricesInNewThread(tabName));
                });
                #endregion

                int thrdCount = 0;
                while (thrdCount < threadCount - 1)
                {
                    
                    thrdCount++;
                    if (i + thrdCount < (int)numericUpDown6.Value)
                    {                        
                        sqlCommand = "alter table " + resultPortfl + " add column (" + comboBox1.Items[i + thrdCount].ToString() + " INT);";
                        cmnd = new MySqlCommand(sqlCommand, testConn);
                        cmnd.ExecuteNonQuery();   
                        
                        if (thrdCount == 1) task1.Start();                        
                        if (thrdCount == 2) task2.Start();                       
                        if (thrdCount == 3) task3.Start();
                        if (thrdCount == 4) task4.Start();
                        if (thrdCount == 5) task5.Start();
                        if (thrdCount == 6) task6.Start();
                        if (thrdCount == 7) task7.Start();
                        if (thrdCount == 8) task8.Start();
                        if (thrdCount == 9) task9.Start();
                        if (thrdCount == 10) task10.Start();
                        if (thrdCount == 11) task11.Start();
                        if (thrdCount == 12) task12.Start();
                        if (thrdCount == 13) task13.Start();
                        if (thrdCount == 14) task14.Start();
                        if (thrdCount == 15) task15.Start();
                    }              
                }
                TableName = "futsbrfarch_" + comboBox1.Items[i].ToString();
                portfels[0] = takeResult(GetPrices(TableName));

                if (i == startDay) portfel = portfels[0];
                bool startColumn = true;

                for (int d = 0; d < threadCount; d++)
                    if (i + d < (int)numericUpDown6.Value)
                    {
                        if (d == 1) task1.Wait();
                        if (d == 2) task2.Wait();
                        if (d == 3) task3.Wait();
                        if (d == 4) task4.Wait();
                        if (d == 5) task5.Wait();
                        if (d == 6) task6.Wait();
                        if (d == 7) task7.Wait();
                        if (d == 8) task8.Wait();
                        if (d == 9) task9.Wait();
                        if (d == 10) task10.Wait();
                        if (d == 11) task11.Wait();
                        if (d == 12) task12.Wait();
                        if (d == 13) task13.Wait();
                        if (d == 14) task14.Wait();
                        if (d == 15) task15.Wait();
                        
                        int npp = 0;
                        for (int k = 0; k < portfel.paramPart; k++)
                        for (int j = 0; j < portfel.porogPart; j++)
                            {
                                totalDeposit[k, j] += portfels[d].deposit[0][k, j];
                                totalDeals[k, j] += (int)portfels[d].deposit[1][k, j];

                                npp++;
                                string dataD = "";
                                if (portfels[d].deposit[0][k, j] < 0) dataD += (portfels[d].deposit[0][k, j] - 10000 * portfels[d].deposit[1][k, j]);
                                else dataD += (portfels[d].deposit[0][k, j] + 10000 * portfels[d].deposit[1][k, j]);
                                try
                                {
                                    if (startColumn)
                                    {
                                        sqlCommand = "insert " + resultPortfl + " (npp,param,porog," + comboBox1.Items[i + d].ToString() + ") values (" + npp + "," + (11 + k) + "," + (300 + j) + "," + dataD + ");";
                                        cmnd = new MySqlCommand(sqlCommand, testConn);
                                        cmnd.ExecuteNonQuery();
                                    }
                                    else
                                    {
                                        sqlCommand = "update " + resultPortfl + " SET " + comboBox1.Items[i + d].ToString() + " = " + dataD + " where npp = " + npp + ";";
                                        cmnd = new MySqlCommand(sqlCommand, testConn);
                                        cmnd.ExecuteNonQuery();
                                    }
                                }
                                catch
                                {
                                    sqlCommand = "update " + resultPortfl + " SET " + comboBox1.Items[i + d].ToString() + " = " + dataD + " where npp = " + npp + ";";
                                    cmnd = new MySqlCommand(sqlCommand, testConn);
                                    cmnd.ExecuteNonQuery();
                                }
                            }
                        bestDeposit[i + d] = portfels[d].deposit[0][besti, bestj];
                        form.progressBar1_setvol(i + d - (int)numericUpDown5.Value + 1);
                        startColumn = false;
                    }                
            }

            testConn.Close();

            form.Cursor = Cursors.Default;
            form.Close();

            double[] maxDep = new double[10];
            string[] maxDepParam = new string[10];
            maxDep[0] = totalDeposit[0, 0];
            maxDep[1] = totalDeposit[0, 20];

            double minDep = 0;
            double loss = 0;
            int losscount = 0;
            double profit = 0;
            int profitcount = 0;
            double minParam = (double)numericUpDown10.Value;
            double maxParam = (double)numericUpDown9.Value;
            double minPorog = (double)numericUpDown2.Value;
            double maxPorog = (double)numericUpDown4.Value;
            int depCount = ((int)(maxParam - minParam) + 1) * (((int)(maxPorog - minPorog)) * 10 + 1);
            double[] depspektr = new double[depCount];
            
            //int depPeriod = (int)numericUpDown6.Value - (int)numericUpDown1.Value;
            int depPeriod = (int)numericUpDown6.Value - 1;
            double[][] deposites = new double[depCount][];
            for (int i = 0; i < depCount; i++)
            {
                deposites[i] = new double[depPeriod];
            }

            int maxDepCount = 0;
            int maxDepCount1 = 0;
            int maxDepCount2 = 0;

            for (int i = 0; i < (int)(maxParam - minParam) + 1; i++)
                for (int j = 0; j < ((int)(maxPorog - minPorog)) * 10 + 1; j++)
                {
                    depspektr[i * ((int)(maxPorog - minPorog)) * 10 + 1 + j] = totalDeposit[i, j];
                    if (totalDeposit[i, j] <= maxDep[0] && totalDeposit[i, j] > maxDep[1]) maxDepCount++;
                    if (totalDeposit[i, j] <= maxDep[1] && totalDeposit[i, j] > maxDep[2]) maxDepCount1++;
                    if (totalDeposit[i, j] <= maxDep[2] && totalDeposit[i, j] > maxDep[3]) maxDepCount2++;
                    if (totalDeposit[i, j] > maxDep[0])
                    {
                        maxDepCount2 = maxDepCount1;
                        maxDepCount1 = maxDepCount;
                        maxDepCount = 1;
                        for (int k = maxDep.Length - 1; k > 0; k--)
                        {
                            maxDep[k] = maxDep[k - 1];
                            maxDepParam[k] = maxDepParam[k - 1];

                        }
                        maxDep[0] = totalDeposit[i, j];
                        maxDepParam[0] = "param = " + (minParam + Convert.ToDouble(i)).ToString("0") + " porog = " + (minPorog + Convert.ToDouble(j) / 10).ToString("0.0") + " operation = " + totalDeals[i, j].ToString() + " | ";
                    }
                    

                    if (totalDeposit[i, j] < minDep) minDep = totalDeposit[i, j];

                    if (totalDeposit[i, j] < 0)
                    {
                        loss += totalDeposit[i, j];
                        losscount++;
                    }
                    if (totalDeposit[i, j] > 0)
                    {
                        profit += totalDeposit[i, j];
                        profitcount++;
                    }
                }
            string msg = "Max deposites = " + maxDep[0].ToString() + " count = " + maxDepCount + " next = " +  maxDep[1].ToString() + " count = " + maxDepCount1 + " next = " +  maxDep[2].ToString() + " count = " + maxDepCount2;
            double[] testDep = new double[bestDeposit.Length];
            testDep[0] = bestDeposit[0];
            string bestDeps = bestDeposit[0].ToString();
            for (int i = 1; i < bestDeposit.Length; i++)
            {
                testDep[i] = testDep[i - 1] + bestDeposit[i];
                bestDeps += " " + bestDeposit[i];
            }
            bestDeps += " total = " + testDep[bestDeposit.Length - 1];
            //for (int i = 1; i < 5; i++) msg += maxDep[i].ToString() + "  ";
            label6.Text = msg;
            label8.Text = maxDepParam[0] + maxDepParam[1];
            label9.Text = " minDep = " + minDep.ToString() + " loss = " + (loss / Convert.ToDouble(losscount)).ToString("0.00") + " loss count = " + losscount + " profit = " + (profit / Convert.ToDouble(profitcount)).ToString("0.00") + " profit count = " + profitcount;
            label12.Text = "Доходность = " + testDep[bestDeposit.Length - 1] + "   порог = " + bestPorog + "  параметр = " + bestParam + " i = " + besti + " j = " + bestj;
            Log.LogWrite(" porog = " + bestPorog + "  param = " + bestParam + "  delayTime = " + numericUpDown3.Value.ToString() + "  smooth = " + smth + " spread = " + spread + "  algoritm - " + comboBox2.SelectedItem.ToString(), true);
            Log.LogWrite(bestDeps, true);
            Log.LogWrite("___________________________________________________________________", true);

            ///NewSpektrToChart(Spektr(depspektr, 50), chart2);
            NewDataToChart(depspektr, 1, chart2);            
            NewDataToChart(testDep, 1, chart1);
            AddPointDataToChart(testDep, 1, chart1);
            AddDataToChart(bestDeposit, 1, chart1);
            AddPointDataToChart(bestDeposit, 1, chart1);

            label20.Text = "Количество ядер процессора = " + coreCount + "  Количество потоков = " + threadCount + "  Имя компьютера - " + Environment.MachineName + "   время расчета = " + DateTime.Now.Subtract(analiseTime).ToString();
            this.Cursor = Cursors.Default;

        }

        private void button3_Click(object sender, EventArgs e)
        {
            enableLog = false;
            DateTime analiseTime = DateTime.Now;
            int[] timeDelays = { 0, 10, 20, 50, 100, 200, 500, 800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1500, 1800, 2000, 2500, 3000, 4000, 5000, 7000, 10000, 20000, 1000000 };
            //int[] timeDelays = {  500, 800, 850, 900, 1000, 1100, 1200, 1500, 1800, 2000, 2500, 3000, 4000, 5000, 7000 };
            Form2 form = new Form2();
            form.progressBar1_setmax(((int)numericUpDown6.Value - (int)numericUpDown5.Value + 1) * 3 * timeDelays.Length);
            form.Show();
            form.progressBar1_setvol(1);
            this.Cursor = Cursors.WaitCursor;
            form.Cursor = Cursors.WaitCursor;
            int volForm = 0;
            double totalMaxDep = 0;
            string totalMaxDepParam = "";
            


            for (int tdCount = 0; tdCount < timeDelays.Length; tdCount++)
            {
                numericUpDown3.Value = timeDelays[tdCount];
                for (smth = 2; smth < 5; smth++)
                {
                    numericUpDown12.Value = smth;
                    double[,] totalDeposit = new double[(int)(numericUpDown9.Value) - (int)(numericUpDown10.Value) + 1, ((int)numericUpDown4.Value - (int)numericUpDown2.Value) * 10 + 1];
                    int[,] totalDeals = new int[(int)(numericUpDown9.Value) - (int)(numericUpDown10.Value) + 1, ((int)numericUpDown4.Value - (int)numericUpDown2.Value) * 10 + 1];

                    //Доходность лучшего портфеля, пока так
                    double[] bestDeposit = new double[comboBox1.Items.Count];

                    int besti = (int)(numericUpDown8.Value) - (int)(numericUpDown10.Value);
                    int bestj = (int)((((double)(numericUpDown7.Value) / 10) - (double)(numericUpDown2.Value)) * 10);


                    for (int i = (int)numericUpDown5.Value - 1; i < (int)numericUpDown6.Value; i += coreCount)
                    {

                        Portfel[] portfels = new Portfel[coreCount];


                        #region Таски на парралельные вычисления
                        Task task1 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 1].ToString();
                            portfels[1] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task2 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 2].ToString();
                            portfels[2] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task3 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 3].ToString();
                            portfels[3] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task4 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 4].ToString();
                            portfels[4] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task5 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 5].ToString();
                            portfels[5] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task6 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 6].ToString();
                            portfels[6] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task7 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 7].ToString();
                            portfels[7] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task8 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 8].ToString();
                            portfels[8] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task9 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 9].ToString();
                            portfels[9] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task10 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 10].ToString();
                            portfels[10] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task11 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 11].ToString();
                            portfels[11] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task12 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 12].ToString();
                            portfels[12] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task13 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 13].ToString();
                            portfels[13] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task14 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 14].ToString();
                            portfels[14] = takeResult(GetPricesInNewThread(tabName));
                        });
                        Task task15 = new Task(() =>
                        {
                            string tabName = "futsbrfarch_" + comboBox1.Items[i + 15].ToString();
                            portfels[15] = takeResult(GetPricesInNewThread(tabName));
                        });
                        #endregion

                        int threadCount = 0;
                        while (threadCount < coreCount - 1)
                        {

                            threadCount++;
                            if (i + threadCount < (int)numericUpDown6.Value)
                            {
                                if (threadCount == 1) task1.Start();
                                if (threadCount == 2) task2.Start();
                                if (threadCount == 3) task3.Start();
                                if (threadCount == 4) task4.Start();
                                if (threadCount == 5) task5.Start();
                                if (threadCount == 6) task6.Start();
                                if (threadCount == 7) task7.Start();
                                if (threadCount == 8) task8.Start();
                                if (threadCount == 9) task9.Start();
                                if (threadCount == 10) task10.Start();
                                if (threadCount == 11) task11.Start();
                                if (threadCount == 12) task12.Start();
                                if (threadCount == 13) task13.Start();
                                if (threadCount == 14) task14.Start();
                                if (threadCount == 15) task15.Start();
                            }
                        }
                        TableName = "futsbrfarch_" + comboBox1.Items[i].ToString();
                        portfels[0] = takeResult(GetPrices(TableName));

                        if (i == (int)numericUpDown5.Value - 1) portfel = portfels[0];

                        for (int d = 0; d < coreCount; d++)
                            if (i + d < (int)numericUpDown6.Value)
                            {
                                #region ждём таски
                                if (d == 1) task1.Wait();
                                if (d == 2) task2.Wait();
                                if (d == 3) task3.Wait();
                                if (d == 4) task4.Wait();
                                if (d == 5) task5.Wait();
                                if (d == 6) task6.Wait();
                                if (d == 7) task7.Wait();
                                if (d == 8) task8.Wait();
                                if (d == 9) task9.Wait();
                                if (d == 10) task10.Wait();
                                if (d == 11) task11.Wait();
                                if (d == 12) task12.Wait();
                                if (d == 13) task13.Wait();
                                if (d == 14) task14.Wait();
                                if (d == 15) task15.Wait();
                                # endregion
                                for (int k = 0; k < portfel.paramPart; k++)
                                    for (int j = 0; j < portfel.porogPart; j++)
                                    {
                                        totalDeposit[k, j] += portfels[d].deposit[0][k, j];
                                        totalDeals[k, j] += (int)portfels[d].deposit[1][k, j];
                                    }
                                bestDeposit[i + d] = portfels[d].deposit[0][besti, bestj];
                                volForm++;
                                form.progressBar1_setvol(volForm);
                            }
                    }


                    double[] maxDep = new double[10];
                    string[] maxDepParam = new string[10];
                    maxDep[0] = totalDeposit[0, 0];
                    maxDep[1] = totalDeposit[0, 20];

                    double minDep = 0;
                    double loss = 0;
                    int losscount = 0;
                    double profit = 0;
                    int profitcount = 0;
                    int depCount = portfel.paramPart * portfel.porogPart;
                    double[] depspektr = new double[depCount];

                    //int depPeriod = (int)numericUpDown6.Value - (int)numericUpDown1.Value;
                    int depPeriod = (int)numericUpDown6.Value - 1;
                    double[][] deposites = new double[depCount][];
                    for (int i = 0; i < depCount; i++)
                    {
                        deposites[i] = new double[depPeriod];
                    }

                    int maxDepCount = 0;
                    int maxDepCount1 = 0;
                    int maxDepCount2 = 0;

                    for (int i = 0; i < portfel.paramPart; i++)
                        for (int j = 0; j < portfel.porogPart; j++)
                        {
                            depspektr[i * portfel.porogPart + j] = totalDeposit[i, j];
                            if (totalDeposit[i, j] <= maxDep[0] && totalDeposit[i, j] > maxDep[1]) maxDepCount++;
                            if (totalDeposit[i, j] <= maxDep[1] && totalDeposit[i, j] > maxDep[2]) maxDepCount1++;
                            if (totalDeposit[i, j] <= maxDep[2] && totalDeposit[i, j] > maxDep[3]) maxDepCount2++;
                            if (totalDeposit[i, j] > maxDep[0])
                            {
                                maxDepCount2 = maxDepCount1;
                                maxDepCount1 = maxDepCount;
                                maxDepCount = 1;
                                for (int k = maxDep.Length - 1; k > 0; k--)
                                {
                                    maxDep[k] = maxDep[k - 1];
                                    maxDepParam[k] = maxDepParam[k - 1];
                                }
                                maxDep[0] = totalDeposit[i, j];
                                //maxDepParam[0] = "param = " + (portfel.minParam + Convert.ToDouble(i) * (portfel.maxParam - portfel.minParam) / (portfel.paramPart - 1)).ToString("0.00") + " porog = " + (portfel.minPorog + j * (portfel.maxPorog - portfel.minPorog) / (portfel.porogPart - 1)).ToString("0.00") + " operation = " + totalDeals[i, j].ToString() + " | ";
                                maxDepParam[0] = "param = " + (portfel.minParam + Convert.ToDouble(i) * (portfel.maxParam - portfel.minParam) / Convert.ToDouble(portfel.paramPart - 1)).ToString("0.00") + " porog = " + (portfel.minPorog + Convert.ToDouble(j) * (portfel.maxPorog - portfel.minPorog) / Convert.ToDouble(portfel.porogPart - 1)).ToString("0.00") + " operation = " + totalDeals[i, j].ToString() + " | ";
                            }


                            if (totalDeposit[i, j] < minDep) minDep = totalDeposit[i, j];

                            if (totalDeposit[i, j] > (double)(numericUpDown6.Value - numericUpDown5.Value + 1) * 175 && totalDeposit[i, j] < (double)(numericUpDown6.Value - numericUpDown5.Value + 1) * 190)
                            {
                                Log.LogWrite("param = " + (portfel.minParam + Convert.ToDouble(i) * (portfel.maxParam - portfel.minParam) / (portfel.paramPart - 1)).ToString("0.00") + " porog = " + (portfel.minPorog + j * (portfel.maxPorog - portfel.minPorog) / (portfel.porogPart - 1)).ToString("0.00") + " time delay = " + timeDelays[tdCount] + " smooth = " + smth, true);                                
                            }

                            if (totalDeposit[i, j] < 0)
                            {
                                loss += totalDeposit[i, j];
                                losscount++;
                            }
                            if (totalDeposit[i, j] > 0)
                            {
                                profit += totalDeposit[i, j];
                                profitcount++;
                            }
                        }
                    string msg = "Max deposites = " + maxDep[0].ToString() + " count = " + maxDepCount + " next = " +  maxDep[1].ToString() + " count = " + maxDepCount1 + " next = " +  maxDep[2].ToString() + " count = " + maxDepCount2;
                    double testDep = 0;
                    for (int i = 0; i < bestDeposit.Length; i++) testDep += bestDeposit[i];
                    //for (int i = 1; i < 5; i++) msg += maxDep[i].ToString() + "  ";
                    if (totalMaxDep < maxDep[0])
                    {
                        totalMaxDep = maxDep[0];
                        totalMaxDepParam = maxDepParam[0] + " time delay = " + timeDelays[tdCount] + " smooth = " + smth;
                        label5.Text = "TotalMaxDep = " + totalMaxDep +  "  " + totalMaxDepParam;
                        
                    }
                    
                    label6.Text = msg;
                    label8.Text = maxDepParam[0] + maxDepParam[1];
                    label9.Text = " minDep = " + minDep.ToString() + " loss = " + (loss / Convert.ToDouble(losscount)).ToString("0.00") + " loss count = " + losscount + " profit = " + (profit / Convert.ToDouble(profitcount)).ToString("0.00") + " profit count = " + profitcount;
                    label12.Text = "Доходность = " + testDep + "   порог = " + bestPorog + "  параметр = " + bestParam + "    i = " + besti + " j = " + bestj;

                }
            }

            this.Cursor = Cursors.Default;
            form.Cursor = Cursors.Default;
            form.Close();
            //label5.Text = "Максимальный депозит по тоталу = " + totalMaxDep;
            //label8.Text = totalMaxDepParam;
            label20.Text = "Количество ядер процессора = " + coreCount + "  Количество потоков = " + threadCount + "  Имя компьютера - " + Environment.MachineName + "   время расчета = " + DateTime.Now.Subtract(analiseTime).ToString();
            smth = 3;
            numericUpDown12.Value = smth;
        }

        #region  Функции управления чартами

        private void NewDataToChart(double[] data, int count, Chart chrt) //Заливка данных в чарт
        {
           DataToChart(data, 0, data.Length, count, chrt);
        }

        private void AddPointDataToChart(double[] data, int count, Chart chrt) //Доливка данных в чарт
        {           
            double minX = 0;
            double maxX = Convert.ToDouble(data.Length);

            int num = chrt.Series.Count;
            
            chrt.Series.Add("Series" + num.ToString());
            chrt.Series[num].ChartType = (SeriesChartType)0;
            chrt.Series[num].Color = Color.Red;
            chrt.Series[num].BorderWidth = 1;

            for (int i = 0; i < data.Length; i += count)
            {
                chrt.Series[num].Points.AddXY(minX + (maxX - minX) * Convert.ToDouble(i) / (Convert.ToDouble(data.Length) - 1), data[i]);
            }
        }

        private void AddDataToChart(double[] data, int count, Chart chrt) //Доливка данных в чарт
        {
            double minX = chrt.ChartAreas[0].AxisX.Minimum;
            double maxX = chrt.ChartAreas[0].AxisX.Maximum;

            int num = chrt.Series.Count;
            Color[] color = new Color[10] { Color.Red, Color.Blue, Color.DarkOrange, Color.DarkMagenta, Color.Gold, Color.Fuchsia, Color.SeaGreen, Color.Brown, Color.Chocolate, Color.Cyan };

            chrt.Series.Add("Series" + num.ToString());
            chrt.Series[num].ChartType = (SeriesChartType)4;
            chrt.Series[num].Color = color[num - 1];
            chrt.Series[num].BorderWidth = 1;

            for (int i = 0; i < data.Length; i += count)
            {
                chrt.Series[num].Points.AddXY(minX + (maxX - minX) * Convert.ToDouble(i) / (maxX - minX - 1), data[i]);
            }
        }

        private void DataToChart(double[] data, double minX, double maxX, int count, Chart chrt) //Заливка данных в чарт
        {
            double maxdata = 0;
            double mindata = 0;
            for (int i = 1; i < data.Length; i++)
            {                
                if (maxdata < data[i]) maxdata = data[i];
                if (mindata > data[i]) mindata = data[i];
            }

            double mult = 0.01;
            while (mult < Math.Abs(maxdata) | mult < Math.Abs(mindata)) mult *= 10;
            mult /= 10;
            maxdata = (double)((int)(maxdata / mult) + 1) * mult;
            mindata = (double)((int)(mindata / mult) - 1) * mult;
            maxdata = 8000;
            mindata = -2000;
            
            //Чистим чарт и задаем серию
            chrt.Series.Clear();
            chrt.Series.Add("Series0");
            chrt.Series[0].ChartType = (SeriesChartType)4;
            chrt.Series[0].Color = Color.Green;
            chrt.Series[0].BorderWidth = 1;

            chrt.ChartAreas[0].AxisY.Minimum = mindata;
            chrt.ChartAreas[0].AxisY.Maximum = maxdata;
            chrt.ChartAreas[0].AxisX.Minimum = minX;
            chrt.ChartAreas[0].AxisX.Maximum = maxX;
            chrt.ChartAreas[0].AxisX.LabelStyle.Format = "0";
            chrt.ChartAreas[0].AxisY.LabelStyle.Format = "0.00";
            chrt.ChartAreas[0].AxisY.MajorGrid.Interval = (maxdata - mindata) / 10;
            chrt.ChartAreas[0].AxisX.MajorGrid.Interval = (maxX - minX) / 10;
            chrt.ChartAreas[0].AxisY.LabelStyle.Interval = (maxdata - mindata) / 10;
            chrt.ChartAreas[0].AxisX.LabelStyle.Interval = (maxX - minX) / 5;

            for (int i = 0; i < data.Length; i += count)
            {
                chrt.Series[0].Points.AddXY(minX + (maxX - minX) * Convert.ToDouble(i) / (maxX - minX - 1), data[i]);
            }
        }

        private void NewSpektrToChart(double[,] data, Chart chrt) //Заливка данных в чарт
        {
            double[] res = new double[data.GetLength(1)];
            for (int i = 0; i < res.Length; i++) res[i] = data[1, i];
            SpektrToChart(res, data[0, 0], data[0, res.Length - 1], 1, chrt);
        }

        private void SpektrToChart(double[] data, double minX, double maxX, int count, Chart chrt) //Заливка данных в чарт
        {
            double maxdata = 0;
            double mindata = 0;
            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] > maxdata) maxdata = data[i];
                if (data[i] < mindata) mindata = data[i];               
            }
            maxdata = maxdata * 1.1;
            mindata = mindata * 0.9;
            
            //Чистим чарт и задаем серию
            chrt.Series.Clear();
            chrt.Series.Add("Series0");
            chrt.Series[0].ChartType = (SeriesChartType)4;
            chrt.Series[0].Color = Color.Red;
            chrt.Series[0].BorderWidth = 1;

            //chrt.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(mindata);
            //chrt.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(maxdata);
            chrt.ChartAreas[0].AxisY.Maximum = 100;
            chrt.ChartAreas[0].AxisY.Minimum = 0;
            //chrt.ChartAreas[0].AxisY.Maximum = 1000;
            //chrt.ChartAreas[0].AxisX.Minimum = minX;
            //chrt.ChartAreas[0].AxisX.Maximum = maxX;
            chrt.ChartAreas[0].AxisX.Minimum = -1000;
            chrt.ChartAreas[0].AxisX.Maximum = 1000;
            chrt.ChartAreas[0].AxisX.LabelStyle.Interval = 100;
            chrt.ChartAreas[0].AxisY.LabelStyle.Format = "0.0";
            chrt.ChartAreas[0].AxisY.LabelStyle.Interval = 10;
            chrt.ChartAreas[0].AxisY.MajorGrid.Interval = 10;
            chrt.ChartAreas[0].AxisX.MajorGrid.Interval = 100;
            

            for (int i = 0; i < data.Length; i += count)
            {
                chrt.Series[0].Points.AddXY(minX + (maxX - minX) * Convert.ToDouble(i) / Convert.ToDouble(data.Length), data[i]);
            }
        }

        private void NewTrendToChart(int[] data, int period, Chart chrt) //Заливка нового тренда в чарт
        {
            //Чистим чарт и задаем серию
            chrt.Series.Clear();
            chrt.Series.Add("Series0");
            chrt.Series[0].ChartType = (SeriesChartType)4;
            chrt.Series[0].Color = Color.Green;
            chrt.Series[0].BorderWidth = 1;

            chrt.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(pricemin);
            chrt.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(pricemax);
            chrt.ChartAreas[0].AxisX.Minimum = 0;
            chrt.ChartAreas[0].AxisX.Maximum = Convert.ToDouble(data.Length);
            chrt.ChartAreas[0].AxisX.LabelStyle.Format = "0.0";
            chrt.ChartAreas[0].AxisX.MajorGrid.Interval = Math.Round((chrt.ChartAreas[0].AxisX.Maximum - chrt.ChartAreas[0].AxisX.Minimum) / 10);

            TrendToChart(data, chrt, 0, period);
        }

        private void AddTrendToChart(int[] data, int period, Chart chrt) //Добавление нового тренда в чарт
        {
            int num = chrt.Series.Count;
            Color[] color = new Color[10] { Color.Red, Color.Blue, Color.DarkOrange, Color.DarkMagenta, Color.Gold, Color.Fuchsia, Color.SeaGreen, Color.Brown, Color.Chocolate, Color.Cyan };
            if (num == 0)
            {
                NewTrendToChart(data, period, chrt);
                return;
            }
            if (num == 10)
            {
                MessageBox.Show("Перебор трендов!");
                NewTrendToChart(data, period, chrt);
            }
            else
            {
                chrt.Series.Add("Series" + num.ToString());
                chrt.Series[num].ChartType = (SeriesChartType)4;
                chrt.Series[num].Color = color[num - 1];
                chrt.Series[num].BorderWidth = 1;
                TrendToChart(data, chrt, num, period);
            }
            return;
        }

        private void TrendToChart(int[] data, Chart chrt, int num, int period) //Заливка тренда в чарт
        {
            //int prd = (int)(npp / data.Length);
            
            for (int i = 0; i < data.Length; i += period)
            {
                chrt.Series[num].Points.AddXY(Convert.ToDouble(i), data[i]);
            }
            //MessageBox.Show("period = " + period + " npp = " + data.Length + " count = " + (int)(data.Length / period) + " series = " + chrt.Series[num].Points.Count);
        }



        private void ScaleChart(Chart chrt, MouseEventArgs e) //Изменение масштаба от прокрутки колеса мыши
        {
            double delta = (chrt.ChartAreas[0].AxisX.Maximum - chrt.ChartAreas[0].AxisX.Minimum) / 20;
            double deltamin = -delta * Convert.ToDouble(chrt.PointToClient(MousePosition).X) / Convert.ToDouble(chrt.Left - chrt.Right);
            double deltamax = -delta * Convert.ToDouble((chrt.Right - chrt.PointToClient(MousePosition).X + Convert.ToDouble(chrt.Left)) / Convert.ToDouble(chrt.Left - chrt.Right));
            if (e.Delta > 0)
            {
                chrt.ChartAreas[0].AxisX.Minimum += deltamin;
                chrt.ChartAreas[0].AxisX.Maximum -= deltamax;
                chrt.ChartAreas[0].AxisX.MajorGrid.Interval = Math.Round((chrt.ChartAreas[0].AxisX.Maximum - chrt.ChartAreas[0].AxisX.Minimum) / 10);

            }
            else
            {
                if (chrt.ChartAreas[0].AxisX.Minimum - delta < 0) chrt.ChartAreas[0].AxisX.Minimum = 0;
                else chrt.ChartAreas[0].AxisX.Minimum -= delta;
                if (chrt.ChartAreas[0].AxisX.Maximum + delta > npp) chrt.ChartAreas[0].AxisX.Maximum = npp;
                else chrt.ChartAreas[0].AxisX.Maximum += delta;
                chrt.ChartAreas[0].AxisX.MajorGrid.Interval = Math.Round((chrt.ChartAreas[0].AxisX.Maximum - chrt.ChartAreas[0].AxisX.Minimum) / 10);
            }
        }
                       
        #endregion

        #region  Расчетные функции

        private int[][] GetPrices(string tabName) //Загрузка данных торгов
        {
            //Запрашиваем количество записей сделок
            query = "select npp,price,cdt from " + tabName + " order by npp desc limit 1;";
            if (conn.State.ToString().Equals("Closed"))
            {
                conn.Open();
            }
            comm = new MySqlCommand(query, conn);
            reader = comm.ExecuteReader();
            int newnpp = 0;
            while (reader.Read())
            {
                newnpp = (int)reader[0];
                pricemin = (int)reader[1];
                pricemax = pricemin;
            }
            reader.Close();

            //Массив цен сделок
            int[][] prc = new int[5][];
            prc[0] = new int[newnpp];
            prc[1] = new int[newnpp];
            prc[2] = new int[newnpp];
            prc[3] = new int[newnpp];
            prc[4] = new int[newnpp];

            //Считываем массив данных из MySQL
            query = "select price,kolvo,timesd,pricespr,priceprdl from " + tabName + ";";
            comm = new MySqlCommand(query, conn);

            int nn = 0;
            reader = comm.ExecuteReader();
            while (reader.Read())
            {
                string prs = reader[0].ToString();
                string kolvo = reader[1].ToString();
                //TimeSpan cdt1 = Convert.ToDateTime(reader[2].ToString()).TimeOfDay;
                TimeSpan cdt1 = TimeSpan.Parse(reader[2].ToString());

                if (prs == "")
                {
                    //Фильтруем строки с пустыми значениями цены
                    prc[0][nn] = prc[0][nn - 1];
                    prc[1][nn] = 0;
                    prc[2][nn] = prc[2][nn - 1];
                }
                else if (prs == "0")
                {
                    //Фильтруем строки с нулевыми значениями цены
                    prc[0][nn] = prc[0][nn - 1];
                    prc[1][nn] = 0;
                    prc[2][nn] = prc[2][nn - 1];
                }
                else
                {
                    //Считываем корректные данные
                    prc[0][nn] = Convert.ToInt32(prs);
                    prc[1][nn] = Convert.ToInt32(kolvo);
                    prc[2][nn] = (int)cdt1.TotalMilliseconds;
                    prc[3][nn] = Convert.ToInt32(reader[3].ToString());
                    prc[4][nn] = Convert.ToInt32(reader[4].ToString());
                }
                //Определяем минимальную и максимальную цены
                if (pricemin > prc[0][nn])
                {
                    if (prc[0][nn] != 0)
                    {
                        pricemin = (int)prc[0][nn];
                    }
                }
                if (pricemax < prc[0][nn])
                {
                    pricemax = (int)prc[0][nn];
                }
                nn++;
            }
            
            //Округляем минимальные и максимальные значения цен до 1000
            pricemin = 100 * (((int)(pricemin / 100)) - 1);
            pricemax = 100 * (((int)(pricemax / 100)) + 1);

            reader.Close();
            conn.Close();
            return prc;
        }

        private int[][] GetPricesInNewThread(string tabName) //Загрузка данных торгов
        {
            MySqlConnection connection = DBUtils.GetDBConnection();
            
            //Запрашиваем количество записей сделок
            string newquery = "select npp,price,cdt from " + tabName + " order by npp desc limit 1;";
            if (connection.State.ToString().Equals("Closed"))
            {
                connection.Open();
            }
            MySqlCommand command = new MySqlCommand(newquery, connection);
            MySqlDataReader newreader = command.ExecuteReader();
            int newnpp = 0;
            while (newreader.Read())
            {
                newnpp = (int)newreader[0];
                pricemin = (int)newreader[1];
                pricemax = pricemin;
            }
            newreader.Close();

            //Массив цен сделок
            int[][] prc = new int[5][];
            prc[0] = new int[newnpp];
            prc[1] = new int[newnpp];
            prc[2] = new int[newnpp];
            prc[3] = new int[newnpp];
            prc[4] = new int[newnpp];

            //Считываем массив данных из MySQL
            newquery = "select price,kolvo,timesd,pricespr,priceprdl from " + tabName + ";";
            command = new MySqlCommand(newquery, connection);

            int nn = 0;
            newreader = command.ExecuteReader();
            while (newreader.Read())
            {
                string prs = newreader[0].ToString();
                string kolvo = newreader[1].ToString();
                //TimeSpan cdt1 = Convert.ToDateTime(newreader[2]).TimeOfDay;
                TimeSpan cdt1 = TimeSpan.Parse(newreader[2].ToString());

                if (prs == "")
                {
                    //Фильтруем строки с пустыми значениями цены
                    prc[0][nn] = prc[0][nn - 1];
                    prc[1][nn] = 0;
                    prc[2][nn] = prc[2][nn - 1];
                }
                else if (prs == "0")
                {
                    //Фильтруем строки с нулевыми значениями цены
                    prc[0][nn] = prc[0][nn - 1];
                    prc[1][nn] = 0;
                    prc[2][nn] = prc[2][nn - 1];
                }
                else
                {
                    //Считываем корректные данные
                    prc[0][nn] = Convert.ToInt32(prs);
                    prc[1][nn] = Convert.ToInt32(kolvo);
                    prc[2][nn] = (int)cdt1.TotalMilliseconds;
                    prc[3][nn] = Convert.ToInt32(newreader[3].ToString());
                    prc[4][nn] = Convert.ToInt32(newreader[4].ToString());
                }
                //Определяем минимальную и максимальную цены
                if (pricemin > prc[0][nn])
                {
                    if (prc[0][nn] != 0)
                    {
                        pricemin = (int)prc[0][nn];
                    }
                }
                if (pricemax < prc[0][nn])
                {
                    pricemax = (int)prc[0][nn];
                }
                nn++;
            }

            //Округляем минимальные и максимальные значения цен до 1000
            pricemin = 100 * (((int)(pricemin / 100)) - 1);
            pricemax = 100 * (((int)(pricemax / 100)) + 1);

            newreader.Close();
            connection.Close();
            return prc;
        }

        private double[] Smooth(double[] data, int count) //Сглаживание массива int
        {
            double[] result = new double[data.Length];
            for (int i = 0; i < data.Length - count; i++)
            {
                result[i] = 0;
                for (int j = 0; j < count; j++)
                {
                    result[i] += data[i + j];
                }
                result[i] = result[i] / Convert.ToDouble(count);
            }
            for (int i = 0; i < count; i++)
            {
                result[data.Length - count + i] = data[data.Length - count + i];
                for (int j = 1; j < count - i; j++)
                {
                    result[data.Length - count + i] += data[data.Length - count + i + j];
                }
                result[data.Length - count + i] = result[data.Length - count + i] / Convert.ToDouble(count - i);
            }

            return result;
        }

        private double[] Indicate(int[][] data, double prm) //Рассчет индикаторов
        {
            double[] result = new double[data[0].Length];
            //double ves = (int)numericUpDown1.Value;
            double ves = 14;

            result[0] = 0;
            prm = prm / 10;
            for (int i = 0; i < result.Length - 1; i++)
            {
                result[i + 1] = result[i] / prm + ves * Convert.ToDouble(data[0][i] - data[0][i + 1]) / Convert.ToDouble(data[4][i] + data[4][i + 1] - data[3][i] - data[3][i + 1]);
            }

            return result;
        }

        private double[] IndicateTime(int[][] data, double prm) //Рассчет индикаторов
        {
            double[] result = new double[data[0].Length];
            //double ves = (int)numericUpDown1.Value;
            double ves = 14;

            int delayTime = 1000;
            result[0] = 0;
            prm = prm / 10;

            for (int i = 0; i < data[0].Length - 1; i++)
            {
                int currentTimeSpread = data[2][i + 1] - data[2][i];
                if (currentTimeSpread > delayTime)
                {
                    int curtime = delayTime;
                    while (currentTimeSpread > curtime)
                    {
                        //if (Math.Abs(result[i]) > portfel.maxPorog * 0.4) result[i] = result[i] * 0.7;
                        curtime += delayTime;
                    }
                }
                double razn = Convert.ToDouble(data[4][i] + data[4][i + 1] - data[3][i] - data[3][i + 1]);
                if (razn == 0) razn = 1;
                result[i + 1] = result[i] / prm + ves * Convert.ToDouble(data[0][i] - data[0][i + 1]) / razn;
                double curTSpread = (double)currentTimeSpread / 1000;
                if (curTSpread < 1) curTSpread = 1;
                result[i + 1] = result[i + 1] / curTSpread;
            }

            return result;
        }

        private double[,] Spektr(double[] data, int prm) //Рассчет индикаторов
        {
            double[,] result = new double[2, prm];
            double maxdata = 0;
            double mindata = 0;
            for (int i = 1; i < data.Length; i++)
            {
                maxdata += Math.Abs(data[i]);
            }
            maxdata = maxdata * 8 / data.Length;
            mindata = -maxdata;

            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < prm; j++)
                {
                    if (data[i] >= mindata + (maxdata - mindata) * Convert.ToDouble(j) / Convert.ToDouble(prm - 1) & data[i] < mindata + (maxdata - mindata) * Convert.ToDouble(j + 1) / Convert.ToDouble(prm - 1)) result[1, j] += 1;
                }
            }

            //Log.LogWrite("Table = " + TableName + " count = " + ((int)numericUpDown4.Value - (int)numericUpDown2.Value) * 10 + " param = " + param + " min = " + mindata.ToString() + " max = " + maxdata.ToString(), enableLog);
            for (int i = 0; i < prm; i++)
            {
                result[0, i] = mindata + (maxdata - mindata) * Convert.ToDouble(i) / Convert.ToDouble(prm - 1);
                //Log.LogWrite(" range = " + result[0, i].ToString() + " data = " + result[1, i]);
            }
            
            return result;
        }

        #endregion

        #region Аналитические процедуры

        private Portfel takeResult(int[][] data) //Процедура торгов
        {
            Portfel result = new Portfel();
            if (metod == 0) result = takeResultTandS(data);           
            else if (metod == 1) result = takeResultInvTandS(data);
            else if (metod == 2) result = takeResultTSProfit(data);
            else if (metod == 3) result = takeResultTandS2(data);
            else if (metod == 4) result = takeResultTandSlimT(data);
            else if (metod == 5) result = takeResultTandSlimTInv(data);
            else if (metod == 6) result = takeResultTandSnoMarket(data);

            return result;
        }
       
        private Portfel takeResultTandS(int[][] data) //Процедура торгов по тикам и отсечке по времени
        {
            double minParam = (double)numericUpDown10.Value;
            double maxParam = (double)numericUpDown9.Value;
            double minPorog = (double)numericUpDown2.Value;
            double maxPorog = (double)numericUpDown4.Value;
            Portfel result = Portfel.Inicialize((int)(maxParam - minParam) + 1, ((int)(maxPorog - minPorog)) * 10 + 1);
            result.minParam = minParam;
            result.maxParam = maxParam;
            result.minPorog = minPorog;
            result.maxPorog = maxPorog;

            //Form2 form = new Form2();
            //form.progressBar1_setmax(result.porogPart * result.paramPart);
            //form.Show();
            int pbvol = 0;
            int newopercount = 0;
            bool buy = false;
            bool sell = false;
            bool none = true;

            int besti = (int)(numericUpDown8.Value) - (int)(numericUpDown10.Value);
            int bestj = (int)((bestPorog - result.minPorog) * 10);
            string testOperation = "Операции проведены: ";
            int testOperCount = 0;
            int operTime = data[2][0];

            for (int i = 0; i < result.paramPart; i++)
            {
                double param = result.minParam + Convert.ToDouble(i) * (result.maxParam - result.minParam) / (result.paramPart - 1);
                double[] indicate = Smooth(IndicateTime(data, param), smth);

                for (int j = 0; j < result.porogPart; j++)
                {
                    double porog = result.minPorog + j * (result.maxPorog - result.minPorog) / (result.porogPart - 1);
                    for (int k = 20; k < indicate.Length; k++)
                    {
                        int cnt = k + smth - 1;
                        if (cnt > indicate.Length - 1) cnt = indicate.Length - 1;
                        if (none)
                        {
                            if (indicate[k] < -porog)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                result.deposit[1][i, j] += 1;
                                //result.deposit[0][i, j] -= spread;
                                none = false;
                                buy = true;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " sell at " + (cnt) + "  price = " + data[3][cnt] + " | ";
                                    Log.LogWrite("open sell at " + (cnt) + "  price sell = " + data[3][cnt] + " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] - Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                    //label13.Text += indicate[k].ToString("0.0") + " ";
                                }
                                operTime = data[2][cnt];
                            }
                            if (indicate[k] > porog)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                result.deposit[1][i, j] += 1;
                                //result.deposit[0][i, j] -= spread;
                                none = false;
                                sell = true;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " buy at " + (cnt) + "  price = " + data[4][cnt] + " | ";
                                    Log.LogWrite("open buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] + Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (buy)
                        {
                            if (indicate[k] > porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                none = true;
                                buy = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (sell)
                        {
                            if (indicate[k] < -porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                none = true;
                                sell = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close sell at " + (cnt) + "  price sell = " + data[3][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                    }
                    if (buy)
                    {
                        double currPrs = Convert.ToDouble(data[4][indicate.Length - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][indicate.Length - 1]);
                        result.deposit[0][i, j] -= currPrs;
                        buy = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price buy = " + data[4][indicate.Length - 1] +  "  price = " + data[0][indicate.Length - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }
                    if (sell)
                    {
                        double currPrs = Convert.ToDouble(data[3][indicate.Length - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][indicate.Length - 1]);
                        result.deposit[0][i, j] += currPrs;
                        sell = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price sell = " + data[3][indicate.Length - 1] +   "  price = " + data[0][indicate.Length - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }


                    pbvol++;
                    //form.progressBar1_setvol(pbvol);
                }
            }
            //form.Close();
            //label5.Text = "Количество совершенных операций = " + newopercount;

            return result;
        }

        private Portfel takeResultInvTandS(int[][] data) //Процедура торгов по тикам и отсечке по времени
        {
            double minParam = (double)numericUpDown10.Value;
            double maxParam = (double)numericUpDown9.Value;
            double minPorog = (double)numericUpDown2.Value;
            double maxPorog = (double)numericUpDown4.Value;
            Portfel result = Portfel.Inicialize((int)(maxParam - minParam) + 1, ((int)(maxPorog - minPorog)) * 10 + 1);
            result.minParam = minParam;
            result.maxParam = maxParam;
            result.minPorog = minPorog;
            result.maxPorog = maxPorog;

            //Form2 form = new Form2();
            //form.progressBar1_setmax(result.porogPart * result.paramPart);
            //form.Show();
            int pbvol = 0;
            int newopercount = 0;
            bool buy = false;
            bool sell = false;
            bool none = true;

            int besti = (int)(numericUpDown8.Value) - (int)(numericUpDown10.Value);
            int bestj = (int)((bestPorog - result.minPorog) * 10);
            string testOperation = "Операции проведены: ";
            int testOperCount = 0;
            int operTime = data[2][0];

            for (int i = 0; i < result.paramPart; i++)
            {
                double param = result.minParam + Convert.ToDouble(i) * (result.maxParam - result.minParam) / (result.paramPart - 1);
                double[] indicate = Smooth(IndicateTime(data, param), smth);

                for (int j = 0; j < result.porogPart; j++)
                {
                    double porog = result.minPorog + j * (result.maxPorog - result.minPorog) / (result.porogPart - 1);
                    for (int k = 20; k < indicate.Length; k++)
                    {
                        int cnt = k + smth - 1;
                        if (cnt > indicate.Length - 1) cnt = indicate.Length - 1;
                        if (none)
                        {
                            if (indicate[k] > porog)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                result.deposit[1][i, j] += 1;
                                //result.deposit[0][i, j] -= spread;
                                none = false;
                                buy = true;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " sell at " + (cnt) + "  price = " + data[3][cnt] + " | ";
                                    Log.LogWrite("open sell at " + (cnt) + "  price sell = " + data[3][cnt] + " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] - Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                    //label13.Text += indicate[k].ToString("0.0") + " ";
                                }
                                operTime = data[2][cnt];
                            }
                            if (indicate[k] < -porog)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                result.deposit[1][i, j] += 1;
                                //result.deposit[0][i, j] -= spread;
                                none = false;
                                sell = true;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " buy at " + (cnt) + "  price = " + data[4][cnt] + " | ";
                                    Log.LogWrite("open buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] + Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (buy)
                        {
                            if (indicate[k] < -porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                none = true;
                                buy = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (sell)
                        {
                            if (indicate[k] > porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                none = true;
                                sell = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close sell at " + (cnt) + "  price sell = " + data[3][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                    }
                    if (buy)
                    {
                        double currPrs = Convert.ToDouble(data[4][indicate.Length - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][indicate.Length - 1]);
                        result.deposit[0][i, j] -= currPrs;
                        buy = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price buy = " + data[4][indicate.Length - 1] +  "  price = " + data[0][indicate.Length - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }
                    if (sell)
                    {
                        double currPrs = Convert.ToDouble(data[3][indicate.Length - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][indicate.Length - 1]);
                        result.deposit[0][i, j] += currPrs;
                        sell = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price sell = " + data[3][indicate.Length - 1] +   "  price = " + data[0][indicate.Length - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }


                    pbvol++;
                    //form.progressBar1_setvol(pbvol);
                }
            }
            //form.Close();
            //label5.Text = "Количество совершенных операций = " + newopercount;

            return result;
        }

        private Portfel takeResultTSProfit(int[][] data) //Процедура торгов по тикам, отсечке по времени и фиксации профита сделки
        {
            double minParam = (double)numericUpDown10.Value;
            double maxParam = (double)numericUpDown9.Value;
            double minPorog = (double)numericUpDown2.Value;
            double maxPorog = (double)numericUpDown4.Value;
            Portfel result = Portfel.Inicialize((int)(maxParam - minParam) + 1, ((int)(maxPorog - minPorog)) * 10 + 1);
            result.minParam = minParam;
            result.maxParam = maxParam;
            result.minPorog = minPorog;
            result.maxPorog = maxPorog;

            //Form2 form = new Form2();
            //form.progressBar1_setmax(result.porogPart * result.paramPart);
            //form.Show();
            int pbvol = 0;
            int newopercount = 0;
            bool buy = false;
            bool sell = false;
            bool none = true;

            int besti = (int)(numericUpDown8.Value) - (int)(numericUpDown10.Value);
            int bestj = (int)((bestPorog - result.minPorog) * 10);
            string testOperation = "Операции проведены: ";
            int testOperCount = 0;
            int operTime = data[2][0];
            double operPrice = 0;

            for (int i = 0; i < result.paramPart; i++)
            {
                double param = result.minParam + Convert.ToDouble(i) * (result.maxParam - result.minParam) / (result.paramPart - 1);
                double[] indicate = Smooth(Indicate(data, param), smth);

                for (int j = 0; j < result.porogPart; j++)
                {
                    double porog = result.minPorog + j * (result.maxPorog - result.minPorog) / (result.porogPart - 1);
                    for (int k = 0; k < indicate.Length; k++)
                    {
                        int cnt = k + smth - 1;
                        if (cnt > indicate.Length - 1) cnt = indicate.Length - 1;
                        if (none)
                        {
                            if (indicate[k] < -porog)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                result.deposit[1][i, j] += 1;
                                result.deposit[0][i, j] -= spread;
                                none = false;
                                buy = true;
                                newopercount++;
                                operPrice = currPrs;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " sell at " + (k - 1) + "  price = " + data[3][cnt] + " | ";
                                    Log.LogWrite("open sell at " + (k - 1) + "  price sell = " + data[3][cnt] + " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] - Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.0") + " | ", enableLog);
                                    testOperCount++;
                                    //label13.Text += indicate[k].ToString("0.0") + " ";
                                }
                                operTime = data[2][cnt];
                            }
                            if (indicate[k] > porog)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                result.deposit[1][i, j] += 1;
                                result.deposit[0][i, j] -= spread;
                                none = false;
                                sell = true;
                                newopercount++;
                                operPrice = currPrs;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " buy at " + (k - 1) + "  price = " + data[4][cnt] + " | ";
                                    Log.LogWrite("open buy at " + (k - 1) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] + Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.0") + " | ", enableLog);
                                    testOperCount++;
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (buy)
                        {
                            double currPrs = Convert.ToDouble(data[4][cnt]);
                            if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);

                            if (currPrs - operPrice > profitPrice | indicate[k] > porog * multPorog && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                
                                result.deposit[0][i, j] -= currPrs;
                                none = true;
                                buy = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close buy at " + (k - 1) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.0") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (sell)
                        {
                            double currPrs = Convert.ToDouble(data[3][cnt]);
                            if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);

                            if (operPrice - currPrs > profitPrice | indicate[k] < -porog * multPorog && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                
                                result.deposit[0][i, j] += currPrs;
                                none = true;
                                sell = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close sell at " + (k - 1) + "  price sell = " + data[3][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.0") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                    }
                    if (buy)
                    {
                        double currPrs = Convert.ToDouble(data[4][indicate.Length - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][indicate.Length - 1]);
                        result.deposit[0][i, j] -= currPrs;
                        buy = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price buy = " + data[4][indicate.Length - 1] +  "  price = " + data[0][indicate.Length - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }
                    if (sell)
                    {
                        double currPrs = Convert.ToDouble(data[3][indicate.Length - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][indicate.Length - 1]);
                        result.deposit[0][i, j] += currPrs;
                        sell = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price sell = " + data[3][indicate.Length - 1] +   "  price = " + data[0][indicate.Length - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }


                    pbvol++;
                    //form.progressBar1_setvol(pbvol);
                }
            }
            //form.Close();
            //label5.Text = "Количество совершенных операций = " + newopercount;

            return result;
        }

        private Portfel takeResultTandS2(int[][] data) //Процедура торгов по тикам и отсечке по времени
        {
            double minParam = (double)numericUpDown10.Value;
            double maxParam = (double)numericUpDown9.Value;
            double minPorog = (double)numericUpDown2.Value;
            double maxPorog = (double)numericUpDown4.Value;
            Portfel result = Portfel.Inicialize((int)(maxParam - minParam) + 1, ((int)(maxPorog - minPorog)) * 10 + 1);
            result.minParam = minParam;
            result.maxParam = maxParam;
            result.minPorog = minPorog;
            result.maxPorog = maxPorog;

            //Form2 form = new Form2();
            //form.progressBar1_setmax(result.porogPart * result.paramPart);
            //form.Show();
            int pbvol = 0;
            int newopercount = 0;
            bool buy = false;
            bool sell = false;
            bool none = true;

            int besti = (int)(numericUpDown8.Value) - (int)(numericUpDown10.Value);
            int bestj = (int)((bestPorog - result.minPorog) * 10);
            string testOperation = "Операции проведены: ";
            int testOperCount = 0;
            int operTime = data[2][0];

            for (int i = 0; i < result.paramPart; i++)
            {
                double param = result.minParam + Convert.ToDouble(i) * (result.maxParam - result.minParam) / (result.paramPart - 1);
                double[] indicate = Smooth(IndicateTime(data, param), smth);

                for (int j = 0; j < result.porogPart; j++)
                {
                    double porog = result.minPorog + j * (result.maxPorog - result.minPorog) / (result.porogPart - 1);
                    for (int k = 20; k < indicate.Length; k++)
                    {
                        int cnt = k + smth - 1;
                        if (cnt > indicate.Length - 1) cnt = indicate.Length - 1;
                        if (none)
                        {
                            if (indicate[k] > porog * multPorog | indicate[k] < -porog  && indicate[k] > -porog * multPorog)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                result.deposit[1][i, j] += 1;
                                result.deposit[0][i, j] -= spread;
                                none = false;
                                buy = true;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " sell at " + (cnt) + "  price = " + data[3][cnt] + " | ";
                                    Log.LogWrite("open sell at " + (cnt) + "  price sell = " + data[3][cnt] + " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] - Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                    //label13.Text += indicate[k].ToString("0.0") + " ";
                                }
                                operTime = data[2][cnt];
                            }
                            if (indicate[k] < -porog * multPorog | indicate[k] > porog && indicate[k] < porog * multPorog)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                result.deposit[1][i, j] += 1;
                                result.deposit[0][i, j] -= spread;
                                none = false;
                                sell = true;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " buy at " + (cnt) + "  price = " + data[4][cnt] + " | ";
                                    Log.LogWrite("open buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] + Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (buy)
                        {
                            if (indicate[k] > porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                none = true;
                                buy = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (sell)
                        {
                            if (indicate[k] < -porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                none = true;
                                sell = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close sell at " + (cnt) + "  price sell = " + data[3][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                    }
                    if (buy)
                    {
                        double currPrs = Convert.ToDouble(data[4][indicate.Length - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][indicate.Length - 1]);
                        result.deposit[0][i, j] -= currPrs;
                        buy = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price buy = " + data[4][indicate.Length - 1] +  "  price = " + data[0][indicate.Length - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }
                    if (sell)
                    {
                        double currPrs = Convert.ToDouble(data[3][indicate.Length - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][indicate.Length - 1]);
                        result.deposit[0][i, j] += currPrs;
                        sell = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price sell = " + data[3][indicate.Length - 1] +   "  price = " + data[0][indicate.Length - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }


                    pbvol++;
                    //form.progressBar1_setvol(pbvol);
                }
            }
            //form.Close();
            //label5.Text = "Количество совершенных операций = " + newopercount;

            return result;
        }

        private Portfel takeResultTandSlimT(int[][] data) //Процедура торгов 4
        {
            double minParam = (double)numericUpDown10.Value;
            double maxParam = (double)numericUpDown9.Value;
            double minPorog = (double)numericUpDown2.Value;
            double maxPorog = (double)numericUpDown4.Value;
            Portfel result = Portfel.Inicialize((int)(maxParam - minParam) + 1, ((int)(maxPorog - minPorog)) * 10 + 1);
            result.minParam = minParam;
            result.maxParam = maxParam;
            result.minPorog = minPorog;
            result.maxPorog = maxPorog;

            //Form2 form = new Form2();
            //form.progressBar1_setmax(result.porogPart * result.paramPart);
            //form.Show();
            int pbvol = 0;
            int newopercount = 0;
            bool buy = false;
            bool sell = false;
            bool none = true;

            int besti = (int)(numericUpDown8.Value) - (int)(numericUpDown10.Value);
            int bestj = (int)((bestPorog - result.minPorog) * 10);
            string testOperation = "Операции проведены: ";
            int testOperCount = 0;
            int operTime = data[2][0];
                        
            int limTCountMax = 50;
            int limTCountMin = 50;
            while (data[2][limTCountMax] < limTmax + 1)
            {
                limTCountMax++;
                if (limTCountMax == data[2].Length) break;
                if (data[2][limTCountMax] < limTmin) limTCountMin = limTCountMax;
            }

            //MessageBox.Show(limTCountMin.ToString() + " " + limTCountMax.ToString());

            for (int i = 0; i < result.paramPart; i++)
            {
                double param = result.minParam + Convert.ToDouble(i) * (result.maxParam - result.minParam) / (result.paramPart - 1);
                double[] indicate = Smooth(IndicateTime(data, param), smth);

                for (int j = 0; j < result.porogPart; j++)
                {
                    double porog = result.minPorog + j * (result.maxPorog - result.minPorog) / (result.porogPart - 1);
                    for (int k = limTCountMin; k < limTCountMax; k++)
                    {
                        int cnt = k + smth - 1;
                        if (cnt > indicate.Length - 1) cnt = indicate.Length - 1;
                        if (none)
                        {
                            if (indicate[k] < -porog)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                result.deposit[1][i, j] += 1;
                                result.deposit[0][i, j] -= spread;
                                none = false;
                                buy = true;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " sell at " + (cnt) + "  price = " + data[3][cnt] + " | ";
                                    Log.LogWrite("open sell at " + (cnt) + "  price sell = " + data[3][cnt] + " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] - Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                    //label13.Text += indicate[k].ToString("0.0") + " ";
                                }
                                operTime = data[2][cnt];
                            }
                            if (indicate[k] > porog)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                result.deposit[1][i, j] += 1;
                                result.deposit[0][i, j] -= spread;
                                none = false;
                                sell = true;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " buy at " + (cnt) + "  price = " + data[4][cnt] + " | ";
                                    Log.LogWrite("open buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] + Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (buy)
                        {
                            if (indicate[k] > porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                none = true;
                                buy = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (sell)
                        {
                            if (indicate[k] < -porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                none = true;
                                sell = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close sell at " + (cnt) + "  price sell = " + data[3][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                    }
                    if (buy)
                    {
                        double currPrs = Convert.ToDouble(data[4][limTCountMax - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][limTCountMax - 1]);
                        result.deposit[0][i, j] -= currPrs;
                        buy = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (limTCountMax - 1) + "  price buy = " + data[4][limTCountMax - 1] +  "  price = " + data[0][limTCountMax - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }
                    if (sell)
                    {
                        double currPrs = Convert.ToDouble(data[3][limTCountMax - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][limTCountMax - 1]);
                        result.deposit[0][i, j] += currPrs;
                        sell = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price sell = " + data[3][limTCountMax - 1] +   "  price = " + data[0][limTCountMax - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }


                    pbvol++;
                    //form.progressBar1_setvol(pbvol);
                }
            }
            //form.Close();
            //label5.Text = "Количество совершенных операций = " + newopercount;

            return result;
        }

        private Portfel takeResultTandSlimTInv(int[][] data) //Процедура торгов 5
        {
            double minParam = (double)numericUpDown10.Value;
            double maxParam = (double)numericUpDown9.Value;
            double minPorog = (double)numericUpDown2.Value;
            double maxPorog = (double)numericUpDown4.Value;
            Portfel result = Portfel.Inicialize((int)(maxParam - minParam) + 1, ((int)(maxPorog - minPorog)) * 10 + 1);
            result.minParam = minParam;
            result.maxParam = maxParam;
            result.minPorog = minPorog;
            result.maxPorog = maxPorog;

            //Form2 form = new Form2();
            //form.progressBar1_setmax(result.porogPart * result.paramPart);
            //form.Show();
            int pbvol = 0;
            int newopercount = 0;
            bool buy = false;
            bool sell = false;
            bool none = true;

            int besti = (int)(numericUpDown8.Value) - (int)(numericUpDown10.Value);
            int bestj = (int)((bestPorog - result.minPorog) * 10);
            string testOperation = "Операции проведены: ";
            int testOperCount = 0;
            int operTime = data[2][0];

            int limTCountMax = 50;
            int limTCountMin = 50;
            while (data[2][limTCountMax] < limTmax + 1)
            {
                limTCountMax++;
                if (limTCountMax == data[2].Length) break;
                if (data[2][limTCountMax] < limTmin) limTCountMin = limTCountMax;
            }

            //MessageBox.Show(limTCountMin.ToString() + " " + limTCountMax.ToString());

            for (int i = 0; i < result.paramPart; i++)
            {
                double param = result.minParam + Convert.ToDouble(i) * (result.maxParam - result.minParam) / (result.paramPart - 1);
                double[] indicate = Smooth(IndicateTime(data, param), smth);

                for (int j = 0; j < result.porogPart; j++)
                {
                    double porog = result.minPorog + j * (result.maxPorog - result.minPorog) / (result.porogPart - 1);
                    for (int k = limTCountMin; k < limTCountMax; k++)
                    {
                        int cnt = k + smth - 1;
                        if (cnt > indicate.Length - 1) cnt = indicate.Length - 1;
                        if (none)
                        {
                            if (indicate[k] > porog)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                result.deposit[1][i, j] += 1;
                                result.deposit[0][i, j] -= spread;
                                none = false;
                                buy = true;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " sell at " + (cnt) + "  price = " + data[3][cnt] + " | ";
                                    Log.LogWrite("open sell at " + (cnt) + "  price sell = " + data[3][cnt] + " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] - Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                    //label13.Text += indicate[k].ToString("0.0") + " ";
                                }
                                operTime = data[2][cnt];
                            }
                            if (indicate[k] < -porog)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                result.deposit[1][i, j] += 1;
                                result.deposit[0][i, j] -= spread;
                                none = false;
                                sell = true;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " buy at " + (cnt) + "  price = " + data[4][cnt] + " | ";
                                    Log.LogWrite("open buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] + Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (buy)
                        {
                            if (indicate[k] < -porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] -= currPrs;
                                none = true;
                                buy = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                        else if (sell)
                        {
                            if (indicate[k] > porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                result.deposit[0][i, j] += currPrs;
                                none = true;
                                sell = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("close sell at " + (cnt) + "  price sell = " + data[3][cnt] +  " price = " + data[0][cnt] + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                                operTime = data[2][cnt];
                            }
                        }
                    }
                    if (buy)
                    {
                        double currPrs = Convert.ToDouble(data[4][limTCountMax - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][limTCountMax - 1]);
                        result.deposit[0][i, j] -= currPrs;
                        buy = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (limTCountMax - 1) + "  price buy = " + data[4][limTCountMax - 1] +  "  price = " + data[0][limTCountMax - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }
                    if (sell)
                    {
                        double currPrs = Convert.ToDouble(data[3][limTCountMax - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][limTCountMax - 1]);
                        result.deposit[0][i, j] += currPrs;
                        sell = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price sell = " + data[3][limTCountMax - 1] +   "  price = " + data[0][limTCountMax - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }


                    pbvol++;
                    //form.progressBar1_setvol(pbvol);
                }
            }
            //form.Close();
            //label5.Text = "Количество совершенных операций = " + newopercount;

            return result;
        }

        private Portfel takeResultTandSnoMarket(int[][] data) //Процедура торгов по тикам и отсечке по времени
        {
            double minParam = (double)numericUpDown10.Value;
            double maxParam = (double)numericUpDown9.Value;
            double minPorog = (double)numericUpDown2.Value;
            double maxPorog = (double)numericUpDown4.Value;
            Portfel result = Portfel.Inicialize((int)(maxParam - minParam) + 1, ((int)(maxPorog - minPorog)) * 10 + 1);
            result.minParam = minParam;
            result.maxParam = maxParam;
            result.minPorog = minPorog;
            result.maxPorog = maxPorog;

            //Form2 form = new Form2();
            //form.progressBar1_setmax(result.porogPart * result.paramPart);
            //form.Show();
            int pbvol = 0;
            int newopercount = 0;
            bool buy = false;
            bool sell = false;
            bool none = true;
            bool order = false;
            double orderPrice = 0;
            int orderTime = 0;

            int besti = (int)(numericUpDown8.Value) - (int)(numericUpDown10.Value);
            int bestj = (int)((bestPorog - result.minPorog) * 10);
            string testOperation = "Операции проведены: ";
            int testOperCount = 0;
            int operTime = data[2][0];

            for (int i = 0; i < result.paramPart; i++)
            {
                double param = result.minParam + Convert.ToDouble(i) * (result.maxParam - result.minParam) / (result.paramPart - 1);
                double[] indicate = Smooth(IndicateTime(data, param), smth);

                buy = false;
                sell = false;
                none = true;
                order = false;
                orderPrice = 0;
                orderTime = 0;

                for (int j = 0; j < result.porogPart; j++)
                {
                    double porog = result.minPorog + j * (result.maxPorog - result.minPorog) / (result.porogPart - 1);
                    for (int k = 20; k < indicate.Length; k++)
                    {
                        int cnt = k + smth - 1;
                        if (cnt > indicate.Length - 1) cnt = indicate.Length - 1;
                                                
                        if (order && buy)
                        {
                            double currPrs = Convert.ToDouble(data[3][cnt]);
                            if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                            if (orderPrice < currPrs)
                            {
                                result.deposit[0][i, j] += currPrs;
                                result.deposit[1][i, j] += 1;
                                result.deposit[0][i, j] -= spread;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " sell at " + (cnt) + "  price = " + data[3][cnt] + " | ";
                                    Log.LogWrite("open sell at " + (cnt) + "  price sell = " + currPrs + " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] - Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    Log.LogWrite("open sell param: order " + order.ToString() + " buy = " + buy.ToString() + " sell = " + sell.ToString(), enableLog);
                                    testOperCount++;
                                }
                                operTime = data[2][cnt];
                                order = false;
                            }
                            if (data[2][cnt] - orderTime > orderIsp)
                            {
                                if (order) buy = false;
                                if (order) none = true;
                                order = false; 
                            }
                        }
                        else if (order && sell)
                        {
                            double currPrs = Convert.ToDouble(data[4][cnt]);
                            if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                            if (orderPrice > currPrs)
                            {
                                result.deposit[0][i, j] -= currPrs;
                                result.deposit[1][i, j] += 1;
                                result.deposit[0][i, j] -= spread;
                                newopercount++;
                                if (i == besti & j == bestj)
                                {
                                    testOperation += " buy at " + (cnt) + "  price = " + data[4][cnt] + " | ";
                                    Log.LogWrite("open buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + data[0][cnt] + " deposit = " + (result.deposit[0][i, j] + Convert.ToDouble(data[0][cnt])) + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    testOperCount++;
                                }
                                operTime = data[2][cnt];
                                order = false;
                            }
                            if (data[2][cnt] - orderTime > orderIsp)
                            {
                                if (order) sell = false;
                                if (order) none = true;
                                order = false;                                
                            }
                        }
                        else if (order && none)
                        {
                            if (result.deposit[0][i, j] > 10000)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                if (orderPrice > currPrs)
                                {
                                    result.deposit[0][i, j] -= orderPrice;
                                    none = true;
                                    order = false;
                                    if (i == besti & j == bestj)
                                    {
                                        Log.LogWrite("close buy at " + (cnt) + "  price buy = " + data[4][cnt] +  " price = " + orderPrice + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    }
                                    operTime = data[2][cnt];
                                }
                            }
                            else if (result.deposit[0][i, j] < -10000)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                if (orderPrice < currPrs)
                                {
                                    result.deposit[0][i, j] += orderPrice;
                                    none = true;
                                    order = false;
                                    if (i == besti & j == bestj)
                                    {
                                        Log.LogWrite("close sell at " + (cnt) + "  price sell = " + data[3][cnt] +  " price = " + orderPrice + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                    }
                                    operTime = data[2][cnt];
                                }
                            }
                        }

                        if (!order && none)
                        {
                            if (indicate[k] < -porog)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                orderPrice = currPrs + 5;
                                order = true;
                                none = false;
                                buy = true;
                                orderTime = data[2][cnt];
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("order open sell at " + (cnt) + "  price sell = " + orderPrice + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                            }
                            if (indicate[k] > porog)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                orderPrice = currPrs - 5;
                                order = true;
                                none = false;
                                sell = true;
                                orderTime = data[2][cnt];
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("order open buy at " + (cnt) + "  price buy = " + orderPrice + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                            }
                        }
                        else if (!order && buy)
                        {
                            if (indicate[k] > porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[4][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                orderPrice = currPrs  - 5;
                                order = true;
                                none = true;
                                buy = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("order close buy at " + (cnt) + "  price buy = " + orderPrice + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                            }
                        }
                        else if (!order && sell)
                        {
                            if (indicate[k] < -porog * 1.2 && data[2][cnt] - operTime > (int)numericUpDown3.Value * 1000)
                            {
                                double currPrs = Convert.ToDouble(data[3][cnt]);
                                if (currPrs == 0) currPrs = Convert.ToDouble(data[0][cnt]);
                                orderPrice = currPrs + 5;
                                order = true;
                                none = true;
                                sell = false;
                                if (i == besti & j == bestj)
                                {
                                    Log.LogWrite("order close sell at " + (cnt) + "  price buy = " + orderPrice + " deposit = " + result.deposit[0][i, j] + " indicate = " + indicate[k].ToString("0.00") + " " + indicate[k-1].ToString("0.00") + " | ", enableLog);
                                }
                            }
                        }
                    }
                   
                    
                    if (result.deposit[0][i, j] > 10000)
                    {
                        double currPrs = Convert.ToDouble(data[4][indicate.Length - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][indicate.Length - 1]);
                        result.deposit[0][i, j] -= currPrs;
                        buy = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session buy at " + (indicate.Length - 1) + "  price buy = " + data[4][indicate.Length - 1] +  "  price = " + data[0][indicate.Length - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }
                    else if (result.deposit[0][i, j] < -10000)
                    {
                        double currPrs = Convert.ToDouble(data[3][indicate.Length - 1]);
                        if (currPrs == 0) currPrs = Convert.ToDouble(data[0][indicate.Length - 1]);
                        result.deposit[0][i, j] += currPrs;
                        sell = false;
                        none = true;
                        if (i == besti & j == bestj)
                        {
                            Log.LogWrite("close session sell at " + (indicate.Length - 1) + "  price sell = " + data[3][indicate.Length - 1] +   "  price = " + data[0][indicate.Length - 1] + " deposit = " + result.deposit[0][i, j] + " | ", enableLog);
                        }
                    }
                         
                pbvol++;
                    //form.progressBar1_setvol(pbvol);
                }
            }
            //form.Close();
            //label5.Text = "Количество совершенных операций = " + newopercount;
            
            return result;
        }

        #endregion

    }
}
