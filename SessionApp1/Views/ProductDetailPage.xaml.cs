using SessionApp1.Models;
using SessionApp1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;


namespace SessionApp1.Views
{
    public partial class ProductDetailPage : Page
    {
        private readonly DatabaseService _databaseService;
        private readonly MaterialAccountingService _materialService;
        private ManufacturedGood _currentProduct;
        private List<ProductSpecificationViewModel> _currentSpecification;
        private List<DateTime> _specificationDates;
        private User? _currentUser;

        public ProductDetailPage(string productArticle, User? currentUser = null)
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _materialService = new MaterialAccountingService();
            _currentUser = currentUser;
            
            // Настраиваем доступность элементов в зависимости от роли пользователя
            ConfigureUIForUserRole();
            
            LoadProductDetails(productArticle);
        }
        
        private void ConfigureUIForUserRole()
        {
            // Если пользователь - заказчик (роль = 3) или пользователь не указан, скрываем кнопку печати спецификации
            if (_currentUser == null || _currentUser.RoleId == 3) // 3 - роль заказчика
            {
                PrintSpecificationButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void LoadProductDetails(string productArticle)
        {
            try
            {
                // Загрузка информации об изделии
                var products = await _databaseService.GetManufacturedGoodsAsync();
                _currentProduct = products.FirstOrDefault(p => p.Article == productArticle);

                if (_currentProduct == null)
                {
                    MessageBox.Show("Изделие не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Отображение информации об изделии
                DisplayProductInfo();

                // Загрузка истории спецификаций
                await LoadSpecificationHistory(productArticle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayProductInfo()
        {
            // Привязка данных к элементам интерфейса
            DataContext = _currentProduct;
            ArticleTextBlock.Text = _currentProduct.Article;
            NameTextBlock.Text = _currentProduct.Name;
            DimensionsTextBlock.Text = $"{_currentProduct.WidthMm} × {_currentProduct.LengthMm} мм";
            UnitTextBlock.Text = _currentProduct.Unit;
            PriceTextBlock.Text = _currentProduct.Price.ToString("C");
            CommentTextBlock.Text = _currentProduct.Comment;
            
            // Дополнительная информация (инструкции по уходу и т.д.)
            // В реальном приложении эти данные могут быть загружены из базы данных
            InstructionsTextBlock.Text = GetCareInstructions(_currentProduct);
        }

        private string GetCareInstructions(ManufacturedGood product)
        {
            // Пример генерации инструкций по уходу в зависимости от типа изделия
            // В реальном приложении эти данные должны быть в базе данных
            if (product.Name.Contains("Полотенце") || product.Name.Contains("полотенце"))
            {
                return "Машинная стирка при 40°C. Не отбеливать. Гладить при низкой температуре. Не подвергать химической чистке.";
            }
            else if (product.Name.Contains("Плед") || product.Name.Contains("плед"))
            {
                return "Машинная стирка при 30°C. Не отбеливать. Не гладить. Не подвергать химической чистке.";
            }
            else if (product.Name.Contains("Подушка") || product.Name.Contains("подушка"))
            {
                return "Машинная стирка при 40°C. Не отбеливать. Сушить в расправленном виде.";
            }
            else if (product.Name.Contains("Комплект белья") || product.Name.Contains("белье"))
            {
                return "Машинная стирка при 60°C. Не отбеливать. Гладить при средней температуре. Возможна деликатная сухая чистка.";
            }
            
            return "Следуйте рекомендациям на этикетке изделия.";
        }

        private async System.Threading.Tasks.Task LoadSpecificationHistory(string productArticle)
        {
            try
            {
                // Получение истории спецификаций для данного изделия
                var specifications = await _materialService.GetProductSpecificationsAsync(productArticle);
                
                if (specifications == null || !specifications.Any())
                {
                    // Если спецификаций нет, отображаем пустую таблицу
                    SpecificationDataGrid.ItemsSource = new List<ProductSpecificationViewModel>();
                    SpecificationHistoryComboBox.ItemsSource = new List<DateTime>();
                    return;
                }

                // Группировка спецификаций по дате создания
                var groupedSpecs = specifications.GroupBy(s => s.CreatedDate.Date)
                                               .OrderByDescending(g => g.Key)
                                               .ToList();

                // Заполнение комбобокса датами
                _specificationDates = groupedSpecs.Select(g => g.Key).ToList();
                SpecificationHistoryComboBox.ItemsSource = _specificationDates;
                SpecificationHistoryComboBox.SelectedIndex = 0; // Выбираем самую последнюю спецификацию
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки спецификаций: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SpecificationHistoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpecificationHistoryComboBox.SelectedItem == null) return;

            var selectedDate = (DateTime)SpecificationHistoryComboBox.SelectedItem;
            await LoadSpecificationForDate(selectedDate);
        }

        private async System.Threading.Tasks.Task LoadSpecificationForDate(DateTime date)
        {
            try
            {
                // Получение спецификаций для выбранной даты
                var specifications = await _materialService.GetProductSpecificationsAsync(_currentProduct.Article, date);
                
                // Преобразование в модель представления для отображения в DataGrid
                _currentSpecification = new List<ProductSpecificationViewModel>();
                
                foreach (var spec in specifications)
                {
                    // Получение названия материала
                    string materialName = "";
                    if (spec.MaterialType == "fabric")
                    {
                        var fabric = await _databaseService.GetFabricByArticleAsync(spec.MaterialArticle);
                        materialName = fabric?.Name ?? "Неизвестная ткань";
                    }
                    else if (spec.MaterialType == "fitting")
                    {
                        var fitting = await _databaseService.GetFittingByArticleAsync(spec.MaterialArticle);
                        materialName = fitting?.Name ?? "Неизвестная фурнитура";
                    }

                    _currentSpecification.Add(new ProductSpecificationViewModel
                    {
                        MaterialType = spec.MaterialType == "fabric" ? "Ткань" : "Фурнитура",
                        MaterialArticle = spec.MaterialArticle,
                        MaterialName = materialName,
                        Quantity = spec.Quantity,
                        Unit = spec.Unit
                    });
                }

                SpecificationDataGrid.ItemsSource = _currentSpecification;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки спецификации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintSpecification_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создание документа для печати
                FlowDocument document = CreatePrintDocument();

                // Настройка печати
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    IDocumentPaginatorSource paginatorSource = document;
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, "Спецификация изделия");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка печати: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreatePrintDocument()
        {
            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(50);
            document.ColumnWidth = 700;

            // Заголовок
            Paragraph header = new Paragraph(new Run("СПЕЦИФИКАЦИЯ ИЗДЕЛИЯ"));
            header.FontSize = 20;
            header.FontWeight = FontWeights.Bold;
            header.TextAlignment = TextAlignment.Center;
            document.Blocks.Add(header);

            // Информация об изделии
            Paragraph productInfo = new Paragraph();
            productInfo.Margin = new Thickness(0, 20, 0, 0);
            productInfo.Inlines.Add(new Run("Артикул: ") { FontWeight = FontWeights.Bold });
            productInfo.Inlines.Add(new Run(_currentProduct.Article));
            productInfo.Inlines.Add(new LineBreak());
            productInfo.Inlines.Add(new Run("Название: ") { FontWeight = FontWeights.Bold });
            productInfo.Inlines.Add(new Run(_currentProduct.Name));
            productInfo.Inlines.Add(new LineBreak());
            productInfo.Inlines.Add(new Run("Размеры: ") { FontWeight = FontWeights.Bold });
            productInfo.Inlines.Add(new Run($"{_currentProduct.WidthMm} × {_currentProduct.LengthMm} мм"));
            document.Blocks.Add(productInfo);

            // Дата спецификации
            if (SpecificationHistoryComboBox.SelectedItem != null)
            {
                var selectedDate = (DateTime)SpecificationHistoryComboBox.SelectedItem;
                Paragraph dateParagraph = new Paragraph(new Run($"Дата спецификации: {selectedDate:dd.MM.yyyy}"));
                dateParagraph.Margin = new Thickness(0, 10, 0, 20);
                document.Blocks.Add(dateParagraph);
            }

            // Таблица спецификации
            if (_currentSpecification != null && _currentSpecification.Any())
            {
                Table table = new Table();
                table.CellSpacing = 0;
                table.BorderBrush = System.Windows.Media.Brushes.Black;
                table.BorderThickness = new Thickness(1);

                // Определение столбцов
                table.Columns.Add(new TableColumn() { Width = new GridLength(100) }); // Тип материала
                table.Columns.Add(new TableColumn() { Width = new GridLength(120) }); // Артикул
                table.Columns.Add(new TableColumn() { Width = new GridLength(250) }); // Название
                table.Columns.Add(new TableColumn() { Width = new GridLength(80) });  // Количество
                table.Columns.Add(new TableColumn() { Width = new GridLength(80) });  // Единица

                // Заголовок таблицы
                TableRowGroup headerGroup = new TableRowGroup();
                TableRow headerRow = new TableRow();
                headerRow.Background = System.Windows.Media.Brushes.LightGray;
                headerRow.FontWeight = FontWeights.Bold;

                headerRow.Cells.Add(CreateTableCell("Тип"));
                headerRow.Cells.Add(CreateTableCell("Артикул"));
                headerRow.Cells.Add(CreateTableCell("Название"));
                headerRow.Cells.Add(CreateTableCell("Количество"));
                headerRow.Cells.Add(CreateTableCell("Единица"));

                headerGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerGroup);

                // Данные таблицы
                TableRowGroup dataGroup = new TableRowGroup();
                foreach (var item in _currentSpecification)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(CreateTableCell(item.MaterialType));
                    row.Cells.Add(CreateTableCell(item.MaterialArticle));
                    row.Cells.Add(CreateTableCell(item.MaterialName));
                    row.Cells.Add(CreateTableCell(item.Quantity.ToString()));
                    row.Cells.Add(CreateTableCell(item.Unit));
                    dataGroup.Rows.Add(row);
                }
                table.RowGroups.Add(dataGroup);

                document.Blocks.Add(table);
            }
            else
            {
                Paragraph noData = new Paragraph(new Run("Спецификация отсутствует"));
                noData.Margin = new Thickness(0, 10, 0, 0);
                document.Blocks.Add(noData);
            }

            // Подпись
            Paragraph signature = new Paragraph();
            signature.Margin = new Thickness(0, 40, 0, 0);
            signature.Inlines.Add(new Run("Подпись ответственного лица: ________________"));
            signature.Inlines.Add(new LineBreak());
            signature.Inlines.Add(new Run($"Дата печати: {DateTime.Now:dd.MM.yyyy HH:mm}"));
            document.Blocks.Add(signature);

            return document;
        }

        private TableCell CreateTableCell(string text)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text)));
            cell.BorderBrush = System.Windows.Media.Brushes.Black;
            cell.BorderThickness = new Thickness(1);
            cell.Padding = new Thickness(5);
            return cell;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Возврат на предыдущую страницу
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }
        }
    }

    // Модель представления для спецификации изделия
    public class ProductSpecificationViewModel
    {
        public string MaterialType { get; set; } = string.Empty;
        public string MaterialArticle { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}