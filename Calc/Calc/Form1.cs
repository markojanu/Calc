﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;


namespace Calc
{
    public partial class Form1 : Form
    {
        System.Diagnostics.Process bc;
        StreamReader bcOut;
        StreamWriter bcIn;
        float memory = 0.0f;
        protected bool graphOpened;
        protected GrafForm grafoveOkno;
        private bool isShiftDown=false;

        public Form1()
        {
            InitializeComponent();
            startBC();
            graphOpened = false;
            
        }

        ~Form1()
        {
            bc.StandardInput.WriteLine("quit");//ukoncenie programu bc
        }
        /// <summary>
        /// spustenie programu bc a nasypanie funkcii do neho
        /// </summary>
        private void startBC() 
        {
            
            bc = new System.Diagnostics.Process();
            try
            {
         
                String strCommand = "..\\..\\..\\..\\bc\\bc.exe";//cesta k programu

                bc.StartInfo.FileName = strCommand;

                bc.StartInfo.UseShellExecute = false;

                bc.StartInfo.RedirectStandardOutput = true;//presmerovanie standardneho vstupu a vystupu programu
                bc.StartInfo.RedirectStandardInput = true;
                bc.StartInfo.CreateNoWindow = true;//aby sa nezobrazovalo to cierne okno konzoly ked sa spusti program
                bc.StartInfo.Arguments = "-l";
                bc.EnableRaisingEvents = true;                
                bc.Exited += new EventHandler(bcExited);//event handler na ukoncenie programu bc
                bc.StartInfo.RedirectStandardError = true;



                bc.Start();//spustenie programu bc                
            }
            catch (Exception e)
            {
                resultTextBox.Text = "Error: unable to run bc.exe";
            }
            bcOut = bc.StandardOutput;
            bcIn = bc.StandardInput;

            TextReader functions = new StreamReader("..\\..\\..\\..\\bc\\funkcie.bc");
            String line = "";
            //resultTextBox.Text = line;
            while ((line = functions.ReadLine()) != null)
            {
                bcIn.WriteLine(line);
            }
        }
        /// <summary>
        /// funkcia pre handler ukone=cenia programu bc
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bcExited(object sender, EventArgs e)
        {
            startBC();//ak sa bc ukonci tak sa znova spusti
        }

        /// <summary>
        /// funkcia ktoru vykonava vlakno na ziskanie vysledku z programu bc
        /// </summary>
        private void getResult()
        {
            resultTextBox.Text = "";
            String text = "";
            for (int i = 0; i < expressionTextBox.Lines.Length; i++) text += expressionTextBox.Lines[i];
            try
            {
                bcIn.WriteLine(text);//vlozenie vyrazu na vstup programu                        
                string line = "";
                while ((line = bcOut.ReadLine()) != null)
                {
                    resultTextBox.Text += line;
                }
            }
            catch (ThreadAbortException e) 
            {
                if (resultTextBox.Text == "") resultTextBox.Text = "Calculation timeout."; 
            }           
        }

        /// <summary>
        /// funkcia pre timer na ukoncenie vlakna kde sa ziskava vysledok a aj programu bc
        /// </summary>
        /// <param name="data"></param>
        private void abortGettingResult(object data)
        {
            if (((Thread)data).IsAlive)
            {
                ((Thread)data).Abort();
                bc.Kill();
            }
        }

        /// <summary>
        /// Funkcia ktora sluzi ako event na spracovanie stlaceny zakladnych tlacidiel (hodnot)
        /// </summary>
        /// <param name="sender">No fucking idee</param>
        /// <param name="e">same as sender</param>
        private void buttonNum_Click(object sender, EventArgs e)
        {
            Button s = sender as Button;
            int cursorPosition = expressionTextBox.SelectionStart;
            expressionTextBox.Text = expressionTextBox.Text.Substring(0, cursorPosition) + s.Text + expressionTextBox.Text.Substring(cursorPosition, expressionTextBox.Text.Length - cursorPosition);
            expressionTextBox.SelectionStart = cursorPosition + 1;
        }

        private void buttonEquals_Click(object sender, EventArgs e)
        {
            String text = "";
            for (int i = 0; i < expressionTextBox.Lines.Length; i++) text += expressionTextBox.Lines[i];   
                //text = text.Replace("\n", "");
            if (text.StartsWith("f(x)=") || text.StartsWith("y="))
            {
                if (!graphOpened)   //grafove okno este neni otvorene
                {
                    grafoveOkno = new GrafForm(text, ref graphOpened);
                    grafoveOkno.Visible = true;
                    graphOpened = true;
                }
                else                //grafove okno uz je otvorene
                {
                    grafoveOkno.AddFunkcia(text);
                    grafoveOkno.Show();
                    grafoveOkno.TopMost = true;
                    grafoveOkno.Focus();
                    grafoveOkno.BringToFront();
                    grafoveOkno.TopMost = false;
                }
            }
            else { 
                Thread t = new Thread(getResult);
                System.Threading.Timer timer = new System.Threading.Timer(abortGettingResult, t, 1000, Timeout.Infinite);
                t.Start();
            }
        }

        private void expressionTextBox_KeyDown(object sender, KeyEventArgs e)//odstranil som lebo chceme aby sa dal ten vyraz nejako formatovat
        {
            if (e.KeyCode == Keys.ShiftKey)isShiftDown = true;
            if (e.KeyCode == Keys.Enter && isShiftDown)buttonEquals_Click(sender, e);                
        }


