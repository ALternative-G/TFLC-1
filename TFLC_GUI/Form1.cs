using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TFLC_GUI
{
    public partial class Form1 : Form
    {
        // Класс для хранения информации о файле на вкладке
        private class FileTabInfo
        {
            public string FilePath { get; set; } = string.Empty;
            public bool IsChanged { get; set; } = false;
            public RichTextBox TextBox { get; set; }
            public RichTextBox LineNumberBox { get; set; }
            public TabPage TabPage { get; set; }
        }

        private Dictionary<TabPage, FileTabInfo> fileTabs = new Dictionary<TabPage, FileTabInfo>();
        private float currentFontSize = 10f;
        private bool isEnglish = false;
        private DateTime startTime;

        // Словарь для хранения переводов
        private Dictionary<string, string> ruEnDictionary = new Dictionary<string, string>();

        public Form1()
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(Form1_KeyDown);
            this.KeyPreview = true;
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            startTime = DateTime.Now;
            InitializeTranslationDictionary();

            InitializeFirstTab();

            UpdateStatus("Программа запущена");

            // Событие смены вкладки
            tabControlFile.SelectedIndexChanged += TabControlFile_SelectedIndexChanged;
        }

        private void InitializeFirstTab()
        {
            var tabInfo = new FileTabInfo
            {
                TextBox = richTextBox1,
                LineNumberBox = richTextBox_scroll1,
                TabPage = tabPage1,
                FilePath = string.Empty,
                IsChanged = false
            };

            fileTabs[tabPage1] = tabInfo;

            // Ожидание событий обработки вкладки
            richTextBox1.TextChanged += RichTextBox_TextChanged;
            richTextBox1.SelectionChanged += RichTextBox_SelectionChanged;
            richTextBox1.VScroll += RichTextBox_VScroll;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateFontSize();
        }

        #region Управление вкладками

        private void AddNewTab(string filePath = "")
        {
            TabPage newTabPage = new TabPage();
            newTabPage.Text = GetTranslation("Файл") + " " + (tabControlFile.TabCount + 1);
            newTabPage.Padding = new Padding(3);
            newTabPage.UseVisualStyleBackColor = true;

            // RichTextBox для номеров строк
            RichTextBox lineNumberBox = new RichTextBox();
            lineNumberBox.BorderStyle = BorderStyle.FixedSingle;
            lineNumberBox.Dock = DockStyle.Left;
            lineNumberBox.Name = "richTextBox_scroll_" + (tabControlFile.TabCount + 1);
            lineNumberBox.ReadOnly = true;
            lineNumberBox.ScrollBars = RichTextBoxScrollBars.None;
            lineNumberBox.Size = new Size(80, 441);
            lineNumberBox.Font = new Font(lineNumberBox.Font.FontFamily, currentFontSize);
            lineNumberBox.Text = "1\n2\n3\n4\n5";

            // RichTextBox для текста
            RichTextBox textBox = new RichTextBox();
            textBox.Dock = DockStyle.Fill;
            textBox.Name = "richTextBox_" + (tabControlFile.TabCount + 1);
            textBox.Size = new Size(2110, 441);
            textBox.Font = new Font(textBox.Font.FontFamily, currentFontSize);

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    textBox.Text = File.ReadAllText(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{GetTranslation("Ошибка при открытии файла")}: {ex.Message}",
                        GetTranslation("Ошибка"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            newTabPage.Controls.Add(textBox);
            newTabPage.Controls.Add(lineNumberBox);

            tabControlFile.TabPages.Add(newTabPage);

            var tabInfo = new FileTabInfo
            {
                TextBox = textBox,
                LineNumberBox = lineNumberBox,
                TabPage = newTabPage,
                FilePath = filePath,
                IsChanged = false
            };

            fileTabs[newTabPage] = tabInfo;

            textBox.TextChanged += RichTextBox_TextChanged;
            textBox.SelectionChanged += RichTextBox_SelectionChanged;
            textBox.VScroll += RichTextBox_VScroll;

            tabControlFile.SelectedTab = newTabPage;

            UpdateLineNumbers(textBox, lineNumberBox);

            UpdateTitle();
        }

        private void CloseCurrentTab()
        {
            if (tabControlFile.TabCount <= 1)
            {
                // Если это последняя вкладка то происходит очистка
                var currentTab = tabControlFile.SelectedTab;
                var tabInfo = fileTabs[currentTab];

                if (tabInfo.IsChanged)
                {
                    if (!ConfirmSaveChanges(tabInfo))
                        return;
                }

                tabInfo.TextBox.Clear();
                tabInfo.FilePath = string.Empty;
                tabInfo.IsChanged = false;
                tabInfo.TabPage.Text = GetTranslation("Файл") + " 1";
                UpdateLineNumbers(tabInfo.TextBox, tabInfo.LineNumberBox);
            }
            else
            {
                var currentTab = tabControlFile.SelectedTab;
                var tabInfo = fileTabs[currentTab];

                if (tabInfo.IsChanged)
                {
                    if (!ConfirmSaveChanges(tabInfo))
                        return;
                }

                tabInfo.TextBox.TextChanged -= RichTextBox_TextChanged;
                tabInfo.TextBox.SelectionChanged -= RichTextBox_SelectionChanged;
                tabInfo.TextBox.VScroll -= RichTextBox_VScroll;

                fileTabs.Remove(currentTab);
                tabControlFile.TabPages.Remove(currentTab);
            }

            UpdateTitle();
        }

        private bool ConfirmSaveChanges(FileTabInfo tabInfo)
        {
            string fileName = string.IsNullOrEmpty(tabInfo.FilePath) ?
                (isEnglish ? "Untitled" : "Безымянный") :
                Path.GetFileName(tabInfo.FilePath);

            DialogResult result = MessageBox.Show(
                string.Format(isEnglish ?
                    "File '{0}' has been changed. Save changes?" :
                    "Файл '{0}' был изменен. Сохранить изменения?", fileName),
                isEnglish ? "Warning" : "Предупреждение",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                return SaveCurrentFile();
            }
            else if (result == DialogResult.No)
            {
                return true;
            }

            return false;
        }

        private void TabControlFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTitle();
            UpdateStatus(GetTranslation("Переключение на вкладку") +
                $": {tabControlFile.SelectedTab.Text}");

            
            //HighlightSyntax(currentTabInfo.TextBox);
        }

        private FileTabInfo GetCurrentTabInfo()
        {
            if (tabControlFile.SelectedTab != null &&
                fileTabs.ContainsKey(tabControlFile.SelectedTab))
            {
                return fileTabs[tabControlFile.SelectedTab];
            }
            return null;
        }

        #endregion

        #region Обработчики событий RichTextBox

        private void RichTextBox_TextChanged(object sender, EventArgs e)
        {
            var textBox = sender as RichTextBox;
            if (textBox != null)
            {
                foreach (var tabInfo in fileTabs.Values)
                {
                    if (tabInfo.TextBox == textBox)
                    {
                        tabInfo.IsChanged = true;
                        tabInfo.TabPage.Text = GetFileNameForTab(tabInfo) + "*";

                        UpdateLineNumbers(tabInfo.TextBox, tabInfo.LineNumberBox);
                        //HighlightSyntax(tabInfo.TextBox);

                        if (tabInfo == GetCurrentTabInfo())
                        {
                            UpdateTitle();
                        }
                        break;
                    }
                }
            }
        }

        private void RichTextBox_SelectionChanged(object sender, EventArgs e)
        {
            var textBox = sender as RichTextBox;
            if (textBox != null)
            {
                foreach (var tabInfo in fileTabs.Values)
                {
                    if (tabInfo.TextBox == textBox)
                    {
                        SyncLineNumbers(tabInfo.TextBox, tabInfo.LineNumberBox);
                        break;
                    }
                }
            }
        }

        private void RichTextBox_VScroll(object sender, EventArgs e)
        {
            var textBox = sender as RichTextBox;
            if (textBox != null)
            {
                foreach (var tabInfo in fileTabs.Values)
                {
                    if (tabInfo.TextBox == textBox)
                    {
                        SyncLineNumbers(tabInfo.TextBox, tabInfo.LineNumberBox);
                        break;
                    }
                }
            }
        }

        #endregion

        #region Работа с файлами

        private bool SaveCurrentFile()
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo == null) return false;

            if (string.IsNullOrEmpty(currentTabInfo.FilePath))
            {
                return SaveFileAs(currentTabInfo);
            }
            else
            {
                try
                {
                    File.WriteAllText(currentTabInfo.FilePath, currentTabInfo.TextBox.Text);
                    currentTabInfo.IsChanged = false;
                    currentTabInfo.TabPage.Text = Path.GetFileName(currentTabInfo.FilePath);
                    UpdateTitle();
                    UpdateStatus(GetTranslation("Файл сохранен") + $": {Path.GetFileName(currentTabInfo.FilePath)}");
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{GetTranslation("Ошибка при сохранении файла")}: {ex.Message}",
                        GetTranslation("Ошибка"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }

        private bool SaveFileAs(FileTabInfo tabInfo)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveFileDialog.FileName, tabInfo.TextBox.Text);
                        tabInfo.FilePath = saveFileDialog.FileName;
                        tabInfo.IsChanged = false;
                        tabInfo.TabPage.Text = Path.GetFileName(tabInfo.FilePath);

                        if (tabInfo == GetCurrentTabInfo())
                        {
                            UpdateTitle();
                        }

                        UpdateStatus(GetTranslation("Файл сохранен как") + $": {Path.GetFileName(tabInfo.FilePath)}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{GetTranslation("Ошибка при сохранении файла")}: {ex.Message}",
                            GetTranslation("Ошибка"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
            return false;
        }

        private void OpenFileInCurrentTab(string filePath)
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo == null) return;

            try
            {
                currentTabInfo.TextBox.Text = File.ReadAllText(filePath);
                currentTabInfo.FilePath = filePath;
                currentTabInfo.IsChanged = false;
                currentTabInfo.TabPage.Text = Path.GetFileName(filePath);

                UpdateTitle();
                //HighlightSyntax(currentTabInfo.TextBox);
                UpdateStatus(GetTranslation("Файл загружен") + $": {Path.GetFileName(currentTabInfo.FilePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{GetTranslation("Ошибка при открытии файла")}: {ex.Message}",
                    GetTranslation("Ошибка"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenFileInNewTab(string filePath)
        {
            AddNewTab(filePath);
        }

        #endregion

        #region Вспомогательные методы

        private void UpdateLineNumbers(RichTextBox textBox, RichTextBox lineNumberBox)
        {
            string lineLabels = "";
            for (int i = 0; i < textBox.Lines.Count(); i++)
            {
                lineLabels = lineLabels + (i + 1).ToString() + ".\n";
            }
            lineNumberBox.Text = lineLabels;
        }

        private void SyncLineNumbers(RichTextBox source, RichTextBox lineNumberBox)
        {
            int firstVisibleLine = source.GetLineFromCharIndex(
                source.GetCharIndexFromPosition(new Point(0, 0)));

            if (firstVisibleLine != 0)
            {
                firstVisibleLine += 1;
            }

            int charIndex = lineNumberBox.GetFirstCharIndexFromLine(firstVisibleLine);
            if (charIndex >= 0)
            {
                lineNumberBox.SelectionStart = charIndex;
                lineNumberBox.SelectionLength = 0;
                lineNumberBox.ScrollToCaret();
            }
        }

        private string GetFileNameForTab(FileTabInfo tabInfo)
        {
            if (string.IsNullOrEmpty(tabInfo.FilePath))
            {
                return GetTranslation("Файл") + " " +
                    (tabControlFile.TabPages.IndexOf(tabInfo.TabPage) + 1);
            }
            return Path.GetFileName(tabInfo.FilePath);
        }

        private void HighlightSyntax(RichTextBox textBox)
        {
            if (textBox.Text.Length == 0) return;

            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            textBox.SuspendLayout();

            string text = textBox.Text;

            textBox.SelectAll();
            textBox.SelectionColor = Color.Black;

            string[] keywords = { "if", "for", "while", "else", "switch", "case",
                                 "break", "continue", "return", "class", "public",
                                 "private", "protected", "static", "void", "int",
                                 "string", "bool", "float", "double", "char" };

            string pattern = @"\b(" + string.Join("|", keywords) + @")\b";
            MatchCollection matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                textBox.Select(match.Index, match.Length);
                textBox.SelectionColor = Color.Blue;
            }

            textBox.Select(selectionStart, selectionLength);
            textBox.SelectionColor = Color.Black;

            textBox.ResumeLayout();
        }

        private void UpdateFontSize()
        {
            foreach (var tabInfo in fileTabs.Values)
            {
                if (tabInfo.TextBox != null)
                {
                    tabInfo.TextBox.Font = new Font(tabInfo.TextBox.Font.FontFamily, currentFontSize);
                }
                if (tabInfo.LineNumberBox != null)
                {
                    tabInfo.LineNumberBox.Font = new Font(tabInfo.LineNumberBox.Font.FontFamily, currentFontSize);
                }
            }
        }

        private void UpdateTitle()
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo == null) return;

            string fileName = GetFileNameForTab(currentTabInfo);
            string title = isEnglish ? "Text Editor" : "Текстовый редактор";
            this.Text = $"{title} - {fileName}{(currentTabInfo.IsChanged ? "*" : "")}";
        }

        private bool CheckUnsavedChanges()
        {
            foreach (var tabInfo in fileTabs.Values)
            {
                if (tabInfo.IsChanged)
                {
                    string fileName = GetFileNameForTab(tabInfo);
                    DialogResult result = MessageBox.Show(
                        string.Format(isEnglish ?
                            "File '{0}' has been changed. Save changes?" :
                            "Файл '{0}' был изменен. Сохранить изменения?", fileName),
                        isEnglish ? "Warning" : "Предупреждение",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // Переключение на вкладку с измененным файлом
                        tabControlFile.SelectedTab = tabInfo.TabPage;
                        if (!SaveCurrentFile())
                            return false;
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #region Перетаскивание файлов

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                if ((ModifierKeys & Keys.Control) == Keys.Control)
                {
                    foreach (string file in files)
                    {
                        OpenFileInNewTab(file);
                    }
                }
                else
                {
                    DialogResult result = MessageBox.Show(
                        isEnglish ?
                            "Open in new tab?" :
                            "Открыть в новой вкладке?",
                        isEnglish ? "Question" : "Вопрос",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        foreach (string file in files)
                        {
                            OpenFileInNewTab(file);
                        }
                    }
                    else if (result == DialogResult.No && files.Length > 0)
                    {
                        OpenFileInCurrentTab(files[0]);
                    }
                }
            }
        }

        #endregion

        #region Локализация

        private void InitializeTranslationDictionary()
        {
            ruEnDictionary.Add("Меню", "File");
            ruEnDictionary.Add("Создать", "New");
            ruEnDictionary.Add("Открыть", "Open");
            ruEnDictionary.Add("Сохранить", "Save");
            ruEnDictionary.Add("Сохранить как", "Save As");
            ruEnDictionary.Add("Выход", "Exit");

            ruEnDictionary.Add("Правка", "Edit");
            ruEnDictionary.Add("Отменить", "Undo");
            ruEnDictionary.Add("Повторить", "Redo");
            ruEnDictionary.Add("Вырезать", "Cut");
            ruEnDictionary.Add("Копировать", "Copy");
            ruEnDictionary.Add("Вставить", "Paste");
            ruEnDictionary.Add("Удалить", "Delete");
            ruEnDictionary.Add("Выделить все", "Select All");

            ruEnDictionary.Add("Текст", "Text");
            ruEnDictionary.Add("Постановка задачи", "Problem Statement");
            ruEnDictionary.Add("Грамматика", "Grammar");
            ruEnDictionary.Add("Классификация грамматики", "Grammar Classification");
            ruEnDictionary.Add("Метод анализа", "Analysis Method");
            ruEnDictionary.Add("Тестовый пример", "Test Example");
            ruEnDictionary.Add("Список литературы", "References");
            ruEnDictionary.Add("Исходный код программы", "Source Code");

            ruEnDictionary.Add("Пуск", "Run");

            ruEnDictionary.Add("Справка", "Help");
            ruEnDictionary.Add("Вызов справки", "Help Contents");
            ruEnDictionary.Add("О программе", "About");

            ruEnDictionary.Add("Локализация", "Localization");

            ruEnDictionary.Add("Вид", "View");
            ruEnDictionary.Add("Увеличить шрифт", "Increase Font");
            ruEnDictionary.Add("Уменьшить шрифт", "Decrease Font");
            ruEnDictionary.Add("Сбросить шрифт", "Reset Font");

            ruEnDictionary.Add("Сканер", "Scanner");
            ruEnDictionary.Add("Парсер", "Parser");
            ruEnDictionary.Add("Оптимизация", "Optimization");
            ruEnDictionary.Add("Файл 1", "File 1");
            ruEnDictionary.Add("Файл 2", "File 2");
            ruEnDictionary.Add("Новая вкладка", "New Tab");
            ruEnDictionary.Add("Закрыть вкладку", "Close Tab");

            ruEnDictionary.Add("Статус", "Status");
            ruEnDictionary.Add("Файл", "File");
            ruEnDictionary.Add("Изменен", "Modified");

            ruEnDictionary.Add("Позиция", "Position");
            ruEnDictionary.Add("Код", "Code");
            ruEnDictionary.Add("Ошибка", "Error");
        }

        private void localrusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isEnglish = false;
            ApplyLocalization();
            localrusToolStripMenuItem.Checked = true;
            localengToolStripMenuItem.Checked = false;
            UpdateStatus("Язык изменен на русский");
        }

        private void localengToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isEnglish = true;
            ApplyLocalization();
            localrusToolStripMenuItem.Checked = false;
            localengToolStripMenuItem.Checked = true;
            UpdateStatus("Language changed to English");
        }

        private void ApplyLocalization()
        {
            fileToolStripMenuItem.Text = GetTranslation("Меню");
            correctToolStripMenuItem.Text = GetTranslation("Правка");
            textToolStripMenuItem.Text = GetTranslation("Текст");
            launchToolStripMenuItem.Text = GetTranslation("Пуск");
            infoToolStripMenuItem.Text = GetTranslation("Справка");
            lacalToolStripMenuItem.Text = GetTranslation("Локализация");
            viewToolStripMenuItem.Text = GetTranslation("Вид");

            createToolStripMenuItem.Text = GetTranslation("Создать");
            openToolStripMenuItem.Text = GetTranslation("Открыть");
            saveToolStripMenuItem.Text = GetTranslation("Сохранить");
            saveasToolStripMenuItem.Text = GetTranslation("Сохранить как");
            exitToolStripMenuItem.Text = GetTranslation("Выход");

            cancelToolStripMenuItem.Text = GetTranslation("Отменить");
            repeatToolStripMenuItem.Text = GetTranslation("Повторить");
            cutToolStripMenuItem.Text = GetTranslation("Вырезать");
            copyToolStripMenuItem.Text = GetTranslation("Копировать");
            pasteToolStripMenuItem.Text = GetTranslation("Вставить");
            deleteToolStripMenuItem.Text = GetTranslation("Удалить");
            selectallToolStripMenuItem.Text = GetTranslation("Выделить все");

            taskToolStripMenuItem.Text = GetTranslation("Постановка задачи");
            grammarToolStripMenuItem.Text = GetTranslation("Грамматика");
            grammarclassToolStripMenuItem.Text = GetTranslation("Классификация грамматики");
            analysismethodToolStripMenuItem.Text = GetTranslation("Метод анализа");
            testexampleToolStripMenuItem.Text = GetTranslation("Тестовый пример");
            literatureToolStripMenuItem.Text = GetTranslation("Список литературы");
            sourcecodeToolStripMenuItem.Text = GetTranslation("Исходный код программы");

            callinfoToolStripMenuItem.Text = GetTranslation("Вызов справки");
            aboutToolStripMenuItem.Text = GetTranslation("О программе");

            increaseFontToolStripMenuItem.Text = GetTranslation("Увеличить шрифт");
            decreaseFontToolStripMenuItem.Text = GetTranslation("Уменьшить шрифт");
            resetFontToolStripMenuItem.Text = GetTranslation("Сбросить шрифт");

            tabPage1.Text = GetTranslation("Файл 1");
            tabPageScanner.Text = GetTranslation("Сканер");
            tabPageParcer.Text = GetTranslation("Парсер");
            tabPageOptimisation.Text = GetTranslation("Оптимизация");

            ColumnFile1.HeaderText = GetTranslation("Файл");
            ColumnPos1.HeaderText = GetTranslation("Позиция");
            ColumnCode1.HeaderText = GetTranslation("Код");
            ColumnError1.HeaderText = GetTranslation("Ошибка");

            ColumnFile2.HeaderText = GetTranslation("Файл");
            ColumnPos2.HeaderText = GetTranslation("Позиция");
            ColumnCode2.HeaderText = GetTranslation("Код");
            ColumnError2.HeaderText = GetTranslation("Ошибка");

            ColumnFile3.HeaderText = GetTranslation("Файл");
            ColumnPos3.HeaderText = GetTranslation("Позиция");
            ColumnCode3.HeaderText = GetTranslation("Код");
            ColumnError3.HeaderText = GetTranslation("Ошибка");

            foreach (var tabInfo in fileTabs.Values)
            {
                tabInfo.TabPage.Text = GetFileNameForTab(tabInfo);
            }
            UpdateTitle();
            UpdateStatus(GetTranslation("Язык изменен"));
        }

        private string GetTranslation(string russianText)
        {
            if (isEnglish && ruEnDictionary.ContainsKey(russianText))
                return ruEnDictionary[russianText];
            return russianText;
        }

        #endregion

        #region Обработчики меню

        private void CreateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddNewTab();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (openFileDialog.FileNames.Length > 1)
                    {
                        // Если выбрано несколько файлов, каждый открывается в новой вкладке
                        foreach (string file in openFileDialog.FileNames)
                        {
                            OpenFileInNewTab(file);
                        }
                    }
                    else
                    {
                        DialogResult result = MessageBox.Show(
                            isEnglish ?
                                "Open in new tab?" :
                                "Открыть в новой вкладке?",
                            isEnglish ? "Question" : "Вопрос",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            OpenFileInNewTab(openFileDialog.FileName);
                        }
                        else
                        {
                            OpenFileInCurrentTab(openFileDialog.FileName);
                        }
                    }
                }
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCurrentFile();
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo != null)
            {
                SaveFileAs(currentTabInfo);
            }
        }

        private void CloseTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseCurrentTab();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        #region Меню "Правка"

        private void CancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo?.TextBox != null && currentTabInfo.TextBox.CanUndo)
            {
                currentTabInfo.TextBox.Undo();
                UpdateStatus(GetTranslation("Действие отменено"));
            }
        }

        private void RepeatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo?.TextBox != null && currentTabInfo.TextBox.CanRedo)
            {
                currentTabInfo.TextBox.Redo();
                UpdateStatus(GetTranslation("Действие повторено"));
            }
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo?.TextBox != null && currentTabInfo.TextBox.SelectedText.Length > 0)
            {
                currentTabInfo.TextBox.Cut();
                UpdateStatus(GetTranslation("Текст вырезан"));
            }
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo?.TextBox != null && currentTabInfo.TextBox.SelectedText.Length > 0)
            {
                currentTabInfo.TextBox.Copy();
                UpdateStatus(GetTranslation("Текст скопирован"));
            }
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo?.TextBox != null && Clipboard.ContainsText())
            {
                currentTabInfo.TextBox.Paste();
                UpdateStatus(GetTranslation("Текст вставлен"));
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo?.TextBox != null && currentTabInfo.TextBox.SelectedText.Length > 0)
            {
                int selectionStart = currentTabInfo.TextBox.SelectionStart;
                int selectionLength = currentTabInfo.TextBox.SelectionLength;
                currentTabInfo.TextBox.Text = currentTabInfo.TextBox.Text.Remove(selectionStart, selectionLength);
                currentTabInfo.TextBox.SelectionStart = selectionStart;
                UpdateStatus(GetTranslation("Текст удален"));
            }
        }

        private void SelectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo?.TextBox != null)
            {
                currentTabInfo.TextBox.SelectAll();
                UpdateStatus(GetTranslation("Весь текст выделен"));
            }
        }

        #endregion

        #region Изменение размера шрифта

        private void IncreaseFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentFontSize += 2;
            if (currentFontSize > 72) currentFontSize = 72;
            UpdateFontSize();
            UpdateStatus(GetTranslation("Размер шрифта увеличен") + $": {currentFontSize}pt");
        }

        private void DecreaseFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentFontSize -= 2;
            if (currentFontSize < 6) currentFontSize = 6;
            UpdateFontSize();
            UpdateStatus(GetTranslation("Размер шрифта уменьшен") + $": {currentFontSize}pt");
        }

        private void ResetFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentFontSize = 10f;
            UpdateFontSize();
            UpdateStatus(GetTranslation("Размер шрифта сброшен") + $": {currentFontSize}pt");
        }

        #endregion

        #region Меню "Справка"

        private void CallInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string helpText = isEnglish ?
                "ABOUT:\n" +
                "Multi-tab text editor with syntax highlighting, multiple file support,\n" +
                "and localization features.\n\n" +

                "FILE MENU:\n" +
                "  - New File (Ctrl+N)         - Creates a new empty text file in current tab\n" +
                "  - New Tab (Ctrl+Shift+T)    - Creates a new empty tab\n" +
                "  - Open (Ctrl+O)             - Opens file(s) - multiple selection opens in new tabs\n" +
                "  - Save (Ctrl+S)             - Saves current changes to the open file\n" +
                "  - Save As                   - Saves current text to a new file\n" +
                "  - Close Tab (Ctrl+W)        - Closes current tab (with save confirmation)\n" +
                "  - Exit (Alt+F4)             - Closes the program (checks all tabs for unsaved changes)\n\n" +

                "EDIT MENU:\n" +
                "  - Undo (Ctrl+Z)             - Undoes the last action\n" +
                "  - Redo (Ctrl+Y)             - Redoes a previously undone action\n" +
                "  - Cut (Ctrl+X)              - Removes selected text and places it in clipboard\n" +
                "  - Copy (Ctrl+C)             - Copies selected text to clipboard\n" +
                "  - Paste (Ctrl+V)            - Pastes text from clipboard\n" +
                "  - Delete (Ctrl+D)           - Deletes selected text without saving to clipboard\n" +
                "  - Select All (Ctrl+A)       - Selects all text in the active tab\n\n" +

                "VIEW MENU:\n" +
                "  - Increase Font             - Increases font size (max 72pt)\n" +
                "  - Decrease Font             - Decreases font size (min 6pt)\n" +
                "  - Reset Font                - Resets font to default size (10pt)\n\n" +

                "LOCALIZATION MENU:\n" +
                "  - Русский  - Switch interface to Russian\n" +
                "  - English  - Switch interface to English\n\n" +

                "ADDITIONAL FEATURES:\n" +
                "  - Drag & Drop Support            - Drag files into the window\n" +
                "  - Normal drop                    - Ask to open in new/current tab\n" +
                "  - Ctrl+drag                      - Automatically open in new tab\n" +
                "  - Syntax Highlighting            - Automatic highlighting of keywords\n" +
                "  - Line Numbers                   - Each tab has its own line number panel\n" +
                "  - Multiple Files                 - Work with several files simultaneously\n" +
                "  - Unsaved Changes Indicator      - Asterisk (*) shows modified files\n" +
                "  - Tab Switching                  - Click tabs to switch between files\n" +
                "  - Status Bar                     - Shows current operations and file info\n\n" +

                "STATUS BAR INFORMATION:\n" +
                "  - Current operation status\n" +
                "  - File name and modification status\n" +
                "  - Language indicator\n\n" :


                "О ПРОГРАММЕ:\n" +
                "Текстовый редактор с подсветкой синтаксиса, поддержкой\n" +
                "нескольких файлов и функцией локализации интерфейса.\n\n" +

                "МЕНЮ \"ФАЙЛ\":\n" +
                "  - Создать (Ctrl+N)             - Создает новый пустой файл в текущей вкладке\n" +
                "  - Новая вкладка (Ctrl+Shift+T) - Создает новую пустую вкладку\n" +
                "  - Открыть (Ctrl+O)             - Открывает файл(ы) - множественный выбор открывает в новых вкладках\n" +
                "  - Сохранить (Ctrl+S)           - Сохраняет текущие изменения в открытом файле\n" +
                "  - Сохранить как                - Сохраняет текущий текст в новый файл\n" +
                "  - Закрыть вкладку (Ctrl+W)     - Закрывает текущую вкладку (с подтверждением сохранения)\n" +
                "  - Выход (Alt+F4)               - Закрывает программу (проверяет все вкладки на несохраненные изменения)\n\n" +

                "МЕНЮ \"ПРАВКА\":\n" +
                "  - Отменить (Ctrl+Z)            - Отменяет последнее действие\n" +
                "  - Повторить (Ctrl+Y)           - Повторяет ранее отмененное действие\n" +
                "  - Вырезать (Ctrl+X)            - Удаляет выделенный текст и помещает его в буфер обмена\n" +
                "  - Копировать (Ctrl+C)          - Копирует выделенный текст в буфер обмена\n" +
                "  - Вставить (Ctrl+V)            - Вставляет текст из буфера обмена\n" +
                "  - Удалить (Ctrl+D)             - Удаляет выделенный текст без сохранения в буфер\n" +
                "  - Выделить всё (Ctrl+A)        - Выделяет весь текст в активной вкладке\n\n" +

                "МЕНЮ \"ВИД\":\n" +
                "  - Увеличить шрифт              - Увеличивает размер шрифта (макс. 72pt)\n" +
                "  - Уменьшить шрифт              - Уменьшает размер шрифта (мин. 6pt)\n" +
                "  - Сбросить шрифт               - Возвращает стандартный размер шрифта (10pt)\n\n" +

                "МЕНЮ \"ЛОКАЛИЗАЦИЯ\":\n" +
                "  - Русский  - Переключить интерфейс на русский язык\n" +
                "  - English  - Переключить интерфейс на английский язык\n\n" +

                "ДОПОЛНИТЕЛЬНЫЕ ВОЗМОЖНОСТИ:\n" +
                "  - Поддержка Drag & Drop           - Перетащите файлы в окно программы\n" +
                "  - Обычное перетаскивание          - запрос на открытие в новой/текущей вкладке\n" +
                "  - Ctrl+перетаскивание             - автоматическое открытие в новой вкладке\n" +
                "  - Подсветка синтаксиса            - Автоматическая подсветка ключевых слов\n" +
                "  - Номера строк                    - Каждая вкладка имеет свою панель нумерации\n" +
                "  - Множественные файлы             - Работа с несколькими файлами одновременно\n" +
                "  - Индикатор изменений             - Звездочка (*) показывает измененные файлы\n" +
                "  - Переключение вкладок            - Клик по вкладке для переключения между файлами\n" +
                "  - Строка состояния                - Показывает текущие операции и информацию о файле\n\n" +

                "ИНФОРМАЦИЯ В СТРОКЕ СОСТОЯНИЯ:\n" +
                "  - Текущий статус операции\n" +
                "  - Имя файла и статус изменений\n" +
                "  - Индикатор текущего языка\n\n";

            MessageBox.Show(helpText, GetTranslation("Справка"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string aboutText = isEnglish ?
                "Text Editor\n\n" +
                "Version: 0.1.0\n" +
                "Developer: Alan Arifullin, NSTU, AP-327\n" +
                "Year: 2026\n\n" +
                "An application for editing text files with support for custom code analyzers." :

                "Текстовый редактор\n\n" +
                "Версия: 0.1.0\n" +
                "Разработчик: Алан Арифуллин, НГТУ, АП-327\n" +
                "Год выпуска: 2026\n\n" +
                "Приложение для редактирования текстовых файлов с поддержкой кастомных анализаторов кода.";

            MessageBox.Show(aboutText, GetTranslation("О программе"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        private void UpdateStatus(string message)
        {
            statuslabel.Text = $"{(isEnglish ? "Status" : "Статус")}: {message}";
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.D)
            {
                DeleteToolStripMenuItem_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.T)
            {
                AddNewTab();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                CloseCurrentTab();
                e.Handled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!CheckUnsavedChanges())
            {
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        private void launchStripMenuItem_Click(object sender, EventArgs e)
        {
            // Подсветка синтаксиса для активной вкладки
            var currentTabInfo = GetCurrentTabInfo();
            if (currentTabInfo != null)
            {
                HighlightSyntax(currentTabInfo.TextBox);
            }
        }
    }
}