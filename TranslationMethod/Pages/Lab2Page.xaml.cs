using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TranslationMethod.Core2;

namespace TranslationMethod.Pages
{
    /// <summary>
    /// Логика взаимодействия для Lab2Page.xaml
    /// </summary>
    public partial class Lab2Page : Page
    {
        private readonly LexicalAnalyzer _lexicalAnalyzer;
        public Lab2Page()
        {
            InitializeComponent();
            _lexicalAnalyzer = new LexicalAnalyzer();
        }

        private void btn_Start_Click(object sender, RoutedEventArgs e)
        {
            string sourceText = tb_SourceText.Text;

            if (sourceText.Any())
            {
                try
                {
                    StringBuilder debugOutput = new StringBuilder();
                    LexicalAnalyzer analyzer = new LexicalAnalyzer();

                    AnalysisResult result = _lexicalAnalyzer.Analyze(sourceText);

                    if (result.IsSuccess)
                        debugOutput.AppendLine(result.Message);
                    else
                        debugOutput.AppendLine(result.Message);

                    tb_FinishText.Text = debugOutput.ToString();
                }
                catch (LexicalException ex)
                {
                    tb_FinishText.Text = $"Лексическая ошибка: {ex.Message} (строка {ex.Line}, столбец {ex.Column})";
                }
            }
            else
                MessageBox.Show("Введите текст!");
        }
    }
}