        private void expressionTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && isShiftDown)//a zmaz enter ktory sa zapisal pri odpaleni vypoctu
            {
                int cursorPosition = expressionTextBox.SelectionStart;
                expressionTextBox.Text = expressionTextBox.Text.Substring(0, cursorPosition - 1) + expressionTextBox.Text.Substring(cursorPosition, expressionTextBox.Text.Length - cursorPosition);
                expressionTextBox.SelectionStart = cursorPosition - 1;
            }
            if (e.KeyCode == Keys.ShiftKey) isShiftDown = false;
        }  

        private void buttonC_Click(object sender, EventArgs e)
        {
            expressionTextBox.Text = "";
        }

        private void buttonDel_Click(object sender, EventArgs e)
        {
            if(expressionTextBox.Text=="") {
                return;
            }
            int cursorPosition = expressionTextBox.SelectionStart;
            expressionTextBox.Text = expressionTextBox.Text.Substring(0, cursorPosition-1) + expressionTextBox.Text.Substring(cursorPosition, expressionTextBox.Text.Length - cursorPosition);
            expressionTextBox.SelectionStart = cursorPosition - 1;
        }

        private void buttonMS_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(getResult);
            System.Threading.Timer timer = new System.Threading.Timer(abortGettingResult, t, 1000, Timeout.Infinite);
            t.Start();
            t.Join();
            try
            {
                memory = float.Parse(resultTextBox.Text);
            }
            catch (FormatException err) { }
        }

        private void buttonMR_Click(object sender, EventArgs e)
        {
            int cursorPosition = expressionTextBox.SelectionStart;
            expressionTextBox.Text = expressionTextBox.Text.Substring(0, cursorPosition) + memory.ToString() + expressionTextBox.Text.Substring(cursorPosition, expressionTextBox.Text.Length - cursorPosition);
            expressionTextBox.SelectionStart = cursorPosition + memory.ToString().Length;
        }

        private void buttonMC_Click(object sender, EventArgs e)
        {
            memory = 0.0f;
        }

        private void buttonMplus_Click(object sender, EventArgs e)
        {
            expressionTextBox.Text = memory.ToString() + "+(" + expressionTextBox.Text + ")";
            Thread t = new Thread(getResult);
            System.Threading.Timer timer = new System.Threading.Timer(abortGettingResult, t, 1000, Timeout.Infinite);
            t.Start();
            t.Join();
            try
            {              
                    memory = float.Parse(resultTextBox.Text);
            }
            catch (FormatException err) { }

        }

        private void buttonMminus_Click(object sender, EventArgs e)
        {
            expressionTextBox.Text = memory.ToString() + "-(" + expressionTextBox.Text + ")";
            Thread t = new Thread(getResult);
            System.Threading.Timer timer = new System.Threading.Timer(abortGettingResult, t, 1000, Timeout.Infinite);
            t.Start();
            t.Join();
            try
            {
                    memory = float.Parse(resultTextBox.Text);
            }
            catch (FormatException err) { }
        }

        private void buttonAns_Click(object sender, EventArgs e)
        {
            String ans;
            try
            {
                ans = float.Parse(resultTextBox.Text).ToString();
            }
            catch (FormatException err) { ans="0";}
            int cursorPosition = expressionTextBox.SelectionStart;
            expressionTextBox.Text = expressionTextBox.Text.Substring(0, cursorPosition) + ans + expressionTextBox.Text.Substring(cursorPosition, expressionTextBox.Text.Length - cursorPosition);
            expressionTextBox.SelectionStart = cursorPosition + ans.Length;
        }

        private void HexRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            buttonA.Enabled = true;
            buttonB.Enabled = true;
            buttonC.Enabled = true;
            buttonD.Enabled = true;
            buttonE.Enabled = true;
            buttonF.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = true;
            button8.Enabled = true;
            button9.Enabled = true;
        }

        private void DecRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            buttonA.Enabled = false;
            buttonB.Enabled = false;
            buttonC.Enabled = false;
            buttonD.Enabled = false;
            buttonE.Enabled = false;
            buttonF.Enabled = false;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = true;
            button8.Enabled = true;
            button9.Enabled = true;

        }

        private void OctRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            buttonA.Enabled = false;
            buttonB.Enabled = false;
            buttonC.Enabled = false;
            buttonD.Enabled = false;
            buttonE.Enabled = false;
            buttonF.Enabled = false;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = true;
            button8.Enabled = false;
            button9.Enabled = false;
        }

        private void BinRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            buttonA.Enabled = false;
            buttonB.Enabled = false;
            buttonC.Enabled = false;
            buttonD.Enabled = false;
            buttonE.Enabled = false;
            buttonF.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
            button9.Enabled = false;
            }

        private void expressionTextBox_TextChanged(object sender, EventArgs e)
        {
            int length = expressionTextBox.Text.Length;
            int cursor_position = expressionTextBox.SelectionStart;
            
            if (DecRadioButton.Checked == true)
            {
                expressionTextBox.Text = expressionTextBox.Text.Replace("A", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("B", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("C", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("D", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("E", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("F", "");                
            }
            if (OctRadioButton.Checked == true)
            {
                expressionTextBox.Text = expressionTextBox.Text.Replace("A", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("B", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("C", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("D", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("E", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("F", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("8", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("9", "");
            }
            if (BinRadioButton.Checked == true)
            {
                expressionTextBox.Text = expressionTextBox.Text.Replace("A", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("B", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("C", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("D", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("E", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("F", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("2", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("3", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("4", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("5", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("6", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("7", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("8", "");
                expressionTextBox.Text = expressionTextBox.Text.Replace("9", "");
            }
            expressionTextBox.SelectionStart = cursor_position - (expressionTextBox.Text.Length - length);
            //ak sa nejaky znak zmazal tak sa posunie nastavenie kurzora
        }

    }
}
