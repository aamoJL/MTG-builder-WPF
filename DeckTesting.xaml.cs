using Microsoft.Win32;
using MTG;
using MTG.Scryfall;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;

namespace MTG_builder
{
    /// <summary>
    /// Interaction logic for DeckTesting.xaml
    /// </summary>
    public partial class DeckTesting : Window
    {
        private readonly List<DependencyObject> hitTestResults = new();
        private readonly float canvasScaleFactor = 1.1f;
        private readonly MatrixTransform canvasTransform = new();
        private readonly CardCollection deckOneCollection = new(); // Selected deck
        private readonly CardCollection deckTwoCollection = new(); // Selected deck
        private readonly string counterImagePath = "Resources/PlusCounter.png";

        private FrameworkElement mouseDragObject;
        private Point mouseDragPivot;
        private bool canvasPanning;
        private Point canvasPanningStartPoint;
        private List<Card> deckOneCards = new(); // Gameplay cards
        private List<Card> deckTwoCards = new(); // Gameplay cards

        private bool MouseDragging => mouseDragObject != null;

        public DeckTesting()
        {
            InitializeComponent();
        }

        #region Menu Item Events
        private void MenuNewGame_Click(object sender, RoutedEventArgs e)
        {
            PlayerOneLife.Text = "20";
            PlayerTwoLife.Text = "20";

            GetDeckCards(1);
            GetDeckCards(2);

            ShuffleDeck(deckOneCards);
            ShuffleDeck(deckTwoCards);

            UIElement[] elements = new UIElement[2]
            {
                GameCanvas.Children[0],
                GameCanvas.Children[1]
            };

            GameCanvas.Children.Clear();

            _ = GameCanvas.Children.Add(elements[0]);
            _ = GameCanvas.Children.Add(elements[1]);

            for (int i = 0; i < 7; i++)
            {
                DrawCard(0, 1);
                DrawCard(0, 2);
            }
        }
        #endregion

        #region Canvas Zoom and Pan
        private void CanvasZoom(int delta, Point pivot)
        {
            float scaleFactor = canvasScaleFactor;

            // Limit zooming
            if (delta > 0 && canvasTransform.Matrix.M11 > 1.7f) { return; }
            if (delta < 0 && canvasTransform.Matrix.M11 < .3f) { return; }
            if (delta < 0) { scaleFactor = 1f / scaleFactor; }

            Matrix canvasMatrix = canvasTransform.Matrix;
            canvasMatrix.ScaleAt(scaleFactor, scaleFactor, pivot.X, pivot.Y);
            canvasTransform.Matrix = canvasMatrix;

            foreach (UIElement child in GameCanvas.Children)
            {
                Matrix scaleMatrix = (child.RenderTransform as MatrixTransform).Matrix;
                scaleMatrix.ScaleAt(scaleFactor, scaleFactor, pivot.X, pivot.Y);

                double x = Canvas.GetLeft(child);
                double y = Canvas.GetTop(child);

                double sx = x * scaleFactor;
                double sy = y * scaleFactor;

                Canvas.SetLeft(child, sx);
                Canvas.SetTop(child, sy);
                (child.RenderTransform as MatrixTransform).Matrix = scaleMatrix;
            }
        }
        private void CanvasStartPan(Point pivot)
        {
            canvasPanning = true;
            canvasPanningStartPoint = canvasTransform.Inverse.Transform(pivot);
        }
        private void CanvasPan()
        {
            Point mousePosition = canvasTransform.Inverse.Transform(Mouse.GetPosition(GameCanvas));
            Vector delta = Point.Subtract(mousePosition, canvasPanningStartPoint);

            foreach (UIElement child in GameCanvas.Children)
            {
                double x = Canvas.GetLeft(child);
                double y = Canvas.GetTop(child);

                double sx = x + (delta.X * canvasTransform.Matrix.M11);
                double sy = y + (delta.Y * canvasTransform.Matrix.M11);

                Canvas.SetLeft(child, sx);
                Canvas.SetTop(child, sy);
            }

            canvasPanningStartPoint = mousePosition;
        }
        private void CanvasEndPan()
        {
            canvasPanning = false;
        }
        private void CanvasRemoveElement(UIElement element)
        {
            GameCanvas.Children.Remove(element);
        }
        #endregion

