using MultiLanquageModule;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PaperLess_Emeeting
{
    /// <summary>
    /// StrokeToolPanelHorizontal.xaml 的互動邏輯
    /// </summary>

    //public delegate void StrokeChangeEvent(DrawingAttributes d);
    //public delegate void StrokeUndoEvent();
    //public delegate void StrokeRedoEvent();
    //public delegate void StrokeDeleteAllEvent();
    //public delegate void StrokeDeleteEvent();
    //public delegate void StrokeEraseEvent();
    //public delegate void StrokeLineEvent();
    //public delegate void StrokeCurveEvent();

    //public delegate void showPenToolPanelEvent(bool isCanvasShowed);


    public partial class StrokeToolPanelHorizontal_Reader : UserControl
    {

        public event StrokeChangeEvent strokeChange;
        public event StrokeUndoEvent strokeUndo;
        public event StrokeRedoEvent strokeRedo;
        public event StrokeDeleteAllEvent strokeDelAll;
        public event StrokeDeleteEvent strokeDel;
        public event StrokeEraseEvent strokeErase;
        public event showPenToolPanelEvent showPenToolPanel;
        public event StrokeCurveEvent strokeCurve;
        public event StrokeLineEvent strokeLine;

        private Color currentColor;
        private double currentRadiusWidth;

        private DrawingAttributes strokeAtt;
        private bool isHighlighter;
        public DrawingAttributes drawingAttr;
        public MultiLanquageManager langMng;



        public StrokeToolPanelHorizontal_Reader()
        {
            InitializeComponent();

            //currentColor = System.Windows.Media.Colors.Gray;
            currentColor = System.Windows.Media.Colors.Red;

            currentRadiusWidth = 5;
            strokeAtt = new DrawingAttributes();
            isHighlighter = true;

            //if (true)
            //{
            //    penSubPanelGrid.Visibility = System.Windows.Visibility.Visible;
            //    //testPanelGrid.Visibility = System.Windows.Visibility.Collapsed;
            //    penToolPanelGrid.Visibility = System.Windows.Visibility.Collapsed;
            //    colorPanel.Visibility = System.Windows.Visibility.Collapsed;
            //    //showPenToolPanel(150, 100);
            //}
        }



        public void setColor(object sender, RoutedEventArgs e)
        {
            string targetColor;
            Button b = sender as Button;
            targetColor = (String)b.Tag;
            Color color = System.Windows.Media.Colors.Red;
            if (b.Background is SolidColorBrush)
            {
                color = (b.Background as SolidColorBrush).Color;
            }


            currentColor = color;
            demoStroke.Stroke = new SolidColorBrush(color);
            dispatchStrokeAttChanged();

        }


        public void dispatchStrokeAttChanged()
        {
            if (strokeAtt != null && strokeChange != null)
            {
                strokeAtt.IsHighlighter = isHighlighter;
                strokeAtt.Color = currentColor;
                strokeChange(strokeAtt);
            }
        }



        private void strokeHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (strokeAtt != null)
            {
                strokeAtt.Width = strokeAtt.Height = currentRadiusWidth = demoStroke.StrokeThickness = strokeWidthSlider.Value;
                //strokeAtt.Height = currentRadiusHeight = strokeHeightSlider.Value;

                dispatchStrokeAttChanged();
            }
        }

        private void transparentButton_Click(object sender, RoutedEventArgs e)
        {
            isHighlighter = true;
            dispatchStrokeAttChanged();
            nonTransparentButton.Opacity = 0.5;
            transparentButton.Opacity = 1;

        }


        private void curveButtonClick(object sender, RoutedEventArgs e)
        {
            strokeCurve();
            curveButton.Opacity = 1;
            straightPanelButton.Opacity = 0.5;

            Image img = new Image() { Source = ((Image)curveButton.Content).Source };
            penButton.Content = img;
            //penButton.Content = imgCollection[0];
            penTypePopup.IsOpen = false;
            showPenToolPanel(false);
        }

        private void LineButtonClick(object sender, RoutedEventArgs e)
        {
            strokeLine();
            curveButton.Opacity = 0.5;
            straightPanelButton.Opacity = 1;
            Image img = new Image() { Source = ((Image)straightPanelButton.Content).Source };
            penButton.Content = img;
            //penButton.Content = imgCollection[1];
            penTypePopup.IsOpen = false;
            showPenToolPanel(false);
        }


        private void nonTransparentButton_Click(object sender, RoutedEventArgs e)
        {
            isHighlighter = false;
            dispatchStrokeAttChanged();
            nonTransparentButton.Opacity = 1;
            transparentButton.Opacity = 0.5;
        }

        private void undoClickButton_Click(object sender, RoutedEventArgs e)
        {
            strokeUndo();
        }


        private void deleteAllClickButton_Click(object sender, RoutedEventArgs e)
        {
            strokeDelAll();
        }

        private void redoClickButton_Click(object sender, RoutedEventArgs e)
        {
            strokeRedo();
        }

        private void delClickButton_Click(object sender, RoutedEventArgs e)
        {
            strokeDel();
        }

        private void penButtonClick(object sender, RoutedEventArgs e)
        {
            penButton.Opacity = 1;
            colorPanelButton.Opacity = 0.5;
            eraserButton.Opacity = 0.5;
            deleteAllButton.Opacity = 0.5;
            redoButton.Opacity = 0.5;
            undoButton.Opacity = 0.5;

            if (!penTypePopup.IsOpen)
            {
                penTypePopup.IsOpen = true;
                showPenToolPanel(true);
                colorPopup.IsOpen = false;
            }
            else
            {
                if (straightPanelButton.Opacity == 1)
                {
                    strokeLine();
                }
                else
                {
                    strokeCurve();
                }
                penTypePopup.IsOpen = false;
                showPenToolPanel(false);
            }


            //penSubPanelGrid.Visibility = System.Windows.Visibility.Visible;
            ////testPanelGrid.Visibility = System.Windows.Visibility.Collapsed;
            //penToolPanelGrid.Visibility = System.Windows.Visibility.Collapsed;
            //colorPanel.Visibility = System.Windows.Visibility.Collapsed;
            //showPenToolPanel(150, 100);
            ///*
            //colorPanelButton.IsEnabled = false;
            //eraserButton.IsEnabled = false;
            //deleteAllButton.IsEnabled = false;
            //redoButton.IsEnabled = false;
            //undoButton.IsEnabled = false;
            //*/

            //penButton.Opacity = 1;
            //colorPanelButton.Opacity = 0.5;
            //eraserButton.Opacity = 0.5;
            //deleteAllButton.Opacity = 0.5;
            //redoButton.Opacity = 0.5;
            //undoButton.Opacity = 0.5;

            ////判斷現在是直線還曲線 分別發出event
            //if (straightPanelButton.Opacity == 1)
            //{
            //    strokeLine();
            //}
            //else
            //{
            //    strokeCurve();
            //}
        }

        private void eraserButtonClick(object sender, RoutedEventArgs e)
        {
            penTypePopup.IsOpen = false;
            colorPopup.IsOpen = false;

            penButton.Opacity = 0.5;
            colorPanelButton.Opacity = 0.5;
            eraserButton.Opacity = 1;
            deleteAllButton.Opacity = 0.5;
            redoButton.Opacity = 0.5;
            undoButton.Opacity = 0.5;
            //penSubPanelGrid.Visibility = System.Windows.Visibility.Collapsed;
            //penToolPanelGrid.Visibility = System.Windows.Visibility.Collapsed;
            //showPenToolPanel(150, 100);

            strokeErase();
            showPenToolPanel(false);
        }

        private void deleteAllButtonClick(object sender, RoutedEventArgs e)
        {
            penTypePopup.IsOpen = false;
            colorPopup.IsOpen = false;

            penButton.Opacity = 0.5;
            colorPanelButton.Opacity = 0.5;
            eraserButton.Opacity = 0.5;
            deleteAllButton.Opacity = 1;
            redoButton.Opacity = 0.5;
            undoButton.Opacity = 0.5;
            showPenToolPanel(true);
            //show alert
            //if (MessageBox.Show("您確定要刪除所有筆畫嗎?", "確定", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            if (MessageBox.Show(langMng.getLangString("sureDelAllStrokes"), langMng.getLangString("submit"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                strokeDelAll();
            }
            penButton.Opacity = 1;
            colorPanelButton.Opacity = 0.5;
            eraserButton.Opacity = 0.5;
            deleteAllButton.Opacity = 0.5;
            redoButton.Opacity = 0.5;
            undoButton.Opacity = 0.5;
            showPenToolPanel(false);
            if (straightPanelButton.Opacity == 1)
            {
                strokeLine();
            }
            else
            {
                strokeCurve();
            }

        }

        private void redoButtonClick(object sender, RoutedEventArgs e)
        {
            penButton.Opacity = 0.5;
            colorPanelButton.Opacity = 0.5;
            eraserButton.Opacity = 0.5;
            deleteAllButton.Opacity = 0.5;
            redoButton.Opacity = 1;
            undoButton.Opacity = 0.5;
        }

        private void undoButtonClick(object sender, RoutedEventArgs e)
        {
            penButton.Opacity = 0.5;
            colorPanelButton.Opacity = 0.5;
            eraserButton.Opacity = 0.5;
            deleteAllButton.Opacity = 0.5;
            redoButton.Opacity = 0.5;
            undoButton.Opacity = 1;
        }

        public void determineDrawAtt(DrawingAttributes d, bool isStrokeLine)
        {
            drawingAttr = d;
            demoStroke.StrokeThickness = strokeWidthSlider.Value = currentRadiusWidth = drawingAttr.Width;
            demoStroke.Stroke = new SolidColorBrush(drawingAttr.Color);
            if (d.IsHighlighter)
            {
                isHighlighter = true;
                nonTransparentButton.Opacity = 0.5;
                transparentButton.Opacity = 1;
            }
            else
            {
                isHighlighter = false;

                nonTransparentButton.Opacity = 1;
                transparentButton.Opacity = 0.5;
            }
            //demoStroke.

            if (isStrokeLine)
            {
                Image img = new Image() { Source = ((Image)straightPanelButton.Content).Source };
                penButton.Content = img;
                //penButton.Content = imgCollection[1];
                curveButton.Opacity = 0.5;
                straightPanelButton.Opacity = 1;
            }
            else
            {
                Image img = new Image() { Source = ((Image)curveButton.Content).Source };
                penButton.Content = img;
                //penButton.Content = imgCollection[0];
                curveButton.Opacity = 1;
                straightPanelButton.Opacity = 0.5;
            }
        }

        private void colorPanelButtonClick(object sender, RoutedEventArgs e)
        {
            if (!colorPopup.IsOpen)
            {
                penButton.Opacity = 0.5;
                colorPanelButton.Opacity = 1;
                eraserButton.Opacity = 0.5;
                deleteAllButton.Opacity = 0.5;
                redoButton.Opacity = 0.5;
                undoButton.Opacity = 0.5;

                penTypePopup.IsOpen = false;
                colorPopup.IsOpen = true;
                showPenToolPanel(true);
            }
            else
            {
                penButton.Opacity = 1;
                colorPanelButton.Opacity = 0.5;
                eraserButton.Opacity = 0.5;
                deleteAllButton.Opacity = 0.5;
                redoButton.Opacity = 0.5;
                undoButton.Opacity = 0.5;

                penTypePopup.IsOpen = false;
                colorPopup.IsOpen = false;
                showPenToolPanel(false);
            }

            //penSubPanelGrid.Visibility = System.Windows.Visibility.Collapsed;
            //penButton.Opacity = 0.5;
            //colorPanelButton.Opacity = 1;
            //eraserButton.Opacity = 0.5;
            //deleteAllButton.Opacity = 0.5;
            //redoButton.Opacity = 0.5;
            //undoButton.Opacity = 0.5;
            ///*penButton.IsEnabled = false;
            //eraserButton.IsEnabled = false;
            //deleteAllButton.IsEnabled = false;
            //redoButton.IsEnabled = false;
            //undoButton.IsEnabled = false;
            //*/
            //if (penToolPanelGrid.Visibility.Equals(System.Windows.Visibility.Collapsed))
            //{
            //    penToolPanelGrid.Visibility = System.Windows.Visibility.Visible;
            //    colorPanel.Visibility = System.Windows.Visibility.Visible;
            //    mainPanel.Height = 500;
            //    showPenToolPanel(100, 500);
            //}
            //else
            //{
            //    penToolPanelGrid.Visibility = System.Windows.Visibility.Collapsed;
            //    colorPanel.Visibility = System.Windows.Visibility.Collapsed;
            //    mainPanel.Height = 100;
            //    showPenToolPanel(500, 100);
            //}
        }
        public void closePopup()
        {
            penButton.Opacity = 1;
            colorPanelButton.Opacity = 0.5;
            eraserButton.Opacity = 0.5;
            deleteAllButton.Opacity = 0.5;
            redoButton.Opacity = 0.5;
            undoButton.Opacity = 0.5;

            penTypePopup.IsOpen = false;
            colorPopup.IsOpen = false;

            if (straightPanelButton.Opacity == 1)
            {
                strokeLine();
            }
            else
            {
                strokeCurve();
            }
        }




    }
}
