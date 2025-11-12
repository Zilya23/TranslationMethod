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
using TranslationMethod.Core;

namespace TranslationMethod.Pages
{
    /// <summary>
    /// Логика взаимодействия для Lab1Page.xaml
    /// </summary>
    public partial class Lab1Page : Page
    {
        private readonly LexicalAnalyzer _lexicalAnalyzer;
        public Lab1Page()
        {
            InitializeComponent();
            _lexicalAnalyzer = new LexicalAnalyzer();
        }

        private void btn_Start_Click(object sender, RoutedEventArgs e)
        {
            string sourceText = tb_SourceText.Text;

            if (sourceText.Any())
            {
                AnalysisResult result = _lexicalAnalyzer.Analyze(sourceText);

                tb_FinishText.Text = result.Message;
            }
            else
                MessageBox.Show("Введите текст!");
        }
    }
}