        #region Mouse Drag
        private void MouseStartDrag(FrameworkElement element)
        {
            Panel.SetZIndex(element, 10);
            Point imgPos = new(Canvas.GetLeft(element), Canvas.GetTop(element));
            mouseDragPivot = (Point)(imgPos - Mouse.GetPosition(GameCanvas));
            mouseDragObject = element;
        }
        private void MouseDrag()
        {
            Point newPoint = Mouse.GetPosition(GameCanvas);
            Canvas.SetLeft(mouseDragObject, newPoint.X + mouseDragPivot.X);
            Canvas.SetTop(mouseDragObject, newPoint.Y + mouseDragPivot.Y);
        }
        private void MouseEndDrag(Point pivot)
        {
            hitTestResults.Clear();

            VisualTreeHelper.HitTest(GameCanvas, null,
                new HitTestResultCallback(MyHitTestResult),
                new PointHitTestParameters(pivot));

            // Perform actions on the hit test results list.
            if (hitTestResults.Count > 2)
            {
                foreach (DependencyObject item in hitTestResults)
                {
                    if (mouseDragObject.DataContext is Card card)
                    {
                        string itemName = item.GetValue(NameProperty).ToString();
                        switch (itemName)
                        {
                            case "DeckOneTop":
                                PutCardToDeck(card, 1, false);
                                CanvasRemoveElement(mouseDragObject);
                                break;
                            case "DeckOneBottom":
                                PutCardToDeck(card, 1, true);
                                CanvasRemoveElement(mouseDragObject);
                                break;
                            case "DeckTwoTop":
                                PutCardToDeck(card, 2, false);
                                CanvasRemoveElement(mouseDragObject);
                                break;
                            case "DeckTwoBottom":
                                PutCardToDeck(card, 2, true);
                                CanvasRemoveElement(mouseDragObject);
                                break;
                            default:
                                break;
                        }
                    }
                }
                Panel.SetZIndex(mouseDragObject, (int)hitTestResults[1].GetValue(Panel.ZIndexProperty) + 1);
            }
            else
            {
                Panel.SetZIndex(mouseDragObject, 1);
            }

            mouseDragObject = null;
        }
        #endregion

