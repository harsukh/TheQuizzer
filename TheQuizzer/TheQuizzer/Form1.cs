﻿/*
 * Copyright 2013 Gregory M Chen
   This file is part of TheQuizzer.

    TheQuizzer is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    TheQuizzer is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with TheQuizzer.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Media;
using System.Threading;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TheQuizzer
{
    public partial class Form1 : Form
    {
        private const int EDUCATIONAL_FREQUENCY = 10;
        private const int TIME_LIMIT = 60;
        private const float MAX_QUESTION_FONT_SIZE = 20;
        private const float MIN_QUESTION_FONT_SIZE = 12;
        private const float FONT_SHRINK_SPEED = 1.0f;
        private int numCorrect = 0, numIncorrect = 0;
        private int educationalIndex1 = -1, educationalIndex2 = -1;
        private Random random = new Random();
        private List<Entry> entryList = new List<Entry>();
        private List<int> incorrectEntryIndexes = new List<int>();
        private int currentEntryIndex;
        private string lastOpenPath;
        private bool educationalEnhancements = false;
        private bool useQuestionsAsAnswers = false;
        private bool playWithPoints = false;
        private bool currentQuestionElement1;
        private int points = 0;
        private int countdown = TIME_LIMIT;
        private bool enterKeyDown;
        private string hsFilePath;
        HighScore hs;
        SoundPlayer yep = new SoundPlayer("zsrc\\yep.wav");
        SoundPlayer nope = new SoundPlayer("zsrc\\nope.wav");
        SoundPlayer tick = new SoundPlayer("zsrc\\tick.wav");
        SoundPlayer ding = new SoundPlayer("zsrc\\ding.wav");
        List<SoundPlayer> yay = new List<SoundPlayer>();

        public Form1()
        {
            InitializeComponent();
            
            for (int i = 1; File.Exists("zsrc\\yay" + i + ".wav"); i++) 
            {
                SoundPlayer p = new SoundPlayer("zsrc\\yay" + i + ".wav");
                yay.Add(p);
            }
            hs = new HighScore();
            hs.FormClosed += new FormClosedEventHandler(hs_FormClosed);
        }

        void hs_FormClosed(object sender, FormClosedEventArgs e)
        {
            checkHighScores();
            hs.Dispose();
            hs = new HighScore();
            hs.FormClosed += new FormClosedEventHandler(hs_FormClosed);
        }

        private void submitAnswer()
        {
            textBox2.SelectAll();
            // lame-o Easter egg
            if (isCorrectAnswer(textBox2.Text, "Greg is cool"))
            {
                yep.Play();
                textBox3.Text = "You betcha";
                return;
            }
            if (isCorrectAnswer(textBox2.Text, "Hi Greg") || isCorrectAnswer(textBox2.Text, "Hi, Greg"))
            {
                yep.Play();
                textBox3.Text = "Hi";
                return;
            }
            bool correct = false;
        
            if (currentQuestionElement1 && isCorrectAnswer(textBox2.Text, entryList[currentEntryIndex].getElementTwo()))
                    correct = true;
            else if (!currentQuestionElement1 && isCorrectAnswer(textBox2.Text, entryList[currentEntryIndex].getElementOne()))
                    correct = true;
        
            if (correct)
            {
                yep.Stop();
                nope.Stop();
                yep.Play();
                textBox3.Text = "That is correct";
                points += 40;
                numCorrect++;
                educationalIndex2 = -1;
            }
            else
            {
                yep.Stop();
                nope.Stop();
                nope.Play();
                if (currentQuestionElement1)
                {
                    textBox3.Text = "Nice try.  The correct answer is\r\n" + entryList[currentEntryIndex].getElementTwo()[0];
                }
                else //current question is element2
                {
                    textBox3.Text = "Nice try.  The correct answer is\r\n" + entryList[currentEntryIndex].getElementOne()[0];
                }
                points -= 10;
                
                if (playWithPoints && points < 0)
                {
                    
                    points = 0;
                    if (!timer1.Enabled)
                    {
                        button2.Text = ":(";
                        button2.Update();
                        textBox3.Update();
                        Splash gameOverScreen = new Splash();
                        gameOverScreen.BackgroundImage = Image.FromFile("zsrc\\gameover.jpg");
                        gameOverScreen.Width = gameOverScreen.BackgroundImage.Width;
                        gameOverScreen.Height = gameOverScreen.BackgroundImage.Height;
                        gameOverScreen.timer1.Interval = 1500;
                        gameOverScreen.Show();
                        Thread.Sleep(1500);
                    }
                }
                numIncorrect++;
                educationalIndex2 = currentEntryIndex;
                if (!incorrectEntryIndexes.Contains(currentEntryIndex))
                {
                    incorrectEntryIndexes.Add(currentEntryIndex);
                }
            }

            if (playWithPoints)
            {
                button2.Text = points.ToString();
            }

            generateNextEntry();


        }

        private void generateNextEntry()
        {
            //choose an index
            if (educationalEnhancements && educationalIndex1 != -1)
            {
                currentEntryIndex = educationalIndex1;
            }
            else
            {
                if (educationalEnhancements && incorrectEntryIndexes.Count != 0 && random.Next(EDUCATIONAL_FREQUENCY) == 0)
                {
                    textBox1.ForeColor = System.Drawing.Color.DarkBlue;
                    currentEntryIndex = incorrectEntryIndexes[random.Next(incorrectEntryIndexes.Count)];
                }
                else
                {
                    textBox1.ForeColor = System.Drawing.Color.Black;
                    currentEntryIndex = random.Next(entryList.Count);
                }
            }
            educationalIndex1 = educationalIndex2;
            //choose to use either element1 or element2
            if (useQuestionsAsAnswers && random.Next(2) == 0)
            {
                currentQuestionElement1 = false;
                textBox1.Text = entryList[currentEntryIndex].getElementTwo()[0];
            }
            else
            {
                currentQuestionElement1 = true;
                textBox1.Text = entryList[currentEntryIndex].getElementOne()[0];
            }
            if (!questionboxTimer.Enabled)
            {
                beginFancyTextInBox1();
            }
        }

        private bool isCorrectAnswer(string givenAnswer, string[] answers)
        {
            foreach (string answer in answers)
            {
                if (isCorrectAnswer(givenAnswer, answer)) // compare ignoring case
                {
                    return true;
                }
            }
            return false;
        }

        private bool isCorrectAnswer(string givenAnswer, string actualAnswer)
        {
            givenAnswer = givenAnswer.Replace(" ", "");
            actualAnswer = actualAnswer.Replace(" ", "");
            
            if (String.Compare(givenAnswer, actualAnswer, true) == 0) // compare ignoring case
            {
                return true;
            }
            if (String.Compare(actualAnswer, "yes", true) == 0)
            {
                string[] yesVals = { "yeah", "yesh", "yea", "yep", "booyeah", 
                                       "hellyeah", "fuckyeah", "wellduh", "wellduhh",
                                       "heckyeah", "affirmative"
                                        };
                foreach (string validAnswer in yesVals)
                {
                    if (String.Compare(givenAnswer, validAnswer) == 0)
                    {
                        return true;
                    }
                }
            }
            if (String.Compare(actualAnswer, "no", true) == 0)
            {
                string[] noVals = { "nope", "nah", "lolno", "noo", 
                                      "nooo", "noooo", "nooooo", "noooooo", 
                                      "nooooooo", "noooooooo", "nooooooooo", 
                                      "noooooooooo", "hellno", "heckno",
                                      "negative"};
                foreach (string validAnswer in noVals)
                {
                    if (String.Compare(givenAnswer, validAnswer) == 0)
                    {
                        return true;
                    }
                }
            }
            if(givenAnswer.EndsWith("duh", true, null))
            {
                givenAnswer = givenAnswer.Substring(0, givenAnswer.Length - 3);
            }
            else if (givenAnswer.EndsWith("duhh", true, null))
            {
                givenAnswer = givenAnswer.Substring(0, givenAnswer.Length - 4);
            }
            if (givenAnswer.EndsWith("stupid", true, null))
            {
                givenAnswer = givenAnswer.Substring(0, givenAnswer.Length - 6);
            }
            if (givenAnswer.EndsWith("lol", true, null))
            {
                givenAnswer = givenAnswer.Substring(0, givenAnswer.Length - 3);
            }

            if (String.Compare(givenAnswer, actualAnswer, true) == 0) // compare ignoring case
            {
                return true;
            }
            
            // strip away ending 's' from both given and actual answers to negate possible plural forms
            if (givenAnswer.EndsWith("s", true, null))
            {
                givenAnswer = givenAnswer.Substring(0, givenAnswer.Length - 1);
            }
            if (actualAnswer.EndsWith("s", true, null))
            {
                actualAnswer = actualAnswer.Substring(0, actualAnswer.Length -1);
            }
            if (String.Compare(givenAnswer, actualAnswer, true) == 0) // compare ignoring case
            {
                return true;
            }

            //TODO: deal with plural forms, answers ending with 'duh'
            
            return false;
        }

        private void openDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tick.Play();
            openDatabase();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            tick.Play();
            openDatabase();
        }

        private void openDatabase()
        {
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.Title = "Open Database";
            fDialog.Filter = "Input files (*.txt;*.docx)|*.txt;*.docx";
            fDialog.Multiselect = true;
            if (lastOpenPath == null)
            {
                fDialog.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath) + "\\Anatomy";
            }
            else
            {
                fDialog.InitialDirectory = lastOpenPath;
            }
            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                hsFilePath = fDialog.FileName.Substring(0, fDialog.FileName.LastIndexOf('.')) + ".HighScore";
                
                lastOpenPath = fDialog.FileName.Substring(0, fDialog.FileName.LastIndexOf('\\'));
                string[] filesToOpen = fDialog.FileNames;
                foreach (string s in filesToOpen)
                {
                    if (s.EndsWith("txt"))
                    {
                        AddEntriesFromTxt(s);
                    }
                    else if (s.EndsWith("docx"))
                    {
                        AddEntriesFromDocx(s);
                    }
                }
                if (entryList.Count > 1)
                {
                    hsFilePath = null;
                }
            }
        }

        private void AddEntriesFromTxt(string fileName)
        {
            TextReader tr = new StreamReader(fileName);
            string line;
            while ( (line = tr.ReadLine()) != null)
            {
                //check if line is valid: exactly 1 equals sign
                if (line.IndexOf('=') == line.LastIndexOf('=') && line.IndexOf('=') != -1)
                {
                    string firstStuff = line.Substring(0, line.IndexOf('='));
                    string secondStuff = line.Substring(line.IndexOf('=') + 1);

                    AddEntries(firstStuff, secondStuff);

                    string justFileName = fileName.Substring(0, fileName.LastIndexOf('.'));
                    justFileName = justFileName.Substring(justFileName.LastIndexOf('\\') + 1);
                    textBox1.Text = justFileName + " - Added";
                }
            }
        }

        private void AddEntriesFromDocx(string fileName)
        {
            // The following method was adapted from some copypasta from http://stackoverflow.com/questions/11240933/extract-table-from-docx
            StringBuilder result = new StringBuilder();
            WordprocessingDocument wordProcessingDoc = null;
            try
            {
                wordProcessingDoc = WordprocessingDocument.Open(fileName, true);
            }
            catch (IOException)
            {
                MessageBox.Show("Looks like another process (probably Microsoft Word) has document " + fileName + " open - close it and try again.");
                return;
            }

            IEnumerable<Paragraph> paragraphElement = wordProcessingDoc.MainDocumentPart.Document.Descendants<Paragraph>();

            foreach (OpenXmlElement section in wordProcessingDoc.MainDocumentPart.Document.Body.Elements<OpenXmlElement>())
            {
                if (section.GetType().Name == "Table")
                {
                    Table tab = (Table)section;
                    string[] text = new string[2];
                    foreach (TableRow row in tab.Descendants<TableRow>())
                    {
                        int columnIndex = 0;
                        foreach (TableCell cell in row.Descendants<TableCell>())
                        {
                            if (columnIndex > 1) break;

                            text[columnIndex] = cell.InnerText.Trim();
                            columnIndex++;
                        }
                        if (text[0] != null && text[1] != null && text[0].Length != 0 && text[1].Length != 0)
                        {
                            string firstStuff = text[0];
                            string secondStuff = text[1];
                            // treat anything aftere a # as a comment
                            if (!secondStuff.StartsWith("."))
                            {
                                int index = secondStuff.IndexOf("//");
                                if (index >= 0)
                                {
                                    secondStuff = secondStuff.Substring(0, index);
                                }
                                AddEntries(firstStuff, secondStuff);
                            }
                        }
                    }
                }
            }

            wordProcessingDoc.Close();
            string justFileName = fileName.Substring(0, fileName.LastIndexOf('.'));
            justFileName = justFileName.Substring(justFileName.LastIndexOf('\\') + 1);
            textBox1.Text = justFileName + " - Added";
        }

        private void AddEntries(string firstStuff, string secondStuff)
        {
            // TODO: implement questions with multiple answers, possibly plural structures
            /* Get the first elements */
            string[] firstElements = firstStuff.Split('|');

            /* Get the second elements */

            string[] secondElements = secondStuff.Split('|');

            entryList.Add(new Entry(firstElements, secondElements));
            
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            textBox2.Focus();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            activateButton();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            beginFancyTextInBox1();
        }

        private void beginFancyTextInBox1()
        {
            textBox1.Font = changeFontSize(textBox1.Font, MAX_QUESTION_FONT_SIZE);
            questionboxTimer.Enabled = true;
        }

        private System.Drawing.Font changeFontSize(System.Drawing.Font font, float size)
        { 
            return new System.Drawing.Font(font.Name, size, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont );
        }

        private void questionboxTimer_Tick(object sender, EventArgs e)
        {
            if (textBox1.Font.Size > MIN_QUESTION_FONT_SIZE)
            {
                textBox1.Font = changeFontSize(textBox1.Font, textBox1.Font.Size - FONT_SHRINK_SPEED);
            }
            else
            {
                textBox1.Font = changeFontSize(textBox1.Font, MIN_QUESTION_FONT_SIZE);
                questionboxTimer.Enabled = false;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            tick.Play();
            if (checkBox1.Checked)
            {
                educationalEnhancements = true;
            }
            else
            {
                educationalEnhancements = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string text = "You have answered " + (numCorrect + numIncorrect) + " questions.\r\n" + numCorrect + " answers were correct.";
            if (incorrectEntryIndexes.Count != 0)
            {
                text += "\r\nYou answered the following incorrectly:\r\n";
            }
            foreach(int i in incorrectEntryIndexes)
            {
                text += "\r\n" + entryList[i].getElementOne()[0];
            }
            MessageBox.Show(text);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            tick.Play();
            if (checkBox2.Checked)
            {
                useQuestionsAsAnswers = true;
            }
            else
            {
                useQuestionsAsAnswers = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Splash splash = new Splash();
            splash.Show();
        }

        private void refreshDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tick.Play();
            numCorrect = 0;
            numIncorrect = 0;
            points = 0;
            entryList.Clear();
            incorrectEntryIndexes.Clear();
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            button1.Font = new System.Drawing.Font(button1.Font, System.Drawing.FontStyle.Bold);
            button1.Text = "Start";
            button2.Text = points.ToString();
        }

        private void textBox2_Click(object sender, EventArgs e)
        {
            textBox2.SelectAll();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (!timer1.Enabled)
            {
                tick.Play();
            }
            if (checkBox3.Checked)
            {
                playWithPoints = true;
                points = 0;
                button2.Text = points.ToString();
            }
            else
            {
                playWithPoints = false;
                button2.Text = "";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.SelectAll();
            if (button3.Text == "Start Timed Game" && entryList.Count != 0)
            {
                educationalEnhancements = false;
                useQuestionsAsAnswers = false;
                playWithPoints = true;

                points = 0;
                button2.Text = points.ToString();
                checkBox1.Checked = false;
                checkBox1.Enabled = false;
                checkBox2.Checked = false;
                checkBox2.Enabled = false;
                checkBox3.Checked = true;
                checkBox3.Enabled = false;
                countdown = TIME_LIMIT;
                button3.Text = countdown.ToString();
                generateNextEntry();
                yep.Play();
                if (button1.Text == "Start")
                {
                    button1.Text = "Enter";
                }
                timer1.Enabled = true;
            }
            else
            {
                button3.Text = "Start Timed Game";
                timer1.Enabled = false;
            }
        }

        public void checkHighScores()
        {
            TextReader tr = new StreamReader(hsFilePath);
            List<string> highscores = new List<string>();
            string line = tr.ReadLine();
            while (line != null)
            {
                highscores.Add(line);
                line = tr.ReadLine();
            }
            tr.Close();
            bool inserted = false;
            for (int i = 0; i < highscores.Count; i++ )
            {
                if (this.points > Convert.ToInt32(highscores[i].Substring(highscores[i].LastIndexOf('=') + 1)))
                {
                    string name = hs.GetName();
                    highscores.Insert(i, name + "=" + points);
                    inserted = true;
                    break;
                }
            }
            if (highscores.Count < 5 && inserted == false)
            {
                string name = hs.GetName();
                highscores.Add(name + "=" + points);
            }
            if (highscores.Count > 5)
            {
                highscores.RemoveAt(highscores.Count - 1);
            }
            File.Delete(hsFilePath);
            File.Create(hsFilePath).Close();
            TextWriter tw = new StreamWriter(hsFilePath);
            foreach (string h in highscores)
            {
                tw.WriteLine(h);
            }
            tw.Close();
            String messageBoxString = "High Scores:\n";
            foreach(string s in highscores)
            {
                messageBoxString += s.Substring(0, s.IndexOf('=')).Replace("=", "") + " - ";
                messageBoxString += s.Substring(s.IndexOf('=') + 1).Replace("=", "") + "\n";
            }
            MessageBox.Show(messageBoxString);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            countdown--;
            if (countdown <= 0)
            {
                bool awarded = false;
                for (int i = yay.Count - 1; i >= 0; i--)
                {
                    if (points >= 50 * (i+1))
                    {
                        Splash award = new Splash();
                        try
                        {
                            Image img = Image.FromFile("zsrc\\yay" + (i+1) + ".jpg");
                            award.BackgroundImage = img;
                            award.Height = img.Height;
                            award.Width = img.Width;
                            award.timer1.Interval = 4000;
                            award.Show();
                            award.PlaySound(yay[i]);
                            awarded = true;
                            break;
                        }
                        catch (FileNotFoundException) {  }
                    }
                }
                if(awarded == false)
                {
                    try
                    {
                        Splash emptySplash = new Splash();
                        Image img = Image.FromFile("zsrc\\gameover.jpg");
                        emptySplash.BackgroundImage = img;
                        emptySplash.Width = img.Width;
                        emptySplash.Height = img.Height;
                        emptySplash.timer1.Interval = 1000;
                        emptySplash.Show();
                        emptySplash.PlaySound(ding);
                        
                    }
                    catch (FileNotFoundException) { }
                }
                button3.Text = "Start Timed Game";
                timer1.Enabled = false;
                
                checkBox1.Enabled = true;
                checkBox2.Enabled = true;
                checkBox3.Enabled = true;
                if (hsFilePath != null)
                {
                    bool refreshHighScores = false;
                    if (!File.Exists(hsFilePath))
                    {
                        File.Create(hsFilePath).Close();
                    }
                    TextReader tr = new StreamReader(hsFilePath);
                    string line;
                    List<string> highscores = new List<string>();
                    line = tr.ReadLine();
                    while (line != null)
                    {
                        highscores.Add(line);
                        line = tr.ReadLine();
                    }
                    tr.Close();
                    foreach (string s in highscores)
                    {
                        if (this.points > Convert.ToInt32(s.Substring(s.LastIndexOf('=') + 1)))
                        {
                            refreshHighScores = true;
                        }
                    }
                    if (highscores.Count < 5 && refreshHighScores == false)
                    {
                        refreshHighScores = true;
                    }
                    if (refreshHighScores)
                    {
                        hs.Show();
                    }
                }
            }
            else
            {
                button3.Text = countdown.ToString();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void activateButton()
        {
            if (entryList.Count == 0 || enterKeyDown)
            {
                return;
            }

            if (button1.Text == "Start")
            {
                yep.Play();
                button1.Font = new System.Drawing.Font(button1.Font, System.Drawing.FontStyle.Bold);
                button1.Text = "Enter";
                generateNextEntry();
            }
            else
            {
                submitAnswer();
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (!enterKeyDown && e.KeyData == Keys.Enter)
            {
                activateButton();
                enterKeyDown = true;
                e.SuppressKeyPress = true;
            }
            
        }

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            enterKeyDown = false;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void viewHighScoresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (hsFilePath != null)
            {
                TextReader tr = new StreamReader(hsFilePath);
                List<string> highscores = new List<string>();
                string line = tr.ReadLine();
                while (line != null)
                {
                    highscores.Add(line);
                    line = tr.ReadLine();
                }
                tr.Close();

                String messageBoxString = "High Scores:\n";
                foreach (string s in highscores)
                {
                    messageBoxString += s.Substring(0, s.IndexOf('=')).Replace("=", "") + " - ";
                    messageBoxString += s.Substring(s.IndexOf('=') + 1).Replace("=", "") + "\n";
                }
                MessageBox.Show(messageBoxString);
            }
            else
            {
                MessageBox.Show("High scores are only kept on single quizzes");
            }
        }
    }
}
