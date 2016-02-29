using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace PointMarker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<UIElement, uint> _pointsDictionaryForward = new Dictionary<UIElement, uint>();
        private SortedDictionary<uint, UIElement> _pointsDictionaryReverse = new SortedDictionary<uint, UIElement>();
        private Dictionary<UIElement, TextBox> _pointIndexerMapper = new Dictionary<UIElement, TextBox>();


        public MainWindow()
        {
            InitializeComponent();
            _serialNumberTextBoxYPosShift = SerialNumberPositionScrollBar.Value;
        }

        private readonly double _pointRadius = 3;

        private readonly SolidColorBrush _pointColorBrush = new SolidColorBrush(Colors.Red);

        private readonly DropShadowEffect _pointSelectedEffect = new DropShadowEffect()
        {
            Color = Colors.DarkRed,
            ShadowDepth = 0
        };

        private readonly double _indexFontSize = 8;
        private double _serialNumberTextBoxYPosShift;

        private void SetPointUIElementPosition(UIElement element, Point position)
        {
            Canvas.SetLeft(element, position.X);
            Canvas.SetTop(element, position.Y);
        }

        private void SetIndexUIElementPostion(UIElement element, Point position)
        {
            Canvas.SetLeft(element, position.X - 10);
            Canvas.SetTop(element, position.Y - _serialNumberTextBoxYPosShift);
        }

        private readonly Point _zeroPoint = new Point(0, 0);
        private void DrawPointOnCanvas(Point position)
        {
            if (!_isPointsModified)
            {
                UI_TurnPointFileLoadStateToSaveState();
                _isPointsModified = true;
            }
            Path circle = new Path
            {
                Data = new EllipseGeometry(_zeroPoint, _pointRadius, _pointRadius),
                Fill = _pointColorBrush
            };
            circle.MouseLeftButtonDown += Shape_OnMouseDown;
            circle.MouseLeftButtonUp += Shape_OnMouseUp;
            circle.MouseMove += Shape_OnMouseMove;
            circle.MouseRightButtonUp += Shape_OnRightMouseUp;
            circle.MouseRightButtonDown += Shape_OnRightMouseDown;
            circle.MouseLeave += Shape_OnMouseLeave;

            Canvas.Children.Add(circle);
            SetPointUIElementPosition(circle, position);

            uint pointIndex;

            var pointsSetEnumerator = _pointsDictionaryReverse.Keys.GetEnumerator();
            if (pointsSetEnumerator.MoveNext() && pointsSetEnumerator.Current == 1)
            {
                uint lastPointIndex = pointsSetEnumerator.Current;

                while (pointsSetEnumerator.MoveNext())
                {
                    uint currentPointIndex = pointsSetEnumerator.Current;
                    if (currentPointIndex != lastPointIndex + 1)
                        break;
                    lastPointIndex = currentPointIndex;
                }
                pointIndex = lastPointIndex + 1;
            }
            else
            {
                pointIndex = 1;
            }


            TextBox textBox = new TextBox()
            {
                Background = new SolidColorBrush(Colors.Transparent),
                Text = pointIndex.ToString(),
                IsEnabled = false,
                FontSize = _indexFontSize
            };
            textBox.GotKeyboardFocus += PointIndex_TextBoxOnGotKeyboardFocus;
            textBox.PreviewTextInput += PointIndex_TextBoxOnPreviewTextInput;
            textBox.LostKeyboardFocus += PointIndex_TextBoxOnLostKeyboardFocus;
            textBox.KeyDown += PointIndex_TextBoxOnKeyDown;
            
            if (!_isSerialNumberVisible)
                textBox.Visibility = Visibility.Collapsed;
            
            Canvas.Children.Add(textBox);

            SetIndexUIElementPostion(textBox, position);
            Panel.SetZIndex(textBox, 0);

            _pointsDictionaryForward.Add(circle, pointIndex);
            _pointsDictionaryReverse.Add(pointIndex, circle);
            _pointIndexerMapper.Add(circle, textBox);
        }

        private void PointIndex_TextBoxOnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Enter)
                Keyboard.ClearFocus();
        }

        private uint _lastIndex;
        private void PointIndex_TextBoxOnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs keyboardFocusChangedEventArgs)
        {
            _lastIndex = uint.Parse((sender as TextBox).Text);
        }

        private void PointIndex_TextBoxOnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs keyboardFocusChangedEventArgs)
        {
            uint index = uint.Parse((sender as TextBox).Text);
            if (_pointsDictionaryReverse.ContainsKey(index))
            {
                (sender as TextBox).Text = _lastIndex.ToString();
                return;
            }
            if (!_pointsDictionaryReverse.ContainsKey(_lastIndex))
                return;

            UIElement element = _pointsDictionaryReverse[_lastIndex];
            _pointsDictionaryForward[element] = index;
            _pointsDictionaryReverse.Remove(_lastIndex);
            _pointsDictionaryReverse.Add(index, element);
        }

        private void PointIndex_TextBoxOnPreviewTextInput(object sender, TextCompositionEventArgs textCompositionEventArgs)
        {
            textCompositionEventArgs.Handled = !textCompositionEventArgs.Text.All(char.IsDigit);
        }

        private bool _isOnMouseDown = false;
        private bool _isOnDraging = false;

        private int _zindex = 1;

        private UIElement _currentDraggingElement = null;

        private UIElement _currentSelectedElement = null;

        private bool _isPointsModified = false;

        private void ActivatePointUIElement(UIElement point)
        {
            point.Effect = _pointSelectedEffect;
            TextBox textBox = _pointIndexerMapper[point];
            textBox.IsEnabled = true;
            Panel.SetZIndex(textBox, int.MaxValue);
        }

        private void DeactivatePointUIElement(UIElement point)
        {
            _currentSelectedElement.Effect = null;
            Keyboard.ClearFocus();
            TextBox textBox = _pointIndexerMapper[_currentSelectedElement];
            textBox.IsEnabled = false;
            Panel.SetZIndex(textBox, 0);
        }

        private UIElement currentSelectedElement
        {
            get { return _currentSelectedElement; }
            set
            {
                if (value != null)
                {
                    if (_currentSelectedElement != null)
                        DeactivatePointUIElement(_currentSelectedElement);

                    _currentSelectedElement = value;

                    ActivatePointUIElement(_currentSelectedElement);
                }
                else
                {
                    DeactivatePointUIElement(_currentSelectedElement);

                    _currentSelectedElement = value;
                }
            }
        }

        private void MoveShape(UIElement element, Point position)
        {
            SetPointUIElementPosition(element, position);

            SetIndexUIElementPostion(_pointIndexerMapper[element], position);
        }

        private void OnPostShapeMove(UIElement element)
        {
            Panel.SetZIndex(element, _zindex++);

            _currentDraggingElement = null;
        }

        private void ClearPointSelectedState()
        {
            if (_currentSelectedElement != null)
                currentSelectedElement = null;

            Keyboard.ClearFocus();
        }

        private void Shape_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isOnMouseDown = true;

            Panel.SetZIndex(sender as UIElement, int.MaxValue);

            _currentDraggingElement = sender as UIElement;
        }

        private void Shape_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isOnMouseDown)
                return;

            _isOnDraging = true;

            MoveShape(sender as UIElement, e.GetPosition(Canvas));

            e.Handled = true;
        }

        private void Shape_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isOnMouseDown)
                return;

            _isOnMouseDown = false;

            _currentDraggingElement = null;

            if (_isOnDraging)
            {
                OnPostShapeMove(sender as UIElement);
                _isOnDraging = false;
            }
            else
            {
                if (sender == _currentSelectedElement)
                    currentSelectedElement = null;
                else
                    currentSelectedElement = sender as UIElement;
            }

            e.Handled = true;
        }

        private bool _isOnRightMouseDown = false;

        private void Shape_OnRightMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isOnRightMouseDown = true;

            e.Handled = true;
        }

        private void RemovePoint(UIElement element)
        {
            Canvas.Children.Remove(element);

            Canvas.Children.Remove(_pointIndexerMapper[element]);

            _pointIndexerMapper.Remove(element);
            uint index = _pointsDictionaryForward[element];
            _pointsDictionaryForward.Remove(element);
            _pointsDictionaryReverse.Remove(index);

            _currentDraggingElement = null;
            _currentSelectedElement = null;
        }

        private void Shape_OnRightMouseUp(object sender, MouseButtonEventArgs e)
        {
            ClearPointSelectedState();

            if (_isOnRightMouseDown)
                RemovePoint(sender as UIElement);

            _isOnRightMouseDown = false;

            e.Handled = true;
        }

        void Shape_OnMouseLeave(object sender, MouseEventArgs e)
        {
            _isOnRightMouseDown = false;

            e.Handled = true;
        }

        private void Canvas_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isOnMouseDown = true;

            Keyboard.ClearFocus();

            e.Handled = true;
        }

        private void Canvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isOnMouseDown)
                return;

            if (_currentDraggingElement != null)
                MoveShape(_currentDraggingElement, e.GetPosition(Canvas));

            _isOnDraging = true;

            e.Handled = true;
        }

        private void Canvas_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isOnMouseDown)
                return;

            ClearPointSelectedState();

            Point pos = e.GetPosition(Canvas);

            if (!_isOnDraging)
                DrawPointOnCanvas(pos);
            else
            {
                if (_currentDraggingElement != null)
                    OnPostShapeMove(_currentDraggingElement);
                _isOnDraging = false;
            }

            _isOnMouseDown = false;

            e.Handled = true;
        }

        private void TextBoxPointFilePath_OnGotFocus(object sender, RoutedEventArgs e)
        {
            ButtonOpenPointFile.IsDefault = true;
        }

        private void TextBoxPointFilePath_OnLostFocus(object sender, RoutedEventArgs e)
        {
            ButtonOpenPointFile.IsDefault = false;
        }

        private void TextBoxImageFilePath_OnGotFocus(object sender, RoutedEventArgs e)
        {
            ButtonOpenImageFile.IsDefault = true;
        }

        private void TextBoxImageFilePath_OnLostFocus(object sender, RoutedEventArgs e)
        {
            ButtonOpenImageFile.IsDefault = false;
        }

        private void AddPoints(IList<Point> points)
        {
            foreach (var point in points)
            {
                DrawPointOnCanvas(point);
            }
        }

        private async void ButtonOpenPointFile_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Uri filePath = new Uri(TextBoxPointFilePath.Text);
                if (!_isPointsModified)
                {
                    IsEnabled = false;
                    IList<Point> pointsFromFile = await PointFileOperation.Read(filePath);
                    IList<Point> points = new List<Point>(pointsFromFile.Count);
                    foreach (var point in pointsFromFile)
                    {
                        Point newPoint = new Point(point.X * ratio, point.Y * ratio);
                        points.Add(newPoint);
                    }
                    IsEnabled = true;
                    AddPoints(points);
                }
                else
                {
                    IsEnabled = false;
                    IList<Point> points = new List<Point>(_pointsDictionaryReverse.Count);
                    foreach (var pointUIElement in _pointsDictionaryReverse.Values)
                    {
                        Point point = pointUIElement.TransformToVisual(Canvas).Transform(new Point(0, 0));
                        point.X /= ratio;
                        point.Y /= ratio;
                        points.Add(point);
                    }
                    await PointFileOperation.Write(points, filePath);
                    IsEnabled = true;
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void showOpenFileDialog(string filter, TextBox relativeTextBox)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog {Filter = filter};


            bool? result = openFileDialog.ShowDialog();
            if (result.HasValue == false || result.Value == false)
                return;

            relativeTextBox.Text = openFileDialog.FileName;

            relativeTextBox.Focus();
        }

        private void showSaveFileDialog(string filter, TextBox relativeTextBox)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog() {Filter = filter};

            bool? result = saveFileDialog.ShowDialog();
            if (result.HasValue == false || result.Value == false)
                return;

            relativeTextBox.Text = saveFileDialog.FileName;

            relativeTextBox.Focus();
        }

        private void UI_TurnPointFileLoadStateToSaveState()
        {
            ButtonOpenPointFile.Content = "Save";
        }

        private void UI_TurnPointFileSaveStateToLoadState()
        {
            ButtonOpenPointFile.Content = "Load";
        }

        private void ButtonBrowseImageFile_OnClick(object sender, RoutedEventArgs e)
        {
            showOpenFileDialog("Image Files(*.PNG;*.JPG;*.JPEG;*.TIFF;*.BMP;*.GIF)|*.PNG;*.JPG;*.JPEG;*.TIFF;*.BMP;*.GIF|All files (*.*)|*.*", TextBoxImageFilePath);
        }

        private void ButtonBrowsePointFile_OnClick(object sender, RoutedEventArgs e)
        {
            string filter = "Point File (.pts)|*.pts";
            if (!_isPointsModified)
                showOpenFileDialog(filter, TextBoxPointFilePath);
            else
                showSaveFileDialog(filter, TextBoxPointFilePath);
        }

        private void ButtonReset_OnClick(object sender, RoutedEventArgs e)
        {
            _currentSelectedElement = null;
            _currentDraggingElement = null;

            Canvas.Children.Clear();
            _pointIndexerMapper.Clear();
            _pointsDictionaryForward.Clear();
            _pointsDictionaryReverse.Clear();
            Canvas.Background = new SolidColorBrush(Colors.Transparent);

            _isPointsModified = false;
            UI_TurnPointFileSaveStateToLoadState();
        }

        private async void ButtonOpenImageFile_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = TextBoxImageFilePath.Text;
                var image = await Task.Run(() =>
                {
                    try
                    {
                        BitmapImage bitmapImage = new BitmapImage(new Uri(path));
                        bitmapImage.Freeze();
                        return bitmapImage;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                });
                if (image == null)
                    return;

                Canvas.Width = image.PixelWidth;
                Canvas.Height = image.PixelHeight;

                _originalWidth = image.PixelWidth;

                Canvas.Background = new ImageBrush(image);
            }
            catch (Exception)
            {
                return;
            }
        }

        private bool _isSerialNumberVisible = true;

        private void IsSerialNumberVisibleToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            _isSerialNumberVisible = true;
            foreach (var textBox in _pointIndexerMapper.Values)
            {
                textBox.Visibility = Visibility.Visible;
            }
        }

        private void IsSerialNumberVisibleToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            _isSerialNumberVisible = false;
            foreach (var textBox in _pointIndexerMapper.Values)
            {
                textBox.Visibility = Visibility.Collapsed;
            }
        }

        private double _originalWidth;
        private double ratio;

        private void Canvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ratio = e.NewSize.Width / _originalWidth;
        }

        private void SerialNumberPositionRangeBase_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _serialNumberTextBoxYPosShift = e.NewValue;
            foreach (var textBox in _pointIndexerMapper.Values)
            {
                Point point = textBox.TransformToVisual(Canvas).Transform(_zeroPoint);
                point.Y -= e.NewValue - e.OldValue;
                Canvas.SetTop(textBox, point.Y);
            }
        }
    }
}