        #region Deck Card Image Events
        private void DeckCardImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image img)
            {
                if (e.ClickCount == 2)
                {
                    if (img.DataContext is DeckTestingCard card)
                    {
                        SwapCardFace(img, card);
                    }
                }
                else
                {
                    MouseStartDrag(img);
                }
            }
            else if (sender is Grid grid)
            {
                MouseStartDrag(grid);
            }
        }
        private void DeckCardImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Rotate card
            if (sender is Image card)
            {
                if ((card.DataContext as DeckTestingCard).Tapped)
                {
                    Matrix m = (card.RenderTransform as MatrixTransform).Matrix;
                    m.RotatePrepend(-90);
                    (card.RenderTransform as MatrixTransform).Matrix = m;
                    (card.DataContext as DeckTestingCard).Tapped = false;
                }
                else
                {
                    Matrix m = (card.RenderTransform as MatrixTransform).Matrix;
                    m.RotatePrepend(90);
                    (card.RenderTransform as MatrixTransform).Matrix = m;
                    (card.DataContext as DeckTestingCard).Tapped = true;
                }
            }
        }
        private void DeckCardImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is Image image && image.DataContext is Card card)
            {
                if (card.HasTwoFaces)
                {
                    image.Source = image.Source == card.PrimaryFace ? card.SecondaryFace : card.PrimaryFace;
                }
            }
        }
        #endregion

        #region Canvas Events
        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            CanvasZoom(e.Delta, e.GetPosition(GameCanvas));
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (canvasPanning)
            {
                CanvasPan();
            }

            if (MouseDragging)
            {
                MouseDrag();
            }
        }
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                CanvasStartPan(Mouse.GetPosition(GameCanvas));
            }
        }
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton != MouseButtonState.Pressed)
            {
                CanvasEndPan();
            }
        }
        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MouseDragging)
            {
                MouseEndDrag(Mouse.GetPosition(GameCanvas));
            }
        }
        #endregion

        #region Deck One Events
        private void DeckOneOpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = IO.OpenFileDialog(IO.CollectionsPath);

            if (openFileDialog.ShowDialog() == true)
            {
                List<CollectionCard> collectionCards = IO.ReadCollectionFromFile(openFileDialog.FileName);
                SelectDeck(Path.GetFileNameWithoutExtension(openFileDialog.FileName), collectionCards, 1);
            }
        }
        private void DeckOneHideButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckOneListBox.Visibility == Visibility.Visible)
            {
                DeckOneListBox.Visibility = Visibility.Hidden;
                DeckOneHideButton.Content = "Show";
            }
            else
            {
                DeckOneListBox.Visibility = Visibility.Visible;
                DeckOneHideButton.Content = "Hide";
            }
        }
        private void DeckOneShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            ShuffleDeck(deckOneCards);
            DeckOneListBox.Items.Refresh();
        }
        private void DeckOneDrawButton_Click(object sender, RoutedEventArgs e)
        {
            if (deckOneCards.Count > 0)
            {
                DrawCard(0, 1);
            }
        }
        private void DeckOneDrawSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckOneListBox.SelectedIndex != -1)
            {
                DrawCard(DeckOneListBox.SelectedIndex, 1);
            }
        }
        #endregion

        #region Deck Two Events
        private void DeckTwoOpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = IO.OpenFileDialog(IO.CollectionsPath);

            if (openFileDialog.ShowDialog() == true)
            {
                List<CollectionCard> collectionCards = IO.ReadCollectionFromFile(openFileDialog.FileName);
                SelectDeck(openFileDialog.SafeFileName, collectionCards, 2);
            }
        }
        private void DeckTwoDrawButton_Click(object sender, RoutedEventArgs e)
        {
            if (deckTwoCards.Count > 0)
            {
                DrawCard(0, 2);
            }
        }
        private void DeckTwoDrawSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckTwoListBox.SelectedIndex != -1)
            {
                DrawCard(DeckTwoListBox.SelectedIndex, 2);
            }
        }
        private void DeckTwoHideButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckTwoListBox.Visibility == Visibility.Visible)
            {
                DeckTwoListBox.Visibility = Visibility.Hidden;
                DeckTwoHideButton.Content = "Show";
            }
            else
            {
                DeckTwoListBox.Visibility = Visibility.Visible;
                DeckTwoHideButton.Content = "Hide";
            }
        }
        private void DeckTwoShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            ShuffleDeck(deckTwoCards);
            DeckTwoListBox.Items.Refresh();
        }
        #endregion

        #region Side Deck Events
        private void DeckSideDrawButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckSideListBox.SelectedIndex != -1)
            {
                DrawCard(DeckSideListBox.SelectedIndex, 3);
            }
        }
        private void DeckSideOpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = IO.OpenFileDialog(IO.CollectionsPath);

            if (openFileDialog.ShowDialog() == true)
            {
                List<CollectionCard> collectionCards = IO.ReadCollectionFromFile(openFileDialog.FileName);
                DeckSideListBox.ItemsSource = collectionCards;
            }
        }
        private void PlusCounterButton_Click(object sender, RoutedEventArgs e)
        {
            Image counterImage = new();
            counterImage.Height = 48;
            counterImage.Source = new BitmapImage(new Uri(Path.GetFullPath(counterImagePath)));

            counterImage.RenderTransformOrigin = new(.5f, .5f);

            MatrixTransform matrixTransform = new(canvasTransform.Matrix);

            // Set card position
            double x = Canvas.GetLeft(DeckOnePile);
            double y = Canvas.GetTop(DeckOnePile);
            Canvas.SetTop(counterImage, y);
            Canvas.SetLeft(counterImage, x + ((DeckOnePile.ActualWidth + 20) * canvasTransform.Matrix.M11));

            counterImage.RenderTransform = matrixTransform;

            counterImage.MouseLeftButtonDown += DeckCardImage_MouseLeftButtonDown;
            counterImage.MouseRightButtonDown += CounterImage_MouseRightButtonDown;

            _ = GameCanvas.Children.Add(counterImage);
        }
        private void CounterImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            GameCanvas.Children.Remove(sender as UIElement);
        }
        #endregion

        private void PlayerLife_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TextBlock textBlock = sender as TextBlock;
            if (int.TryParse(textBlock.Text, out int lifeCount))
            {
                lifeCount += e.Delta > 0 ? 1 : -1;
                textBlock.Text = lifeCount.ToString(new CultureInfo("en-us"));
            }
        }

        private void SelectDeck(string name, List<CollectionCard> cards, int deckNumber)
        {
            if (deckNumber == 1)
            {
                deckOneCollection.ChangeCollection(cards, name);
                GetDeckCards(1);
            }
            else if (deckNumber == 2)
            {
                deckTwoCollection.ChangeCollection(cards, name);
                GetDeckCards(2);
            }
            else { return; }
        }
        private void GetDeckCards(int deckNumber)
        {
            List<Card> cards = new();

            if (deckNumber == 1)
            {
                foreach (CollectionCard collectionCard in deckOneCollection.Cards)
                {
                    for (int i = 0; i < collectionCard.Count; i++)
                    {
                        cards.Add(collectionCard.Card);
                    }
                }
                deckOneCards = cards;
                DeckOneListBox.ItemsSource = deckOneCards;
            }
            else if (deckNumber == 2)
            {
                foreach (CollectionCard collectionCard in deckTwoCollection.Cards)
                {
                    for (int i = 0; i < collectionCard.Count; i++)
                    {
                        cards.Add(collectionCard.Card);
                    }
                }
                deckTwoCards = cards;
                DeckTwoListBox.ItemsSource = deckTwoCards;
            }
            else { return; }
        }
        private void DrawCard(int cardIndex, int deckNumber)
        {
            if (deckNumber == 3)
            {
                // Side
                Card card = (DeckSideListBox.SelectedItem as CollectionCard).Card;
                CreateCanvasCard(DeckOnePile, card);
            }
            else
            {
                List<Card> deck = deckNumber == 1 ? deckOneCards : deckTwoCards;
                if (deck.Count == 0) { return; }

                Card card = deck[cardIndex];
                deck.RemoveAt(cardIndex);

                if (card != null)
                {
                    FrameworkElement pile = deckNumber == 1 ? DeckOnePile : DeckTwoPile;
                    CreateCanvasCard(pile, card);
                }

                if (deckNumber == 1) { DeckOneListBox.Items.Refresh(); }
                else if (deckNumber == 2) { DeckTwoListBox.Items.Refresh(); }
            }
        }
        private void CreateCanvasCard(FrameworkElement pile, Card card)
        {
            Image cardImage = new();
            cardImage.Height = 250;
            cardImage.Source = card.PrimaryFace;
            cardImage.DataContext = new DeckTestingCard(card);

            cardImage.RenderTransformOrigin = new(.5f, .5f);

            MatrixTransform matrixTransform = new(canvasTransform.Matrix);

            // Set card position
            double x = Canvas.GetLeft(pile);
            double y = Canvas.GetTop(pile);
            Canvas.SetTop(cardImage, y);
            Canvas.SetLeft(cardImage, x + ((DeckOnePile.ActualWidth + 20) * canvasTransform.Matrix.M11));

            cardImage.RenderTransform = matrixTransform;

            cardImage.MouseLeftButtonDown += DeckCardImage_MouseLeftButtonDown;
            cardImage.MouseRightButtonDown += DeckCardImage_MouseRightButtonDown;

            _ = GameCanvas.Children.Add(cardImage);
        }
        private void PutCardToDeck(Card card, int deckNumber, bool toBottom)
        {
            List<Card> cards = deckNumber == 1 ? deckOneCards : deckTwoCards;
            if (toBottom)
            {
                cards.Add(card);
            }
            else
            {
                cards.Insert(0, card);
            }

            DeckOneListBox.Items.Refresh();
            DeckTwoListBox.Items.Refresh();
        }

        private static void ShuffleDeck(List<Card> deck)
        {
            RNGCryptoServiceProvider rng = new();

            // http://csharphelper.com/blog/2014/08/use-a-cryptographic-random-number-generator-in-c/
            int GetRNGInt(int min, int max)
            {
                uint scale = uint.MaxValue;
                while (scale == uint.MaxValue)
                {
                    byte[] fourBytes = new byte[4];
                    rng.GetBytes(fourBytes);

                    scale = BitConverter.ToUInt32(fourBytes, 0);
                }

                return (int)(min + (max - min) * (scale / (double)uint.MaxValue));
            }

            // Fisher Yates shuffle
            int count = deck.Count;
            int last = count - 1;
            for (int i = 0; i < last; ++i)
            {
                int r = GetRNGInt(i, count);
                Card tmp = deck[i];
                deck[i] = deck[r];
                deck[r] = tmp;
            }
        }
        private static void SwapCardFace(Image img, DeckTestingCard card)
        {
            if (card.Card.HasTwoFaces)
            {
                if (card.Side == DeckTestingCard.CardSide.Front)
                {
                    card.Side = DeckTestingCard.CardSide.Back;
                    img.Source = card.Card.SecondaryFace;
                }
                else
                {
                    card.Side = DeckTestingCard.CardSide.Front;
                    img.Source = card.Card.PrimaryFace;
                }
            }
        }

        // Return the result of the hit test to the callback.
        private HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            // Add the hit test result to the list that will be processed after the enumeration.
            hitTestResults.Add(result.VisualHit);

            // Set the behavior to return visuals at all z-order levels.
            return HitTestResultBehavior.Continue;
        }
    }
}