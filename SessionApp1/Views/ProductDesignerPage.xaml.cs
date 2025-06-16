using SessionApp1.Services;
using SessionApp1.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace SessionApp1.Views
{
    public partial class ProductDesignerPage : Page
    {
        private readonly DatabaseService _databaseService;
        private readonly MaterialAccountingService _materialService;
        private List<Fabric> _fabrics;
        private List<Fitting> _fittings;
        private List<UIElement> _placedFittings;
        private UIElement _selectedElement;
        private Point _lastMousePosition;
        private bool _isDragging;

        public ProductDesignerPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _materialService = new MaterialAccountingService();
            _placedFittings = new List<UIElement>();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                _fabrics = await _databaseService.GetFabricsAsync();
                _fittings = await _databaseService.GetFittingsAsync();

                FabricComboBox.ItemsSource = _fabrics;
                FittingListBox.ItemsSource = _fittings;

                if (_fabrics.Any())
                {
                    FabricComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Dimensions_Changed(object sender, TextChangedEventArgs e)
        {
            if (ProductRectangle == null) return;

            if (double.TryParse(WidthTextBox.Text, out double width) && width > 0)
            {
                ProductRectangle.Width = Math.Min(width, DesignCanvas.ActualWidth - 50);
            }

            if (double.TryParse(HeightTextBox.Text, out double height) && height > 0)
            {
                ProductRectangle.Height = Math.Min(height, DesignCanvas.ActualHeight - 50);
            }
        }

        private void Fabric_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (FabricComboBox.SelectedItem is Fabric selectedFabric)
            {
                try
                {
                    // Попытка загрузить изображение ткани
                    var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", selectedFabric.ImagePath);
                    if (File.Exists(imagePath))
                    {
                        var bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                        var imageBrush = new ImageBrush(bitmap)
                        {
                            Stretch = Stretch.UniformToFill,
                            TileMode = TileMode.Tile,
                            Viewport = new Rect(0, 0, 0.1, 0.1),
                            ViewportUnits = BrushMappingMode.RelativeToBoundingBox
                        };
                        ProductRectangle.Fill = imageBrush;
                    }
                    else
                    {
                        // Если изображение не найдено, используем цветную заливку
                        var colors = new[] { Colors.LightBlue, Colors.LightGreen, Colors.LightPink,
                                           Colors.LightYellow, Colors.LightGray };
                        var colorIndex = selectedFabric.Article.GetHashCode() % colors.Length;
                        ProductRectangle.Fill = new SolidColorBrush(colors[Math.Abs(colorIndex)]);
                    }
                }
                catch
                {
                    ProductRectangle.Fill = new SolidColorBrush(Colors.LightBlue);
                }
            }
        }

        private void Fitting_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FittingListBox.SelectedItem is Fitting selectedFitting)
            {
                AddFittingToCanvas(selectedFitting, new Point(100, 100));
            }
        }

        private void AddFittingToCanvas(Fitting fitting, Point position)
        {
            var fittingElement = new Border
            {
                Width = (double)Math.Max(fitting.WidthMm / 10, 20), // Масштабируем размер
                Height = (double)Math.Max(fitting.LengthMm / 10, 20),
                Background = new SolidColorBrush(Colors.Orange),
                BorderBrush = new SolidColorBrush(Colors.DarkOrange),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(3),
                Cursor = Cursors.Hand,
                Tag = fitting
            };

            // Добавляем текст с названием фурнитуры
            var textBlock = new TextBlock
            {
                Text = fitting.Name.Length > 10 ? fitting.Name.Substring(0, 10) + "..." : fitting.Name,
                FontSize = 8,
                Foreground = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            fittingElement.Child = textBlock;

            // Устанавливаем позицию
            Canvas.SetLeft(fittingElement, position.X);
            Canvas.SetTop(fittingElement, position.Y);

            // Добавляем обработчики событий для перетаскивания
            fittingElement.MouseLeftButtonDown += FittingElement_MouseLeftButtonDown;
            fittingElement.MouseMove += FittingElement_MouseMove;
            fittingElement.MouseLeftButtonUp += FittingElement_MouseLeftButtonUp;
            fittingElement.MouseRightButtonDown += FittingElement_MouseRightButtonDown;

            DesignCanvas.Children.Add(fittingElement);
            _placedFittings.Add(fittingElement);
        }

        private void FittingElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _selectedElement = sender as UIElement;
            _lastMousePosition = e.GetPosition(DesignCanvas);
            _isDragging = true;
            _selectedElement?.CaptureMouse();
            e.Handled = true;
        }

        private void FittingElement_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedElement != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(DesignCanvas);
                var deltaX = currentPosition.X - _lastMousePosition.X;
                var deltaY = currentPosition.Y - _lastMousePosition.Y;

                var newX = Canvas.GetLeft(_selectedElement) + deltaX;
                var newY = Canvas.GetTop(_selectedElement) + deltaY;

                // Ограничиваем перемещение границами холста
                newX = Math.Max(0, Math.Min(newX, DesignCanvas.ActualWidth - ((FrameworkElement)_selectedElement).Width));
                newY = Math.Max(0, Math.Min(newY, DesignCanvas.ActualHeight - ((FrameworkElement)_selectedElement).Height));

                Canvas.SetLeft(_selectedElement, newX);
                Canvas.SetTop(_selectedElement, newY);

                _lastMousePosition = currentPosition;
            }
        }

        private void FittingElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _selectedElement?.ReleaseMouseCapture();
                _selectedElement = null;
            }
        }

        private void FittingElement_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Поворот фурнитуры на 45 градусов
            if (sender is FrameworkElement element)
            {
                var transform = element.RenderTransform as RotateTransform ?? new RotateTransform();
                transform.Angle += 45;
                transform.CenterX = element.Width / 2;
                transform.CenterY = element.Height / 2;
                element.RenderTransform = transform;
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Снимаем выделение при клике на пустое место
            _selectedElement = null;
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Fitting)))
            {
                var fitting = e.Data.GetData(typeof(Fitting)) as Fitting;
                var position = e.GetPosition(DesignCanvas);
                AddFittingToCanvas(fitting, position);
            }
        }

        private void LoadCustomFabric_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*",
                Title = "Выберите изображение ткани"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage(new Uri(openFileDialog.FileName, UriKind.Absolute));
                    var imageBrush = new ImageBrush(bitmap)
                    {
                        Stretch = Stretch.UniformToFill,
                        TileMode = TileMode.Tile,
                        Viewport = new Rect(0, 0, 0.1, 0.1),
                        ViewportUnits = BrushMappingMode.RelativeToBoundingBox
                    };
                    ProductRectangle.Fill = imageBrush;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadCustomFitting_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*",
                Title = "Выберите изображение фурнитуры"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var customFitting = new Fitting
                    {
                        Article = "CUSTOM_" + DateTime.Now.Ticks,
                        Name = "Пользовательская фурнитура",
                        WidthMm = 30,
                        LengthMm = 30,
                        ImagePath = openFileDialog.FileName
                    };

                    AddFittingToCanvas(customFitting, new Point(150, 150));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления фурнитуры: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productData = new
                {
                    Width = WidthTextBox.Text,
                    Height = HeightTextBox.Text,
                    SelectedFabric = FabricComboBox.SelectedItem as Fabric,
                    PlacedFittings = _placedFittings.Select(f => new
                    {
                        Fitting = (f as Border)?.Tag as Fitting,
                        X = Canvas.GetLeft(f),
                        Y = Canvas.GetTop(f),
                        Rotation = ((f as FrameworkElement)?.RenderTransform as RotateTransform)?.Angle ?? 0
                    }).ToList()
                };

                MessageBox.Show($"Изделие сохранено!\n" +
                               $"Размеры: {productData.Width}x{productData.Height} мм\n" +
                               $"Ткань: {productData.SelectedFabric?.FabricName}\n" +
                               $"Фурнитура: {productData.PlacedFittings.Count} элементов",
                               "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            // Удаляем всю размещенную фурнитуру
            foreach (var fitting in _placedFittings)
            {
                DesignCanvas.Children.Remove(fitting);
            }
            _placedFittings.Clear();

            // Сбрасываем размеры
            WidthTextBox.Text = "500";
            HeightTextBox.Text = "700";
            ProductRectangle.Width = 500;
            ProductRectangle.Height = 700;

            // Сбрасываем ткань
            ProductRectangle.Fill = new SolidColorBrush(Colors.LightBlue);
            FabricComboBox.SelectedIndex = -1;
        }
    }
}
