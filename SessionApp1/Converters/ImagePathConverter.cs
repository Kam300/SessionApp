using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace SessionApp1.Converters
{
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string imagePath && !string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string fullPath = null;

                    // Если путь уже содержит подпапку (после обновления БД)
                    if (imagePath.Contains("/") || imagePath.Contains("\\"))
                    {
                        // Нормализуем разделители для Windows
                        var normalizedPath = imagePath.Replace("/", "\\");
                        fullPath = Path.Combine(baseDirectory, "images", normalizedPath);
                    }
                    else
                    {
                        // Старая логика для обратной совместимости
                        string subfolder = DetermineSubfolder(imagePath);

                        if (!string.IsNullOrEmpty(subfolder))
                        {
                            fullPath = Path.Combine(baseDirectory, "images", subfolder, imagePath);
                        }
                        else
                        {
                            // Пробуем найти файл во всех подпапках
                            var subfolders = new[] { "Фурнитура", "Ткани", "Изделия" };

                            foreach (var folder in subfolders)
                            {
                                var testPath = Path.Combine(baseDirectory, "images", folder, imagePath);
                                if (File.Exists(testPath))
                                {
                                    fullPath = testPath;
                                    break;
                                }
                            }
                        }
                    }

                    // Проверяем существование файла
                    if (fullPath != null && File.Exists(fullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.DecodePixelWidth = 100;
                        bitmap.DecodePixelHeight = 100;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                    else
                    {
                        // Отладочная информация
                        System.Diagnostics.Debug.WriteLine($"Файл не найден: {fullPath}");
                        System.Diagnostics.Debug.WriteLine($"Исходный путь: {imagePath}");
                        System.Diagnostics.Debug.WriteLine($"Базовая директория: {baseDirectory}");

                        // Попробуем найти файл с любым расширением
                        if (fullPath != null)
                        {
                            var directory = Path.GetDirectoryName(fullPath);
                            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);

                            if (Directory.Exists(directory))
                            {
                                var files = Directory.GetFiles(directory, $"{fileNameWithoutExt}.*");
                                if (files.Length > 0)
                                {
                                    var bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.UriSource = new Uri(files[0], UriKind.Absolute);
                                    bitmap.DecodePixelWidth = 100;
                                    bitmap.DecodePixelHeight = 100;
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                    bitmap.Freeze();
                                    return bitmap;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения {imagePath}: {ex.Message}");
                }
            }

            return CreatePlaceholderImage();
        }

        private string DetermineSubfolder(string imagePath)
        {
            // Расширенная логика определения подпапки для фурнитуры
            var fileName = imagePath.ToUpper();

            if (fileName.StartsWith("GB") || fileName.StartsWith("MP") ||
                fileName.StartsWith("GP") || fileName.StartsWith("51") ||
                fileName.StartsWith("PK") || fileName.StartsWith("CB") ||
                fileName.StartsWith("GH") || fileName.StartsWith("MA") ||
                fileName.StartsWith("ZBG") || fileName.StartsWith("MI") ||
                fileName.StartsWith("FER") || fileName.StartsWith("ARS") ||
                fileName.StartsWith("FCI") || fileName.StartsWith("FCA") ||
                fileName.StartsWith("340A") || fileName.StartsWith("HOL") ||
                fileName.StartsWith("ART") || fileName.StartsWith("STTR") ||
                fileName.StartsWith("DB") || fileName.StartsWith("A22") ||
                fileName.StartsWith("A11") || fileName.StartsWith("A55") ||
                fileName.StartsWith("DA") || fileName.StartsWith("MK") ||
                fileName.StartsWith("B-") || fileName.StartsWith("ZB") ||
                fileName.StartsWith("ZP") || fileName.StartsWith("RP") ||
                fileName.StartsWith("BR") || fileName.StartsWith("KKP") ||
                fileName.StartsWith("ZBL") || fileName.StartsWith("CP") ||
                fileName.StartsWith("BD") || fileName.StartsWith("HP") ||
                fileName.StartsWith("KB") || fileName.StartsWith("BM") ||
                fileName.StartsWith("BT") || fileName.StartsWith("TF") ||
                fileName.StartsWith("T3") || fileName.StartsWith("W2") ||
                fileName.StartsWith("W4") || fileName.StartsWith("37") ||
                fileName.StartsWith("OI") || fileName.StartsWith("KI") ||
                fileName.StartsWith("KC") || fileName.StartsWith("ST") ||
                fileName.StartsWith("HA") || fileName.StartsWith("HR") ||
                fileName.StartsWith("HW") || fileName.StartsWith("HKR") ||
                fileName.StartsWith("AL") || fileName.StartsWith("CLP") ||
                fileName.StartsWith("GRC") || fileName.StartsWith("SR") ||
                fileName.StartsWith("JZ") || fileName.StartsWith("JFL") ||
                fileName.StartsWith("HVK") || fileName.StartsWith("WD") ||
                fileName.StartsWith("SRM") || fileName.StartsWith("MRP") ||
                fileName.StartsWith("MTR") || fileName.StartsWith("TRJ") ||
                fileName.StartsWith("LRW") || fileName.StartsWith("30") ||
                fileName.StartsWith("FBL") || fileName.StartsWith("GML") ||
                fileName.StartsWith("RG") || fileName.StartsWith("JL") ||
                fileName.StartsWith("52") || fileName.Contains("FLORANTA") ||
                fileName.StartsWith("000") || fileName.StartsWith("КЛ") ||
                fileName.Contains("КЛ-") || fileName.StartsWith("Б-") ||
                fileName.StartsWith("В-") || fileName.StartsWith("Г-") ||
                fileName.StartsWith("Д-") || fileName.StartsWith("Е-") ||
                fileName.StartsWith("Ж-") || fileName.StartsWith("З-") ||
                fileName.StartsWith("И-") || fileName.StartsWith("К-") ||
                fileName.StartsWith("Л-") || fileName.StartsWith("М-") ||
                fileName.StartsWith("Н-") || fileName.StartsWith("О-") ||
                fileName.StartsWith("П-") || fileName.StartsWith("Р-") ||
                fileName.StartsWith("С-") || fileName.StartsWith("Т-") ||
                fileName.StartsWith("У-") || fileName.StartsWith("Ф-") ||
                fileName.StartsWith("Х-") || fileName.StartsWith("Ц-") ||
                fileName.StartsWith("Ч-") || fileName.StartsWith("Ш-") ||
                fileName.StartsWith("Щ-") || fileName.StartsWith("Э-") ||
                fileName.StartsWith("Ю-") || fileName.StartsWith("Я-"))
            {
                return "Фурнитура";
            }
            else if (fileName.Contains("E12") || fileName.Contains("E13") ||
                     fileName.StartsWith("150") || fileName.StartsWith("40") ||
                     fileName.StartsWith("66") || fileName.StartsWith("11") ||
                     fileName.StartsWith("12") || fileName.StartsWith("70") ||
                     fileName.StartsWith("72") || fileName.StartsWith("89") ||
                     fileName.StartsWith("86") || fileName.StartsWith("GR") ||
                     fileName.StartsWith("14") || fileName.StartsWith("13") ||
                     fileName.StartsWith("80"))
            {
                return "Изделия";
            }
            else if (fileName.StartsWith("16") || fileName.StartsWith("17") ||
                     fileName.StartsWith("18") || fileName.StartsWith("15"))
            {
                return "Ткани";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private DrawingImage CreatePlaceholderImage()
        {
            var drawingGroup = new DrawingGroup();

            // Фон
            var backgroundRect = new RectangleGeometry(new System.Windows.Rect(0, 0, 100, 100));
            var backgroundBrush = new SolidColorBrush(Colors.LightGray);
            drawingGroup.Children.Add(new GeometryDrawing(backgroundBrush, null, backgroundRect));

            // Рамка
            var borderPen = new Pen(Brushes.Gray, 2);
            drawingGroup.Children.Add(new GeometryDrawing(null, borderPen, backgroundRect));

            // Текст "Нет изображения"
            var formattedText = new System.Windows.Media.FormattedText(
                "Нет\nизображения",
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface("Arial"),
                10,
                Brushes.DarkGray,
                96);

            var textGeometry = formattedText.BuildGeometry(new System.Windows.Point(15, 35));
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.DarkGray, null, textGeometry));

            return new DrawingImage(drawingGroup);
        }
    }
}
